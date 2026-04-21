using SmartTrafficLight_Domain.Entities;
namespace SmartTrafficLight_Domain.Interfaces
{
    public interface IIntersectionRepository
    {
        Task<Intersection?> GetByIdAsync(Guid id);
        Task<IEnumerable<Intersection>> GetAllAsync();
        Task AddAsync(Intersection intersection);
        Task UpdateAsync(Intersection intersection);
    }
}
