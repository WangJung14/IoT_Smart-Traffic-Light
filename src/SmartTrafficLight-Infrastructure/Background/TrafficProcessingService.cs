using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartTrafficLight.Application.Interfaces;
using SmartTrafficLight.Application.DTOs;
using SmartTrafficLight.Domain.Enums;
using SmartTrafficLight_Domain.Interfaces;

namespace SmartTrafficLight_Infrastructure.Background
{
    public class TrafficProcessingService : BackgroundService
    {
        private readonly ILogger<TrafficProcessingService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public TrafficProcessingService(ILogger<TrafficProcessingService> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Bắt đầu khởi động TrafficProcessingService...");

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Đang kiểm tra lưu lượng xe...");

                try
                {
                    using var scope = _scopeFactory.CreateScope();

                    var intersectionRepo = scope.ServiceProvider.GetRequiredService<IIntersectionRepository>();
                    var detectionService = scope.ServiceProvider.GetRequiredService<ITrafficDetectionService>();
                    var mlService = scope.ServiceProvider.GetRequiredService<IMLPredictionService>();
                    var lightControlService = scope.ServiceProvider.GetRequiredService<ILightControlService>();
                    var notificationService = scope.ServiceProvider.GetRequiredService<ITrafficNotificationService>();

                    // Bước 1: Lấy các giao lộ (Intersection)
                    var intersections = await intersectionRepo.GetAllAsync();
                    
                    foreach (var intersection in intersections)
                    {
                        // Lấy dữ liệu Vehicle Count mới nhất do YOLO bắn lên
                        int vehicleCount = await detectionService.GetCurrentTrafficAsync(intersection.Id, Direction.NORTH);

                        // Broadcast lưu lượng xe qua SignalR
                        try
                        {
                            var trafficPayload = new TrafficUpdatePayload(Direction.NORTH, vehicleCount);
                            await notificationService.SendTrafficUpdateAsync(trafficPayload);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Lỗi khi broadcast TrafficUpdatePayload");
                        }

                        // Bước 2: Gọi ML để dự đoán
                        var timingConfig = await mlService.PredictTimingAsync(vehicleCount, DateTime.UtcNow);
                        
                        _logger.LogInformation("Đã tính toán xong thời gian đèn: {GreenDuration}s cho giao lộ {IntersectionId}", timingConfig.GreenDuration, intersection.Id);

                        // Bước 3: Cập nhật trạng thái đèn vào DB hoặc gọi UI Update (Giả lập chuyển sang Xanh)
                        // Trong thực tế sẽ cần logic State Machine để đổi đèn an toàn.
                        // await lightControlService.SetLightStateAsync(intersection.Id, Direction.NORTH, LightState.GREEN);
                        
                        // Broadcast trạng thái đèn qua SignalR
                        try
                        {
                            var lightPayload = new LightStatePayload(intersection.Id, Direction.NORTH, LightState.GREEN);
                            await notificationService.SendLightStateAsync(lightPayload);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Lỗi khi broadcast LightStatePayload");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Ignore expected cancellation during shutdown
                    break;
                }
                catch (ObjectDisposedException) when (stoppingToken.IsCancellationRequested)
                {
                    // Ignore disposed services during shutdown
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Có lỗi xảy ra trong Background Service: {Message}", ex.Message);
                }

                // Nghỉ 2 giây để tránh quá tải CPU
                await Task.Delay(2000, stoppingToken);
            }
        }
    }
}
