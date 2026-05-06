using Microsoft.AspNetCore.SignalR;
using Moq;
using SmartTrafficLight.Application.DTOs;
using SmartTrafficLight.Application.Interfaces;
using SmartTrafficLight.Domain.Enums;
using SmartTrafficLight_Web.Hubs;
using SmartTrafficLight_Web.Services;

namespace SmartTrafficLight.Tests.Web;

/// <summary>
/// Unit tests cho TrafficNotificationService.
/// Kiểm tra logic broadcast SignalR đến tất cả clients.
/// </summary>
public class TrafficNotificationServiceTests
{
    private readonly Mock<IHubContext<TrafficHub>> _hubContextMock;
    private readonly Mock<IHubClients> _clientsMock;
    private readonly Mock<IClientProxy> _clientProxyMock;
    private readonly TrafficNotificationService _service;

    public TrafficNotificationServiceTests()
    {
        _hubContextMock = new Mock<IHubContext<TrafficHub>>();
        _clientsMock = new Mock<IHubClients>();
        _clientProxyMock = new Mock<IClientProxy>();

        _clientsMock.Setup(c => c.All).Returns(_clientProxyMock.Object);
        _hubContextMock.Setup(h => h.Clients).Returns(_clientsMock.Object);

        _service = new TrafficNotificationService(_hubContextMock.Object);
    }

    [Fact]
    public async Task SendTrafficUpdateAsync_ShouldBroadcastToAllClients()
    {
        // Arrange
        var payload = new TrafficUpdatePayload(Direction.NORTH, 42);

        // Act
        await _service.SendTrafficUpdateAsync(payload);

        // Assert - verify SendAsync("ReceiveTrafficUpdate", ...) was called
        _clientProxyMock.Verify(
            c => c.SendCoreAsync("ReceiveTrafficUpdate", It.Is<object[]>(o => o.Length == 1), default),
            Times.Once);
    }

    [Fact]
    public async Task SendLightStateAsync_ShouldBroadcastToAllClients()
    {
        // Arrange
        var id = Guid.NewGuid();
        var payload = new LightStatePayload(id, LightState.GREEN);

        // Act
        await _service.SendLightStateAsync(payload);

        // Assert
        _clientProxyMock.Verify(
            c => c.SendCoreAsync("ReceiveLightState", It.Is<object[]>(o => o.Length == 1), default),
            Times.Once);
    }
}
