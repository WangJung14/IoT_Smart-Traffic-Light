import cv2
import numpy as np
import time
import json
from ultralytics import YOLO

# ==================== CORE CONFIGURATION ====================
FRAME_W = 640
FRAME_H = 720
DIVIDER_X = 640  # Vertical divider between Axis A and Axis B

VEHICLE_CLASSES = {2: "car", 3: "motorbike", 5: "bus", 7: "truck"}
CONFIDENCE = 0.25  # Hạ thêm một chút để bắt các xe bị khuất
IOU_THRESHOLD = 0.6 # Tăng ngưỡng giao nhau (IoU) để đếm được các xe đứng san sát nhau trong kẹt xe
REPORT_INTERVAL = 2.0

# ==================== DETECTION ====================
def detect_vehicles(model: YOLO, frame: np.ndarray) -> list:
    """Run YOLOv8 detection on the combined frame."""
    # - imgsz=1920: Bơm độ phân giải lên SIÊU CAO (Super High-Res)
    # - augment=True: Bật Test-Time Augmentation (TTA)
    # - iou=IOU_THRESHOLD: Chống dính xe
    results = model(frame, verbose=False, conf=CONFIDENCE, iou=IOU_THRESHOLD, imgsz=1920, augment=True)
    detections = []
    for result in results:
        for box in result.boxes:
            cls_id = int(box.cls[0])
            if cls_id in VEHICLE_CLASSES:
                x1, y1, x2, y2 = map(int, box.xyxy[0])
                conf = float(box.conf[0])
                detections.append((x1, y1, x2, y2, cls_id, conf))
    return detections

# ==================== COUNTING ====================
def count_by_axis(detections: list) -> tuple:
    """Count vehicles per axis based on bounding box center X."""
    count_a, count_b = 0, 0
    for (x1, y1, x2, y2, cls_id, conf) in detections:
        cx = (x1 + x2) / 2
        if cx < DIVIDER_X:
            count_a += 1
        else:
            count_b += 1
    return count_a, count_b

def count_by_axis_detailed(input_data) -> tuple:
    """Count vehicles per axis, broken down by type.
       Handles both YOLO Results object and raw list of detections."""
    axis_a = {"car": 0, "motorbike": 0, "bus": 0, "truck": 0}
    axis_b = {"car": 0, "motorbike": 0, "bus": 0, "truck": 0}
    
    detections = []
    # Case 1: Results object from ultralytics
    if hasattr(input_data, 'boxes') and input_data.boxes is not None:
        raw_data = input_data.boxes.data.tolist()
        for det in raw_data:
            if len(det) < 6: continue
            x1, y1, x2, y2, conf, cls_id = det[:6]
            detections.append((x1, y1, x2, y2, int(cls_id), conf))
    # Case 2: Raw list of detections (x1, y1, x2, y2, cls_id, conf)
    elif isinstance(input_data, list):
        detections = input_data

    for det in detections:
        if len(det) < 5: continue
        x1, y1, x2, y2, cls_id = det[:5]
        
        if cls_id not in VEHICLE_CLASSES: continue
        
        cx = (x1 + x2) / 2
        vtype = VEHICLE_CLASSES[cls_id]
        if cx < DIVIDER_X:
            axis_a[vtype] += 1
        else:
            axis_b[vtype] += 1
    return axis_a, axis_b

def count_total_detailed(input_data) -> dict:
    """Count total vehicles in a frame, broken down by type."""
    counts = {"car": 0, "motorbike": 0, "bus": 0, "truck": 0}
    
    detections = []
    if hasattr(input_data, 'boxes') and input_data.boxes is not None:
        raw_data = input_data.boxes.data.tolist()
        for det in raw_data:
            if len(det) < 6: continue
            x1, y1, x2, y2, conf, cls_id = det[:6]
            detections.append((x1, y1, x2, y2, int(cls_id), conf))
    elif isinstance(input_data, list):
        detections = input_data

    for det in detections:
        if len(det) < 5: continue
        cls_id = det[4]
        if cls_id not in VEHICLE_CLASSES: continue
        
        vtype = VEHICLE_CLASSES[cls_id]
        counts[vtype] += 1
    return counts

# ==================== DRAWING ====================
def draw_overlay(frame: np.ndarray, detections: list, count_a: int, count_b: int) -> np.ndarray:
    """Draw bounding boxes, divider, and counts."""
    # Draw vertical divider line
    cv2.line(frame, (DIVIDER_X, 0), (DIVIDER_X, FRAME_H), (255, 255, 255), 2)
    
    # Draw bounding boxes with class labels
    for (x1, y1, x2, y2, cls_id, conf) in detections:
        cx = (x1 + x2) / 2
        color = (0, 255, 0) if cx < DIVIDER_X else (255, 165, 0)
        label = f"{VEHICLE_CLASSES[cls_id]} {conf:.2f}"
        cv2.rectangle(frame, (x1, y1), (x2, y2), color, 2)
        cv2.putText(frame, label, (x1, y1 - 8), cv2.FONT_HERSHEY_SIMPLEX, 0.5, color, 1)

    # Draw axis count overlays
    cv2.putText(frame, f"Axis A: {count_a}", (20, 50), cv2.FONT_HERSHEY_SIMPLEX, 1.2, (0, 255, 0), 3)
    cv2.putText(frame, f"Axis B: {count_b}", (DIVIDER_X + 20, 50), cv2.FONT_HERSHEY_SIMPLEX, 1.2, (255, 165, 0), 3)
    return frame

# ==================== REPORTING ====================
def report_to_console(count_a: int, count_b: int):
    """Print vehicle counts as JSON to console."""
    payload = {
        "axisA": count_a,
        "axisB": count_b,
        "timestamp": time.strftime("%Y-%m-%d %H:%M:%S")
    }
    print(json.dumps(payload, indent=2))
