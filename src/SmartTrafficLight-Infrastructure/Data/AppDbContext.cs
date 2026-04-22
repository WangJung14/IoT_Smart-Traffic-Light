using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using SmartTrafficLight_Domain.Entities;
using SmartTrafficLight_Domain.ValueObjects;

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

    //Dbset table in database
    public DbSet<Intersection> Intersections => Set<Intersection>();
    public DbSet<TrafficLight> TrafficLights => Set<TrafficLight>();
    public DbSet<TrafficData> TrafficDatas => Set<TrafficData>();
    public DbSet<PredictionResult> PredictionResults => Set<PredictionResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from the Infrastructure assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
