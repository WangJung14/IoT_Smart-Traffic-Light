namespace SmartTrafficLight.Application.Interfaces;

public interface IArduinoSerialService
{
    void SendTimingUpdate(int nsGreenDuration, int ewGreenDuration);
    void SendReset();
    string GetLatestStatus();
}
