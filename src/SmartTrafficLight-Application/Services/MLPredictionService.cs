using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using SmartTrafficLight.Application.Interfaces;
using SmartTrafficLight_Domain.ValueObjects;

namespace SmartTrafficLight.Application.Services;

public class MLPredictionService : IMLPredictionService
{

    private readonly ILogger<MLPredictionService> _logger;

    // dependency injection constructor
    public MLPredictionService(ILogger<MLPredictionService> logger)
    {
        _logger = logger;
    }

    public Task<TimingConfig> PredictTimingAsync(int currentVehicleCount, DateTime timeOfDay)
    {
        _logger.LogInformation("Mock data for couting number of vehicle : {count}", currentVehicleCount);

        //  Base on number of vehicle for prediction time
        TimingConfig suggestTiming;

        // v1 use if else
        // heavy traffic
        if (currentVehicleCount > 30)
        {
            suggestTiming = new TimingConfig(greenDuration: 60, yellowDuration: 3, redDuration: 30);
        }
        else if (currentVehicleCount > 20)
        {
            suggestTiming = new TimingConfig(greenDuration: 40, yellowDuration: 3, redDuration: 30);
        }
        // no traffic in road
        else
        {
            suggestTiming = new TimingConfig(greenDuration: 20, yellowDuration: 3, redDuration: 40);
        }
        return Task.FromResult(suggestTiming);
    }
}
