using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartTrafficLight.Application.DTOs;
using SmartTrafficLight.Application.Interfaces;

namespace SmartTrafficLight.Application.Services;

/// <summary>
/// Implements the Webster method for calculating optimal traffic signal timing.
/// 
/// Pipeline:
///   YOLO vehicle counts → PCU conversion → Flow ratios → Webster formula → Green allocation
///   
/// Reference: logic_traffic_light.md
/// </summary>
public class WebsterTimingService : IWebsterTimingService
{
    private readonly ILogger<WebsterTimingService> _logger;

    // ==================== Configurable Parameters ====================
    
    /// <summary>PCU factor for motorbikes (default: 0.35)</summary>
    private readonly double _pcuMotorbike;
    
    /// <summary>PCU factor for trucks/buses (default: 1.75)</summary>
    private readonly double _pcuTruckBus;
    
    /// <summary>Saturation flow rate in PCU/hour/lane (default: 1850)</summary>
    private readonly double _saturationFlow;
    
    /// <summary>Startup loss time per phase in seconds (default: 3)</summary>
    private readonly double _startupLossPerPhase;
    
    /// <summary>All-red clearance time in seconds (default: 2)</summary>
    private readonly int _allRedClearance;
    
    /// <summary>Yellow duration in seconds (default: 4)</summary>
    private readonly int _yellowDuration;
    
    /// <summary>Minimum cycle time in seconds (default: 40)</summary>
    private readonly int _minCycle;
    
    /// <summary>Maximum cycle time in seconds (default: 120)</summary>
    private readonly int _maxCycle;
    
    /// <summary>Minimum green time per phase in seconds (default: 10)</summary>
    private readonly int _minGreen;
    
    /// <summary>Number of phases (fixed at 2 for N-S / E-W intersection)</summary>
    private const int NUM_PHASES = 2;

    // ==================== Anti-Hysteresis (Moving Average) ====================
    
    /// <summary>Buffer for smoothing PCU values over time</summary>
    private readonly Queue<(double ns, double ew)> _pcuHistory = new();
    
    /// <summary>Max number of samples to keep for moving average</summary>
    private readonly int _smoothingWindow;
    
    /// <summary>Last computed result</summary>
    private WebsterResult? _lastResult;
    
    private readonly object _lock = new();

    // ==================== Constructor ====================
    
    public WebsterTimingService(ILogger<WebsterTimingService> logger, IConfiguration config)
    {
        _logger = logger;

        // Read parameters from appsettings.json section "Webster", with defaults
        var section = config.GetSection("Webster");
        _pcuMotorbike       = section.GetValue("PcuMotorbike", 0.35);
        _pcuTruckBus        = section.GetValue("PcuTruckBus", 1.75);
        _saturationFlow     = section.GetValue("SaturationFlow", 1850.0);
        _startupLossPerPhase = section.GetValue("StartupLossPerPhase", 3.0);
        _allRedClearance    = section.GetValue("AllRedClearance", 2);
        _yellowDuration     = section.GetValue("YellowDuration", 4);
        _minCycle           = section.GetValue("MinCycle", 40);
        _maxCycle           = section.GetValue("MaxCycle", 120);
        _minGreen           = section.GetValue("MinGreen", 10);
        _smoothingWindow    = section.GetValue("SmoothingWindow", 10);

        _logger.LogInformation(
            "Webster initialized: s={Saturation}, PCU_moto={PcuMoto}, PCU_truck={PcuTruck}, Yellow={Yellow}s, MinCycle={Min}s, MaxCycle={Max}s",
            _saturationFlow, _pcuMotorbike, _pcuTruckBus, _yellowDuration, _minCycle, _maxCycle);
    }

    // ==================== Main Calculation ====================
    
