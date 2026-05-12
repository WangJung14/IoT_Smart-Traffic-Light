using Microsoft.AspNetCore.Mvc;
using SmartTrafficLight.Application.DTOs;
using SmartTrafficLight.Application.Interfaces;
using SmartTrafficLight_Domain.Entities;
using SmartTrafficLight_Domain.Interfaces;

namespace SmartTrafficLight_Web.Controllers
{
    [ApiController]
    [Route("hardware")]
    public class HardwareController : ControllerBase
    {
        private static DateTime _lastJumpTime = DateTime.MinValue;
        private readonly IArduinoSerialService _arduinoService;
        private readonly IWebsterTimingService _websterService;
        private readonly ITrafficNotificationService _notificationService;
        private readonly IDetectionLogRepository _detectionLogRepo;

        public HardwareController(
            IArduinoSerialService arduinoService,
            IWebsterTimingService websterService,
            ITrafficNotificationService notificationService,
            IDetectionLogRepository detectionLogRepo)
        {
            _arduinoService     = arduinoService;
            _websterService     = websterService;
            _notificationService = notificationService;
            _detectionLogRepo   = detectionLogRepo;
        }

        /// <summary>
        /// POST: Receive vehicle counts from YOLO and calculate optimal timing via Webster
        /// URL: POST /api/v1/hardware/vehicle-counts
        /// Body: { "source": "IMAGE", "nsVehicles": {...}, "ewVehicles": {...} }
        /// </summary>
        [HttpPost("vehicle-counts")]
        public async Task<IActionResult> ReceiveVehicleCounts([FromBody] VehicleCountRequest request)
        {
            if (request.NsVehicles == null || request.EwVehicles == null)
                return BadRequest(new { error = "Vehicle counts for both directions are required" });

            // 1. Check current mode
            bool isInfinite = _arduinoService.IsInfiniteMode;

            // 2. Tính toán thời gian tối ưu bằng Webster (vẫn tính để lưu log)
            var result = _websterService.Calculate(request.NsVehicles, request.EwVehicles);

            if (isInfinite)
            {
                // AI Actuated Logic (Chỉ quyết định đổi đèn khi ở chế độ Vô Tận)
                string currentStatus = _arduinoService.GetLatestStatus();
                int threshold = 10; // Ngưỡng xe chờ để quyết định chuyển đèn
                
                // Tránh tình trạng AI đổi đèn liên tục nếu cả 2 bên đều đông xe (Cooldown 30 giây)
                if ((DateTime.UtcNow - _lastJumpTime).TotalSeconds > 30)
                {
                    if (currentStatus.Contains("B-N:XANH") && request.EwVehicles.Total >= threshold)
                    {
                        // Hướng NS đang xanh, nhưng EW đang có nhiều xe chờ quá -> Chuyển sang EW Xanh
                        _arduinoService.RequestJump(2); 
                        _lastJumpTime = DateTime.UtcNow;
                    }
                    else if (currentStatus.Contains("D-T:XANH") && request.NsVehicles.Total >= threshold)
                    {
                        // Hướng EW đang xanh, nhưng NS đang có nhiều xe chờ quá -> Chuyển sang NS Xanh
                        _arduinoService.RequestJump(0);
                        _lastJumpTime = DateTime.UtcNow;
                    }
                }
            }
            else
            {
                // Chế độ Auto bình thường: Gửi lệnh cập nhật giây đếm ngược
                _arduinoService.SendTimingUpdate(result.GreenNS, result.GreenEW);
            }

            // 3. Phát tín hiệu lên Dashboard qua SignalR
            await _notificationService.SendWebsterUpdateAsync(new WebsterUpdatePayload
            {
                CycleTime      = result.CycleTime,
                GreenNS        = result.GreenNS,
                GreenEW        = result.GreenEW,
                TotalFlowRatio = result.TotalFlowRatio,
                Status         = result.Status,
                PcuNS          = result.PcuNS,
                PcuEW          = result.PcuEW
            });

            // 4. Lưu kết quả vào Database
            var ns = request.NsVehicles;
            var ew = request.EwVehicles;
            var log = new DetectionLog
            {
                Timestamp            = DateTime.UtcNow,
                NsCars               = ns.Car,
                NsMotorbikes         = ns.Motorbike,
                NsBuses              = ns.Bus,
                NsTrucks             = ns.Truck,
                EwCars               = ew.Car,
                EwMotorbikes         = ew.Motorbike,
                EwBuses              = ew.Bus,
                EwTrucks             = ew.Truck,
                CalculatedCycleTime  = result.CycleTime,
                CalculatedGreenNS    = result.GreenNS,
                CalculatedGreenEW    = result.GreenEW,
                TotalFlowRatio       = result.TotalFlowRatio,
                Status               = result.Status,
                Source               = request.Source ?? "VIDEO"
            };
            await _detectionLogRepo.AddAsync(log);

            return Ok(result);
        }

