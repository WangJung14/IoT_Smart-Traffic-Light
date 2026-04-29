# File: src/tracker.py
import cv2
import numpy as np
from ultralytics import YOLO

class TrafficTracker:
    def __init__(self):
        print("[AI] Đang tải mô hình YOLOv8 Nano...")
        self.model = YOLO("yolov8n.pt")
        # Chỉ nhận diện: Car (2), Motorcycle (3), Bus (5), Truck (7)
        self.target_classes = [2, 3, 5, 7] 

        # Tọa độ 4 vùng đếm cho Màn hình ghép 1280x720 (4 ô 640x360)
        # Bạn có thể điều chỉnh các số này cho khớp với góc quay video thực tế
        self.zones = {
            0: np.array([[50, 300], [550, 300], [450, 150], [150, 150]], np.int32),     # North (Trái - Trên)
            1: np.array([[690, 300], [1190, 300], [1090, 150], [790, 150]], np.int32),  # East (Phải - Trên)
            2: np.array([[50, 660], [550, 660], [450, 510], [150, 510]], np.int32),     # South (Trái - Dưới)
            3: np.array([[690, 660], [1190, 660], [1090, 510], [790, 510]], np.int32)   # West (Phải - Dưới)
        }

    def process_grid(self, grid_frame):
        # Yêu cầu YOLO quét toàn bộ màn hình ghép
        results = self.model.track(grid_frame, persist=True, tracker="bytetrack.yaml", classes=self.target_classes, verbose=False)
        
        counts = {0: 0, 1: 0, 2: 0, 3: 0}
        annotated_grid = grid_frame.copy()

        # Vẽ 4 hình thang màu vàng
        for idx, polygon in self.zones.items():
            cv2.polylines(annotated_grid, [polygon], isClosed=True, color=(0, 255, 255), thickness=2)

        # Xử lý đếm xe
        if results[0].boxes.id is not None:
            boxes = results[0].boxes.xyxy.cpu().numpy()
            for box in boxes:
                x1, y1, x2, y2 = map(int, box)
                
                # Điểm neo: Bánh xe (Tâm của cạnh dưới)
                cx, cy = int((x1 + x2) / 2), y2 
                
                # Kiểm tra xe rớt vào vùng nào
                for dir_idx, polygon in self.zones.items():
                    if cv2.pointPolygonTest(polygon, (cx, cy), False) >= 0:
                        counts[dir_idx] += 1
                        # Viền xanh lá nếu hợp lệ
                        cv2.rectangle(annotated_grid, (x1, y1), (x2, y2), (0, 255, 0), 2)
                        break

        return counts, annotated_grid