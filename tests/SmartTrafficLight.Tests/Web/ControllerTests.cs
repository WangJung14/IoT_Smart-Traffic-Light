using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartTrafficLight.Application.DTOs;
using SmartTrafficLight.Application.Interfaces;
using SmartTrafficLight.Domain.Enums;
using SmartTrafficLight_Application.DTOs;
using SmartTrafficLight_Web.Controllers;

namespace SmartTrafficLight.Tests.Web;

/// <summary>
/// Unit tests cho các API Controllers.
/// Kiểm tra HTTP response và delegate đúng đến service layer.
/// </summary>
public class ControllerTests
{
    private readonly Guid _testIntersectionId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    // ===================== TrafficController Tests =====================

    [Fact]
    public async Task TrafficController_SaveDetection_ShouldReturnOk()
    {
        // Arrange
        var serviceMock = new Mock<ITrafficDetectionService>();
        var controller = new TrafficController(serviceMock.Object);
        var request = new TrafficController.TrafficDetectionRequest(
            _testIntersectionId, Direction.NORTH, 15);

        // Act
        var result = await controller.SaveDetectionData(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode ?? 200);
        serviceMock.Verify(s => s.SaveDetectionDataAsync(
            _testIntersectionId, Direction.NORTH, 15), Times.Once);
    }

    [Fact]
    public async Task TrafficController_GetCurrentTraffic_ShouldReturnOk()
    {
        var serviceMock = new Mock<ITrafficDetectionService>();
        serviceMock.Setup(s => s.GetCurrentTrafficAsync(_testIntersectionId, Direction.NORTH))
            .ReturnsAsync(25);

        var controller = new TrafficController(serviceMock.Object);

        var result = await controller.GetCurrentTraffic(_testIntersectionId, Direction.NORTH);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode ?? 200);
    }

    [Fact]
    public async Task TrafficController_GetHistory_ShouldReturnOk()
    {
        var serviceMock = new Mock<ITrafficDetectionService>();
        serviceMock.Setup(s => s.GetTrafficHistoryAsync(_testIntersectionId, 30))
            .ReturnsAsync(new List<TrafficHistoryDto>());

        var controller = new TrafficController(serviceMock.Object);

        var result = await controller.GetTrafficHistory(_testIntersectionId, 30);

        Assert.IsType<OkObjectResult>(result);
    }

    // ===================== LightController Tests =====================

    [Fact]
    public async Task LightController_OverrideLight_ShouldReturnOk()
    {
        var serviceMock = new Mock<ILightControlService>();
        var controller = new LightController(serviceMock.Object);
        var request = new LightController.LightOverrideRequest(
            _testIntersectionId, Direction.NORTH, LightState.RED);

        var result = await controller.OverrideLight(request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode ?? 200);
        serviceMock.Verify(s => s.ManualOverrideAsync(
            _testIntersectionId, Direction.NORTH, LightState.RED), Times.Once);
    }

    // ===================== AdminController Tests =====================

    [Fact]
    public async Task AdminController_GetDashboard_ShouldReturnOk()
    {
        var serviceMock = new Mock<ILightControlService>();
        var dashboardData = new DashboardDataDto
        {
            IntersectionId = _testIntersectionId,
            IntersectionName = "Test",
            CurrentLightState = LightState.GREEN,
            CurrentVehicleCount = 20
        };
        serviceMock.Setup(s => s.GetDashboardDataAsync(_testIntersectionId))
            .ReturnsAsync(dashboardData);

        var controller = new AdminController(serviceMock.Object);

        var result = await controller.GetDashboardData(_testIntersectionId);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task AdminController_GetDashboard_NotFound_ShouldReturnNotFound()
    {
        var serviceMock = new Mock<ILightControlService>();
        serviceMock.Setup(s => s.GetDashboardDataAsync(_testIntersectionId))
            .ThrowsAsync(new Exception("Intersection not found."));

        var controller = new AdminController(serviceMock.Object);

        var result = await controller.GetDashboardData(_testIntersectionId);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ===================== PredictionController Tests =====================

    [Fact]
    public async Task PredictionController_GetTiming_ShouldReturnOk()
    {
        var serviceMock = new Mock<IMLPredictionService>();
        serviceMock.Setup(s => s.PredictTimingAsync(45, It.IsAny<DateTime>()))
            .ReturnsAsync(new SmartTrafficLight_Domain.ValueObjects.TimingConfig(60, 3, 30));

        var controller = new PredictionController(serviceMock.Object);

        var result = await controller.GetPredictedTiming(45, null);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task PredictionController_GetTiming_WithTimeOfDay_ShouldPassCorrectTime()
    {
        var serviceMock = new Mock<IMLPredictionService>();
        var specificTime = new DateTime(2026, 5, 6, 8, 0, 0, DateTimeKind.Utc);
        serviceMock.Setup(s => s.PredictTimingAsync(10, specificTime))
            .ReturnsAsync(new SmartTrafficLight_Domain.ValueObjects.TimingConfig(20, 3, 40));

        var controller = new PredictionController(serviceMock.Object);

        var result = await controller.GetPredictedTiming(10, specificTime);

        Assert.IsType<OkObjectResult>(result);
        serviceMock.Verify(s => s.PredictTimingAsync(10, specificTime), Times.Once);
    }
}
