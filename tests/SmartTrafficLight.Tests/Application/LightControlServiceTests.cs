using Microsoft.Extensions.Logging;
using Moq;
using SmartTrafficLight.Application.DTOs;
using SmartTrafficLight.Application.Interfaces;
using SmartTrafficLight.Application.Services;
using SmartTrafficLight.Domain.Enums;
using SmartTrafficLight_Domain.Entities;
using SmartTrafficLight_Domain.Interfaces;
using SmartTrafficLight_Domain.ValueObjects;

namespace SmartTrafficLight.Tests.Application;

/// <summary>
/// Unit tests cho LightControlService.
/// Kiểm tra logic chuyển đèn (State Machine), Manual Override, và Dashboard Data.
/// </summary>
public class LightControlServiceTests
{
    private readonly Mock<ITrafficLightRepository> _lightRepoMock;
    private readonly Mock<ITrafficDataRepository> _dataRepoMock;
    private readonly Mock<IIntersectionRepository> _intersectionRepoMock;
    private readonly Mock<ILogger<LightControlService>> _loggerMock;
    private readonly LightControlService _service;

    private readonly Guid _testIntersectionId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public LightControlServiceTests()
    {
        _lightRepoMock = new Mock<ITrafficLightRepository>();
        _dataRepoMock = new Mock<ITrafficDataRepository>();
        _intersectionRepoMock = new Mock<IIntersectionRepository>();
        _loggerMock = new Mock<ILogger<LightControlService>>();

        _service = new LightControlService(
            _lightRepoMock.Object,
            _dataRepoMock.Object,
            _intersectionRepoMock.Object,
            _loggerMock.Object
        );
    }

    // ===================== SetLightStateAsync Tests =====================

    [Fact]
    public async Task SetLightState_GreenToYellow_ShouldSucceed()
    {
        // Arrange
        var light = CreateTestLight(LightState.GREEN);
        SetupLightRepo(light);

        // Act
        await _service.SetLightStateAsync(_testIntersectionId, Direction.NORTH, LightState.YELLOW);

        // Assert
        Assert.Equal(LightState.YELLOW, light.CurrentState);
        _lightRepoMock.Verify(r => r.UpdateAsync(light), Times.Once);
    }

    [Fact]
    public async Task SetLightState_YellowToRed_ShouldSucceed()
    {
        var light = CreateTestLight(LightState.YELLOW);
        SetupLightRepo(light);

        await _service.SetLightStateAsync(_testIntersectionId, Direction.NORTH, LightState.RED);

        Assert.Equal(LightState.RED, light.CurrentState);
        _lightRepoMock.Verify(r => r.UpdateAsync(light), Times.Once);
    }

    [Fact]
    public async Task SetLightState_RedToGreen_ShouldSucceed()
    {
        var light = CreateTestLight(LightState.RED);
        SetupLightRepo(light);

        await _service.SetLightStateAsync(_testIntersectionId, Direction.NORTH, LightState.GREEN);

        Assert.Equal(LightState.GREEN, light.CurrentState);
        _lightRepoMock.Verify(r => r.UpdateAsync(light), Times.Once);
    }

    [Fact]
    public async Task SetLightState_GreenToRed_ShouldNotUpdate_InvalidTransition()
    {
        // GREEN -> RED is not allowed (must go through YELLOW)
        var light = CreateTestLight(LightState.GREEN);
        SetupLightRepo(light);

        await _service.SetLightStateAsync(_testIntersectionId, Direction.NORTH, LightState.RED);

        // State should remain GREEN
        Assert.Equal(LightState.GREEN, light.CurrentState);
        _lightRepoMock.Verify(r => r.UpdateAsync(It.IsAny<TrafficLight>()), Times.Never);
    }

    [Fact]
    public async Task SetLightState_YellowToGreen_ShouldNotUpdate_InvalidTransition()
    {
        var light = CreateTestLight(LightState.YELLOW);
        SetupLightRepo(light);

        await _service.SetLightStateAsync(_testIntersectionId, Direction.NORTH, LightState.GREEN);

        Assert.Equal(LightState.YELLOW, light.CurrentState);
        _lightRepoMock.Verify(r => r.UpdateAsync(It.IsAny<TrafficLight>()), Times.Never);
    }

    [Fact]
    public async Task SetLightState_RedToYellow_ShouldNotUpdate_InvalidTransition()
    {
        var light = CreateTestLight(LightState.RED);
        SetupLightRepo(light);

        await _service.SetLightStateAsync(_testIntersectionId, Direction.NORTH, LightState.YELLOW);

        Assert.Equal(LightState.RED, light.CurrentState);
        _lightRepoMock.Verify(r => r.UpdateAsync(It.IsAny<TrafficLight>()), Times.Never);
    }

    [Fact]
    public async Task SetLightState_SameState_ShouldNotUpdate()
    {
        var light = CreateTestLight(LightState.GREEN);
        SetupLightRepo(light);

        await _service.SetLightStateAsync(_testIntersectionId, Direction.NORTH, LightState.GREEN);

        _lightRepoMock.Verify(r => r.UpdateAsync(It.IsAny<TrafficLight>()), Times.Never);
    }

