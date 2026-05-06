using Microsoft.AspNetCore.SignalR;
using SmartTrafficLight.Application.DTOs;
using SmartTrafficLight.Application.Interfaces;
using SmartTrafficLight_Web.Hubs;

namespace SmartTrafficLight_Web.Services;

public class TrafficNotificationService : ITrafficNotificationService
{
    private readonly IHubContext<TrafficHub> _hubContext;

    public TrafficNotificationService(IHubContext<TrafficHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendLightStateAsync(LightStatePayload payload)
    {
        await _hubContext.Clients.All.SendAsync("ReceiveLightState", payload);
    }

    public async Task SendTrafficUpdateAsync(TrafficUpdatePayload payload)
    {
        await _hubContext.Clients.All.SendAsync("ReceiveTrafficUpdate", payload);
    }
}
