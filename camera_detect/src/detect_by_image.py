import cv2
import time
import requests
import os
from ultralytics import YOLO
from core import count_by_axis_detailed

# Configuration
BACKEND_API_URL = "http://localhost:5212/api/v1/hardware/vehicle-counts"
MODEL_PATH = "yolov8n.pt"

def send_to_backend(ns_counts, ew_counts):
    payload = {
        "nsVehicles": ns_counts,
        "ewVehicles": ew_counts
    }
    try:
        response = requests.post(BACKEND_API_URL, json=payload, timeout=5)
        if response.status_code == 200:
            res_data = response.json()
            print(f"[API] Success! New Timing -> Co:{res_data['cycleTime']}s, g_NS:{res_data['greenNS']}s, g_EW:{res_data['greenEW']}s")
        else:
            print(f"[API] Error: {response.status_code}")
    except Exception as e:
        print(f"[API] Connection failed: {e}")

def process_demo_images(img_ns_path, img_ew_path):
    print(f"\n[INFO] Processing Images:\n NS: {img_ns_path}\n EW: {img_ew_path}")
    
    model = YOLO(MODEL_PATH)
    
    # Load images
    img_ns = cv2.imread(img_ns_path)
    img_ew = cv2.imread(img_ew_path)
    
    if img_ns is None or img_ew is None:
        print("[ERROR] Could not read image files. Check paths!")
        return

    # Detection NS
    results_ns = model(img_ns, verbose=False)
    counts_ns = count_by_axis_detailed(results_ns[0])
    
    # Detection EW
    results_ew = model(img_ew, verbose=False)
    counts_ew = count_by_axis_detailed(results_ew[0])
    
    print(f"[RESULT] NS Counts: {counts_ns}")
    print(f"[RESULT] EW Counts: {counts_ew}")
    
    # Send to backend
    send_to_backend(counts_ns, counts_ew)
    
    # Draw and show
    res_ns_plotted = results_ns[0].plot()
    res_ew_plotted = results_ew[0].plot()
    
    # Resize for display
    h, w = 480, 640
    res_ns_plotted = cv2.resize(res_ns_plotted, (w, h))
    res_ew_plotted = cv2.resize(res_ew_plotted, (w, h))
    
    import numpy as np
    combined = np.hstack((res_ns_plotted, res_ew_plotted))
    
    cv2.putText(combined, f"NORTH-SOUTH (PCU High)", (20, 40), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 255, 0), 2)
    cv2.putText(combined, f"EAST-WEST (PCU Low)", (w + 20, 40), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 255, 0), 2)
    
    cv2.imshow("AI Traffic Demo - Image Mode", combined)
    print("\n[HINT] Press any key to close the window.")
    cv2.waitKey(0)
    cv2.destroyAllWindows()

if __name__ == "__main__":
    # Bạn hãy thay đổi đường dẫn ảnh ở đây để demo
    # Ví dụ: đặt 2 tấm ảnh vào thư mục camera_detect/demo_data/
    IMG_NS = "demo_data/heavy_traffic.jpg" 
    IMG_EW = "demo_data/low_traffic.jpg"
    
    if not os.path.exists("demo_data"):
        os.makedirs("demo_data")
        print("[INFO] Created 'demo_data' folder. Please put your images there.")
    else:
        process_demo_images(IMG_NS, IMG_EW)