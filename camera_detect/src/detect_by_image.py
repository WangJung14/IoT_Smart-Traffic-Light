import cv2
import numpy as np
import time
from ultralytics import YOLO

# Import shared logic and configuration
from core import (FRAME_W, FRAME_H, 
                  detect_vehicles, count_by_axis, draw_overlay, report_to_console)

IMAGE_LEFT = "data/img/heavy_traffic.png"
IMAGE_RIGHT = "data/img/low_traffic.png"

def load_and_resize_image(path: str, width: int, height: int) -> np.ndarray:
    """Load an image and resize it. Return black frame if failed."""
    img = cv2.imread(path)
    if img is None:
        print(f"[ERROR] Cannot open image: {path} - Using black frame instead.")
        return np.zeros((height, width, 3), dtype=np.uint8)
    return cv2.resize(img, (width, height))

def main():
    print("=" * 60)
    print("  Smart Traffic Camera - IMAGE Detection Node")
    print("=" * 60)
    print(f"Reading images: {IMAGE_LEFT} & {IMAGE_RIGHT}")
    print("[CONTROLS] Press 'q' to Quit")
    print("=" * 60)

    print("[INFO] Loading YOLOv8 model (yolov8x - EXTRA LARGE)...")
    model = YOLO("yolov8x.pt")

    # Load static images
    img_left = load_and_resize_image(IMAGE_LEFT, FRAME_W, FRAME_H)
    img_right = load_and_resize_image(IMAGE_RIGHT, FRAME_W, FRAME_H)

    # Combine frames
    combined = np.hstack((img_left, img_right))
    
    # Detect -> Count -> Draw
    detections = detect_vehicles(model, combined)
    count_a, count_b = count_by_axis(detections)
    display_frame = draw_overlay(combined, detections, count_a, count_b)

    # Print report once for image
    print("\n[INFO] Detection Results:")
    report_to_console(count_a, count_b)

    # Display loop (keeps window open until 'q' is pressed)
    while True:
        cv2.imshow("Image Detection", display_frame)
        key = cv2.waitKey(100) & 0xFF
        if key == ord('q'):
            print("[EXIT] Quitting...")
            break

    cv2.destroyAllWindows()
    print("[INFO] Image Detection stopped.")

if __name__ == "__main__":
    main()
