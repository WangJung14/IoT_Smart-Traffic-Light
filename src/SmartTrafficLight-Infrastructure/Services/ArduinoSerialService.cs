using System;
using System.IO.Ports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using SmartTrafficLight.Application.Interfaces;

namespace SmartTrafficLight.Infrastructure.Services;

public class ArduinoSerialService : IArduinoSerialService, IDisposable
{
    private readonly SerialPort _serialPort;
    private readonly ILogger<ArduinoSerialService> _logger;
    private string _latestStatus = "N/A";

    public ArduinoSerialService(ILogger<ArduinoSerialService> logger, IConfiguration config)
    {
        _logger = logger;
        
        string portName = config["Arduino:Port"] ?? "COM2";
        int baudRate = config.GetValue<int>("Arduino:BaudRate", 9600);

        _serialPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);
        _serialPort.DataReceived += SerialPort_DataReceived;
        
        try
        {
            _serialPort.Open();
            _logger.LogInformation($"Opened {portName} successfully for Arduino communication.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Could not open {portName}: {ex.Message}");
        }
    }

    private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        try
        {
            string data = _serialPort.ReadLine();
            if (!string.IsNullOrWhiteSpace(data))
            {
                _latestStatus = data.Trim();
                _logger.LogDebug($"[Arduino] {_latestStatus}");
            }
        }
        catch (Exception ex)
        {
            // Ignore timeout exceptions on read
        }
    }

    public void SendTimingUpdate(int nsGreenDuration, int ewGreenDuration)
    {
        if (_serialPort.IsOpen)
        {
            string command = $"T:{nsGreenDuration},{ewGreenDuration}\n";
            _serialPort.Write(command);
            _logger.LogInformation($"Sent timing update to Arduino: {command.Trim()}");
        }
        else
        {
            _logger.LogWarning("Cannot send timing update, Serial port is closed.");
        }
    }

    public void SendReset()
    {
        if (_serialPort.IsOpen)
        {
            _serialPort.Write("R\n");
            _logger.LogInformation("Sent Reset command to Arduino.");
        }
    }

    public string GetLatestStatus()
    {
        return _latestStatus;
    }

    public void Dispose()
    {
        if (_serialPort != null)
        {
            if (_serialPort.IsOpen)
                _serialPort.Close();
            _serialPort.Dispose();
        }
    }
}
