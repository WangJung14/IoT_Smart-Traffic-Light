using Microsoft.AspNetCore.Mvc;
using SmartTrafficLight.Application.DTOs;
using SmartTrafficLight.Application.Interfaces;
using SmartTrafficLight_Application.DTOs;

namespace SmartTrafficLight_Web.Controllers
{
    [ApiController]
    [Route("admin")]
    public class AdminController : Controller
    {
        private readonly ILightControlService _lightService;

        // DI constructor
        public AdminController(ILightControlService lightService)
        {
            _lightService = lightService;
        }
        /// <summary>
        /// GET: Get all data for dashboard of a specific intersection, including current light state, traffic data, and historical trends
        /// URL: GET /api/v1/admin/dashboard/{intersectionId}
        /// </summary>
        /// <param name="intersectionId">ID of the intersection from which data needs to be retrieved</param>

        [HttpGet("dashboard/{intersectionId:guid}")]
        public async Task<IActionResult> GetDashboardData(Guid intersectionId)
        {
            try
            {
                // Call service to get all necessary data for the dashboard in one go
                var dashboardData = await _lightService.GetDashboardDataAsync(intersectionId);

                return Ok(ApiResponse<DashboardDataDto>.Ok(dashboardData, "Success to load data"));
            }
            catch (Exception ex)
            {
                // Case where intersection is not found or DB error occurs
                return NotFound(ApiResponse<object>.Fail($"Cannot load data: {ex.Message}"));
            }
        }
    }
}
