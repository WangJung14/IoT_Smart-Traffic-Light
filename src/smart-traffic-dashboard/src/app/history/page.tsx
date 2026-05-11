"use client";

import { useEffect, useState, useCallback } from "react";
import { Database, RefreshCw, Car, Truck, Bus, Zap, TrendingUp, Clock } from "lucide-react";

const API = "http://localhost:5212/api/v1/hardware/detection-history?count=50";

interface VehicleGroup {
  cars: number;
  motorbikes: number;
  buses: number;
  trucks: number;
}

interface Webster {
  cycleTime: number;
  greenNS: number;
  greenEW: number;
  totalFlowRatio: number;
  status: string;
}

interface DetectionEntry {
  id: string;
  timestamp: string;
  source: string;
  ns: VehicleGroup;
  ew: VehicleGroup;
  webster: Webster;
}

function totalVehicles(g: VehicleGroup) {
  return g.cars + g.motorbikes + g.buses + g.trucks;
}

function formatTime(ts: string) {
  const d = new Date(ts);
  return d.toLocaleString("vi-VN", {
    day: "2-digit", month: "2-digit", year: "numeric",
    hour: "2-digit", minute: "2-digit", second: "2-digit",
  });
}

function StatusBadge({ status }: { status: string }) {
  const isOverloaded = status === "OVERLOADED";
  return (
    <span
      className="inline-block px-2 py-0.5 rounded text-[10px] font-bold border"
      style={isOverloaded
        ? { background: "rgba(239,68,68,0.1)", color: "#ef4444", borderColor: "rgba(239,68,68,0.3)" }
        : { background: "rgba(34,197,94,0.1)", color: "#22c55e", borderColor: "rgba(34,197,94,0.3)" }
      }
    >
      {isOverloaded ? "⚠ OVERLOADED" : "✓ NORMAL"}
    </span>
  );
}

function SourceBadge({ source }: { source: string }) {
  return (
    <span
      className="inline-block px-2 py-0.5 rounded text-[10px] font-bold border"
      style={source === "IMAGE"
        ? { background: "rgba(168,85,247,0.1)", color: "#a855f7", borderColor: "rgba(168,85,247,0.3)" }
        : { background: "rgba(14,165,233,0.1)", color: "#0ea5e9", borderColor: "rgba(14,165,233,0.3)" }
      }
    >
      {source === "IMAGE" ? "📷 IMAGE" : "🎥 VIDEO"}
    </span>
  );
}

