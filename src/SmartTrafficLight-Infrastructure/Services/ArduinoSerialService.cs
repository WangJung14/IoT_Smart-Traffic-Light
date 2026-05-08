using System;
using System.IO.Ports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartTrafficLight.Application.Interfaces;
using SmartTrafficLight.Application.DTOs;

namespace SmartTrafficLight.Infrastructure.Services;

public class ArduinoSerialService : IArduinoSerialService, IDisposable
{
    private readonly SerialPort _serialPort;
    private readonly ILogger<ArduinoSerialService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private string _latestStatus = "N/A";
    private System.Threading.Timer? _pollingTimer;

    public ArduinoSerialService(ILogger<ArduinoSerialService> logger, IConfiguration config, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        
        string portName = config["Arduino:Port"] ?? "COM2";
        
        int baudRate = 9600;
        if (int.TryParse(config["Arduino:BaudRate"], out int parsedBaud))
        {
            baudRate = parsedBaud;
        }

        _serialPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);
        _serialPort.DataReceived += SerialPort_DataReceived;
        
        try
        {
            _serialPort.Open();
            _logger.LogInformation($"Opened {portName} successfully for Arduino communication.");
            Console.WriteLine($"[SYSTEM] Opened {portName} successfully!");
            
            // Start polling every second
            _pollingTimer = new System.Threading.Timer(PollArduino, null, 1000, 1000);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Could not open {portName}: {ex.Message}");
            Console.WriteLine($"[SYSTEM] Could not open {portName}: {ex.Message}");
        }
    }

    private void PollArduino(object? state)
    {
        if (_serialPort != null && _serialPort.IsOpen)
        {
            try
            {
                _serialPort.Write("S\n");
            }
            catch (Exception)
            {
                // ignore
            }
        }
    }

    private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        try
        {
            while (_serialPort.IsOpen && _serialPort.BytesToRead > 0)
            {
                string data = _serialPort.ReadLine();
                if (!string.IsNullOrWhiteSpace(data))
                {
                    _latestStatus = data.Trim();
                    
                    Console.WriteLine($"[COM2 RECV] {_latestStatus}");
                    
                    // Fire and forget push to SignalR
                    _ = PushStatusToClientsAsync(_latestStatus);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[COM2 ERROR] {ex.Message}");
        }
    }

    private async Task PushStatusToClientsAsync(string status)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<ITrafficNotificationService>();
            await notificationService.SendHardwareStatusAsync(new HardwareStatusPayload(status));
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to push hardware status to SignalR: {ex.Message}");
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

    public void ForceState(int stateIndex)
    {
        if (_serialPort.IsOpen)
        {
            string command = $"F:{stateIndex}\n";
            _serialPort.Write(command);
            _logger.LogInformation($"ADMIN: Sent force state command to Arduino: F:{stateIndex}");
            Console.WriteLine($"[ADMIN CMD] Force state -> F:{stateIndex}");
        }
        else
        {
            _logger.LogWarning("Cannot force state, Serial port is closed.");
        }
    }

    public void Dispose()
    {
        _pollingTimer?.Dispose();
        if (_serialPort != null)
        {
            if (_serialPort.IsOpen)
                _serialPort.Close();
            _serialPort.Dispose();
        }
    }
}
