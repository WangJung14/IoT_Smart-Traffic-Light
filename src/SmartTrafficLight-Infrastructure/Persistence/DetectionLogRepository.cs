using Microsoft.EntityFrameworkCore;
using SmartTrafficLight_Domain.Entities;
using SmartTrafficLight_Domain.Interfaces;
using SmartTrafficLight_Infrastructure.Data;

namespace SmartTrafficLight_Infrastructure.Persistence;

public class DetectionLogRepository : IDetectionLogRepository
{
    private readonly AppDbContext _context;

    public DetectionLogRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(DetectionLog log)
    {
        await _context.DetectionLogs.AddAsync(log);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<DetectionLog>> GetRecentAsync(int count = 20)
    {
        return await _context.DetectionLogs
            .AsNoTracking()
            .OrderByDescending(d => d.Timestamp)
            .Take(count)
            .ToListAsync();
    }
}
