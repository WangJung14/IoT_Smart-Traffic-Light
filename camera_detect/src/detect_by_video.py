import cv2
import numpy as np
import time
from ultralytics import YOLO

# Import shared logic and configuration
from core import (FRAME_W, FRAME_H, REPORT_INTERVAL, 
                  detect_vehicles, count_by_axis, draw_overlay, report_to_console)

DEFAULT_LEFT = "data/videos/heavy_traffic.mp4"
DEFAULT_RIGHT = "data/videos/low_traffic.mp4"

def load_video(path: str) -> cv2.VideoCapture:
    cap = cv2.VideoCapture(path)
    if not cap.isOpened():
        print(f"[ERROR] Cannot open video: {path}")
    return cap

def read_and_resize(cap: cv2.VideoCapture, width: int, height: int) -> tuple:
    ret, frame = cap.read()
    if not ret:
        # Loop video: reset to first frame
        cap.set(cv2.CAP_PROP_POS_FRAMES, 0)
        ret, frame = cap.read()
        if not ret: 
            return False, None
    frame = cv2.resize(frame, (width, height))
    return True, frame

def handle_hotkeys(key: int, cap_left: cv2.VideoCapture, cap_right: cv2.VideoCapture) -> tuple:
    should_quit = False
    if key == ord('1'):
        print("[HOTKEY] Switching to: BOTH heavy_traffic.mp4")
        cap_left.release(); cap_right.release()
        cap_left, cap_right = load_video(DEFAULT_LEFT), load_video(DEFAULT_LEFT)
    elif key == ord('2'):
        print("[HOTKEY] Switching to: LEFT heavy, RIGHT clear")
        cap_left.release(); cap_right.release()
        cap_left, cap_right = load_video(DEFAULT_LEFT), load_video(DEFAULT_RIGHT)
    elif key == ord('3'):
        print("[HOTKEY] Switching to: BOTH low_traffic.mp4")
        cap_left.release(); cap_right.release()
        cap_left, cap_right = load_video(DEFAULT_RIGHT), load_video(DEFAULT_RIGHT)
    elif key == ord('q'):
        print("[EXIT] Quitting...")
        should_quit = True
    return cap_left, cap_right, should_quit

def main():
    print("=" * 60)
    print("  Smart Traffic Camera - VIDEO Detection Node")
    print("=" * 60)
    print("[CONTROLS] 1 = Both heavy | 2 = Heavy/Clear | 3 = Both clear | q = Quit")
    print("=" * 60)

    print("[INFO] Loading YOLOv8 model (yolov8x - EXTRA LARGE)...")
    model = YOLO("yolov8x.pt")

    cap_left = load_video(DEFAULT_LEFT)
    cap_right = load_video(DEFAULT_RIGHT)
    last_report_time = time.time()

    while True:
        ok_left, frame_left = read_and_resize(cap_left, FRAME_W, FRAME_H)
        ok_right, frame_right = read_and_resize(cap_right, FRAME_W, FRAME_H)
        
        if not ok_left or not ok_right:
            print("[ERROR] Failed to read video frames. Exiting.")
            break

        combined = np.hstack((frame_left, frame_right))
        
        # Detect -> Count -> Draw
        detections = detect_vehicles(model, combined)
        count_a, count_b = count_by_axis(detections)
        display_frame = draw_overlay(combined, detections, count_a, count_b)
        
        cv2.imshow("Video Detection", display_frame)

        # Time-based reporting
        now = time.time()
        if now - last_report_time >= REPORT_INTERVAL:
            report_to_console(count_a, count_b)
            last_report_time = now

        # Handle keyboard input
        key = cv2.waitKey(1) & 0xFF
        if key != 255:
            cap_left, cap_right, should_quit = handle_hotkeys(key, cap_left, cap_right)
            if should_quit: 
                break

    # Cleanup
    cap_left.release()
    cap_right.release()
    cv2.destroyAllWindows()
    print("[INFO] Video Detection stopped.")

if __name__ == "__main__":
    main()
