using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace SmartTrafficLight_Web.Hubs;

public class TrafficHub : Hub
{
    private readonly ILogger<TrafficHub> _logger;

    public TrafficHub(ILogger<TrafficHub> logger)
    {
        _logger = logger;
    }

    public override Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}
