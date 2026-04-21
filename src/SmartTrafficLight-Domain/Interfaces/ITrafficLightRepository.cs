using SmartTrafficLight_Domain.Entities;

namespace SmartTrafficLight_Domain.Interfaces
{
    public interface ITrafficLightRepository
    {
        Task<TrafficLight?> GetByIdAsync(Guid id);
        Task<IEnumerable<TrafficLight>> GetByIntersectionIdAsync(Guid intersectionId);
        Task UpdateAsync(TrafficLight trafficLight);
    }
}
