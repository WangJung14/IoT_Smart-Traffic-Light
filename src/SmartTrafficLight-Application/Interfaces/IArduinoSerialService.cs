namespace SmartTrafficLight.Application.Interfaces;

public interface IArduinoSerialService
{
    void SendTimingUpdate(int nsGreenDuration, int ewGreenDuration);
    void SendReset();
    /// <summary>
    /// Force Arduino to a specific traffic phase.
    /// 0 = NS_GREEN/EW_RED, 1 = NS_YELLOW/EW_RED, 2 = NS_RED/EW_GREEN, 3 = NS_RED/EW_YELLOW
    /// </summary>
    void ForceState(int stateIndex);
    void SetInfiniteMode(bool enable);
    void RequestJump(int targetState);
    bool IsInfiniteMode { get; }
    string GetLatestStatus();
}
