import pandas as pd
import mysql.connector
from sklearn.ensemble import RandomForestClassifier, RandomForestRegressor
from sklearn.model_selection import train_test_split
from sklearn.metrics import accuracy_score, mean_absolute_error
import joblib
import os
from dotenv import load_dotenv

# Load .env từ thư mục gốc camera_detect/
load_dotenv(os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), '.env'))

DB_CONFIG = {
    "host": os.environ.get("DB_HOST", "localhost"),
    "port": int(os.environ.get("DB_PORT", 3307)),
    "database": os.environ.get("DB_NAME", "SmartTrafficLightDb"),
    "user": os.environ.get("DB_USER", "smarttraffic"),
    "password": os.environ.get("DB_PASS", "SmartTraffic@2026")
}
MODEL_DIR = os.path.dirname(os.path.abspath(__file__))


def fetch_data():
    print("Fetching data from MySQL ...")
    conn = mysql.connector.connect(**DB_CONFIG)
    query = """
        SELECT Timestamp, Status, CalculatedCycleTime,
               NsCars, NsMotorbikes, NsBuses, NsTrucks,
               EwCars, EwMotorbikes, EwBuses, EwTrucks
        FROM DetectionLogs
        WHERE Source = 'SIMULATION' OR Source = 'VIDEO'
    """
    df = pd.read_sql(query, conn)
    conn.close()
    return df


def preprocess_data(df):
    print("Preprocessing data ...")
    df['Timestamp'] = pd.to_datetime(df['Timestamp'])
    df['hour']       = df['Timestamp'].dt.hour
    df['dayofweek']  = df['Timestamp'].dt.dayofweek
    df['is_weekend'] = (df['dayofweek'] >= 5).astype(int)

    # Tổng phương tiện thực (giúp model thấy sự khác biệt rõ ràng giữa đêm / cao điểm)
    df['total_vehicles'] = (
        df['NsCars']       + df['NsMotorbikes'] + df['NsBuses']  + df['NsTrucks'] +
        df['EwCars']       + df['EwMotorbikes'] + df['EwBuses']  + df['EwTrucks']
    )

    # Peak hour flag
    df['is_rush_hour'] = df['hour'].apply(
        lambda h: 1 if (7 <= h <= 9 or 17 <= h <= 19) else 0
    )

    # Mã hoá Status → int
    status_map = {"NORMAL": 0, "HEAVY": 1, "OVERLOADED": 2}
    df['status_encoded'] = df['Status'].map(status_map)

    df = df.dropna()

    # ── Features sử dụng để train ────────────────────────────
    FEATURES = ['hour', 'dayofweek', 'is_weekend', 'is_rush_hour', 'total_vehicles']

    X       = df[FEATURES]
    y_status = df['status_encoded']
    y_time   = df['CalculatedCycleTime']

    return X, y_status, y_time, FEATURES


def train_and_save(X, y_status, y_time, features):
    print("Training models ...")
    X_train, X_test, ys_train, ys_test, yt_train, yt_test = train_test_split(
        X, y_status, y_time, test_size=0.2, random_state=42
    )

    # 1. Classifier: Trạng thái kẹt xe
    clf = RandomForestClassifier(n_estimators=150, max_depth=12, random_state=42)
    clf.fit(X_train, ys_train)
    acc = accuracy_score(ys_test, clf.predict(X_test))
    print(f"  [OK] Status Classifier   Accuracy : {acc * 100:.2f}%")

    # 2. Regressor: Chu kỳ đèn (giây)
    reg = RandomForestRegressor(n_estimators=150, max_depth=12, random_state=42)
    reg.fit(X_train, yt_train)
    mae = mean_absolute_error(yt_test, reg.predict(X_test))
    print(f"  [OK] Cycle Regressor     MAE      : {mae:.2f}s")

    payload = {
        'classifier':    clf,
        'regressor':     reg,
        'features':      features,
        'status_map_inv': {0: "NORMAL", 1: "HEAVY", 2: "OVERLOADED"}
    }

    model_path = os.path.join(MODEL_DIR, "random_forest_model.pkl")
    joblib.dump(payload, model_path)
    print(f"  [SAVED] Models -> {model_path}")


if __name__ == "__main__":
    df = fetch_data()
    X, y_status, y_time, features = preprocess_data(df)
    train_and_save(X, y_status, y_time, features)
