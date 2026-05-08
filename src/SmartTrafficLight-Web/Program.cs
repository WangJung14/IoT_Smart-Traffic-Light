using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartTrafficLight.Application.Interfaces;
using SmartTrafficLight.Application.Services;
using SmartTrafficLight_Application.Services;
using SmartTrafficLight_Infrastructure.Data;
using SmartTrafficLight_Domain.Interfaces;
using SmartTrafficLight_Infrastructure.Persistence;
using SmartTrafficLight_Infrastructure.Background;
using SmartTrafficLight_Web.Hubs;
using SmartTrafficLight_Web.Services;
using SmartTrafficLight.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// ===================== Services =====================

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<ITrafficDetectionService, TrafficDetectionService>();
builder.Services.AddScoped<IMLPredictionService, MLPredictionService>();
builder.Services.AddScoped<ILightControlService,LightControlService>();
builder.Services.AddScoped<ITrafficNotificationService, TrafficNotificationService>();
builder.Services.AddSingleton<IArduinoSerialService, ArduinoSerialService>();

builder.Services.AddSignalR();

// Config API Route Prefix
builder.Services.AddControllers(options =>
{
    options.Conventions.Add(new RoutePrefixConvention("api/v1"));
});

// Enable CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://127.0.0.1:5500", "http://localhost:5500")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// ===================== Dependency Injection =====================
builder.Services.AddScoped<IIntersectionRepository, IntersectionRepository>();
builder.Services.AddScoped<ITrafficLightRepository, TrafficLightRepository>();
builder.Services.AddScoped<ITrafficDataRepository, TrafficDataRepository>();

// ===================== Background Service =====================
// builder.Services.AddHostedService<TrafficProcessingService>();

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

// Khởi tạo ArduinoSerialService ngay lập tức để mở kết nối COM2
app.Services.GetRequiredService<IArduinoSerialService>();

// ===================== Middleware =====================

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");

app.UseHttpsRedirection();

app.MapControllers();
app.MapHub<TrafficHub>("/hubs/traffic");

// ===================== Health Check Endpoint =====================
app.MapGet("/api/health/db", async ([FromServices] AppDbContext dbContext) =>
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

// ===================== Test Database Connection Endpoint =====================
//app.MapGet("/api/test-db", async ([FromServices] IIntersectionRepository repo) =>
//{
//    
//    var intersections = await repo.GetAllAsync();

//    if (intersections.Any())
//        return Results.Ok(new { Message = "Kết nối Database THÀNH CÔNG! 🎉", Data = intersections });

//    return Results.Ok(new { Message = "Kết nối thành công nhưng chưa có dữ liệu." });
//});

app.Run();