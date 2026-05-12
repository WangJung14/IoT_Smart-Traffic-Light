import sys
import os
sys.path.insert(0, os.path.dirname(__file__))

import cv2
import numpy as np
import requests
from ultralytics import YOLO
from core import count_total_detailed

# ======================================================
#  CẤU HÌNH
# ======================================================
BACKEND_API_URL = "http://localhost:5212/api/v1/hardware/vehicle-counts"
MODEL_PATH      = "yolov8n.pt"

# Kích thước khung hình thu nhỏ để hiển thị lưới 2x2
DISPLAY_W, DISPLAY_H = 480, 320
# ======================================================

def send_to_backend(ns_counts, ew_counts):
    """Gửi kết quả đếm xe lên Backend."""
    payload = { "nsVehicles": ns_counts, "ewVehicles": ew_counts }
    try:
        print("\n[→ API] Đang gửi dữ liệu lên Backend...")
        response = requests.post(BACKEND_API_URL, json=payload, timeout=5)
        if response.status_code == 200:
            d = response.json()
            if "greenNS" in d:
                print(f"[✓ API] Backend phản hồi thành công!")
                print(f"  ┌─ Chu kỳ tối ưu (Co) : {d['cycleTime']}s")
                print(f"  ├─ Xanh Bắc–Nam        : {d['greenNS']}s")
                print(f"  ├─ Xanh Đông–Tây       : {d['greenEW']}s")
                print(f"  ├─ Hệ số bão hòa (Y)   : {d['totalFlowRatio']:.3f}")
                print(f"  └─ Trạng thái          : {d['status']}")
            else:
                print(f"[✓ API] Backend phản hồi: {d}")
        else:
            print(f"[✗ API] Lỗi từ server: HTTP {response.status_code}")
            print(f"        Response: {response.text[:200]}")
    except requests.exceptions.ConnectionError:
        print("[✗ API] Không kết nối được Backend! Hãy chạy: dotnet run")
    except Exception as e:
        print(f"[✗ API] Lỗi không xác định: {e}")


def sum_counts(dict1, dict2):
    res = {}
    for k in ["car", "motorbike", "bus", "truck"]:
        res[k] = dict1.get(k, 0) + dict2.get(k, 0)
    return res

def print_count_table(n: dict, s: dict, e: dict, w: dict, ns_total: dict, ew_total: dict):
    print("\n┌─────────────────────────────────────────────────────────────┐")
    print("│             KẾT QUẢ NHẬN DIỆN 4 CAMERA ĐỘC LẬP              │")
    print("├────────────┬───────┬───────┬──────────┬───────┬───────┬─────┤")
    print("│ Loại xe    │ Bắc   │ Nam   │ Tổng B-N │ Đông  │ Tây   │ Đ-T │")
    print("├────────────┼───────┼───────┼──────────┼───────┼───────┼─────┤")
    labels = {"car": "Ô tô", "motorbike": "Xe máy", "bus": "Xe buýt", "truck": "Xe tải"}
    
    t_n, t_s, t_ns, t_e, t_w, t_ew = 0, 0, 0, 0, 0, 0
    for k in ["car", "motorbike", "bus", "truck"]:
        n_v, s_v, e_v, w_v = n[k], s[k], e[k], w[k]
        ns_v, ew_v = ns_total[k], ew_total[k]
        t_n += n_v; t_s += s_v; t_e += e_v; t_w += w_v; t_ns += ns_v; t_ew += ew_v
        
        print(f"│ {labels[k]:<10} │ {n_v:<5} │ {s_v:<5} │ {ns_v:<8} │ {e_v:<5} │ {w_v:<5} │ {ew_v:<3} │")
    print("├────────────┼───────┼───────┼──────────┼───────┼───────┼─────┤")
    print(f"│ {'TỔNG CỘNG':<10} │ {t_n:<5} │ {t_s:<5} │ {t_ns:<8} │ {t_e:<5} │ {t_w:<5} │ {t_ew:<3} │")
    print("└────────────┴───────┴───────┴──────────┴───────┴───────┴─────┘")


