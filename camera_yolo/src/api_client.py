import requests
from src.config import Config

class TrafficApiClient:
    def __init__(self):
        self.headers = {'Content-Type': 'application/json'}

    def get_light_states(self) -> dict:
        """Hỏi Backend C# xem trạng thái đèn hiện tại là gì"""
        try:
            url = f"{Config.API_DASHBOARD}{Config.INTERSECTION_ID}"
            res = requests.get(url, timeout=1.0)
            
            if res.status_code == 200:
                data = res.json()
                main_light = data["data"]["currentLightState"] # 0: Green, 1: Yellow, 2: Red
                
                # Trục Bắc-Nam (0, 2) đi chung màu, Đông-Tây (1, 3) màu ngược lại
                if main_light == 0: 
                    return {0: "GREEN", 2: "GREEN", 1: "RED", 3: "RED"}
                elif main_light == 1:
                    return {0: "YELLOW", 2: "YELLOW", 1: "RED", 3: "RED"}
                else:
                    return {0: "RED", 2: "RED", 1: "GREEN", 3: "GREEN"}
        except Exception as e:
            print(f"[API_WARN] Không thể lấy trạng thái đèn: {e}")
            
        # Mặc định an toàn nếu mất mạng
        return {0: "RED", 1: "RED", 2: "RED", 3: "RED"}

    def send_detection(self, direction: int, count: int):
        """Báo cáo số lượng xe lên C#"""
        payload = {
            "intersectionId": Config.INTERSECTION_ID,
            "direction": direction,
            "vehicleCount": count
        }
        try:
            requests.post(Config.API_TRAFFIC, json=payload, headers=self.headers, timeout=0.5)
        except:
            # Bỏ qua lỗi timeout để không làm giật luồng Video
            pass