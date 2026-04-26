using Microsoft.AspNetCore.Mvc;
using SmartTrafficLight.Application.Interfaces;
using SmartTrafficLight_Application.DTOs;
using SmartTrafficLight_Domain.ValueObjects;

namespace SmartTrafficLight_Web.Controllers
{
    [ApiController]
    [Route("prediction")]
    public class PredictionController : ControllerBase
    {
        private readonly IMLPredictionService _predictionService;

        // DI constructor
        public PredictionController(IMLPredictionService predictionService)
        {
            _predictionService = predictionService;
        }
        /// <summary>
        /// GET: Asking AI for traffic light timing prediction based on current traffic data
        /// URL: GET /api/v1/prediction/timing?currentVehicleCount=45
        /// </summary>
        [HttpGet("timing")]
        public async Task<IActionResult> GetPredictedTiming([FromQuery] int currentVehicleCount, [FromQuery] DateTime? timeOfDay)
        {
            // If frontend doesn't provide timeOfDay, we will use current UTC time as default (you can adjust this to local time if needed)
            var time = timeOfDay ?? DateTime.UtcNow;

            // Call service for compling the prediction logic
            var result = await _predictionService.PredictTimingAsync(currentVehicleCount, time);

            return Ok(ApiResponse<TimingConfig>.Ok(result, "Đã tính toán xong thời gian đề xuất."));
        }
    }
}
