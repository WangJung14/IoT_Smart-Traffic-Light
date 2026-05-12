from fastapi import FastAPI, HTTPException, Query
from fastapi.middleware.cors import CORSMiddleware
from contextlib import asynccontextmanager
from datetime import datetime
import pandas as pd
import mysql.connector
import joblib
import os
from dotenv import load_dotenv

# Load .env
load_dotenv(os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), '.env'))

ML_PORT = int(os.environ.get("ML_PORT", 8000))
MODEL_PATH = os.path.join(os.path.dirname(os.path.abspath(__file__)), "random_forest_model.pkl")

DB_CONFIG = {
    "host":     os.environ.get("DB_HOST", "localhost"),
    "port":     int(os.environ.get("DB_PORT", 3307)),
    "database": os.environ.get("DB_NAME", "SmartTrafficLightDb"),
    "user":     os.environ.get("DB_USER", "smarttraffic"),
    "password": os.environ.get("DB_PASS", "SmartTraffic@2026"),
}

# Vehicle totals (NS + EW combined) per hour
VEHICLE_ESTIMATE = {
    0: 12,   1: 8,   2: 6,   3: 5,   4: 7,   5: 20,
    6: 60,   7: 620, 8: 660, 9: 580, 10: 180, 11: 160,
    12: 200, 13: 170, 14: 160, 15: 185, 16: 220, 17: 700,
    18: 720, 19: 640, 20: 240, 21: 130, 22: 70, 23: 30,
}

models = None

@asynccontextmanager
async def lifespan(app: FastAPI):
    global models
    if os.path.exists(MODEL_PATH):
        models = joblib.load(MODEL_PATH)
        print(f"[OK] Model loaded from {MODEL_PATH}")
        print(f"  Features: {models['features']}")
    else:
        print("[WARN] Model not found -- run train_model.py first")
    yield

app = FastAPI(title="Smart Traffic ML API", lifespan=lifespan)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)


def get_db():
    return mysql.connector.connect(**DB_CONFIG)


@app.get("/")
def read_root():
    return {"message": "Smart Traffic ML Prediction API", "status": "ok"}


@app.get("/predict")
def predict_traffic(date: str):
    """
    Du bao ket xe cho 24 gio trong mot ngay.
    Param: date = YYYY-MM-DD
    """
    if models is None:
        raise HTTPException(status_code=500, detail="Model not loaded -- run train_model.py first")

    try:
        target_date = datetime.strptime(date, "%Y-%m-%d")
    except ValueError:
        raise HTTPException(status_code=400, detail="Invalid date format. Use YYYY-MM-DD")

    dayofweek  = target_date.weekday()
    is_weekend = 1 if dayofweek >= 5 else 0

    rows = []
    for h in range(24):
        est_vehicles = VEHICLE_ESTIMATE[h]
        if is_weekend:
            est_vehicles = int(est_vehicles * 0.65) if h in (7, 8, 9) else est_vehicles
        rows.append({
            "hour":           h,
            "dayofweek":      dayofweek,
            "is_weekend":     is_weekend,
            "is_rush_hour":   1 if (7 <= h <= 9 or 17 <= h <= 19) else 0,
            "total_vehicles": est_vehicles,
        })

    df_input   = pd.DataFrame(rows)[models['features']]
    ys_pred    = models['classifier'].predict(df_input)
    yt_pred    = models['regressor'].predict(df_input)
    status_inv = models['status_map_inv']

    results = [
        {
            "hour":          f"{h:02d}:00",
            "status":        status_inv[int(ys_pred[h])],
            "cycleTime":     int(yt_pred[h]),
            "totalVehicles": VEHICLE_ESTIMATE[h],
        }
        for h in range(24)
    ]

    return {"date": date, "forecast": results}


@app.get("/vehicle-stats")
def vehicle_stats(
    source: str = Query(default="ALL", description="SIMULATION | VIDEO | ALL"),
    day_type: str = Query(default="ALL", description="WEEKDAY | WEEKEND | ALL"),
):
    """
    Tra ve so luong xe trung binh theo tung gio trong ngay, phan loai theo tung loai xe.
    Lay du lieu thuc te tu database DetectionLogs.
    """
    try:
        conn = get_db()
        cursor = conn.cursor(dictionary=True)

        # Build WHERE clause
        conditions = []
        if source != "ALL":
            conditions.append(f"Source = '{source}'")
        if day_type == "WEEKDAY":
            conditions.append("DAYOFWEEK(Timestamp) NOT IN (1, 7)")  # 1=Sun, 7=Sat in MySQL
        elif day_type == "WEEKEND":
            conditions.append("DAYOFWEEK(Timestamp) IN (1, 7)")

        where = ("WHERE " + " AND ".join(conditions)) if conditions else ""

        query = f"""
            SELECT
                HOUR(Timestamp) AS hour,
                ROUND(AVG(NsCars + EwCars), 1)             AS cars,
                ROUND(AVG(NsMotorbikes + EwMotorbikes), 1) AS motorbikes,
                ROUND(AVG(NsBuses + EwBuses), 1)           AS buses,
                ROUND(AVG(NsTrucks + EwTrucks), 1)         AS trucks,
                ROUND(AVG(CalculatedCycleTime), 1)         AS avgCycleTime,
                COUNT(*)                                    AS sampleCount
            FROM DetectionLogs
            {where}
            GROUP BY HOUR(Timestamp)
            ORDER BY hour
        """

        cursor.execute(query)
        rows = cursor.fetchall()

        # Fill missing hours with zeros
        hour_map = {row["hour"]: row for row in rows}
        result = []
        for h in range(24):
            if h in hour_map:
                r = hour_map[h]
                result.append({
                    "hour":         f"{h:02d}:00",
                    "cars":         float(r["cars"] or 0),
                    "motorbikes":   float(r["motorbikes"] or 0),
                    "buses":        float(r["buses"] or 0),
                    "trucks":       float(r["trucks"] or 0),
                    "avgCycleTime": float(r["avgCycleTime"] or 0),
                    "sampleCount":  int(r["sampleCount"]),
                })
            else:
                result.append({
                    "hour": f"{h:02d}:00",
                    "cars": 0, "motorbikes": 0, "buses": 0, "trucks": 0,
                    "avgCycleTime": 0, "sampleCount": 0,
                })

        return {"source": source, "dayType": day_type, "data": result}

    except mysql.connector.Error as err:
        raise HTTPException(status_code=500, detail=f"Database error: {err}")
    finally:
        if 'conn' in locals() and conn.is_connected():
            cursor.close()
            conn.close()


if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=ML_PORT)
