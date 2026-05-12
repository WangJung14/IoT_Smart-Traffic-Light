using SmartTrafficLight.Application.DTOs;

namespace SmartTrafficLight.Application.Interfaces;

/// <summary>
/// Service for calculating optimal traffic light timing using the Webster method.
/// Input: vehicle counts per direction (from YOLO detection).
/// Output: optimal cycle time and green phase durations.
/// </summary>
public interface IWebsterTimingService
{
    /// <summary>
    /// Calculate optimal timing based on current vehicle counts.
    /// Applies moving average smoothing to prevent jitter.
    /// </summary>
    WebsterResult Calculate(VehicleCounts nsVehicles, VehicleCounts ewVehicles);

    /// <summary>
    /// Get the last computed Webster result without recalculating.
    /// Returns null if no calculation has been performed yet.
    /// </summary>
    WebsterResult? GetLastResult();
}
