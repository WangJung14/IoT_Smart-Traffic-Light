using SmartTrafficLight.Application.DTOs;

namespace SmartTrafficLight.Application.Interfaces;

public interface ITrafficNotificationService
{
    Task SendTrafficUpdateAsync(TrafficUpdatePayload payload);
    Task SendLightStateAsync(LightStatePayload payload);
}
