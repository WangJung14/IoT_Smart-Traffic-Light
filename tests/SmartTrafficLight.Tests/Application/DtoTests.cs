using SmartTrafficLight.Application.DTOs;
using SmartTrafficLight.Domain.Enums;
using SmartTrafficLight_Application.DTOs;

namespace SmartTrafficLight.Tests.Application;

/// <summary>
/// Unit tests cho DTOs và Payloads.
/// Kiểm tra khởi tạo, mapping, và factory methods.
/// </summary>
public class DtoTests
{
    // ===================== ApiResponse Tests =====================

    [Fact]
    public void ApiResponse_Ok_ShouldSetSuccessAndData()
    {
        var response = ApiResponse<string>.Ok("test data", "All good");

        Assert.True(response.Success);
        Assert.Equal("All good", response.Message);
        Assert.Equal("test data", response.Data);
    }

    [Fact]
    public void ApiResponse_Ok_DefaultMessage_ShouldBeSuccess()
    {
        var response = ApiResponse<int>.Ok(42);

        Assert.True(response.Success);
        Assert.Equal("Success", response.Message);
        Assert.Equal(42, response.Data);
    }

    [Fact]
    public void ApiResponse_Fail_ShouldSetFailureAndNullData()
    {
        var response = ApiResponse<string>.Fail("Something went wrong");

        Assert.False(response.Success);
        Assert.Equal("Something went wrong", response.Message);
        Assert.Null(response.Data);
    }

    // ===================== SignalR Payload Tests =====================

    [Fact]
    public void TrafficUpdatePayload_ShouldStoreValues()
    {
        var payload = new TrafficUpdatePayload(Direction.NORTH, 42);

        Assert.Equal(Direction.NORTH, payload.Direction);
        Assert.Equal(42, payload.VehicleCount);
    }

    [Fact]
    public void LightStatePayload_ShouldStoreValues()
    {
        var id = Guid.NewGuid();
        var payload = new LightStatePayload(id, Direction.NORTH, LightState.GREEN);

        Assert.Equal(id, payload.IntersectionId);
        Assert.Equal(Direction.NORTH, payload.Direction);
        Assert.Equal(LightState.GREEN, payload.CurrentLightState);
    }

    [Fact]
    public void TrafficUpdatePayload_RecordEquality_ShouldWork()
    {
        var p1 = new TrafficUpdatePayload(Direction.EAST, 10);
        var p2 = new TrafficUpdatePayload(Direction.EAST, 10);

        Assert.Equal(p1, p2);
    }

    // ===================== TrafficHistoryDto Tests =====================

    [Fact]
    public void TrafficHistoryDto_ShouldStoreValues()
    {
        var id = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;
        var dto = new TrafficHistoryDto(id, Direction.WEST, 15, timestamp);

        Assert.Equal(id, dto.IntersectionId);
        Assert.Equal(Direction.WEST, dto.Direction);
        Assert.Equal(15, dto.VehicleCount);
        Assert.Equal(timestamp, dto.Timestamp);
    }

    // ===================== DashboardDataDto Tests =====================

    [Fact]
    public void DashboardDataDto_DefaultValues_ShouldBeCorrect()
    {
        var dto = new DashboardDataDto();

        Assert.Equal(Guid.Empty, dto.IntersectionId);
        Assert.Equal(string.Empty, dto.IntersectionName);
        Assert.Equal(LightState.RED, dto.CurrentLightState); // enum default = 0 = RED
        Assert.Equal(0, dto.CurrentVehicleCount);
        Assert.Equal(0, dto.RemainingSeconds);
        Assert.Empty(dto.RecentTraffic);
    }
}