        /// <summary>
        /// GET: Lấy lịch sử phân tích AI (20 bản ghi mới nhất)
        /// URL: GET /api/v1/hardware/detection-history
        /// </summary>
        [HttpGet("detection-history")]
        public async Task<IActionResult> GetDetectionHistory([FromQuery] int count = 20)
        {
            var logs = await _detectionLogRepo.GetRecentAsync(count);
            var result = logs.Select(l => new
            {
                id             = l.Id,
                timestamp      = l.Timestamp,
                source         = l.Source,
                ns = new { cars = l.NsCars, motorbikes = l.NsMotorbikes, buses = l.NsBuses, trucks = l.NsTrucks },
                ew = new { cars = l.EwCars, motorbikes = l.EwMotorbikes, buses = l.EwBuses, trucks = l.EwTrucks },
                webster = new
                {
                    cycleTime      = l.CalculatedCycleTime,
                    greenNS        = l.CalculatedGreenNS,
                    greenEW        = l.CalculatedGreenEW,
                    totalFlowRatio = l.TotalFlowRatio,
                    status         = l.Status
                }
            });
            return Ok(result);
        }

        /// <summary>
        /// GET: Get the latest Webster calculation result
        /// URL: GET /api/v1/hardware/webster-result
        /// </summary>
        [HttpGet("webster-result")]
        public IActionResult GetWebsterResult()
        {
            var result = _websterService.GetLastResult();
            if (result == null) return NotFound(new { message = "No Webster calculation has been performed yet" });
            return Ok(result);
        }

        public record VehicleCountRequest(VehicleCounts NsVehicles, VehicleCounts EwVehicles, string? Source = "VIDEO");

        /// <summary>GET: Get the latest status from Arduino</summary>
        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            var status = _arduinoService.GetLatestStatus();
            return Ok(new { status, timestamp = DateTime.UtcNow });
        }

        /// <summary>POST: Force Arduino to a specific traffic phase</summary>
        [HttpPost("force-state")]
        public IActionResult ForceState([FromBody] ForceStateRequest request)
        {
            if (request.StateIndex < 0 || request.StateIndex > 3)
                return BadRequest(new { error = "stateIndex must be 0-3" });

            _arduinoService.ForceState(request.StateIndex);

            string[] stateNames = {
                "Bắc-Nam: XANH, Đông-Tây: ĐỎ",
                "Bắc-Nam: VÀNG, Đông-Tây: ĐỎ",
                "Bắc-Nam: ĐỎ, Đông-Tây: XANH",
                "Bắc-Nam: ĐỎ, Đông-Tây: VÀNG"
            };

            return Ok(new {
                message    = $"Force state sent: {stateNames[request.StateIndex]}",
                stateIndex = request.StateIndex,
                timestamp  = DateTime.UtcNow
            });
        }

        /// <summary>POST: Reset Arduino to initial state (NS_GREEN)</summary>
        [HttpPost("reset")]
        public IActionResult Reset()
        {
            _arduinoService.SendReset();
            return Ok(new { message = "Reset command sent to Arduino", timestamp = DateTime.UtcNow });
        }

        /// <summary>POST: Update timing durations for green phases</summary>
        [HttpPost("timing")]
        public IActionResult UpdateTiming([FromBody] TimingRequest request)
        {
            if (request.NsGreenDuration <= 0 || request.EwGreenDuration <= 0)
                return BadRequest(new { error = "Durations must be positive" });

            _arduinoService.SendTimingUpdate(request.NsGreenDuration, request.EwGreenDuration);
            return Ok(new {
                message   = $"Timing updated: NS={request.NsGreenDuration}s, EW={request.EwGreenDuration}s",
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>POST: Enable or disable infinite mode</summary>
        [HttpPost("mode")]
        public async Task<IActionResult> SetMode([FromBody] ModeRequest request)
        {
            _arduinoService.SetInfiniteMode(request.IsInfinite);
            await _notificationService.SendHardwareStatusAsync(new HardwareStatusPayload(
                request.IsInfinite ? "MODE:INFINITE" : "MODE:AUTO"
            ));
            return Ok(new { message = $"Infinite mode set to {request.IsInfinite}", timestamp = DateTime.UtcNow });
        }

        /// <summary>POST: Safely jump to a specific traffic phase</summary>
        [HttpPost("jump")]
        public IActionResult JumpToState([FromBody] ForceStateRequest request)
        {
            if (request.StateIndex != 0 && request.StateIndex != 2)
                return BadRequest(new { error = "stateIndex must be 0 (NS Green) or 2 (EW Green)" });

            _arduinoService.RequestJump(request.StateIndex);

            return Ok(new {
                message    = $"Jump requested to state: {request.StateIndex}",
                stateIndex = request.StateIndex,
                timestamp  = DateTime.UtcNow
            });
        }

        public record ForceStateRequest(int StateIndex);
        public record TimingRequest(int NsGreenDuration, int EwGreenDuration);
        public record ModeRequest(bool IsInfinite);
    }
}
