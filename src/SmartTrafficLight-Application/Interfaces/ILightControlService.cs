using SmartTrafficLight.Application.DTOs;
using SmartTrafficLight.Domain.Enums;

namespace SmartTrafficLight.Application.Interfaces;

public interface ILightControlService
{
    // Change the state of the traffic light for a specific intersection and direction
    Task SetLightStateAsync(Guid intersectionId, Direction direction, LightState newState);

    // Admin can manually override the traffic light state
    Task ManualOverrideAsync(Guid intersectionId, Direction direction, LightState forcedState);

    // Get aggregated data for the Admin dashboard
    Task<DashboardDataDto> GetDashboardDataAsync(Guid intersectionId);

}