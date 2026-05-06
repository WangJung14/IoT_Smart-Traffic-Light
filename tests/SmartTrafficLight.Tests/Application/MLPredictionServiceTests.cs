using Microsoft.Extensions.Logging;
using Moq;
using SmartTrafficLight.Application.Services;
using SmartTrafficLight_Domain.ValueObjects;

namespace SmartTrafficLight.Tests.Application;

/// <summary>
/// Unit tests cho MLPredictionService.
/// Kiểm tra logic dự đoán thời gian đèn dựa trên số lượng xe.
/// </summary>
public class MLPredictionServiceTests
{
    private readonly MLPredictionService _service;
    private readonly Mock<ILogger<MLPredictionService>> _loggerMock;

    public MLPredictionServiceTests()
    {
        _loggerMock = new Mock<ILogger<MLPredictionService>>();
        _service = new MLPredictionService(_loggerMock.Object);
    }

    [Fact]
    public async Task PredictTimingAsync_HeavyTraffic_ShouldReturnLongGreenDuration()
    {
        // Arrange - Heavy traffic (> 30 vehicles)
        int vehicleCount = 50;

        // Act
        var result = await _service.PredictTimingAsync(vehicleCount, DateTime.UtcNow);

        // Assert
        Assert.Equal(60, result.GreenDuration);
        Assert.Equal(3, result.YellowDuration);
        Assert.Equal(30, result.RedDuration);
    }

    [Fact]
    public async Task PredictTimingAsync_MediumTraffic_ShouldReturnMediumGreenDuration()
    {
        // Arrange - Medium traffic (21-30 vehicles)
        int vehicleCount = 25;

        // Act
        var result = await _service.PredictTimingAsync(vehicleCount, DateTime.UtcNow);

        // Assert
        Assert.Equal(40, result.GreenDuration);
        Assert.Equal(3, result.YellowDuration);
        Assert.Equal(30, result.RedDuration);
    }

    [Fact]
    public async Task PredictTimingAsync_LightTraffic_ShouldReturnShortGreenDuration()
    {
        // Arrange - Light traffic (<= 20 vehicles)
        int vehicleCount = 10;

        // Act
        var result = await _service.PredictTimingAsync(vehicleCount, DateTime.UtcNow);

        // Assert
        Assert.Equal(20, result.GreenDuration);
        Assert.Equal(3, result.YellowDuration);
        Assert.Equal(40, result.RedDuration);
    }

    [Fact]
    public async Task PredictTimingAsync_ZeroVehicles_ShouldReturnLightTrafficTiming()
    {
        // Arrange
        int vehicleCount = 0;

        // Act
        var result = await _service.PredictTimingAsync(vehicleCount, DateTime.UtcNow);

        // Assert
        Assert.Equal(20, result.GreenDuration);
        Assert.Equal(3, result.YellowDuration);
        Assert.Equal(40, result.RedDuration);
    }

    [Theory]
    [InlineData(31)]
    [InlineData(100)]
    [InlineData(999)]
    public async Task PredictTimingAsync_AboveThreshold30_ShouldAlwaysReturn60sGreen(int count)
    {
        var result = await _service.PredictTimingAsync(count, DateTime.UtcNow);
        Assert.Equal(60, result.GreenDuration);
    }

    [Theory]
    [InlineData(21)]
    [InlineData(25)]
    [InlineData(30)]
    public async Task PredictTimingAsync_Between21And30_ShouldReturn40sGreen(int count)
    {
        var result = await _service.PredictTimingAsync(count, DateTime.UtcNow);
        Assert.Equal(40, result.GreenDuration);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(20)]
    public async Task PredictTimingAsync_AtOrBelow20_ShouldReturn20sGreen(int count)
    {
        var result = await _service.PredictTimingAsync(count, DateTime.UtcNow);
        Assert.Equal(20, result.GreenDuration);
    }

    [Fact]
    public async Task PredictTimingAsync_ShouldReturnValidTimingConfig()
    {
        // Act
        var result = await _service.PredictTimingAsync(15, DateTime.UtcNow);

        // Assert - TimingConfig must have non-negative values
        Assert.True(result.GreenDuration >= 0);
        Assert.True(result.YellowDuration >= 0);
        Assert.True(result.RedDuration >= 0);
    }
}
