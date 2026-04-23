using SmartTrafficLight.Application.DTOs;
using SmartTrafficLight.Domain.Enums;

namespace SmartTrafficLight.Application.Interfaces;

public interface ITrafficDetectionService
{
    // YOLO call this function to save detection data
    Task SaveDetectionDataAsync(Guid intersectionId, Direction direction, int vehicleCount);

    // Get current traffic for dashboard
    Task<int> GetCurrentTrafficAsync(Guid intersectionId, Direction direction);

    // Get historical traffic data for dashboard
    Task<IEnumerable<TrafficHistoryDto>> GetTrafficHistoryAsync(Guid intersectionId, int minutes);
}