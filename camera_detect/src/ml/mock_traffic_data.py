import uuid
import random
import os
from datetime import datetime, timedelta
import mysql.connector
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
TOTAL_RECORDS = 5000
DAYS_IN_PAST = 30

# Su dung sat flow 1 lan thuc te viet nam
# Webster chuyen dung pcu_per_hour, day gio cao diem len 80-120s
PCU_CAR        = 1.0
PCU_MOTORBIKE  = 0.35
PCU_BUS_TRUCK  = 1.75
SAT_FLOW       = 360.0   # PCU/h moi lan (thuc te Viet Nam 1 lan nho)
PHF            = 0.85    # Peak Hour Factor


def calculate_pcu(cars, motorbikes, buses, trucks):
    """Tra ve PCU/h su dung PHF de chuyen doi"""
    pcu_raw = (cars * PCU_CAR +
               motorbikes * PCU_MOTORBIKE +
               (buses + trucks) * PCU_BUS_TRUCK)
    # Chuyen sang flow rate (PCU/h): chia PHF
    return pcu_raw / PHF


def calculate_webster(pcu_ns, pcu_ew):
    """Webster's formula thuc te"""
    y_ns = min(pcu_ns / SAT_FLOW, 0.95)
    y_ew = min(pcu_ew / SAT_FLOW, 0.95)
    Y = y_ns + y_ew

    if Y >= 0.90:
        status = "OVERLOADED"
        Y = min(Y, 0.95)
    elif Y >= 0.55:
        status = "HEAVY"
    else:
        status = "NORMAL"

    L = 10  # lost time tong (giay)
    Y_capped = min(Y, 0.95)
    cycle = (1.5 * L + 5) / (1.0 - Y_capped)
    cycle = max(40, min(120, int(cycle)))

    effective_green = cycle - L
    if Y > 0:
        g_ns = int((y_ns / Y) * effective_green)
        g_ew = int((y_ew / Y) * effective_green)
    else:
        g_ns = g_ew = effective_green // 2

    return cycle, max(10, g_ns), max(10, g_ew), Y, status


def traffic_profile(hour: int, is_weekend: bool) -> dict:
    """
    Trả về số lượng xe thực tế cho từng khung giờ.
    Giờ cao điểm: cycle ~100-120s
    Đêm khuya:    cycle ~40s
    Buổi chiều bình thường: cycle ~50-70s
    """
    if not is_weekend:
        # ── Ngày thường ─────────────────────────────────────
        if hour in (7, 8, 9):       # Sáng cao điểm
            cars       = random.randint(45, 70)
            motorbikes = random.randint(120, 200)
            buses      = random.randint(4, 10)
            trucks     = 0          # cấm tải giờ cao điểm
        elif hour in (17, 18, 19):  # Chiều cao điểm
            cars       = random.randint(50, 75)
            motorbikes = random.randint(130, 210)
            buses      = random.randint(3, 8)
            trucks     = 0
        elif 10 <= hour <= 16:      # Giờ làm việc
            cars       = random.randint(15, 30)
            motorbikes = random.randint(40, 80)
            buses      = random.randint(2, 5)
            trucks     = random.randint(2, 6)
        elif hour in (20, 21):      # Tối
            cars       = random.randint(10, 20)
            motorbikes = random.randint(20, 50)
            buses      = random.randint(0, 2)
            trucks     = random.randint(0, 2)
        elif hour in (22, 23):      # Khuya
            cars       = random.randint(2, 8)
            motorbikes = random.randint(3, 15)
            buses      = 0
            trucks     = random.randint(2, 8)  # xe tải chạy khuya
        else:                       # 0h–6h đêm/sáng sớm
            cars       = random.randint(0, 3)
            motorbikes = random.randint(0, 6)
            buses      = 0
            trucks     = random.randint(0, 4)
    else:
        # ── Cuối tuần ────────────────────────────────────────
        if 10 <= hour <= 20:        # Đông buổi trưa–tối
            cars       = random.randint(25, 55)
            motorbikes = random.randint(60, 130)
            buses      = random.randint(1, 4)
            trucks     = 0
        elif 22 <= hour or hour <= 5:
            cars       = random.randint(0, 4)
            motorbikes = random.randint(0, 8)
            buses      = 0
            trucks     = random.randint(0, 3)
        else:
            cars       = random.randint(8, 20)
            motorbikes = random.randint(15, 40)
            buses      = random.randint(0, 2)
            trucks     = 0

    return {"cars": cars, "motorbikes": motorbikes, "buses": buses, "trucks": trucks}


def generate_mock_data():
    records = []
    end_date   = datetime.now()
    start_date = end_date - timedelta(days=DAYS_IN_PAST)
    time_step  = timedelta(days=DAYS_IN_PAST) / TOTAL_RECORDS
    current_time = start_date

    print(f"Generating {TOTAL_RECORDS} records ({start_date:%Y-%m-%d} to {end_date:%Y-%m-%d}) ...")

    for _ in range(TOTAL_RECORDS):
        hour       = current_time.hour
        is_weekend = current_time.weekday() >= 5

        ns = traffic_profile(hour, is_weekend)
        ew = traffic_profile(hour, is_weekend)   # mỗi hướng độc lập

        pcu_ns = calculate_pcu(ns["cars"], ns["motorbikes"], ns["buses"], ns["trucks"])
        pcu_ew = calculate_pcu(ew["cars"], ew["motorbikes"], ew["buses"], ew["trucks"])

        cycle, g_ns, g_ew, flow_ratio, status = calculate_webster(pcu_ns, pcu_ew)

        record = (
            str(uuid.uuid4()),
            current_time.strftime('%Y-%m-%d %H:%M:%S'),
            ns["cars"], ns["motorbikes"], ns["buses"], ns["trucks"],
            ew["cars"], ew["motorbikes"], ew["buses"], ew["trucks"],
            cycle, g_ns, g_ew, round(flow_ratio, 4), status, "SIMULATION"
        )
        records.append(record)
        current_time += time_step

    return records


def refresh_db(records):
    try:
        conn   = mysql.connector.connect(**DB_CONFIG)
        cursor = conn.cursor()

        # Xoá data giả lập cũ trước khi chèn mới
        cursor.execute("DELETE FROM DetectionLogs WHERE Source = 'SIMULATION'")
        print(f"Deleted {cursor.rowcount} old simulation records.")

        sql = """INSERT INTO DetectionLogs
                 (Id, Timestamp, NsCars, NsMotorbikes, NsBuses, NsTrucks,
                  EwCars, EwMotorbikes, EwBuses, EwTrucks,
                  CalculatedCycleTime, CalculatedGreenNS, CalculatedGreenEW,
                  TotalFlowRatio, Status, Source)
                 VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s)"""

        cursor.executemany(sql, records)
        conn.commit()
        print(f"Inserted {cursor.rowcount} new records OK")

    except mysql.connector.Error as err:
        print(f"DB Error: {err}")
    finally:
        if 'conn' in locals() and conn.is_connected():
            cursor.close()
            conn.close()


if __name__ == "__main__":
    data = generate_mock_data()
    refresh_db(data)
