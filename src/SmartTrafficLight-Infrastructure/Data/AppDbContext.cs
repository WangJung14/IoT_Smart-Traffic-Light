using Microsoft.EntityFrameworkCore;

namespace SmartTrafficLight_Infrastructure.Data;

/// <summary>
/// Main application database context for Smart Traffic Light System.
/// Manages all entity mappings and database configurations.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // === DbSet declarations will be added here as entities are created ===
    // Example: public DbSet<TrafficLight> TrafficLights => Set<TrafficLight>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from the Infrastructure assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
