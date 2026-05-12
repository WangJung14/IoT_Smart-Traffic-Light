namespace SmartTrafficLight.Application.DTOs;

/// <summary>
/// Vehicle counts broken down by type for PCU calculation.
/// Used as input for the Webster timing algorithm.
/// </summary>
public class VehicleCounts
{
    public int Car { get; set; }
    public int Motorbike { get; set; }
    public int Bus { get; set; }
    public int Truck { get; set; }

    /// <summary>Total raw vehicle count (unweighted)</summary>
    public int Total => Car + Motorbike + Bus + Truck;
}
