using Microsoft.EntityFrameworkCore;
using SmartTrafficLight_Domain.Entities;
using SmartTrafficLight_Domain.Interfaces;
using SmartTrafficLight_Infrastructure.Data;

namespace SmartTrafficLight_Infrastructure.Persistence
{
    public class TrafficLightRepository : ITrafficLightRepository
    {
        private readonly AppDbContext _context;

        public TrafficLightRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<TrafficLight?> GetByIdAsync(Guid id)
        {
            // Sử dụng Include để lấy kèm thông tin Giao lộ nếu cần
            return await _context.TrafficLights
                .Include(tl => tl.Intersection)
                .FirstOrDefaultAsync(tl => tl.Id == id);
        }

        public async Task<IEnumerable<TrafficLight>> GetByIntersectionIdAsync(Guid intersectionId)
        {
            return await _context.TrafficLights
                .Where(tl => tl.IntersectionId == intersectionId)
                .ToListAsync();
        }

        public async Task UpdateAsync(TrafficLight trafficLight)
        {
            // EF Core sẽ tự động track các thay đổi, nhưng gọi Update để chắc chắn
            _context.TrafficLights.Update(trafficLight);
            await _context.SaveChangesAsync();
        }
    }
}
