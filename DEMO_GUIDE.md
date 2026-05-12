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

### Bước 3: Demo Chế độ Vô Tận (Infinite Mode) & Admin Override
Hệ thống nay hỗ trợ chế độ Vô Tận, không sử dụng bộ đếm ngược mà chỉ đổi màu khi có lệnh từ Admin hoặc AI.
1. Trên Dashboard (Góc trên bên phải), nhấn nút **INFINITE: OFF** để chuyển sang **INFINITE: ON** (Phát sáng tím).
2. **Kết quả**: Bộ đếm giây ở các hướng sẽ chuyển thành ký hiệu vô cực (`∞`). Đèn sẽ giữ nguyên trạng thái hiện tại mãi mãi.
3. **Can thiệp thủ công (Admin Jump)**:
   - Các nút **"ƯU TIÊN B-N"** và **"ƯU TIÊN Đ-T"** sẽ xuất hiện.
   - Nhấn **ƯU TIÊN Đ-T** (khi B-N đang Xanh).
   - **Kết quả**: Đèn B-N sẽ KHÔNG lập tức chuyển Đỏ gây nguy hiểm, mà sẽ tự động chuyển sang **VÀNG trong 5 giây**, sau đó mới nhảy sang Đ-T XANH. Đèn trong Proteus sẽ đổi theo.

### Bước 4: Demo AI Actuated (Tự Động Đổi Đèn Trong Chế Độ Vô Tận)
1. Hãy đảm bảo đang ở chế độ **INFINITE: ON** và luồng Đông-Tây (Đ-T) đang **ĐỎ**.
2. Trên màn hình Terminal của Python (YOLO), nhấn phím số `2` để giả lập lưu lượng xe đông (Heavy Traffic) ở hướng Đông-Tây (> 10 xe chờ đèn đỏ).
3. **Kết quả**: Ngay khi Backend nhận được lượng xe > ngưỡng quy định, nó sẽ tự ra quyết định "Actuated" và đẩy lệnh `Jump` xuống Arduino.
4. Đèn sẽ tự động chạy qua Vàng và nhường đường cho Đông-Tây mà không cần bạn bấm trên web!
5. Nhấn nút **RESET** để trả lại hệ thống về chế độ Auto (Tắt Infinite) bình thường.

### Bước 5: Demo với Ảnh (Nếu không có video)
1. Bỏ 2 tấm ảnh vào thư mục `camera_detect/demo_data/`.
2. Chạy `python src/detect_by_image.py`.
3. Chương trình sẽ hiện kết quả nhận diện trên 2 ảnh và cập nhật giây ngay lập tức.
4. Bạn có thể đổi ảnh khác (ví dụ ảnh kẹt xe thực tế) để thầy cô thấy AI phản ứng linh hoạt.

---

### Bước 6: Demo AI Forecasting & Vehicle Stats (Phân tích nâng cao)
Hệ thống nay đã tích hợp Dashboard phân tích dữ liệu lịch sử từ Database.
1. Trên giao diện web, chọn tab **AI FORECAST**.
2. **AI Forecast (Line Chart)**:
   - Hệ thống hiển thị dự báo chu kỳ đèn (Cycle Time) trong 24 giờ tới dựa trên mô hình **Random Forest**.
   - Di chuột vào biểu đồ để xem chi tiết thời gian dự kiến (sáng sớm vắng xe ~40s, giờ cao điểm ~120s).
3. **Lượng xe theo giờ (Stacked Bar Chart)**:
   - Chuyển sang tab **LƯỢNG XE THEO GIỜ**.
   - Tại đây, bạn có thể lọc dữ liệu theo **Nguồn (Source)**: Video hoặc Simulation.
   - Quan sát biểu đồ cột chồng (Stacked Bar) phân chia rõ rệt số lượng: Ô tô, Xe máy, Xe buýt, Xe tải.
   - Hệ thống sẽ tự động tính toán trung bình theo khung giờ từ hàng nghìn bản ghi trong `DetectionLogs`.

---

## 🔍 Cách kiểm tra "Liên kết đã thông chưa?"
1. **YOLO đã gửi data chưa?**: Nhìn terminal Python, nếu thấy dòng `[API] Updated Timing -> Co:120s...` là YOLO đã liên kết thành công với Backend.
2. **ML API đã chạy chưa?**: Truy cập `http://localhost:8000/predict`, nếu thấy JSON chứa 24 giờ dữ liệu là FastAPI đã thông.
3. **Frontend đã nhận AI data chưa?**: Nếu bảng **AI TIMING ANALYSIS** trên web nhảy số theo video là SignalR đã thông.

---
**Lưu ý**: Nếu báo lỗi `Connection failed` ở terminal Python, hãy kiểm tra xem Backend đã chạy chưa (mặc định port 5212).