    [Fact]
    public async Task SetLightState_LightNotFound_ShouldThrowException()
    {
        _lightRepoMock.Setup(r => r.GetByIntersectionIdAsync(_testIntersectionId))
            .ReturnsAsync(Enumerable.Empty<TrafficLight>());

        await Assert.ThrowsAsync<Exception>(
            () => _service.SetLightStateAsync(_testIntersectionId, Direction.NORTH, LightState.GREEN));
    }

    // ===================== ManualOverrideAsync Tests =====================

    [Fact]
    public async Task ManualOverride_ForceRed_ShouldSetState()
    {
        var light = CreateTestLight(LightState.RED);
        SetupLightRepo(light);

        await _service.ManualOverrideAsync(_testIntersectionId, Direction.NORTH, LightState.RED);

        Assert.Equal(LightState.RED, light.CurrentState);
    }

    [Fact]
    public async Task ManualOverride_ForceGreen_ShouldSetState()
    {
        var light = CreateTestLight(LightState.RED);
        SetupLightRepo(light);

        await _service.ManualOverrideAsync(_testIntersectionId, Direction.NORTH, LightState.GREEN);

        Assert.Equal(LightState.GREEN, light.CurrentState);
    }

    [Fact]
    public async Task ManualOverride_LightNotFound_ShouldThrowException()
    {
        _lightRepoMock.Setup(r => r.GetByIntersectionIdAsync(_testIntersectionId))
            .ReturnsAsync(Enumerable.Empty<TrafficLight>());

        await Assert.ThrowsAsync<Exception>(
            () => _service.ManualOverrideAsync(_testIntersectionId, Direction.NORTH, LightState.RED));
    }

    // ===================== GetDashboardDataAsync Tests =====================

    [Fact]
    public async Task GetDashboardData_ShouldReturnCorrectDto()
    {
        // Arrange
        var intersection = new Intersection
        {
            Id = _testIntersectionId,
            Name = "Test Intersection",
            Location = "Test Location"
        };

        var lights = new List<TrafficLight>
        {
            new TrafficLight
            {
                IntersectionId = _testIntersectionId,
                Direction = Direction.NORTH,
                CurrentState = LightState.GREEN
            }
        };

        var trafficData = new List<TrafficData>
        {
            new TrafficData
            {
                IntersectionId = _testIntersectionId,
                Direction = Direction.NORTH,
                VehicleCount = 25,
                Timestamp = DateTime.UtcNow
            }
        };

        _intersectionRepoMock.Setup(r => r.GetByIdAsync(_testIntersectionId))
            .ReturnsAsync(intersection);
        _lightRepoMock.Setup(r => r.GetByIntersectionIdAsync(_testIntersectionId))
            .ReturnsAsync(lights);
        _dataRepoMock.Setup(r => r.GetRecentDataAsync(_testIntersectionId, 5))
            .ReturnsAsync(trafficData);

        // Act
        var result = await _service.GetDashboardDataAsync(_testIntersectionId);

        // Assert
        Assert.Equal(_testIntersectionId, result.IntersectionId);
        Assert.Equal("Test Intersection", result.IntersectionName);
        Assert.Equal(LightState.GREEN, result.CurrentLightState);
        Assert.Equal(25, result.CurrentVehicleCount);
    }

    [Fact]
    public async Task GetDashboardData_IntersectionNotFound_ShouldThrowException()
    {
        _intersectionRepoMock.Setup(r => r.GetByIdAsync(_testIntersectionId))
            .ReturnsAsync((Intersection?)null);

        await Assert.ThrowsAsync<Exception>(
            () => _service.GetDashboardDataAsync(_testIntersectionId));
    }

    [Fact]
    public async Task GetDashboardData_NoTrafficData_ShouldReturnZeroCount()
    {
        var intersection = new Intersection
        {
            Id = _testIntersectionId,
            Name = "Empty Intersection"
        };

        _intersectionRepoMock.Setup(r => r.GetByIdAsync(_testIntersectionId))
            .ReturnsAsync(intersection);
        _lightRepoMock.Setup(r => r.GetByIntersectionIdAsync(_testIntersectionId))
            .ReturnsAsync(new List<TrafficLight>());
        _dataRepoMock.Setup(r => r.GetRecentDataAsync(_testIntersectionId, 5))
            .ReturnsAsync(new List<TrafficData>());

        var result = await _service.GetDashboardDataAsync(_testIntersectionId);

        Assert.Equal(0, result.CurrentVehicleCount);
        Assert.Equal(LightState.RED, result.CurrentLightState); // default fallback
    }

    // ===================== Helpers =====================

    private TrafficLight CreateTestLight(LightState state)
    {
        return new TrafficLight
        {
            IntersectionId = _testIntersectionId,
            Direction = Direction.NORTH,
            CurrentState = state,
            CurrentTiming = new TimingConfig(30, 3, 20)
        };
    }

    private void SetupLightRepo(TrafficLight light)
    {
        _lightRepoMock.Setup(r => r.GetByIntersectionIdAsync(_testIntersectionId))
            .ReturnsAsync(new List<TrafficLight> { light });
    }
}
