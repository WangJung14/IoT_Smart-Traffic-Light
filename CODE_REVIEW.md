# 🔍 CODE REVIEW - Smart Traffic Light System

**Ngày Review**: May 2, 2026  
**Reviewer**: Code Analysis  
**Status**: ⚠️ **CRITICAL ISSUES FOUND** - Cần fix trước khi production

---

## 📊 Tổng Quát

Hệ thống được thiết kế tốt với **Clean Architecture**, tuy nhiên có **5 vấn đề CRITICAL** trong phần **camera_yolo** mà sẽ gây lỗi dữ liệu và không đúng logic nghiệp vụ.

| Mức Độ | Số Lượng | Mô Tả |
|--------|---------|-------|
| 🔴 **CRITICAL** | 5 | Sẽ gây crash hoặc dữ liệu sai |
| 🟠 **MAJOR** | 8 | Ảnh hưởng logic nghiệp vụ |
| 🟡 **MEDIUM** | 7 | Cần optimize |
| 🟢 **MINOR** | 5 | Code quality |

---

## 🎬 CAMERA_YOLO - CHI TIẾT TỪNG VẤN ĐỀ

### 🔴 ISSUE 1: Direction Enum KHÔNG MATCH với Zone Indices

**File**: [camera_yolo/src/tracker.py](camera_yolo/src/tracker.py#L15) & [camera_yolo/src/api_client.py](camera_yolo/src/api_client.py)

**Vấn Đề**:
```python
# tracker.py - zones dict
self.zones = {
    0: ...,  # North
    1: ...,  # East  ❌ NHƯNG Direction.EAST = 2 trong C#
    2: ...,  # South ❌ NHƯNG Direction.SOUTH = 1 trong C#
    3: ...   # West
}

# api_client.py - send_detection
api_client.send_detection(dir_idx, count)  # dir_idx = 0,1,2,3
```

**C# Direction Enum**:
```csharp
enum Direction {
    NORTH = 0,
    SOUTH = 1,  // ❌ Python gửi 2
    EAST = 2,   // ❌ Python gửi 1
    WEST = 3    // ✅ Match
}
```

**Hậu Quả**:
- ❌ **South xe sẽ được ghi nhận thành EAST**
- ❌ **East xe sẽ được ghi nhận thành SOUTH**
- ❌ Dashboard sẽ hiển thị số liệu sai
- ❌ ML prediction sẽ train trên dữ liệu sai lệch

**Fix**:
```python
# Option 1: Sắp xếp lại zones dict để match Direction enum
self.zones = {
    0: np.array([[50, 300], ...]),      # NORTH (Direction.NORTH = 0) ✅
    1: np.array([[50, 660], ...]),      # SOUTH (Direction.SOUTH = 1) ✅
    2: np.array([[690, 300], ...]),     # EAST (Direction.EAST = 2) ✅
    3: np.array([[690, 660], ...])      # WEST (Direction.WEST = 3) ✅
}

# Option 2: Gửi direction enum value từ C# tương ứng
direction_mapping = {
    0: 0,  # Zone 0 (North) -> Direction.NORTH (0)
    1: 2,  # Zone 1 (East) -> Direction.EAST (2)
    2: 1,  # Zone 2 (South) -> Direction.SOUTH (1)
    3: 3   # Zone 3 (West) -> Direction.WEST (3)
}
for dir_idx, count in counts.items():
    api_client.send_detection(direction_mapping[dir_idx], count)
```

---

### 🔴 ISSUE 2: Missing .env File - INTERSECTION_ID không xác định

**File**: [camera_yolo/src/config.py](camera_yolo/src/config.py)

**Vấn Đề**:
```python
INTERSECTION_ID = os.getenv("INTERSECTION_ID", "")  # ❌ Default rỗng!
```

Không có `.env` file trong repo, nên `INTERSECTION_ID = ""`

**Hậu Quả**:
```json
// Khi send_detection gửi API:
{
    "intersectionId": "",  // ❌ UUID rỗng
    "direction": 0,
    "vehicleCount": 10
}
```
- Backend sẽ reject (GUID không hợp lệ) hoặc lưu vào intersection sai
- Tất cả dữ liệu gửi lên sẽ không hợp lệ

**Fix**:
Tạo file `.env`:
```bash
# camera_yolo/.env
API_TRAFFIC=http://localhost:5212/api/v1/traffic
API_DASHBOARD=http://localhost:5212/api/v1/admin/dashboard/
INTERSECTION_ID=<your-actual-intersection-uuid-here>
PROCESS_INTERVAL=2.0
```

Lấy INTERSECTION_ID từ database sau khi tạo Intersection.

---

### 🔴 ISSUE 3: Light State Logic Giả Định cứng Chế độ đèn

**File**: [camera_yolo/src/api_client.py](camera_yolo/src/api_client.py#L11-L20)

**Vấn Đề**:
```python
def get_light_states(self) -> dict:
    data = res.json()
    main_light = data["data"]["currentLightState"]  # 0=Green, 1=Yellow, 2=Red
    
    # Giả định cứng: N-S trục Bắc-Nam đi chung, E-W trục Đông-Tây đi chung
    if main_light == 0:
        return {0: "GREEN", 2: "GREEN", 1: "RED", 3: "RED"}  # Nhưng điều này là giả định!
    elif main_light == 1:
        return {0: "YELLOW", 2: "YELLOW", 1: "RED", 3: "RED"}
    else:
        return {0: "RED", 2: "RED", 1: "GREEN", 3: "GREEN"}
```

**Vấn Đề**:
- Backend không có logic để quản lý 4 đèn riêng biệt
- Dashboard chỉ trả về `currentLightState` (1 giá trị) cho toàn bộ giao lộ
- Logic giả định N-S song song, E-W song song không được confirm

**Backend hiện tại**:
```csharp
// LightControlService chỉ có SetLightStateAsync(Direction) - mỗi hướng riêng
// Nhưng AdminController trả về DashboardDataDto với 1 CurrentLightState chung
```

**Hậu Quả**:
- Nếu backend quản lý 4 đèn độc lập, video sẽ không sync đúng
- Nếu backend chỉ có 1 light state chung, logic này sẽ sai

**Fix - Option A (nếu backend quản lý riêng 4 đèn)**:
```python
# Gọi API riêng cho từng direction thay vì lấy 1 giá trị chung
async def get_light_states_individual(self) -> dict:
    states = {}
    for dir_idx, direction in enumerate([Direction.NORTH, Direction.SOUTH, Direction.EAST, Direction.WEST]):
        url = f"{Config.API_DASHBOARD}{Config.INTERSECTION_ID}/light/{direction}"
        res = requests.get(url, timeout=1.0)
        state_num = res.json()["data"]["currentLightState"]
        states[dir_idx] = {0: "RED", 1: "YELLOW", 2: "GREEN"}[state_num]
    return states
```

**Fix - Option B (xác nhận logic giả định)**:
Nếu logic hiện tại đúng (N-S song song), document rõ điều này.

---

### 🔴 ISSUE 4: Enum Serialization Mismatch - API Response Parsing

**File**: [camera_yolo/src/api_client.py](camera_yolo/src/api_client.py#L13)

**Vấn Đề**:
```python
main_light = data["data"]["currentLightState"]  # Giả định LightState enum = int (0,1,2)
```

Nhưng C# có thể serialize enum khác nhau:
```csharp
// Option 1: Numeric (mặc định trong API JSON)
{ "currentLightState": 0 }  ✅

// Option 2: String name
{ "currentLightState": "RED" }  ❌

// Option 3: Fully qualified
{ "currentLightState": "SmartTrafficLight.Domain.Enums.LightState.RED" }  ❌
```

**Không có test** để xác nhận format thực tế từ API.

**Fix**:
```python
def get_light_states(self) -> dict:
    try:
        data = res.json()
        main_light = data["data"]["currentLightState"]
        
        # Support cả string và int
        if isinstance(main_light, str):
            main_light = {"RED": 0, "YELLOW": 1, "GREEN": 2}.get(main_light, 0)
        
        main_light = int(main_light)  # Ensure int
        ...
    except (KeyError, ValueError, TypeError) as e:
        print(f"[API_ERROR] Failed to parse light state: {e}")
        return {0: "RED", 1: "RED", 2: "RED", 3: "RED"}  # Safe default
```

---

### 🔴 ISSUE 5: Vehicle Deduplication - Xe cũ được đếm lặp lại

**File**: [camera_yolo/src/tracker.py](camera_yolo/src/tracker.py#L28-L42)

**Vấn Đề**:
```python
results = self.model.track(grid_frame, persist=True, tracker="bytetrack.yaml", ...)

if results[0].boxes.id is not None:
    boxes = results[0].boxes.xyxy.cpu().numpy()
    for box in boxes:
        # ❌ Không có logic dedupe - cùng 1 xe xuất hiện frame 1,2,3 sẽ được đếm 3 lần!
        for dir_idx, polygon in self.zones.items():
            if cv2.pointPolygonTest(polygon, (cx, cy), False) >= 0:
                counts[dir_idx] += 1  # Cộng thêm mà không check xe cũ
```

**Vấn Đề Chi Tiết**:
- YOLO.track() trả về tracked object IDs (persistent)
- Nhưng code không lưu state từ frame trước
- Nên cùng 1 xe (cùng ID) nếu vẫn ở trong zone, sẽ bị đếm nhiều lần

**Ví Dụ**:
```
Frame 1: Xe A (ID=1) vào zone 0 -> count[0] += 1 = 1
Frame 2: Xe A (ID=1) vẫn trong zone 0 -> count[0] += 1 = 2  ❌ SAISAI
Frame 3: Xe A (ID=1) vẫn trong zone 0 -> count[0] += 1 = 3  ❌ SAISAI
Frame 4: Xe A (ID=1) rời zone 0 -> count[0] = 3 (đúng lúc này)
```

**PROCESS_INTERVAL = 2.0 giây**:
- Gửi API mỗi 2 giây = cộng count lại từ đầu
- Nhưng vẫn không fix vấn đề đếm lặp lại trong các frame giữa các lần gửi

**Fix**:
```python
class TrafficTracker:
    def __init__(self):
        ...
        self.counted_ids = {0: set(), 1: set(), 2: set(), 3: set()}  # Lưu ID xe đã đếm

    def process_grid(self, grid_frame):
        results = self.model.track(grid_frame, persist=True, ...)
        counts = {0: 0, 1: 0, 2: 0, 3: 0}
        
        if results[0].boxes.id is not None:
            boxes = results[0].boxes.xyxy.cpu().numpy()
            ids = results[0].boxes.id.cpu().numpy().astype(int)
            
            for box, obj_id in zip(boxes, ids):
                x1, y1, x2, y2 = map(int, box)
                cx, cy = int((x1 + x2) / 2), y2
                
                for dir_idx, polygon in self.zones.items():
                    if cv2.pointPolygonTest(polygon, (cx, cy), False) >= 0:
                        # ✅ Chỉ đếm nếu xe chưa được đếm ở zone này
                        if obj_id not in self.counted_ids[dir_idx]:
                            counts[dir_idx] += 1
                            self.counted_ids[dir_idx].add(obj_id)
                        break

        return counts, annotated_grid
    
    def reset_counts_periodically(self):
        """Reset counted IDs mỗi lần gửi API"""
        self.counted_ids = {0: set(), 1: set(), 2: set(), 3: set()}
```

---

## 🟠 MAJOR ISSUES

### ISSUE 6: Zone Polygon Coordinates HARDCODED - Không Flexible

**File**: [camera_yolo/src/tracker.py](camera_yolo/src/tracker.py#L14-L19)

```python
self.zones = {
    0: np.array([[50, 300], [550, 300], [450, 150], [150, 150]], np.int32),
    1: np.array([[690, 300], [1190, 300], [1090, 150], [790, 150]], np.int32),
    # ...
}
```

**Vấn Đề**:
- Giả định video luôn 1280x720 (4 ô 640x360 ghép lại)
- Giả định xe luôn ở cùng vị trí
- Nếu layout video khác, zones sẽ miss hoặc overlap

**Fix**:
```python
# Lưu vào config.py
ZONES_CONFIG = {
    0: [[50, 300], [550, 300], [450, 150], [150, 150]],     # North
    1: [[690, 300], [1190, 300], [1090, 150], [790, 150]],  # East
    2: [[50, 660], [550, 660], [450, 510], [150, 510]],     # South
    3: [[690, 660], [1190, 660], [1090, 510], [790, 510]]   # West
}

# Trong tracker.py
def load_zones_from_config(self):
    self.zones = {}
    for dir_idx, coords in Config.ZONES_CONFIG.items():
        self.zones[dir_idx] = np.array(coords, np.int32)

# Hoặc add UI calibration mode để user điều chỉnh zones
```

---

### ISSUE 7: API Timeout Quá Ngắn - Dễ fail

**File**: [camera_yolo/src/api_client.py](camera_yolo/src/api_client.py)

```python
res = requests.get(url, timeout=1.0)       # ❌ 1 second
requests.post(..., timeout=0.5)             # ❌ 0.5 second - QUÁ NGẮN!
```

**Vấn Đề**:
- Network latency + DB query có thể vượt quá timeout
- API sẽ fail im lặng (exception caught nhưng không log)
- Dữ liệu sẽ không được gửi lên

**Fix**:
```python
# Increase timeout
res = requests.get(url, timeout=3.0)
requests.post(..., timeout=2.0)

# Add retry logic
def send_detection_with_retry(self, direction: int, count: int, retries=3):
    for attempt in range(retries):
        try:
            requests.post(Config.API_TRAFFIC, ..., timeout=2.0)
            return True
        except requests.Timeout:
            if attempt == retries - 1:
                print(f"[API_ERROR] Failed after {retries} retries")
            continue
    return False
```

---

### ISSUE 8: Không Validate Nested Response

**File**: [camera_yolo/src/api_client.py](camera_yolo/src/api_client.py#L13)

```python
data = res.json()
main_light = data["data"]["currentLightState"]  # ❌ No null check
```

**Vấn Đề**:
- Nếu API trả về format khác (e.g., error response), sẽ KeyError
- Exception không được catch

**Fix**:
```python
try:
    data = res.json()
    main_light = data.get("data", {}).get("currentLightState", None)
    
    if main_light is None:
        print(f"[API_WARN] Invalid response: {data}")
        return {0: "RED", 1: "RED", 2: "RED", 3: "RED"}
        
    main_light = int(main_light)
    ...
except (json.JSONDecodeError, ValueError, TypeError) as e:
    print(f"[API_ERROR] Response parse error: {e}")
    return {0: "RED", 1: "RED", 2: "RED", 3: "RED"}
```

---

### ISSUE 9: Hardcoded Video Path và Magic Numbers

**File**: [camera_yolo/src/main.py](camera_yolo/src/main.py#L6-L7)

```python
WAIT_FRAME_END = 150      # ❌ Magic number
TOTAL_FRAMES = 300        # ❌ Magic number
video_manager = VideoGridManager("data/traffic.mp4", ...)  # ❌ Hardcoded path
```

**Vấn Đề**:
- Nếu video khác, cần sửa code
- Không thể reuse cho video khác

**Fix**:
```python
# config.py
VIDEO_PATH = os.getenv("VIDEO_PATH", "data/traffic.mp4")
VIDEO_WAIT_FRAME_END = int(os.getenv("VIDEO_WAIT_FRAME_END", "150"))
VIDEO_TOTAL_FRAMES = int(os.getenv("VIDEO_TOTAL_FRAMES", "300"))

# main.py
video_manager = VideoGridManager(
    Config.VIDEO_PATH,
    Config.VIDEO_WAIT_FRAME_END,
    Config.VIDEO_TOTAL_FRAMES
)
```

---

### ISSUE 10: No Logging Framework - Chỉ dùng print()

**File**: Tất cả Python files

```python
print("[API] Đã đồng bộ...")
print(f"[API_WARN] Không thể lấy trạng thái đèn: {e}")
```

**Vấn Đề**:
- Không có log file
- Không có log level (DEBUG, INFO, WARNING, ERROR)
- Khó debug khi run production

**Fix**:
```python
import logging

logger = logging.getLogger(__name__)
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler('logs/traffic.log'),
        logging.StreamHandler()
    ]
)

# Thay thế print
logger.warning(f"Không thể lấy trạng thái đèn: {e}")
```

---

### ISSUE 11: Video Frame Seeking Logic Có Vấn Đề

**File**: [camera_yolo/src/video_manager.py](camera_yolo/src/video_manager.py#L25-L38)

```python
if state == "RED" and current_frame >= self.wait_frame_end:
    cap.set(cv2.CAP_PROP_POS_FRAMES, 0)  # Reset to frame 0
elif state == "GREEN":
    if current_frame < self.wait_frame_end:
        cap.set(cv2.CAP_PROP_POS_FRAMES, self.wait_frame_end)  # Jump to wait_frame
    elif current_frame >= self.total_frames - 2:
        cap.set(cv2.CAP_PROP_POS_FRAMES, self.total_frames - 5)  # Jump back
```

**Vấn Đề**:
- Logic không rõ ràng: tại sao cần "jump back" lúc RED?
- Có thể gây frame skip hoặc glitch trong detection
- Không có error handling nếu seek fail

**Hậu Quả**:
- Xe ở boundary giữa state transitions dễ bị miss
- Detection có thể bị gián đoạn

---

### ISSUE 12: Flip Modes Không Documented

**File**: [camera_yolo/src/video_manager.py](camera_yolo/src/video_manager.py#L15)

```python
self.flip_modes = {0: None, 1: 1, 2: None, 3: 1}  # ❌ Tại sao lại flip?
```

**Vấn Đề**:
- Không rõ lý do flip East (1) và West (3)
- Nếu flip sai, orientation sẽ lỗi

**Fix**:
```python
# Document the reason
"""
flip_modes: Horizontal flip để giả lập camera direction
- North (0): No flip - camera hướng Bắc
- East (1): Flip - camera hướng Đông (lật lại để đồng bộ)
- South (2): No flip - camera hướng Nam
- West (3): Flip - camera hướng Tây (lật lại để đồng bộ)
"""
self.flip_modes = {0: None, 1: 1, 2: None, 3: 1}
```

---

## 🟡 MEDIUM ISSUES

### ISSUE 13: PROCESS_INTERVAL Quá Lớn (2 giây)

Mỗi 2 giây mới gửi 1 lần. Nếu có sự thay đổi đột ngột (kẹt xe), sẽ delay 2 giây.

**Recommend**: Giảm xuống 0.5-1.0 giây để responsive hơn.

---

### ISSUE 14: No Cross-Frame Tracking State

Nếu 1 xe di chuyển từ zone North sang zone East, không có logic để track.

---

### ISSUE 15: Backend không hỗ trợ Individual Light Control per Direction

[LightController](src/SmartTrafficLight-Web/Controllers/LightController.cs) chỉ có `SetLightStateAsync` nhưng `AdminController` không expose endpoint này - chỉ có `ManualOverrideAsync`.

---

### ISSUE 16: No Database Seeding

Không có script để create sample Intersection + TrafficLights. User phải create manually hoặc via API.

---

### ISSUE 17: ML.NET Integration Missing

`MLPredictionService` không có implementation trong code. Chỉ có interface.

---

### ISSUE 18: No Background Service cho Automatic Light Timing

TASKS.MD nhắc đến "Background Service" nhưng code không có. Lights chỉ được set via API (manual).

---

### ISSUE 19: SignalR Not Implemented

README nói "Web Admin" giám sát real-time nhưng không có SignalR connection.

---

## ✅ POSITIVE FINDINGS

### Architecture & Design

✅ **Clean Architecture** - Domain, Application, Infrastructure layers tách biệt rõ  
✅ **Dependency Injection** - Program.cs configure đầy đủ  
✅ **Entity Validation** - VehicleCount không được âm  
✅ **State Machine** - Light transition validation (no direct GREEN→RED)  
✅ **Repository Pattern** - Abstraction tốt cho data access  

### Code Quality

✅ **Async/Await** - Sử dụng async task đúng cách  
✅ **Error Handling** - Try-catch ở các điểm key  
✅ **Null Coalescing** - `?? 0` để handle null  
✅ **DTOs** - Tách biệt model từ API response  

---

## 📋 PRIORITY FIX CHECKLIST

### 🔴 CRITICAL (Fix ngay)
- [ ] Fix Direction enum mismatch (Issue 1)
- [ ] Create .env file với INTERSECTION_ID (Issue 2)
- [ ] Fix vehicle deduplication logic (Issue 5)
- [ ] Verify light state API format (Issue 4)
- [ ] Document/Fix light state mapping logic (Issue 3)

### 🟠 MAJOR (Fix tuần này)
- [ ] Make zones configurable (Issue 6)
- [ ] Increase API timeouts + add retry (Issue 7)
- [ ] Add response validation (Issue 8)
- [ ] Move hardcoded values to config (Issue 9)
- [ ] Add logging framework (Issue 10)
- [ ] Document flip logic (Issue 12)

### 🟡 MEDIUM (Fix tháng này)
- [ ] Add database seeding script
- [ ] Implement ML.NET integration
- [ ] Add Background Service for auto control
- [ ] Implement SignalR for real-time updates
- [ ] Add unit tests

---

## 🚀 RECOMMENDATIONS

### Immediate Actions
1. Fix Direction enum mismatch trước khi deploy
2. Create .env file với đúng INTERSECTION_ID
3. Test API integration end-to-end

### Short Term (1-2 tuần)
1. Implement vehicle deduplication
2. Add comprehensive logging
3. Create database seeding script

### Long Term (1 tháng)
1. Implement Background Service để auto-control lights
2. Add SignalR for real-time dashboard updates
3. Complete ML.NET integration
4. Add unit & integration tests

---

## 📞 Questions for Clarification

1. **Light State Pattern**: Là traffic lights luôn hoạt động N-S song song và E-W song song, hay mỗi direction độc lập?
2. **Video Format**: Traffic.mp4 luôn 1280x720 với 4 ô ghép, hay có thể khác?
3. **Zone Calibration**: Có cần UI để user adjust zones hay giá trị hiện tại đã correct?
4. **ML Integration**: ML.NET model training data đã sẵn sàng chưa?
5. **Hardware**: Arduino integration qua API hay trực tiếp serial?

---

**Generated**: May 2, 2026  
**Status**: 📌 Pending Fixes
