using SmartTrafficLight_Domain.Entities;

namespace SmartTrafficLight_Domain.Interfaces;

public interface IDetectionLogRepository
{
    Task AddAsync(DetectionLog log);
    Task<IEnumerable<DetectionLog>> GetRecentAsync(int count = 20);
}
