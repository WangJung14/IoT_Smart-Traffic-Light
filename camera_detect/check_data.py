import os
from dotenv import load_dotenv
import mysql.connector

load_dotenv(os.path.join(os.path.dirname(os.path.abspath(__file__)), '.env'))
conn = mysql.connector.connect(
    host=os.environ.get("DB_HOST","localhost"),
    port=int(os.environ.get("DB_PORT",3307)),
    database=os.environ.get("DB_NAME","SmartTrafficLightDb"),
    user=os.environ.get("DB_USER","smarttraffic"),
    password=os.environ.get("DB_PASS","SmartTraffic@2026"),
)
cursor = conn.cursor()
cursor.execute("""
    SELECT HOUR(Timestamp) as hr, Status, AVG(CalculatedCycleTime) as avg_cycle, COUNT(*) as cnt
    FROM DetectionLogs WHERE Source='SIMULATION'
    GROUP BY hr, Status ORDER BY hr
""")
print(f"{'Hour':>5}  {'Status':>10}  {'AvgCycle':>10}  {'Count':>6}")
print("-" * 40)
for row in cursor.fetchall():
    print(f"{row[0]:>5}  {row[1]:>10}  {row[2]:>10.1f}  {row[3]:>6}")
conn.close()
