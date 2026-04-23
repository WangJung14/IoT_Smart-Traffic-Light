using Microsoft.Extensions.Logging;
using SmartTrafficLight.Application.DTOs;
using SmartTrafficLight.Application.Interfaces;
using SmartTrafficLight.Domain.Enums;
using SmartTrafficLight_Domain.Entities;
using SmartTrafficLight_Domain.Interfaces;

namespace SmartTrafficLight_Application.Services
{
    public class TrafficDetectionService : ITrafficDetectionService
    {
        private readonly ITrafficDataRepository _repository;
        private readonly ILogger<TrafficDetectionService> _logger;

        // DI constructor
        public TrafficDetectionService(ITrafficDataRepository repository, ILogger<TrafficDetectionService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task SaveDetectionDataAsync(Guid intersectionId, Direction direction, int vehicleCount)
        {
            // 1. Validate input (bike is not negative)
            if (vehicleCount < 0)
            {
                _logger.LogWarning("Detect data with negative vehicle count: {VehicleCount} at intersection {IntersectionId} direction {Direction}", vehicleCount, intersectionId, direction);
                return;
            }
            // 2. Mapping to domain entity

            var trafficData = new TrafficData
            {
                IntersectionId = intersectionId,
                Direction = direction,
                VehicleCount = vehicleCount,
                Timestamp = DateTime.UtcNow
            };

            // 3. Save to repository
            await _repository.AddAsync(trafficData);

            // 4. Log the detection data
            _logger.LogInformation("Saved traffic data: {VehicleCount} vehicles at intersection {IntersectionId} direction {Direction}", vehicleCount, intersectionId, direction);

        }

        public async Task<int> GetCurrentTrafficAsync(Guid intersectionId, Direction direction)
        {
            // 1. Get recent data (last 2 minutes)
            var recentData = await _repository.GetRecentDataAsync(intersectionId, 2);

            // 2. Filter by direction and get newest record
            var latestData = recentData
             .Where(x => x.Direction == direction)
             .MaxBy(x => x.Timestamp);

            // 3. If do not have any data return 0 (no traffic)
            return latestData?.VehicleCount ?? 0;
        }

        public async Task<IEnumerable<TrafficHistoryDto>> GetTrafficHistoryAsync(Guid intersectionId, int minutes)
        {
            // 1. Get raw data from repository
            var rawData = await _repository.GetRecentDataAsync(intersectionId, minutes);

            // 2. Map Entity to DTO return for Web API
            return rawData.Select(x => new TrafficHistoryDto(
                x.IntersectionId,
                x.Direction,
                x.VehicleCount,
                x.Timestamp
            )).ToList();
        }


    }
}
