using SmartTrafficLight.Domain.Enums;

namespace SmartTrafficLight.Application.DTOs;

public record TrafficUpdatePayload(Direction Direction, int VehicleCount);

public record LightStatePayload(Guid IntersectionId, Direction Direction, LightState CurrentLightState);
