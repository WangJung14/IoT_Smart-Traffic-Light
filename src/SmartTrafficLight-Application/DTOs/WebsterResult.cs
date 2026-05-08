namespace SmartTrafficLight.Application.DTOs;

/// <summary>
/// Output of the Webster timing algorithm.
/// Contains optimal cycle time and green time allocations for each phase.
/// </summary>
public class WebsterResult
{
    /// <summary>Optimal cycle length in seconds (clamped 40–120)</summary>
    public int CycleTime { get; set; }

    /// <summary>Green duration for North–South phase (seconds)</summary>
    public int GreenNS { get; set; }

    /// <summary>Green duration for East–West phase (seconds)</summary>
    public int GreenEW { get; set; }

    /// <summary>Yellow duration (seconds)</summary>
    public int YellowDuration { get; set; }

    /// <summary>All-red clearance duration (seconds)</summary>
    public int RedClearance { get; set; }

    /// <summary>Total flow ratio Y = y_NS + y_EW (overloaded if ≥ 1.0)</summary>
    public double TotalFlowRatio { get; set; }

    /// <summary>PCU/hour for North–South direction</summary>
    public double PcuNS { get; set; }

    /// <summary>PCU/hour for East–West direction</summary>
    public double PcuEW { get; set; }

    /// <summary>Total lost time L (seconds)</summary>
    public double LostTime { get; set; }

    /// <summary>Whether the intersection is in overload state (Y ≥ 1.0)</summary>
    public bool IsOverloaded => TotalFlowRatio >= 1.0;

    /// <summary>Human-readable status</summary>
    public string Status => IsOverloaded ? "OVERLOADED" : "NORMAL";
}
