using Microsoft.AspNetCore.Mvc;
using SmartTrafficLight.Application.Interfaces;
using SmartTrafficLight.Domain.Enums;
using SmartTrafficLight_Application.DTOs;

namespace SmartTrafficLight_Web.Controllers
{
    [ApiController]
    [Route("lights")]
    public class LightController : ControllerBase
    {
        private readonly ILightControlService _lightService;

        // DI constructor
        public LightController(ILightControlService lightService)
        {
            _lightService = lightService;
        }

        public record LightOverrideRequest(Guid IntersectionId, Direction Direction, LightState ForcedState);

        /// <summary>
        /// POST: Admin can manually override the traffic light state for a specific intersection and direction
        /// Mẫu URL: POST /api/v1/lights/override
        /// </summary>
        [HttpPost("override")]
        public async Task<IActionResult> OverrideLight([FromBody] LightOverrideRequest request)
        {
            // call service at layer application to override the traffic light state
            await _lightService.ManualOverrideAsync(request.IntersectionId, request.Direction, request.ForcedState);

            var data = new
            {
                request.IntersectionId,
                Direction = request.Direction.ToString(),
                ForcedState = request.ForcedState.ToString()
            };
            return Ok(ApiResponse<object>.Ok(data, "Traffic light state has been successfully overridden!"));
        }
    }
}
