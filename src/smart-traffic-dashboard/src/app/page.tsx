"use client";

import { useEffect, useRef, useState } from "react";
import * as signalR from "@microsoft/signalr";
import { Activity, Camera, AlertTriangle, ShieldCheck, BarChart3, Wifi, WifiOff, Zap, RotateCcw } from "lucide-react";

const API_BASE = "http://localhost:5212/api/v1";

async function forceState(stateIndex: number): Promise<string> {
  const res = await fetch(`${API_BASE}/hardware/force-state`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ stateIndex }),
  });
  const data = await res.json();
  return data.message || "OK";
}

async function resetArduino(): Promise<string> {
  const res = await fetch(`${API_BASE}/hardware/reset`, { method: "POST" });
  const data = await res.json();
  return data.message || "OK";
}

type LightState = "RED" | "YELLOW" | "GREEN";
type Direction = "NORTH" | "SOUTH" | "EAST" | "WEST";

interface TrafficState {
  currentLightState: LightState;
  countdownSeconds: number;
  vehicleCount: number;
}

interface WebsterState {
  cycleTime: number;
  greenNS: number;
  greenEW: number;
  totalFlowRatio: number;
  status: string;
  pcuNS: number;
  pcuEW: number;
}

interface LogEntry {
  time: string;
  msg: string;
  type: "info" | "warn" | "success" | "error";
}

const DIRECTIONS: Direction[] = ["NORTH", "SOUTH", "EAST", "WEST"];
const DIR_SHORT: Record<Direction, string> = { NORTH: "N", SOUTH: "S", EAST: "E", WEST: "W" };
const DIR_VIET: Record<Direction, string> = { NORTH: "Bắc", SOUTH: "Nam", EAST: "Đông", WEST: "Tây" };

const DEFAULT_STATE: Record<Direction, TrafficState> = {
  NORTH: { currentLightState: "RED", countdownSeconds: 0, vehicleCount: 0 },
  SOUTH: { currentLightState: "RED", countdownSeconds: 0, vehicleCount: 0 },
  EAST: { currentLightState: "RED", countdownSeconds: 0, vehicleCount: 0 },
  WEST: { currentLightState: "RED", countdownSeconds: 0, vehicleCount: 0 },
};

const DEFAULT_WEBSTER: WebsterState = {
  cycleTime: 0,
  greenNS: 0,
  greenEW: 0,
  totalFlowRatio: 0,
  status: "WAITING",
  pcuNS: 0,
  pcuEW: 0,
};

function parseArduinoMessage(msg: string): { nsState: LightState; ewState: LightState; seconds: number } | null {
  if (!msg) return null;

  // Parse light states
  const nsState: LightState = msg.includes("B-N:XANH") ? "GREEN" : msg.includes("B-N:VANG") ? "YELLOW" : "RED";
  const ewState: LightState = msg.includes("D-T:XANH") ? "GREEN" : msg.includes("D-T:VANG") ? "YELLOW" : "RED";

  // Parse seconds - handle both "Con lai: 14s" AND "35s" (first message of new phase)
  let seconds: number | null = null;

  if (msg.includes("Con lai:")) {
    const parts = msg.split("Con lai:");
    if (parts.length > 1) {
      const secStr = parts[1].replace("s", "").trim();
      seconds = parseInt(secStr, 10);
    }
  } else {
    // First message of new phase: "[B-N:XANH D-T:DO] 35s"
    const match = msg.match(/\]\s*(\d+)s/);
    if (match) {
      seconds = parseInt(match[1], 10);
    }
  }

  if (seconds !== null && !isNaN(seconds)) {
    return { nsState, ewState, seconds };
  }
  return null;
}

