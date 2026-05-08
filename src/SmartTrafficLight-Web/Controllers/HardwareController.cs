using Microsoft.AspNetCore.Mvc;
using SmartTrafficLight.Application.Interfaces;

namespace SmartTrafficLight_Web.Controllers
{
    [ApiController]
    [Route("hardware")]
    public class HardwareController : ControllerBase
    {
        private readonly IArduinoSerialService _arduinoService;

        public HardwareController(IArduinoSerialService arduinoService)
        {
            _arduinoService = arduinoService;
        }

        /// <summary>
        /// GET: Get the latest status from Arduino
        /// URL: GET /api/v1/hardware/status
        /// </summary>
        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            var status = _arduinoService.GetLatestStatus();
            return Ok(new { status, timestamp = DateTime.UtcNow });
        }

        /// <summary>
        /// POST: Force Arduino to a specific traffic phase
        /// URL: POST /api/v1/hardware/force-state
        /// Body: { "stateIndex": 0 }
        /// States: 0=NS_GREEN/EW_RED, 1=NS_YELLOW/EW_RED, 2=NS_RED/EW_GREEN, 3=NS_RED/EW_YELLOW
        /// </summary>
        [HttpPost("force-state")]
        public IActionResult ForceState([FromBody] ForceStateRequest request)
        {
            if (request.StateIndex < 0 || request.StateIndex > 3)
            {
                return BadRequest(new { error = "stateIndex must be 0-3" });
            }

            _arduinoService.ForceState(request.StateIndex);

            string[] stateNames = {
                "Bắc-Nam: XANH, Đông-Tây: ĐỎ",
                "Bắc-Nam: VÀNG, Đông-Tây: ĐỎ",
                "Bắc-Nam: ĐỎ, Đông-Tây: XANH",
                "Bắc-Nam: ĐỎ, Đông-Tây: VÀNG"
            };

            return Ok(new {
                message = $"Force state sent: {stateNames[request.StateIndex]}",
                stateIndex = request.StateIndex,
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// POST: Reset Arduino to initial state (NS_GREEN)
        /// URL: POST /api/v1/hardware/reset
        /// </summary>
        [HttpPost("reset")]
        public IActionResult Reset()
        {
            _arduinoService.SendReset();
            return Ok(new { message = "Reset command sent to Arduino", timestamp = DateTime.UtcNow });
        }

        /// <summary>
        /// POST: Update timing durations for green phases
        /// URL: POST /api/v1/hardware/timing
        /// Body: { "nsGreenDuration": 35, "ewGreenDuration": 50 }
        /// </summary>
        [HttpPost("timing")]
        public IActionResult UpdateTiming([FromBody] TimingRequest request)
        {
            if (request.NsGreenDuration <= 0 || request.EwGreenDuration <= 0)
            {
                return BadRequest(new { error = "Durations must be positive" });
            }

            _arduinoService.SendTimingUpdate(request.NsGreenDuration, request.EwGreenDuration);
            return Ok(new {
                message = $"Timing updated: NS={request.NsGreenDuration}s, EW={request.EwGreenDuration}s",
                timestamp = DateTime.UtcNow
            });
        }

        public record ForceStateRequest(int StateIndex);
        public record TimingRequest(int NsGreenDuration, int EwGreenDuration);
    }
}
