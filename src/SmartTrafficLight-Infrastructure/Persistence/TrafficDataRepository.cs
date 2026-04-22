using Microsoft.EntityFrameworkCore;
using SmartTrafficLight_Domain.Entities;
using SmartTrafficLight_Domain.Interfaces;
using SmartTrafficLight_Infrastructure.Data;

namespace SmartTrafficLight_Infrastructure.Persistence
{
    public class TrafficDataRepository : ITrafficDataRepository
    {
        private readonly AppDbContext _context;

        public TrafficDataRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(TrafficData trafficData)
        {
            await _context.TrafficDatas.AddAsync(trafficData);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<TrafficData>> GetRecentDataAsync(Guid intersectionId, int minutes)
        {
            var threshold = DateTime.UtcNow.AddMinutes(-minutes);

            return await _context.TrafficDatas
                .AsNoTracking() // Tối ưu hiệu năng vì đây là truy vấn chỉ đọc (Read-only)
                .Where(td => td.IntersectionId == intersectionId && td.Timestamp >= threshold)
                .OrderByDescending(td => td.Timestamp)
                .ToListAsync();
        }
    }
}
