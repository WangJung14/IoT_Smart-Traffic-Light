using Microsoft.Extensions.Logging;
using SmartTrafficLight.Application.DTOs;
using SmartTrafficLight.Application.Interfaces;
using SmartTrafficLight.Domain.Enums;
using SmartTrafficLight_Domain.Entities;
using SmartTrafficLight_Domain.Interfaces;

namespace SmartTrafficLight.Application.Services;

public class LightControlService : ILightControlService
{
    private readonly ITrafficLightRepository _lightRepo;
    private readonly ITrafficDataRepository _dataRepo;
    private readonly IIntersectionRepository _intersectionRepo;
    private readonly ILogger<LightControlService> _logger;


    // DI constructer
    public LightControlService(
        ITrafficLightRepository lightRepo,
        ITrafficDataRepository dataRepo,
        ILogger<LightControlService> logger
    )
    {
        _lightRepo = lightRepo;
        _dataRepo = dataRepo;
        _logger = logger;
    }

    public async Task<DashboardDataDto> GetDashboardDataAsync(Guid intersectionId)
    {
        _logger.LogInformation("Fetching dashboard data for intersection {IntersectionId}", intersectionId);

        // 1. Get traffic lights for the intersection
        var intersection = await _intersectionRepo.GetByIdAsync(intersectionId);
        if(intersection == null) throw new Exception("Intersection not found.");

        // 2. Get recent traffic data for the intersection
        var lights = await _lightRepo.GetByIntersectionIdAsync(intersectionId);
        var mainLight = lights.FirstOrDefault() ?? new TrafficLight { CurrentState = LightState.RED };

        // 3. Get data from the last 5 minutes
        var recentTraffic = await _dataRepo.GetRecentDataAsync(intersectionId, 5);
        var currentCount = recentTraffic.OrderByDescending(x => x.Timestamp).FirstOrDefault()?.VehicleCount ?? 0;

        // 4. Calculate congestion level (simple heuristic)
        // The part of this will be optimized when we have background timmer 
        int remainingSeconds = 30;

        // 5. Mapping DTO and return for client
        return new DashboardDataDto
        {
            IntersectionId = intersectionId,
            IntersectionName = intersection.Name,
            CurrentLightState = mainLight.CurrentState,
            CurrentVehicleCount = currentCount,
            RemainingSeconds = remainingSeconds,
            RecentTraffic = recentTraffic.Select(t => new TrafficHistoryDto(
                t.IntersectionId,
                t.Direction,
                t.VehicleCount,
                t.Timestamp
            ))
        };
    }

    public async Task ManualOverrideAsync(Guid intersectionId, Direction direction, LightState forcedState)
    {
        _logger.LogInformation("ADMIN OVERRIDE: {IntersectionId}, {Direction}, {ForcedState}", intersectionId, direction, forcedState);

        var light = await GetLightOrThrow(intersectionId, direction);

        if(light.CurrentState == LightState.GREEN && forcedState == LightState.RED)
        {
            _logger.LogInformation("Changing light from GREEN to RED for {IntersectionId} - {Direction}", intersectionId, direction);
            light.CurrentState = LightState.YELLOW;
            await _lightRepo.UpdateAsync(light);

            // wait 3 seconds before turning red
            await Task.Delay(3000);
        }

        light.CurrentState = forcedState;
        await _lightRepo.UpdateAsync(light);
    }

    public async Task SetLightStateAsync(Guid intersectionId, Direction direction, LightState newState)
    {
        var light = await GetLightOrThrow(intersectionId, direction);

        // Check the validity of the transition (Traffic safety)
        if (!IsValidTransition(light.CurrentState, newState))
        {
            _logger.LogWarning("Change from {Current} to {New} is not valid for light {Id}",
                light.CurrentState, newState, light.Id);
            return;
        }

        light.CurrentState = newState;
        await _lightRepo.UpdateAsync(light);

        _logger.LogInformation("Light direction {Dir} changed to: {State}", direction, newState);
    }

    private async Task<TrafficLight> GetLightOrThrow(Guid intersectionId, Direction direction)
    {
        var lights = await _lightRepo.GetByIntersectionIdAsync(intersectionId);
        var light = lights.FirstOrDefault(l => l.Direction == direction);

        if (light == null) throw new Exception($"Can't not found light {direction} at this intersection.");
        return light;
    }
    private bool IsValidTransition(LightState current, LightState next)
    {
        return (current, next) switch
        {
            (LightState.GREEN, LightState.YELLOW) => true,
            (LightState.YELLOW, LightState.RED) => true,
            (LightState.RED, LightState.GREEN) => true,
            _ => false // All other transitions are invalid (e.g., GREEN to RED directly, YELLOW to GREEN, etc.)
        };
    }
}