using SmartTrafficLight.Domain.Enums;

namespace SmartTrafficLight.Application.DTOs;

public class TrafficUpdatePayload
{
    public Direction Direction { get; set; }
    public int VehicleCount { get; set; }
    
    public TrafficUpdatePayload() { }
    public TrafficUpdatePayload(Direction direction, int vehicleCount)
    {
        Direction = direction;
        VehicleCount = vehicleCount;
    }
}

public class LightStatePayload
{
    public Guid IntersectionId { get; set; }
    public Direction Direction { get; set; }
    public LightState CurrentLightState { get; set; }
    
    public LightStatePayload() { }
    public LightStatePayload(Guid intersectionId, Direction direction, LightState currentLightState)
    {
        IntersectionId = intersectionId;
        Direction = direction;
        CurrentLightState = currentLightState;
    }
}

public class HardwareStatusPayload
{
    public string StatusMessage { get; set; } = string.Empty;
    
    public HardwareStatusPayload() { }
    public HardwareStatusPayload(string statusMessage)
    {
        StatusMessage = statusMessage;
    }
}
