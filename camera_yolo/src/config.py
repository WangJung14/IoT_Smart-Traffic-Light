import os
from dotenv import load_dotenv

# Tải các biến từ file .env lên bộ nhớ
load_dotenv()

class Config:
    API_TRAFFIC = os.getenv("API_TRAFFIC", "http://localhost:5212/api/v1/traffic")
    API_DASHBOARD = os.getenv("API_DASHBOARD", "http://localhost:5212/api/v1/admin/dashboard/")
    INTERSECTION_ID = os.getenv("INTERSECTION_ID", "")
    PROCESS_INTERVAL = float(os.getenv("PROCESS_INTERVAL", 2.0))