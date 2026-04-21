using Microsoft.EntityFrameworkCore;
using SmartTrafficLight_Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// ===================== Services =====================

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Config API Route Prefix
builder.Services.AddControllers(options =>
{
    options.Conventions.Add(new RoutePrefixConvention("api/v1"));
});

// ===================== Database =====================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), mySqlOptions =>
    {
        mySqlOptions.MigrationsAssembly("SmartTrafficLight-Infrastructure");
        mySqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    });
});

var app = builder.Build();

// ===================== Middleware =====================

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.MapControllers();

// ===================== Health Check Endpoint =====================
app.MapGet("/api/health/db", async (AppDbContext dbContext) =>
{
    try
    {
        var canConnect = await dbContext.Database.CanConnectAsync();
        return canConnect
            ? Results.Ok(new { Status = "Healthy", Database = "Connected", Timestamp = DateTime.UtcNow })
            : Results.Json(new { Status = "Unhealthy", Database = "Cannot connect", Timestamp = DateTime.UtcNow }, statusCode: 503);
    }
    catch (Exception ex)
    {
        return Results.Json(new { Status = "Unhealthy", Database = "Error", Error = ex.Message, Timestamp = DateTime.UtcNow }, statusCode: 503);
    }
});

app.Run();