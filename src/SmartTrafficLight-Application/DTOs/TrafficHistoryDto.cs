using SmartTrafficLight.Domain.Enums;

namespace SmartTrafficLight.Application.DTOs;

public record TrafficHistoryDto(
    Guid IntersectionId,
    Direction Direction,
    int VehicleCount,
    DateTime Timestamp
);