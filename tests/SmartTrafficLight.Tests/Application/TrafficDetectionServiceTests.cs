using Microsoft.Extensions.Logging;
using Moq;
using SmartTrafficLight.Application.DTOs;
using SmartTrafficLight.Domain.Enums;
using SmartTrafficLight_Application.Services;
using SmartTrafficLight_Domain.Entities;
using SmartTrafficLight_Domain.Interfaces;

namespace SmartTrafficLight.Tests.Application;

/// <summary>
/// Unit tests cho TrafficDetectionService.
/// Kiểm tra logic lưu dữ liệu YOLO, lấy lưu lượng hiện tại, và lịch sử.
/// </summary>
public class TrafficDetectionServiceTests
{
    private readonly Mock<ITrafficDataRepository> _repoMock;
    private readonly Mock<ILogger<TrafficDetectionService>> _loggerMock;
    private readonly TrafficDetectionService _service;

    private readonly Guid _testIntersectionId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public TrafficDetectionServiceTests()
    {
        _repoMock = new Mock<ITrafficDataRepository>();
        _loggerMock = new Mock<ILogger<TrafficDetectionService>>();
        _service = new TrafficDetectionService(_repoMock.Object, _loggerMock.Object);
    }

    // ===================== SaveDetectionDataAsync Tests =====================

    [Fact]
    public async Task SaveDetectionData_ValidData_ShouldCallRepository()
    {
        // Act
        await _service.SaveDetectionDataAsync(_testIntersectionId, Direction.NORTH, 15);

        // Assert
        _repoMock.Verify(r => r.AddAsync(It.Is<TrafficData>(
            d => d.IntersectionId == _testIntersectionId
                && d.Direction == Direction.NORTH
                && d.VehicleCount == 15
        )), Times.Once);
    }

    [Fact]
    public async Task SaveDetectionData_NegativeVehicleCount_ShouldNotSave()
    {
        // Arrange - YOLO đếm sai, trả về số âm
        await _service.SaveDetectionDataAsync(_testIntersectionId, Direction.NORTH, -5);

        // Assert - Repository không được gọi
        _repoMock.Verify(r => r.AddAsync(It.IsAny<TrafficData>()), Times.Never);
    }

    [Fact]
    public async Task SaveDetectionData_ZeroVehicles_ShouldSave()
    {
        await _service.SaveDetectionDataAsync(_testIntersectionId, Direction.SOUTH, 0);

        _repoMock.Verify(r => r.AddAsync(It.Is<TrafficData>(
            d => d.VehicleCount == 0 && d.Direction == Direction.SOUTH
        )), Times.Once);
    }

    // ===================== GetCurrentTrafficAsync Tests =====================

    [Fact]
    public async Task GetCurrentTraffic_WithRecentData_ShouldReturnLatestCount()
    {
        // Arrange
        var data = new List<TrafficData>
        {
            new TrafficData { Direction = Direction.NORTH, VehicleCount = 10, Timestamp = DateTime.UtcNow.AddMinutes(-1) },
            new TrafficData { Direction = Direction.NORTH, VehicleCount = 25, Timestamp = DateTime.UtcNow }, // newest
            new TrafficData { Direction = Direction.SOUTH, VehicleCount = 5, Timestamp = DateTime.UtcNow }
        };

        _repoMock.Setup(r => r.GetRecentDataAsync(_testIntersectionId, 2))
            .ReturnsAsync(data);

        // Act
        var result = await _service.GetCurrentTrafficAsync(_testIntersectionId, Direction.NORTH);

        // Assert - Should return latest NORTH count
        Assert.Equal(25, result);
    }

    [Fact]
    public async Task GetCurrentTraffic_NoData_ShouldReturnZero()
    {
        _repoMock.Setup(r => r.GetRecentDataAsync(_testIntersectionId, 2))
            .ReturnsAsync(new List<TrafficData>());

        var result = await _service.GetCurrentTrafficAsync(_testIntersectionId, Direction.NORTH);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task GetCurrentTraffic_NoMatchingDirection_ShouldReturnZero()
    {
        var data = new List<TrafficData>
        {
            new TrafficData { Direction = Direction.SOUTH, VehicleCount = 20, Timestamp = DateTime.UtcNow }
        };

        _repoMock.Setup(r => r.GetRecentDataAsync(_testIntersectionId, 2))
            .ReturnsAsync(data);

        // Ask for NORTH, but only SOUTH data exists
        var result = await _service.GetCurrentTrafficAsync(_testIntersectionId, Direction.NORTH);

        Assert.Equal(0, result);
    }

    // ===================== GetTrafficHistoryAsync Tests =====================

    [Fact]
    public async Task GetTrafficHistory_ShouldReturnMappedDtos()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var data = new List<TrafficData>
        {
            new TrafficData { IntersectionId = _testIntersectionId, Direction = Direction.NORTH, VehicleCount = 10, Timestamp = now },
            new TrafficData { IntersectionId = _testIntersectionId, Direction = Direction.SOUTH, VehicleCount = 5, Timestamp = now }
        };

        _repoMock.Setup(r => r.GetRecentDataAsync(_testIntersectionId, 30))
            .ReturnsAsync(data);

        // Act
        var result = (await _service.GetTrafficHistoryAsync(_testIntersectionId, 30)).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(10, result[0].VehicleCount);
        Assert.Equal(Direction.NORTH, result[0].Direction);
        Assert.Equal(5, result[1].VehicleCount);
    }

    [Fact]
    public async Task GetTrafficHistory_NoData_ShouldReturnEmptyList()
    {
        _repoMock.Setup(r => r.GetRecentDataAsync(_testIntersectionId, 30))
            .ReturnsAsync(new List<TrafficData>());

        var result = await _service.GetTrafficHistoryAsync(_testIntersectionId, 30);

        Assert.Empty(result);
    }
}
