using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartTrafficLight_Domain.ValueObjects;

namespace SmartTrafficLight.Application.Interfaces
{
    public interface IMLPredictionService
    {
        // Predict the optimal timing for traffic lights based on current vehicle count and time of day
        Task<TimingConfig> PredictTimingAsync(int currentVehicleCount, DateTime timeOfDay);
    }
}
