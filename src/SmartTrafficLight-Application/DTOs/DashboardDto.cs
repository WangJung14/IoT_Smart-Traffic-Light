using SmartTrafficLight.Domain.Enums;

namespace SmartTrafficLight.Application.DTOs;

public record DashboardDataDto
{
    public Guid IntersectionId { get; init; }
    public string IntersectionName { get; init; } = string.Empty;
    public LightState CurrentLightState { get; init; }
    public int CurrentVehicleCount { get; init; }
    public int RemainingSeconds { get; init; }
    public IEnumerable<TrafficHistoryDto> RecentTraffic { get; init; } = new List<TrafficHistoryDto>();
}