export default function Dashboard() {
  const [states, setStates] = useState(DEFAULT_STATE);
  const [webster, setWebster] = useState<WebsterState>(DEFAULT_WEBSTER);
  const [connectionStatus, setConnectionStatus] = useState<"CONNECTED" | "DISCONNECTED" | "RECONNECTING" | "ERROR">("DISCONNECTED");
  const [logs, setLogs] = useState<LogEntry[]>([]);
  const [trafficHistory, setTrafficHistory] = useState<{ time: string; ns: string; ew: string; sec: number }[]>([]);
  const lastUpdateRef = useRef<string>("");

  const addLog = (msg: string, type: LogEntry["type"] = "info") => {
    setLogs((prev) => {
      const entry: LogEntry = { time: new Date().toLocaleTimeString("vi-VN"), msg, type };
      const newLogs = [entry, ...prev];
      return newLogs.slice(0, 30);
    });
  };

  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl("http://localhost:5212/hubs/traffic", {
        skipNegotiation: true,
        transport: signalR.HttpTransportType.WebSockets,
      })
      .withAutomaticReconnect([0, 1000, 2000, 5000])
      .build();

    connection.onreconnecting(() => {
      setConnectionStatus("RECONNECTING");
      addLog("Đang kết nối lại...", "warn");
    });
    connection.onreconnected(() => {
      setConnectionStatus("CONNECTED");
      addLog("Đã kết nối lại thành công!", "success");
    });
    connection.onclose(() => {
      setConnectionStatus("DISCONNECTED");
      addLog("Mất kết nối SignalR", "error");
    });

    connection.on("ReceiveWebsterUpdate", (payload: WebsterState) => {
      setWebster(payload);
      addLog(`AI Timing: Co=${payload.cycleTime}s (NS:${payload.greenNS}s, EW:${payload.greenEW}s)`, "info");
    });

    connection.on("ReceiveHardwareStatus", (payload: Record<string, unknown>) => {
      const msg = (payload?.statusMessage || payload?.StatusMessage) as string | undefined;
      if (!msg) return;

      const parsed = parseArduinoMessage(msg);
      if (!parsed) return;

      const { nsState, ewState, seconds } = parsed;

      // Deduplicate: only skip if state AND seconds are both identical
      const stateKey = `${nsState}-${ewState}-${seconds}`;
      if (stateKey === lastUpdateRef.current) return;
      lastUpdateRef.current = stateKey;

      setStates({
        NORTH: { currentLightState: nsState, countdownSeconds: seconds, vehicleCount: 0 },
        SOUTH: { currentLightState: nsState, countdownSeconds: seconds, vehicleCount: 0 },
        EAST: { currentLightState: ewState, countdownSeconds: seconds, vehicleCount: 0 },
        WEST: { currentLightState: ewState, countdownSeconds: seconds, vehicleCount: 0 },
      });

      // Add to traffic history
      setTrafficHistory((prev) => {
        const entry = {
          time: new Date().toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit", second: "2-digit" }),
          ns: nsState,
          ew: ewState,
          sec: seconds,
        };
        return [entry, ...prev].slice(0, 20);
      });
    });

    const startConnection = async () => {
      try {
        await connection.start();
        setConnectionStatus("CONNECTED");
        addLog("Kết nối SignalR thành công!", "success");
      } catch (err: unknown) {
        setConnectionStatus("ERROR");
        const message = err instanceof Error ? err.message : "Unknown error";
        addLog(`Lỗi kết nối: ${message}`, "error");
      }
    };

    startConnection();
    return () => { connection.stop(); };
  }, []);

  const getLightColor = (state: LightState): string => {
    switch (state) {
      case "RED": return "#ef4444";
      case "YELLOW": return "#eab308";
      case "GREEN": return "#22c55e";
    }
  };

  const handleForceState = async (stateIndex: number, label: string) => {
    try {
      addLog(`Admin: Đang gửi lệnh ${label}...`, "warn");
      const msg = await forceState(stateIndex);
      addLog(`Admin: ${msg}`, "success");
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : "Unknown error";
      addLog(`Admin: Lỗi gửi lệnh - ${message}`, "error");
    }
  };

  const handleReset = async () => {
    try {
      addLog("Admin: Đang gửi lệnh Reset...", "warn");
      const msg = await resetArduino();
      addLog(`Admin: ${msg}`, "success");
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : "Unknown error";
      addLog(`Admin: Lỗi Reset - ${message}`, "error");
    }
  };

  return (
    <div className="min-h-screen" style={{ background: "linear-gradient(135deg, #0c1222 0%, #0f172a 40%, #111827 100%)" }}>
      {/* ========== HEADER ========== */}
      <header className="border-b border-white/5 px-6 py-4">
        <div className="max-w-[1400px] mx-auto flex items-center justify-between">
          <div className="flex items-center gap-4">
            <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-cyan-500 to-blue-600 flex items-center justify-center shadow-lg shadow-cyan-500/20">
              <ShieldCheck size={22} className="text-white" />
            </div>
            <div>
              <h1 className="text-xl font-bold text-white tracking-wide">SMART TRAFFIC CONTROL</h1>
              <p className="text-xs text-slate-500">IoT Final Project — Real-time Monitoring</p>
            </div>
          </div>

          <div className="flex items-center gap-3">
            <button
              onClick={handleReset}
              className="flex items-center gap-1.5 px-3 py-2 rounded-full text-xs font-bold tracking-wider border border-amber-500/30 bg-amber-500/10 text-amber-400 hover:bg-amber-500/20 transition-all"
            >
              <RotateCcw size={13} />
              RESET
            </button>
            <div className={`flex items-center gap-2 px-4 py-2 rounded-full text-xs font-bold tracking-wider border
            ${connectionStatus === "CONNECTED"
              ? "bg-emerald-500/10 text-emerald-400 border-emerald-500/30"
              : connectionStatus === "RECONNECTING"
              ? "bg-amber-500/10 text-amber-400 border-amber-500/30"
              : "bg-red-500/10 text-red-400 border-red-500/30"
            }`}>
            {connectionStatus === "CONNECTED" ? <Wifi size={14} /> : <WifiOff size={14} />}
            {connectionStatus === "CONNECTED" ? "LIVE" : connectionStatus}
            {connectionStatus === "CONNECTED" && (
              <span className="relative flex h-2 w-2">
                <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75"></span>
                <span className="relative inline-flex rounded-full h-2 w-2 bg-emerald-500"></span>
              </span>
            )}
          </div>
          </div>
        </div>
      </header>

      {/* ========== MAIN CONTENT ========== */}
      <main className="max-w-[1400px] mx-auto px-6 py-6 flex flex-col gap-6">

        {/* ===== ROW 1: Traffic Lights + Camera ===== */}
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">

          {/* -- Traffic Lights Panel (Left) -- */}
          <div className="lg:col-span-7 bg-slate-900/50 border border-white/5 rounded-2xl overflow-hidden">
            <div className="px-5 py-3 border-b border-white/5 flex items-center gap-2">
              <Activity size={16} className="text-cyan-400" />
              <span className="text-sm font-semibold text-slate-200 tracking-wider">INTERSECTION STATUS</span>
            </div>

            <div className="p-5">
              <div className="grid grid-cols-4 gap-3">
                {DIRECTIONS.map((dir) => {
                  const state = states[dir];
                  const color = getLightColor(state.currentLightState);

                  return (
                    <div key={dir} className="flex flex-col items-center">
                      {/* Direction Label */}
                      <div className="text-xs font-bold text-slate-400 tracking-[0.2em] mb-3">{DIR_SHORT[dir]}</div>

                      {/* Traffic Light Housing */}
                      <div className="relative bg-gradient-to-b from-slate-800 to-slate-900 border border-slate-700/80 rounded-2xl p-3 flex flex-col items-center gap-3 shadow-[inset_0_2px_10px_rgba(0,0,0,0.6)]">
                        {/* Pole connector */}
                        <div className="absolute -top-2 left-1/2 -translate-x-1/2 w-4 h-2 bg-slate-700 rounded-t" />

                        {(["RED", "YELLOW", "GREEN"] as LightState[]).map((light) => {
                          const isActive = state.currentLightState === light;
                          const lightColor = getLightColor(light);
                          return (
                            <div
                              key={light}
                              className="w-10 h-10 rounded-full border-2 transition-all duration-200"
                              style={{
                                background: isActive
                                  ? `radial-gradient(circle at 35% 35%, ${lightColor}cc, ${lightColor})`
                                  : "rgba(15,23,42,0.8)",
                                borderColor: isActive ? `${lightColor}80` : "rgba(51,65,85,0.5)",
                                boxShadow: isActive
                                  ? `0 0 20px 6px ${lightColor}40, 0 0 40px 12px ${lightColor}20, inset 0 0 8px ${lightColor}60`
                                  : "inset 0 0 6px rgba(0,0,0,0.6)",
                                opacity: isActive ? 1 : 0.25,
                              }}
                            />
                          );
                        })}
                      </div>

                      {/* Timer */}
                      <div
                        className="mt-3 w-full text-center py-2 rounded-lg font-mono text-2xl font-black border"
                        style={{
                          background: "rgba(0,0,0,0.6)",
                          color,
                          borderColor: `${color}30`,
                          textShadow: `0 0 12px ${color}80`,
                        }}
                      >
                        {state.countdownSeconds.toString().padStart(2, "0")}
                      </div>

                      {/* Direction name */}
                      <div className="mt-2 text-[10px] text-slate-500 font-medium">{DIR_VIET[dir]}</div>

                      {/* Admin Override Buttons */}
                      {(dir === "NORTH" || dir === "EAST") && (
                        <div className="mt-3 flex gap-1 w-full">
                          {dir === "NORTH" ? (
                            <>
                              <button
                                onClick={() => handleForceState(0, "B-N → XANH")}
                                className="flex-1 text-[9px] font-bold py-1.5 rounded border transition-all hover:scale-105"
                                style={{
                                  borderColor: "#22c55e40",
                                  color: "#22c55e",
                                  background: "rgba(34,197,94,0.1)",
                                }}
                              >
                                B-N→XANH
                              </button>
                              <button
                                onClick={() => handleForceState(2, "B-N → ĐỎ")}
                                className="flex-1 text-[9px] font-bold py-1.5 rounded border transition-all hover:scale-105"
                                style={{
                                  borderColor: "#ef444440",
                                  color: "#ef4444",
                                  background: "rgba(239,68,68,0.1)",
                                }}
                              >
                                B-N→ĐỎ
                              </button>
                            </>
                          ) : (
                            <>
                              <button
                                onClick={() => handleForceState(2, "Đ-T → XANH")}
                                className="flex-1 text-[9px] font-bold py-1.5 rounded border transition-all hover:scale-105"
                                style={{
                                  borderColor: "#22c55e40",
                                  color: "#22c55e",
                                  background: "rgba(34,197,94,0.1)",
                                }}
                              >
                                Đ-T→XANH
                              </button>
                              <button
                                onClick={() => handleForceState(0, "Đ-T → ĐỎ")}
                                className="flex-1 text-[9px] font-bold py-1.5 rounded border transition-all hover:scale-105"
                                style={{
                                  borderColor: "#ef444440",
                                  color: "#ef4444",
                                  background: "rgba(239,68,68,0.1)",
                                }}
                              >
                                Đ-T→ĐỎ
                              </button>
                            </>
                          )}
                        </div>
                      )}
                    </div>
                  );
                })}
              </div>
            </div>
          </div>

          {/* -- Camera Panel (Right) -- */}
          <div className="lg:col-span-5 bg-slate-900/50 border border-white/5 rounded-2xl overflow-hidden">
            <div className="px-5 py-3 border-b border-white/5 flex items-center justify-between">
              <div className="flex items-center gap-2">
                <Camera size={16} className="text-emerald-400" />
                <span className="text-sm font-semibold text-slate-200 tracking-wider">CAMERA FEEDS</span>
              </div>
              <span className="relative flex h-2.5 w-2.5">
                <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-red-400 opacity-75"></span>
                <span className="relative inline-flex rounded-full h-2.5 w-2.5 bg-red-500"></span>
              </span>
            </div>

            <div className="p-4">
              <div className="grid grid-cols-2 gap-2">
                {["Bắc", "Nam", "Đông", "Tây"].map((label) => (
                  <div key={label} className="relative aspect-video bg-black/60 border border-slate-800 rounded-lg overflow-hidden flex items-center justify-center group">
                    <div className="text-slate-700 text-xs font-mono">NO SIGNAL</div>
                    <div className="absolute top-1.5 left-1.5 bg-black/80 text-emerald-400 px-1.5 py-0.5 rounded text-[10px] font-bold border border-emerald-500/20">
                      CAM — {label}
                    </div>
                    <div className="absolute bottom-1.5 right-1.5 flex items-center gap-1">
                      <div className="w-1.5 h-1.5 rounded-full bg-red-500 animate-pulse"></div>
                      <span className="text-[9px] text-red-400 font-mono">REC</span>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>
        </div>

        {/* ===== ROW 2: Realtime Traffic Flow ===== */}
        <div className="bg-slate-900/50 border border-white/5 rounded-2xl overflow-hidden">
          <div className="px-5 py-3 border-b border-white/5 flex items-center gap-2">
            <BarChart3 size={16} className="text-violet-400" />
            <span className="text-sm font-semibold text-slate-200 tracking-wider">REALTIME TRAFFIC FLOW</span>
          </div>

          <div className="p-4 overflow-x-auto">
            {trafficHistory.length === 0 ? (
              <div className="text-center text-slate-600 py-6 text-sm font-mono">Đang chờ dữ liệu từ Arduino...</div>
            ) : (
              <table className="w-full text-xs font-mono">
                <thead>
                  <tr className="text-slate-500 border-b border-slate-800">
                    <th className="text-left py-2 px-3">Thời gian</th>
                    <th className="text-center py-2 px-3">Bắc–Nam</th>
                    <th className="text-center py-2 px-3">Đông–Tây</th>
                    <th className="text-right py-2 px-3">Đếm ngược</th>
                  </tr>
                </thead>
                <tbody>
                  {trafficHistory.slice(0, 8).map((entry, i) => (
                    <tr key={i} className={`border-b border-slate-800/50 ${i === 0 ? "bg-white/[0.02]" : ""}`}>
                      <td className="py-1.5 px-3 text-slate-400">{entry.time}</td>
                      <td className="py-1.5 px-3 text-center">
                        <span
                          className="inline-block px-2 py-0.5 rounded text-[10px] font-bold"
                          style={{
                            background: `${getLightColor(entry.ns as LightState)}20`,
                            color: getLightColor(entry.ns as LightState),
                          }}
                        >
                          {entry.ns === "GREEN" ? "XANH" : entry.ns === "YELLOW" ? "VÀNG" : "ĐỎ"}
                        </span>
                      </td>
                      <td className="py-1.5 px-3 text-center">
                        <span
                          className="inline-block px-2 py-0.5 rounded text-[10px] font-bold"
                          style={{
                            background: `${getLightColor(entry.ew as LightState)}20`,
                            color: getLightColor(entry.ew as LightState),
                          }}
                        >
                          {entry.ew === "GREEN" ? "XANH" : entry.ew === "YELLOW" ? "VÀNG" : "ĐỎ"}
                        </span>
                      </td>
                      <td className="py-1.5 px-3 text-right text-slate-300">{entry.sec}s</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>
        </div>

        {/* ===== ROW 3: AI Timing Analysis (Webster) ===== */}
        <div className="bg-slate-900/50 border border-white/5 rounded-2xl overflow-hidden shadow-2xl shadow-blue-500/5">
          <div className="px-5 py-3 border-b border-white/5 flex items-center justify-between">
            <div className="flex items-center gap-2">
              <Zap size={16} className="text-blue-400" />
              <span className="text-sm font-semibold text-slate-200 tracking-wider">AI TIMING ANALYSIS (WEBSTER)</span>
            </div>
            <div className={`text-[10px] font-bold px-2 py-0.5 rounded border ${webster.status === "OVERLOADED" ? "bg-red-500/10 text-red-400 border-red-500/30" : "bg-emerald-500/10 text-emerald-400 border-emerald-500/30"}`}>
              {webster.status}
            </div>
          </div>

          <div className="p-6">
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
              {/* Cycle Info */}
              <div className="flex flex-col gap-1">
                <span className="text-[10px] text-slate-500 font-bold uppercase tracking-tighter">Optimal Cycle (Co)</span>
                <div className="text-3xl font-black text-white font-mono">{webster.cycleTime}s</div>
                <div className="h-1.5 w-full bg-slate-800 rounded-full mt-2 overflow-hidden">
                  <div className="h-full bg-blue-500" style={{ width: `${(webster.cycleTime / 120) * 100}%` }}></div>
                </div>
              </div>

              {/* Green Split */}
              <div className="flex flex-col gap-1">
                <span className="text-[10px] text-slate-500 font-bold uppercase tracking-tighter">Green Split (NS : EW)</span>
                <div className="text-2xl font-black text-slate-200 font-mono">
                  <span className="text-emerald-400">{webster.greenNS}s</span>
                  <span className="text-slate-600 mx-2">:</span>
                  <span className="text-cyan-400">{webster.greenEW}s</span>
                </div>
                <div className="flex h-1.5 w-full bg-slate-800 rounded-full mt-2 overflow-hidden">
                  <div className="h-full bg-emerald-500" style={{ width: `${(webster.greenNS / (webster.greenNS + webster.greenEW || 1)) * 100}%` }}></div>
                  <div className="h-full bg-cyan-500" style={{ width: `${(webster.greenEW / (webster.greenNS + webster.greenEW || 1)) * 100}%` }}></div>
                </div>
              </div>

              {/* Flow Density */}
              <div className="flex flex-col gap-1">
                <span className="text-[10px] text-slate-500 font-bold uppercase tracking-tighter">Traffic Density (PCU/h)</span>
                <div className="flex items-center gap-4">
                  <div>
                    <div className="text-[10px] text-slate-600">NS</div>
                    <div className="text-lg font-bold text-slate-300">{webster.pcuNS.toLocaleString()}</div>
                  </div>
                  <div className="w-px h-8 bg-white/5"></div>
                  <div>
                    <div className="text-[10px] text-slate-600">EW</div>
                    <div className="text-lg font-bold text-slate-300">{webster.pcuEW.toLocaleString()}</div>
                  </div>
                </div>
              </div>

              {/* Saturation */}
              <div className="flex flex-col gap-1">
                <span className="text-[10px] text-slate-500 font-bold uppercase tracking-tighter">Flow Ratio (Y)</span>
                <div className="text-3xl font-black font-mono" style={{ color: webster.totalFlowRatio >= 1.0 ? "#ef4444" : "#a855f7" }}>
                  {webster.totalFlowRatio.toFixed(2)}
                </div>
                <div className="text-[9px] text-slate-600">Saturation Threshold: 1.00</div>
              </div>
            </div>
          </div>
        </div>

        {/* ===== ROW 4: System Activity Logs ===== */}
        <div className="bg-slate-900/50 border border-white/5 rounded-2xl overflow-hidden">
          <div className="px-5 py-3 border-b border-white/5 flex items-center gap-2">
            <AlertTriangle size={16} className="text-amber-400" />
            <span className="text-sm font-semibold text-slate-200 tracking-wider">SYSTEM ACTIVITY LOGS</span>
          </div>

          <div className="p-4 max-h-[200px] overflow-y-auto font-mono text-xs">
            {logs.length === 0 ? (
              <div className="text-slate-600 italic text-center py-4">Chưa có log nào...</div>
            ) : (
              logs.map((log, i) => (
                <div key={i} className="flex items-start gap-3 py-1.5 border-b border-slate-800/40 last:border-0">
                  <span className="text-slate-600 shrink-0">[{log.time}]</span>
                  <span
                    className={
                      log.type === "success" ? "text-emerald-400" :
                      log.type === "error" ? "text-red-400" :
                      log.type === "warn" ? "text-amber-400" :
                      "text-slate-400"
                    }
                  >
                    {log.msg}
                  </span>
                </div>
              ))
            )}
          </div>
        </div>
      </main>
    </div>
  );
}