    public WebsterResult Calculate(VehicleCounts nsVehicles, VehicleCounts ewVehicles)
    {
        lock (_lock)
        {
            // --- Step 1: PCU Conversion ---
            double q_NS = ConvertToPCU(nsVehicles);
            double q_EW = ConvertToPCU(ewVehicles);

            _logger.LogInformation("Webster Step 1 (PCU): NS={PcuNs:F1} PCU/h, EW={PcuEw:F1} PCU/h", q_NS, q_EW);

            // --- Anti-Hysteresis: Moving Average ---
            _pcuHistory.Enqueue((q_NS, q_EW));
            while (_pcuHistory.Count > _smoothingWindow)
                _pcuHistory.Dequeue();

            double avg_NS = _pcuHistory.Average(x => x.ns);
            double avg_EW = _pcuHistory.Average(x => x.ew);

            _logger.LogInformation("Webster Smoothed: NS={AvgNs:F1}, EW={AvgEw:F1} (window={Window})",
                avg_NS, avg_EW, _pcuHistory.Count);

            // --- Step 2: Flow Ratios ---
            double y_NS = avg_NS / _saturationFlow;
            double y_EW = avg_EW / _saturationFlow;
            double Y = y_NS + y_EW;

            _logger.LogInformation("Webster Step 2 (Flow Ratio): y_NS={YNs:F3}, y_EW={YEw:F3}, Y={Y:F3}", y_NS, y_EW, Y);

            // --- Step 3: Total Lost Time ---
            double L = (NUM_PHASES * _startupLossPerPhase) + (NUM_PHASES * _yellowDuration) + _allRedClearance;

            _logger.LogInformation("Webster Step 3 (Lost Time): L={L:F1}s", L);

            // --- Step 4: Optimal Cycle (Webster Formula) ---
            double co;
            if (Y >= 1.0)
            {
                co = _maxCycle;
                _logger.LogWarning("Webster Step 4: OVERLOADED (Y≥1.0) → using Max Cycle = {Max}s", _maxCycle);
            }
            else
            {
                co = (1.5 * L + 5.0) / (1.0 - Y);
                co = Math.Clamp(co, _minCycle, _maxCycle);
                _logger.LogInformation("Webster Step 4 (Cycle): Co={Co:F1}s (raw Webster)", co);
            }

            int Co = (int)Math.Round(co);

            // --- Step 5: Allocate Green Time ---
            double totalGreen = Co - L;
            if (totalGreen < 2 * _minGreen)
            {
                totalGreen = 2 * _minGreen;
                Co = (int)(totalGreen + L);
                _logger.LogWarning("Webster Step 5: totalGreen too small, adjusted Co={Co}s", Co);
            }

            int g_NS, g_EW;
            if (Y > 0)
            {
                g_NS = (int)Math.Round((y_NS / Y) * totalGreen);
                g_EW = (int)(totalGreen - g_NS); // remainder to avoid rounding drift
            }
            else
            {
                // No vehicles detected → split evenly
                g_NS = (int)(totalGreen / 2);
                g_EW = (int)(totalGreen - g_NS);
            }

            // Enforce minimum green
            g_NS = Math.Max(g_NS, _minGreen);
            g_EW = Math.Max(g_EW, _minGreen);

            _logger.LogInformation("Webster Step 5 (Green): g_NS={GNs}s, g_EW={GEw}s", g_NS, g_EW);

            // --- Step 6: Build Result ---
            // Verification: total = g_NS + yellow + clearance + g_EW + yellow + clearance
            int verifyTotal = g_NS + _yellowDuration + _allRedClearance + g_EW + _yellowDuration + _allRedClearance;
            _logger.LogInformation(
                "Webster Step 6 (Verify): Co={Co}s, Σ(g+y+r)={Verify}s, match={Match}",
                Co, verifyTotal, verifyTotal == Co ? "✓" : "✗ (rounding)");

            var result = new WebsterResult
            {
                CycleTime = Co,
                GreenNS = g_NS,
                GreenEW = g_EW,
                YellowDuration = _yellowDuration,
                RedClearance = _allRedClearance,
                TotalFlowRatio = Math.Round(Y, 4),
                PcuNS = Math.Round(avg_NS, 1),
                PcuEW = Math.Round(avg_EW, 1),
                LostTime = L,
            };

            _lastResult = result;
            return result;
        }
    }

    public WebsterResult? GetLastResult()
    {
        lock (_lock)
        {
            return _lastResult;
        }
    }

    // ==================== Step 1 Helper: PCU Conversion ====================
    
    /// <summary>
    /// Convert raw vehicle counts to PCU/hour.
    /// Assumes the input counts represent vehicles detected in a single observation window.
    /// We multiply by a factor to estimate hourly flow (e.g., if YOLO sends every 5s → ×720).
    /// For simplicity, we treat input counts as representative instantaneous density
    /// and scale them to approximate hourly flow.
    /// </summary>
    private double ConvertToPCU(VehicleCounts counts)
    {
        double pcu = (counts.Car * 1.0)
                   + (counts.Motorbike * _pcuMotorbike)
                   + ((counts.Truck + counts.Bus) * _pcuTruckBus);

        // Scale from instantaneous count to PCU/hour estimate
        // Assumption: YOLO observes ~5 seconds of traffic at a time
        // Vehicles visible in frame ≈ vehicles that would pass in ~10 seconds
        // So hourly rate ≈ count × 360 (3600/10)
        return pcu * 360.0;
    }
}
