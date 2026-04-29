# File: src/main.py
import time
import cv2
from src.config import Config
from src.api_client import TrafficApiClient
from src.tracker import TrafficTracker
from src.video_manager import VideoGridManager

# Cấu hình Timeline (Chỉnh theo số frame thực tế của video bạn dùng)
WAIT_FRAME_END = 150
TOTAL_FRAMES = 300

def draw_hud(frame, counts, light_states):
    """Hàm phụ trợ vẽ thông tin giao diện (HUD) lên màn hình"""
    colors = {"GREEN": (0, 255, 0), "YELLOW": (0, 255, 255), "RED": (0, 0, 255)}
    cv2.putText(frame, f"North: {counts[0]} ({light_states.get(0, 'RED')})", (20, 40), cv2.FONT_HERSHEY_SIMPLEX, 1, colors[light_states.get(0, 'RED')], 3)
    cv2.putText(frame, f"East: {counts[1]} ({light_states.get(1, 'RED')})", (660, 40), cv2.FONT_HERSHEY_SIMPLEX, 1, colors[light_states.get(1, 'RED')], 3)
    cv2.putText(frame, f"South: {counts[2]} ({light_states.get(2, 'RED')})", (20, 400), cv2.FONT_HERSHEY_SIMPLEX, 1, colors[light_states.get(2, 'RED')], 3)
    cv2.putText(frame, f"West: {counts[3]} ({light_states.get(3, 'RED')})", (660, 400), cv2.FONT_HERSHEY_SIMPLEX, 1, colors[light_states.get(3, 'RED')], 3)

def main():
    # 1. Khởi tạo các Modules (Dependency Injection style)
    api_client = TrafficApiClient()
    tracker = TrafficTracker()
    video_manager = VideoGridManager("data/traffic.mp4", WAIT_FRAME_END, TOTAL_FRAMES)

    last_api_time = time.time()
    print("[SYSTEM] 🚀 Bắt đầu khởi chạy AI Camera (Clean Version)...")

    try:
        while True:
            # 2. Đồng bộ trạng thái từ Backend
            light_states = api_client.get_light_states()

            # 3. Lấy màn hình Video đã được xử lý tua/chạy
            grid_frame = video_manager.get_synced_grid(light_states)

            # 4. Yêu cầu AI quét và đếm xe
            counts, annotated_grid = tracker.process_grid(grid_frame)

            # 5. Gửi báo cáo theo chu kỳ
            current_time = time.time()
            if current_time - last_api_time >= Config.PROCESS_INTERVAL:
                for dir_idx, count in counts.items():
                    api_client.send_detection(dir_idx, count)
                
                print(f"[API] Đã đồng bộ - N:{counts[0]} | E:{counts[1]} | S:{counts[2]} | W:{counts[3]}")
                last_api_time = current_time

            # 6. Cập nhật giao diện và xuất hình
            draw_hud(annotated_grid, counts, light_states)
            cv2.imshow("Security Monitor Room (Clean Architecture)", annotated_grid)

            if cv2.waitKey(30) & 0xFF == ord('q'):
                break
    finally:
        # Đảm bảo tắt video an toàn dù có lỗi xảy ra
        video_manager.release_all()
        cv2.destroyAllWindows()

if __name__ == "__main__":
    main()