export default function HistoryPage() {
  const [logs, setLogs] = useState<DetectionEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [lastRefresh, setLastRefresh] = useState<Date>(new Date());
  const [autoRefresh, setAutoRefresh] = useState(true);

  const fetchLogs = useCallback(async () => {
    try {
      const res = await fetch(API);
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      const data = await res.json();
      setLogs(data);
      setError(null);
      setLastRefresh(new Date());
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : "Unknown error");
    } finally {
      setLoading(false);
    }
  }, []);

  // Initial load
  useEffect(() => { fetchLogs(); }, [fetchLogs]);

  // Auto-refresh every 5s
  useEffect(() => {
    if (!autoRefresh) return;
    const interval = setInterval(fetchLogs, 5000);
    return () => clearInterval(interval);
  }, [autoRefresh, fetchLogs]);

  // Stats
  const totalLogs  = logs.length;
  const overloaded = logs.filter(l => l.webster.status === "OVERLOADED").length;
  const avgCycle   = totalLogs > 0
    ? Math.round(logs.reduce((s, l) => s + l.webster.cycleTime, 0) / totalLogs)
    : 0;
  const maxFlow    = totalLogs > 0
    ? Math.max(...logs.map(l => l.webster.totalFlowRatio)).toFixed(3)
    : "—";

  return (
    <div className="max-w-[1400px] mx-auto px-6 py-6 flex flex-col gap-6">

      {/* ── Header ── */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-violet-500 to-indigo-600 flex items-center justify-center shadow-lg">
            <Database size={20} className="text-white" />
          </div>
          <div>
            <h1 className="text-xl font-bold text-white tracking-wide">DETECTION HISTORY</h1>
            <p className="text-xs text-slate-500">Lịch sử phân tích AI từ camera – lưu vào Database</p>
          </div>
        </div>

        <div className="flex items-center gap-3">
          {/* Auto-refresh toggle */}
          <button
            onClick={() => setAutoRefresh(v => !v)}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-bold border transition-all"
            style={autoRefresh
              ? { background: "rgba(34,197,94,0.1)", color: "#22c55e", borderColor: "rgba(34,197,94,0.3)" }
              : { background: "rgba(100,116,139,0.1)", color: "#64748b", borderColor: "rgba(100,116,139,0.3)" }
            }
          >
            <RefreshCw size={12} className={autoRefresh ? "animate-spin" : ""} style={{ animationDuration: "3s" }} />
            {autoRefresh ? "AUTO 5s" : "MANUAL"}
          </button>

          {/* Manual refresh */}
          <button
            onClick={() => { setLoading(true); fetchLogs(); }}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-bold border transition-all hover:bg-white/5"
            style={{ color: "#94a3b8", borderColor: "rgba(148,163,184,0.2)" }}
          >
            <RefreshCw size={12} />
            Làm mới
          </button>

          <div className="text-[10px] text-slate-600">
            Cập nhật: {lastRefresh.toLocaleTimeString("vi-VN")}
          </div>
        </div>
      </div>

      {/* ── Summary Cards ── */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        {[
          { label: "Tổng bản ghi", value: totalLogs, icon: <Database size={16} />, color: "#a855f7" },
          { label: "Trạng thái OVERLOADED", value: overloaded, icon: <TrendingUp size={16} />, color: "#ef4444" },
          { label: "Chu kỳ TB (s)", value: avgCycle, icon: <Clock size={16} />, color: "#3b82f6" },
          { label: "Flow Ratio cao nhất", value: maxFlow, icon: <Zap size={16} />, color: "#f59e0b" },
        ].map((card) => (
          <div
            key={card.label}
            className="bg-slate-900/50 border border-white/5 rounded-xl p-4 flex flex-col gap-2"
          >
            <div className="flex items-center gap-2" style={{ color: card.color }}>
              {card.icon}
              <span className="text-[10px] font-bold text-slate-500 uppercase tracking-wider">{card.label}</span>
            </div>
            <div className="text-3xl font-black font-mono text-white">{card.value}</div>
          </div>
        ))}
      </div>

      {/* ── Table ── */}
      <div className="bg-slate-900/50 border border-white/5 rounded-2xl overflow-hidden">
        <div className="px-5 py-3 border-b border-white/5 flex items-center gap-2">
          <Database size={15} className="text-violet-400" />
          <span className="text-sm font-semibold text-slate-200 tracking-wider">LỊCH SỬ PHÂN TÍCH AI</span>
          <span className="ml-auto text-[10px] text-slate-600">{totalLogs} bản ghi</span>
        </div>

        <div className="overflow-x-auto">
          {loading ? (
            <div className="text-center py-12 text-slate-600 text-sm">Đang tải dữ liệu...</div>
          ) : error ? (
            <div className="text-center py-12">
              <div className="text-red-400 text-sm mb-1">⚠ Không kết nối được Backend</div>
              <div className="text-slate-600 text-xs">Hãy chạy: <code className="text-slate-400">dotnet run</code></div>
            </div>
          ) : logs.length === 0 ? (
            <div className="text-center py-12">
              <div className="text-slate-600 text-sm mb-1">Chưa có dữ liệu</div>
              <div className="text-slate-700 text-xs">Chạy camera detect để bắt đầu ghi log</div>
            </div>
          ) : (
            <table className="w-full text-xs font-mono">
              <thead>
                <tr className="border-b border-slate-800 text-[10px] text-slate-500 uppercase tracking-wider">
                  <th className="text-left py-3 px-4">Thời gian</th>
                  <th className="text-center py-3 px-3">Nguồn</th>
                  <th className="text-center py-3 px-3">NS (xe)</th>
                  <th className="text-center py-3 px-3">EW (xe)</th>
                  <th className="text-center py-3 px-3">Co (s)</th>
                  <th className="text-center py-3 px-3">NS Xanh</th>
                  <th className="text-center py-3 px-3">EW Xanh</th>
                  <th className="text-center py-3 px-3">Flow (Y)</th>
                  <th className="text-center py-3 px-4">Trạng thái</th>
                </tr>
              </thead>
              <tbody>
                {logs.map((entry, i) => {
                  const nsTotal = totalVehicles(entry.ns);
                  const ewTotal = totalVehicles(entry.ew);
                  return (
                    <tr
                      key={entry.id}
                      className="border-b border-slate-800/40 hover:bg-white/[0.02] transition-colors"
                      style={i === 0 ? { background: "rgba(168,85,247,0.04)" } : {}}
                    >
                      <td className="py-2.5 px-4 text-slate-400 whitespace-nowrap">
                        {i === 0 && <span className="text-violet-400 mr-1">●</span>}
                        {formatTime(entry.timestamp)}
                      </td>
                      <td className="py-2.5 px-3 text-center">
                        <SourceBadge source={entry.source} />
                      </td>
                      {/* NS vehicles breakdown */}
                      <td className="py-2.5 px-3 text-center">
                        <span className="text-slate-200 font-bold">{nsTotal}</span>
                        <span className="text-slate-600 ml-1 text-[9px]">
                          ({entry.ns.cars}🚗 {entry.ns.motorbikes}🛵)
                        </span>
                      </td>
                      {/* EW vehicles breakdown */}
                      <td className="py-2.5 px-3 text-center">
                        <span className="text-slate-200 font-bold">{ewTotal}</span>
                        <span className="text-slate-600 ml-1 text-[9px]">
                          ({entry.ew.cars}🚗 {entry.ew.motorbikes}🛵)
                        </span>
                      </td>
                      <td className="py-2.5 px-3 text-center text-blue-400 font-bold">
                        {entry.webster.cycleTime}
                      </td>
                      <td className="py-2.5 px-3 text-center text-emerald-400 font-bold">
                        {entry.webster.greenNS}s
                      </td>
                      <td className="py-2.5 px-3 text-center text-cyan-400 font-bold">
                        {entry.webster.greenEW}s
                      </td>
                      <td
                        className="py-2.5 px-3 text-center font-bold"
                        style={{ color: entry.webster.totalFlowRatio >= 1.0 ? "#ef4444" : "#a855f7" }}
                      >
                        {entry.webster.totalFlowRatio.toFixed(3)}
                      </td>
                      <td className="py-2.5 px-4 text-center">
                        <StatusBadge status={entry.webster.status} />
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          )}
        </div>
      </div>

      {/* ── Vehicle Type Legend ── */}
      <div className="flex items-center gap-6 text-[10px] text-slate-600">
        <span className="flex items-center gap-1"><Car size={11} />Ô tô = Car</span>
        <span className="flex items-center gap-1">🛵 Xe máy = Motorbike</span>
        <span className="flex items-center gap-1"><Bus size={11} />Xe buýt = Bus</span>
        <span className="flex items-center gap-1"><Truck size={11} />Xe tải = Truck</span>
        <span className="ml-auto">Y ≥ 1.0 → OVERLOADED (bão hòa)</span>
      </div>
    </div>
  );
}
