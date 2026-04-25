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
        // implement later
        throw new NotImplementedException();
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