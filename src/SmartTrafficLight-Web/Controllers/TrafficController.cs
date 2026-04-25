using Microsoft.AspNetCore.Mvc;
using SmartTrafficLight.Application.DTOs;
using SmartTrafficLight.Application.Interfaces;
using SmartTrafficLight.Domain.Enums;
using SmartTrafficLight_Application.DTOs;

namespace SmartTrafficLight_Web.Controllers
{
    [ApiController]
    [Route("traffic")]
    public class TrafficController: ControllerBase
    {
        private readonly ITrafficDetectionService _trafficService;

        // DI constructor
        public TrafficController(ITrafficDetectionService trafficService)
        {
            _trafficService = trafficService;
        }

        public record TrafficDetectionRequest(Guid IntersectionId, Direction Direction, int VehicleCount);
        /// <summary>
        /// POST: Record the number of vehicles from the YOLO detection system.
        /// URL: POST /api/v1/traffic
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SaveDetectionData([FromBody] TrafficDetectionRequest request)
        {
            await _trafficService.SaveDetectionDataAsync(request.IntersectionId, request.Direction, request.VehicleCount);
            return Ok(ApiResponse<TrafficDetectionRequest>.Ok(request, "Traffic data has been successfully recorded!"));
        }
        /// <summary>
        /// GET: Get the current number of vehicles for a specific intersection and direction.
        /// URL: GET /api/v1/traffic/current?intersectionId=...&direction=NORTH
        /// </summary>

        [HttpGet("current")]
        public async Task<IActionResult> GetCurrentTraffic([FromQuery] Guid intersectionId, [FromQuery] Direction direction)
        {
            var count = await _trafficService.GetCurrentTrafficAsync(intersectionId, direction);

            var data = new
            {
                IntersectionId = intersectionId,
                Direction = direction.ToString(),
                VehicleCount = count
            };

            return Ok(ApiResponse<object>.Ok(data, "Current traffic data retrieved successfully!"));
        }

        /// <summary>
        /// GET: Get historical traffic to create graph for dashboard 
        /// Mẫu URL: GET /api/v1/traffic/history?intersectionId=...&minutes=30
        /// </summary>
        [HttpGet("history")]
        public async Task<IActionResult> GetTrafficHistory([FromQuery] Guid intersectionId, [FromQuery] int minutes = 30)
        {
            var history = await _trafficService.GetTrafficHistoryAsync(intersectionId, minutes);
            return Ok(ApiResponse<IEnumerable<TrafficHistoryDto>>.Ok(history, "Get traffic history success!"));
        }

    }
}
