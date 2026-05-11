"""
detect_by_image.py - Demo AI Traffic Detection bằng ảnh tĩnh
Chạy từ thư mục camera_detect/:  python src/detect_by_image.py
"""
import sys
import os
sys.path.insert(0, os.path.dirname(__file__))

import cv2
import numpy as np
import requests
from ultralytics import YOLO
from core import count_by_axis_detailed

# ======================================================
#  CẤU HÌNH – SỬA TẠI ĐÂY ĐỂ ĐỔI ẢNH DEMO
# ======================================================
BACKEND_API_URL = "http://localhost:5212/api/v1/hardware/vehicle-counts"
MODEL_PATH      = "yolov8n.pt"

# Đường dẫn ảnh (tương đối từ thư mục camera_detect/)
IMG_NS = "data/img/heavy_traffic.png"   # Hướng Bắc–Nam (kẹt xe)
IMG_EW = "data/img/low_traffic.png"     # Hướng Đông–Tây (vắng xe)

# Kích thước cửa sổ hiển thị
DISPLAY_W, DISPLAY_H = 720, 480
# ======================================================

def send_to_backend(ns_counts, ew_counts):
    """Gửi kết quả đếm xe lên Backend để Webster tính toán."""
    payload = { "nsVehicles": ns_counts, "ewVehicles": ew_counts }
    try:
        print("\n[→ API] Đang gửi dữ liệu lên Backend...")
        response = requests.post(BACKEND_API_URL, json=payload, timeout=5)
        if response.status_code == 200:
            d = response.json()
            print(f"[✓ API] Backend phản hồi thành công!")
            print(f"  ┌─ Chu kỳ tối ưu (Co) : {d['cycleTime']}s")
            print(f"  ├─ Xanh Bắc–Nam        : {d['greenNS']}s")
            print(f"  ├─ Xanh Đông–Tây       : {d['greenEW']}s")
            print(f"  ├─ Hệ số bão hòa (Y)   : {d['totalFlowRatio']:.3f}")
            print(f"  └─ Trạng thái          : {d['status']}")
        else:
            print(f"[✗ API] Lỗi từ server: HTTP {response.status_code}")
            print(f"        Response: {response.text[:200]}")
    except requests.exceptions.ConnectionError:
        print("[✗ API] Không kết nối được Backend!")
        print("        → Hãy chắc chắn Backend đang chạy: dotnet run")
    except Exception as e:
        print(f"[✗ API] Lỗi không xác định: {e}")


def print_count_table(ns: tuple, ew: tuple):
    """In bảng tóm tắt số lượng xe ra terminal."""
    ns_a, ns_b = ns
    ew_a, ew_b = ew
    print("\n┌─────────────────────────────────────────────┐")
    print("│         KẾT QUẢ NHẬN DIỆN PHƯƠNG TIỆN       │")
    print("├────────────┬────────────────┬────────────────┤")
    print("│ Loại xe    │ Bắc–Nam (NS)   │ Đông–Tây (EW)  │")
    print("├────────────┼────────────────┼────────────────┤")
    types = ["car", "motorbike", "bus", "truck"]
    labels = {"car": "Ô tô", "motorbike": "Xe máy", "bus": "Xe buýt", "truck": "Xe tải"}
    ns_total, ew_total = 0, 0
    for t in types:
        n = ns_a.get(t, 0) + ns_b.get(t, 0)
        e = ew_a.get(t, 0) + ew_b.get(t, 0)
        ns_total += n
        ew_total += e
        print(f"│ {labels[t]:<10} │ {n:<14} │ {e:<14} │")
    print("├────────────┼────────────────┼────────────────┤")
    print(f"│ {'TỔNG':<10} │ {ns_total:<14} │ {ew_total:<14} │")
    print("└────────────┴────────────────┴────────────────┘")


def run_demo(img_ns_path: str, img_ew_path: str):
    print("\n" + "="*55)
    print("  AI Traffic Demo – IMAGE MODE")
    print("="*55)

    # --- Kiểm tra file ảnh ---
    missing = [p for p in [img_ns_path, img_ew_path] if not os.path.exists(p)]
    if missing:
        print(f"[✗] Không tìm thấy file ảnh:")
        for p in missing: print(f"    → {os.path.abspath(p)}")
        print("\nHướng dẫn: đặt ảnh vào camera_detect/data/img/")
        return

    # --- Load model ---
    print(f"\n[1/4] Đang load YOLO model ({MODEL_PATH})...")
    model = YOLO(MODEL_PATH)

    # --- Đọc ảnh ---
    print(f"[2/4] Đọc ảnh:\n  NS: {img_ns_path}\n  EW: {img_ew_path}")
    img_ns = cv2.imread(img_ns_path)
    img_ew = cv2.imread(img_ew_path)

    # --- Nhận diện ---
    print("[3/4] Đang nhận diện phương tiện...")
    results_ns = model(img_ns, verbose=False)
    results_ew = model(img_ew, verbose=False)

    counts_ns = count_by_axis_detailed(results_ns[0])
    counts_ew = count_by_axis_detailed(results_ew[0])
    print_count_table(counts_ns, counts_ew)

    # --- Gửi API ---
    print("[4/4] Gửi lên Backend...")
    send_to_backend(counts_ns[0], counts_ew[0])

    # --- Hiển thị ---
    plotted_ns = cv2.resize(results_ns[0].plot(), (DISPLAY_W, DISPLAY_H))
    plotted_ew = cv2.resize(results_ew[0].plot(), (DISPLAY_W, DISPLAY_H))
    combined = np.hstack((plotted_ns, plotted_ew))

    # Thêm nhãn
    for x, label in [(20, "← BẮC-NAM (NS)"), (DISPLAY_W + 20, "← ĐÔNG-TÂY (EW)")]:
        cv2.putText(combined, label, (x, 35),
                    cv2.FONT_HERSHEY_SIMPLEX, 0.9, (0, 255, 80), 2)

    cv2.imshow("AI Traffic Demo – Image Mode (Press any key to close)", combined)
    print("\n[HINT] Cửa sổ nhận diện đang mở. Nhấn phím bất kỳ để đóng.")
    cv2.waitKey(0)
    cv2.destroyAllWindows()
    print("\n[DONE] Demo hoàn tất!\n")


if __name__ == "__main__":
    run_demo(IMG_NS, IMG_EW)