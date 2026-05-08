# 🚦 Hướng dẫn Demo: Smart Traffic AI System

Tài liệu này hướng dẫn bạn cách chạy demo toàn bộ hệ thống từ Phát hiện xe -> Tính toán AI -> Điều khiển phần cứng -> Dashboard.

## 🛠 Chuẩn bị
1. **Proteus**: Mở file mạch, đảm bảo COMPIM đã cấu hình **COM2** (Baudrate 9600). Nhấn **Play**.
2. **Backend**: 
   - Mở terminal tại `src/SmartTrafficLight-Web`.
   - Chạy: `dotnet run`
3. **Frontend**:
   - Mở terminal tại `src/smart-traffic-dashboard`.
   - Chạy: `npm run dev`
   - Mở trình duyệt: `http://localhost:3000`
4. **AI Vision (YOLO)**:
   - **Cách 1 (Video)**: Chạy `python src/detect_by_video.py`
   - **Cách 2 (Ảnh)**: Chạy `python src/detect_by_image.py` (Dùng khi không có video)

---

## 🎬 Kịch bản Demo (Demo Script)

### Bước 1: Kiểm tra kết nối (Initial Check)
- Trên Dashboard, kiểm tra trạng thái **LIVE** (màu xanh lá).
- Quan sát bảng **SYSTEM ACTIVITY LOGS** xem có nhận được tín hiệu từ Arduino không.

### Bước 2: Demo AI tự động điều phối (AI Auto-Timing)
Lúc này, hãy tập trung vào cửa sổ Python (YOLO) và dùng các phím nóng:
1. **Nhấn phím `2` (Mặc định)**: Hướng Bắc-Nam kẹt xe (Heavy), hướng Đông-Tây vắng (Clear).
   - **Kết quả**: Dashboard sẽ hiện `Status: OVERLOADED`, thời gian Xanh Bắc-Nam sẽ tăng vọt (khoảng 80-90s), Đông-Tây giảm xuống mức tối thiểu (10-15s).
   - Kiểm tra log Backend: Thấy lệnh `Sent timing update to Arduino: T:89,15`.

2. **Nhấn phím `3`**: Cả 2 hướng đều vắng xe (Low Traffic).
   - **Kết quả**: Dashboard hiện `Status: NORMAL`, chu kỳ giảm xuống (Co ≈ 40-50s), thời gian xanh chia đều 50/50.

3. **Nhấn phím `1`**: Cả 2 hướng đều đông xe.
   - **Kết quả**: Chu kỳ sẽ đạt tối đa (120s), xanh chia đều cho cả 2 hướng để giải tỏa tối đa.

### Bước 3: Demo Can thiệp thủ công (Admin Override)
- Trên Dashboard, nhấn nút **"B-N → XANH"**.
- **Kết quả**: Đèn Bắc-Nam lập tức chuyển Xanh bất kể AI đang tính toán gì. Đèn trong Proteus sẽ đổi theo.
- Nhấn nút **RESET** để trả lại quyền điều khiển cho AI.

### Bước 4: Demo với Ảnh (Nếu không có video)
1. Bỏ 2 tấm ảnh vào thư mục `camera_detect/demo_data/`.
2. Chạy `python src/detect_by_image.py`.
3. Chương trình sẽ hiện kết quả nhận diện trên 2 ảnh và cập nhật giây ngay lập tức.
4. Bạn có thể đổi ảnh khác (ví dụ ảnh kẹt xe thực tế) để thầy cô thấy AI phản ứng linh hoạt.

---

## 🔍 Cách kiểm tra "Liên kết đã thông chưa?"
1. **YOLO đã gửi data chưa?**: Nhìn terminal Python, nếu thấy dòng `[API] Updated Timing -> Co:120s...` là YOLO đã liên kết thành công với Backend.
2. **Backend đã gửi xuống Arduino chưa?**: Nhìn terminal Backend, nếu thấy `Sent timing update to Arduino: T:...` là liên kết Serial đã thông.
3. **Frontend đã nhận AI data chưa?**: Nếu bảng **AI TIMING ANALYSIS** trên web nhảy số theo video là SignalR đã thông.

---
**Lưu ý**: Nếu báo lỗi `Connection failed` ở terminal Python, hãy kiểm tra xem Backend đã chạy chưa (mặc định port 5212).