def run_demo(img_n, img_s, img_e, img_w):
    print("\n" + "="*55)
    print("  AI Traffic Demo – 4 CAMERA MODE")
    print("="*55)

    paths = [img_n, img_s, img_e, img_w]
    missing = [p for p in paths if not os.path.exists(p)]
    if missing:
        print(f"[✗] Không tìm thấy file ảnh:")
        for p in missing: print(f"    → {os.path.abspath(p)}")
        return

    print(f"\n[1/4] Đang load YOLO model ({MODEL_PATH})...")
    model = YOLO(MODEL_PATH)

    print(f"[2/4] Đọc 4 ảnh từ data/img/...")
    imgs = [cv2.imread(p) for p in paths]

    print("[3/4] Đang nhận diện phương tiện trên 4 hướng...")
    results = [model(img, verbose=False)[0] for img in imgs]
    
    counts = [count_total_detailed(res) for res in results]
    c_n, c_s, c_e, c_w = counts

    # Tính tổng 2 trục
    ns_total = sum_counts(c_n, c_s)
    ew_total = sum_counts(c_e, c_w)

    print_count_table(c_n, c_s, c_e, c_w, ns_total, ew_total)

    print("[4/4] Gửi lên Backend...")
    send_to_backend(ns_total, ew_total)

    # Hiển thị
    plotted = [cv2.resize(res.plot(), (DISPLAY_W, DISPLAY_H)) for res in results]
    
    # Gắn nhãn lên ảnh
    labels = ["BACC (NORTH)", "NAM (SOUTH)", "DONG (EAST)", "TAY (WEST)"]
    for i in range(4):
        cv2.putText(plotted[i], labels[i], (10, 30), cv2.FONT_HERSHEY_SIMPLEX, 0.8, (0, 255, 255), 2)
        
    top_row = np.hstack((plotted[0], plotted[1])) # N, S
    bot_row = np.hstack((plotted[2], plotted[3])) # E, W
    grid = np.vstack((top_row, bot_row))

    cv2.imshow("AI Traffic 4-Camera Demo", grid)
    print("\n[HINT] Cửa sổ hiển thị đang mở. Nhấn phím bất kỳ để đóng và trở về Menu.")
    cv2.waitKey(0)
    cv2.destroyAllWindows()


def show_menu():
    while True:
        print("\n" + "="*50)
        print("🚦 CHỌN KỊCH BẢN DEMO (4 CAMERA) 🚦")
        print("="*50)
        print("[1] Mật độ cân bằng (Low / Balanced Traffic)")
        print("    → 4 đường vắng xe, thời gian xanh chia đều.")
        print("[2] Kẹt xe trục chính Bắc-Nam (Rush Hour)")
        print("    → B-N kẹt nặng, Đ-T vắng. B-N được ưu tiên xanh dài.")
        print("[3] Kẹt xe một chiều (Bắc kẹt, Nam vắng)")
        print("    → Chỉ luồng Bắc đông. Hệ thống vẫn dồn ưu tiên cho B-N.")
        print("[4] AI Actuated (Kích hoạt Đổi Đèn Khẩn Cấp)")
        print("    → Dùng khi bật Infinite Mode. Giả lập Đông-Tây kẹt nặng để ép đổi đèn.")
        print("[0] Thoát")
        print("="*50)
        
        choice = input("Nhập lựa chọn của bạn (0-4): ").strip()
        
        if choice == '0':
            print("Đã thoát.")
            break
        elif choice == '1':
            run_demo(
                "data/img/low_traffic_1.jpg", 
                "data/img/low_traffic_2.png", 
                "data/img/low_traffic.png", 
                "data/img/low_traffic.jfif"
            )
        elif choice == '2':
            run_demo(
                "data/img/heavy_traffic_1.jpg", 
                "data/img/heavy_traffic_2.jpg", 
                "data/img/low_traffic.png", 
                "data/img/low_traffic.jfif"
            )
        elif choice == '3':
            run_demo(
                "data/img/heavy_traffic_1.jpg", 
                "data/img/low_traffic_2.png", 
                "data/img/low_traffic.png", 
                "data/img/low_traffic.jfif"
            )
        elif choice == '4':
            run_demo(
                "data/img/low_traffic_1.jpg", 
                "data/img/low_traffic_2.png", 
                "data/img/heavy_traffic_3.jpg", 
                "data/img/heavy_traffic_4.jpg"
            )
        else:
            print("Lựa chọn không hợp lệ!")

if __name__ == "__main__":
    show_menu()