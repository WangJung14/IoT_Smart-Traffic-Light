using SmartTrafficLight.Domain.Enums;
using SmartTrafficLight_Domain.Entities;
using SmartTrafficLight_Domain.ValueObjects;

namespace SmartTrafficLight.Tests.Domain;

/// <summary>
/// Unit tests cho các Entity của Domain Layer.
/// Kiểm tra khởi tạo mặc định và navigation property.
/// </summary>
public class EntityTests
{
    // ===================== Intersection Tests =====================

    [Fact]
    public void Intersection_NewInstance_ShouldHaveDefaultValues()
    {
        var intersection = new Intersection();

        Assert.NotEqual(Guid.Empty, intersection.Id);
        Assert.Equal(string.Empty, intersection.Name);
        Assert.Equal(string.Empty, intersection.Location);
        Assert.Equal(0, intersection.NumberOfLanes);
        Assert.Empty(intersection.TrafficLights);
        Assert.Empty(intersection.TrafficDatas);
    }

    [Fact]
    public void Intersection_SetProperties_ShouldRetainValues()
    {
        var id = Guid.NewGuid();
        var intersection = new Intersection
        {
            Id = id,
            Name = "Ngã tư Bình Triệu",
            Location = "Q.Thủ Đức",
            NumberOfLanes = 4
        };

        Assert.Equal(id, intersection.Id);
        Assert.Equal("Ngã tư Bình Triệu", intersection.Name);
        Assert.Equal("Q.Thủ Đức", intersection.Location);
        Assert.Equal(4, intersection.NumberOfLanes);
    }

    // ===================== TrafficData Tests =====================

    [Fact]
    public void TrafficData_NewInstance_ShouldHaveDefaultValues()
    {
        var data = new TrafficData();

        Assert.NotEqual(Guid.Empty, data.Id);
        Assert.Equal(0, data.VehicleCount);
        Assert.True(data.Timestamp <= DateTime.UtcNow);
    }

    [Fact]
    public void TrafficData_SetProperties_ShouldRetainValues()
    {
        var intersectionId = Guid.NewGuid();
        var data = new TrafficData
        {
            IntersectionId = intersectionId,
            Direction = Direction.NORTH,
            VehicleCount = 15,
            Timestamp = new DateTime(2026, 5, 6, 10, 0, 0, DateTimeKind.Utc)
        };

        Assert.Equal(intersectionId, data.IntersectionId);
        Assert.Equal(Direction.NORTH, data.Direction);
        Assert.Equal(15, data.VehicleCount);
    }

    // ===================== TrafficLight Tests =====================

    [Fact]
    public void TrafficLight_NewInstance_ShouldHaveDefaultTiming()
    {
        var light = new TrafficLight();

        Assert.NotEqual(Guid.Empty, light.Id);
        Assert.Equal(30, light.CurrentTiming.GreenDuration);
        Assert.Equal(3, light.CurrentTiming.YellowDuration);
        Assert.Equal(20, light.CurrentTiming.RedDuration);
    }

    [Fact]
    public void TrafficLight_SetState_ShouldRetainValue()
    {
        var light = new TrafficLight
        {
            CurrentState = LightState.GREEN
        };

        Assert.Equal(LightState.GREEN, light.CurrentState);
    }

    // ===================== Enum Tests =====================

    [Fact]
    public void Direction_ShouldHaveCorrectValues()
    {
        Assert.Equal(0, (int)Direction.NORTH);
        Assert.Equal(1, (int)Direction.SOUTH);
        Assert.Equal(2, (int)Direction.EAST);
        Assert.Equal(3, (int)Direction.WEST);
    }

    [Fact]
    public void LightState_ShouldHaveCorrectValues()
    {
        Assert.Equal(0, (int)LightState.RED);
        Assert.Equal(1, (int)LightState.YELLOW);
        Assert.Equal(2, (int)LightState.GREEN);
    }
}
