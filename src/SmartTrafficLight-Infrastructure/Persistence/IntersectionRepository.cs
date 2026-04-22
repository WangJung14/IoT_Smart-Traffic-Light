using Microsoft.EntityFrameworkCore;
using SmartTrafficLight_Domain.Entities;
using SmartTrafficLight_Domain.Interfaces;
using SmartTrafficLight_Infrastructure.Data;

namespace SmartTrafficLight_Infrastructure.Persistence
{
    public class IntersectionRepository : IIntersectionRepository
    {
        private readonly AppDbContext _context;

        public IntersectionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Intersection?> GetByIdAsync(Guid id)
        {
            return await _context.Intersections
                .Include(i => i.TrafficLights) // Load kèm luôn thông tin các đèn
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<IEnumerable<Intersection>> GetAllAsync()
        {
            return await _context.Intersections.ToListAsync();
        }

        public async Task AddAsync(Intersection intersection)
        {
            await _context.Intersections.AddAsync(intersection);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Intersection intersection)
        {
            _context.Intersections.Update(intersection);
            await _context.SaveChangesAsync();
        }
    }
}
