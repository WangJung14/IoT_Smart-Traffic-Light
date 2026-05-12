"use client";

import { useState, useEffect, useCallback } from "react";
import Link from "next/link";
import {
  ArrowLeft, Calendar, Brain, Clock, Activity, Car, Bus, Truck, Filter
} from "lucide-react";
import {
  LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer,
  BarChart, Bar, Legend
} from "recharts";

const ML_API = "http://localhost:8000";

// ── Types ─────────────────────────────────────────────────────────────────────
type ForecastPoint = {
  hour: string;
  status: string;
  cycleTime: number;
  totalVehicles: number;
};

type VehiclePoint = {
  hour: string;
  cars: number;
  motorbikes: number;
  buses: number;
  trucks: number;
  avgCycleTime: number;
  sampleCount: number;
};

type ActiveTab = "forecast" | "vehicles";

// ── Helpers ───────────────────────────────────────────────────────────────────
function statusColor(status: string) {
  switch (status) {
    case "NORMAL":     return "#10b981";
    case "HEAVY":      return "#f59e0b";
    case "OVERLOADED": return "#ef4444";
    default:           return "#6b7280";
  }
}

// Custom tooltip for forecast chart
function ForecastTooltip({ active, payload, label }: any) {
  if (!active || !payload?.length) return null;
  const d = payload[0].payload;
  return (
    <div className="bg-slate-800 border border-slate-700 p-3 rounded-lg shadow-lg text-sm">
      <p className="text-white font-bold mb-1">Luc {label}</p>
      <p className="text-slate-300">
        Trang thai:{" "}
        <span style={{ color: statusColor(d.status), fontWeight: "bold" }}>{d.status}</span>
      </p>
      <p className="text-cyan-400">Chu ky cho: {d.cycleTime}s</p>
    </div>
  );
}

// Custom tooltip for vehicle chart
function VehicleTooltip({ active, payload, label }: any) {
  if (!active || !payload?.length) return null;
  const total = payload.reduce((s: number, p: any) => s + (p.value || 0), 0);
  return (
    <div className="bg-slate-800 border border-slate-700 p-3 rounded-lg shadow-lg text-sm min-w-[160px]">
      <p className="text-white font-bold mb-2">Luc {label}</p>
      {payload.map((p: any) => (
        <p key={p.dataKey} style={{ color: p.fill }} className="flex justify-between gap-4">
          <span>{p.name}</span>
          <span className="font-medium">{p.value.toFixed(1)}</span>
        </p>
      ))}
      <p className="border-t border-slate-600 mt-2 pt-1 text-slate-400 flex justify-between">
        <span>Tong</span><span className="font-bold text-white">{total.toFixed(0)}</span>
      </p>
    </div>
  );
}

// ── Main Page ──────────────────────────────────────────────────────────────────
export default function ForecastPage() {
  const [activeTab, setActiveTab] = useState<ActiveTab>("forecast");

  // Forecast state
  const [date, setDate]         = useState<string>(new Date().toISOString().split("T")[0]);
  const [forecast, setForecast] = useState<ForecastPoint[]>([]);
  const [forecastLoading, setForecastLoading] = useState(false);
  const [forecastError, setForecastError]     = useState<string | null>(null);

  // Vehicle stats state
  const [source, setSource]       = useState<string>("ALL");
  const [dayType, setDayType]     = useState<string>("ALL");
  const [vehicles, setVehicles]   = useState<VehiclePoint[]>([]);
  const [vehicleLoading, setVehicleLoading] = useState(false);
  const [vehicleError, setVehicleError]     = useState<string | null>(null);

  // ── Fetch forecast ───────────────────────────────────────────────────────────
  const fetchForecast = useCallback(async (d: string) => {
    setForecastLoading(true);
    setForecastError(null);
    try {
      const res = await fetch(`${ML_API}/predict?date=${d}`);
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      const data = await res.json();
      setForecast(data.forecast);
    } catch (e: unknown) {
      setForecastError(e instanceof Error ? e.message : "Loi ket noi API");
    } finally {
      setForecastLoading(false);
    }
  }, []);

  // ── Fetch vehicle stats ──────────────────────────────────────────────────────
  const fetchVehicles = useCallback(async (src: string, dt: string) => {
    setVehicleLoading(true);
    setVehicleError(null);
    try {
      const res = await fetch(`${ML_API}/vehicle-stats?source=${src}&day_type=${dt}`);
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      const data = await res.json();
      setVehicles(data.data);
    } catch (e: unknown) {
      setVehicleError(e instanceof Error ? e.message : "Loi ket noi API");
    } finally {
      setVehicleLoading(false);
    }
  }, []);

  useEffect(() => { fetchForecast(date); }, [date, fetchForecast]);
  useEffect(() => { fetchVehicles(source, dayType); }, [source, dayType, fetchVehicles]);

  // ── Summary stats ────────────────────────────────────────────────────────────
  const statusCount = (s: string) => forecast.filter(f => f.status === s).length;

  // ── Render ───────────────────────────────────────────────────────────────────
  return (
    <div className="min-h-screen text-slate-200 p-4 md:p-8"
      style={{ background: "linear-gradient(135deg, #0c1222 0%, #0f172a 50%, #111827 100%)" }}>
      <div className="max-w-6xl mx-auto space-y-6">

        {/* ── Header ─────────────────────────────────────────────────────────── */}
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div className="flex items-center gap-4">
            <Link href="/"
              className="p-2 hover:bg-slate-800 rounded-lg border border-slate-800 hover:border-slate-700 transition-colors">
              <ArrowLeft size={20} className="text-slate-400" />
            </Link>
            <div>
              <h1 className="text-2xl font-bold text-white flex items-center gap-2">
                <Brain className="text-purple-500" /> AI Traffic Forecast
              </h1>
              <p className="text-slate-500 text-sm">Phan tich va du bao giao thong bang Random Forest</p>
            </div>
          </div>

          {/* Date picker — only visible on forecast tab */}
          {activeTab === "forecast" && (
            <div className="flex items-center gap-2 bg-slate-900 border border-slate-800 p-2 rounded-lg">
              <Calendar size={16} className="text-slate-400" />
              <input type="date" value={date} onChange={e => setDate(e.target.value)}
                className="bg-transparent text-white outline-none text-sm" />
            </div>
          )}
        </div>

        {/* ── Tabs ───────────────────────────────────────────────────────────── */}
        <div className="flex gap-1 bg-slate-900/60 p-1 rounded-xl w-fit border border-slate-800">
          {([
            { id: "forecast", label: "Du bao (AI)", icon: <Brain size={14} /> },
            { id: "vehicles", label: "Luong xe theo gio", icon: <Car size={14} /> },
          ] as const).map(tab => (
            <button key={tab.id}
              onClick={() => setActiveTab(tab.id)}
              className={`flex items-center gap-1.5 px-4 py-2 rounded-lg text-sm font-medium transition-all ${
                activeTab === tab.id
                  ? "bg-purple-600 text-white shadow"
                  : "text-slate-400 hover:text-white hover:bg-slate-800"
              }`}>
              {tab.icon} {tab.label}
            </button>
          ))}
        </div>

        {/* ════════════════════════════════════════════════════════════════════ */}
        {/* TAB: FORECAST                                                       */}
        {/* ════════════════════════════════════════════════════════════════════ */}
        {activeTab === "forecast" && (
          forecastError ? (
            <ErrorBox message={forecastError}
              hint="Kiem tra FastAPI da chay chua: python src/ml/api.py" />
          ) : forecastLoading ? <LoadingBox /> : (
            <div className="space-y-6">
              {/* Line chart */}
              <div className="bg-slate-900 border border-slate-800 p-6 rounded-xl">
                <h2 className="text-base font-semibold text-white mb-5 flex items-center gap-2">
                  <Clock size={16} className="text-purple-400" />
                  Du bao chu ky cho (Wait Time) — {date}
                </h2>
                <div className="h-[320px]">
                  <ResponsiveContainer width="100%" height="100%">
                    <LineChart data={forecast} margin={{ top: 5, right: 20, bottom: 5, left: 0 }}>
                      <CartesianGrid strokeDasharray="3 3" stroke="#1e293b" vertical={false} />
                      <XAxis dataKey="hour" stroke="#64748b" tick={{ fill: "#64748b", fontSize: 11 }} tickLine={false} />
                      <YAxis stroke="#64748b" tick={{ fill: "#64748b", fontSize: 11 }} tickLine={false} axisLine={false}
                        domain={[0, 140]} label={{ value: "Giay (s)", angle: -90, position: "insideLeft", fill: "#64748b", fontSize: 11 }} />
                      <Tooltip content={<ForecastTooltip />} />
                      <Line type="monotone" dataKey="cycleTime" stroke="#a855f7" strokeWidth={3}
                        dot={{ r: 3, fill: "#a855f7", strokeWidth: 2, stroke: "#0f172a" }}
                        activeDot={{ r: 6, fill: "#c084fc" }}
                        animationDuration={1000} />
                    </LineChart>
                  </ResponsiveContainer>
                </div>
              </div>

              {/* Status summary */}
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                {(["NORMAL", "HEAVY", "OVERLOADED"] as const).map(s => {
                  const hrs = forecast.filter(f => f.status === s).map(f => f.hour.split(":")[0]);
                  return (
                    <div key={s} className="bg-slate-900 border border-slate-800 p-5 rounded-xl">
                      <p className="text-xs text-slate-500 mb-1">Trang thai</p>
                      <p className="text-xl font-bold mb-2" style={{ color: statusColor(s) }}>{s}</p>
                      <p className="text-3xl text-white font-light">
                        {statusCount(s)} <span className="text-sm text-slate-500">gio / ngay</span>
                      </p>
                      {hrs.length > 0 && (
                        <p className="mt-3 pt-3 border-t border-slate-800 text-xs text-slate-500">
                          Khung gio: {hrs.join("h, ")}h
                        </p>
                      )}
                    </div>
                  );
                })}
              </div>
            </div>
          )
        )}

        {/* ════════════════════════════════════════════════════════════════════ */}
        {/* TAB: VEHICLE BREAKDOWN                                              */}
        {/* ════════════════════════════════════════════════════════════════════ */}
        {activeTab === "vehicles" && (
          <div className="space-y-6">
            {/* Filters */}
            <div className="flex flex-wrap gap-4 bg-slate-900 border border-slate-800 p-4 rounded-xl">
              <div className="flex items-center gap-2 text-sm text-slate-400">
                <Filter size={14} />
                <span className="font-medium">Loc du lieu:</span>
              </div>

              <div className="flex items-center gap-2">
                <label className="text-xs text-slate-500">Nguon</label>
                <select value={source} onChange={e => setSource(e.target.value)}
                  className="bg-slate-800 border border-slate-700 text-white text-sm rounded-lg px-3 py-1.5 outline-none focus:border-purple-500">
                  <option value="ALL">Tat ca</option>
                  <option value="SIMULATION">Simulation</option>
                  <option value="VIDEO">Video (Thuc te)</option>
                  <option value="IMAGE">Image</option>
                </select>
              </div>

              <div className="flex items-center gap-2">
                <label className="text-xs text-slate-500">Loai ngay</label>
                <select value={dayType} onChange={e => setDayType(e.target.value)}
                  className="bg-slate-800 border border-slate-700 text-white text-sm rounded-lg px-3 py-1.5 outline-none focus:border-purple-500">
                  <option value="ALL">Tat ca</option>
                  <option value="WEEKDAY">Ngay thuong</option>
                  <option value="WEEKEND">Cuoi tuan</option>
                </select>
              </div>
            </div>

            {vehicleError ? (
              <ErrorBox message={vehicleError} hint="Kiem tra API va ket noi database" />
            ) : vehicleLoading ? <LoadingBox /> : vehicles.length > 0 ? (
              <>
                {/* Stacked Bar Chart */}
                <div className="bg-slate-900 border border-slate-800 p-6 rounded-xl">
                  <h2 className="text-base font-semibold text-white mb-5 flex items-center gap-2">
                    <Car size={16} className="text-cyan-400" />
                    So luong xe trung binh theo tung loai — moi khung gio
                  </h2>
                  <div className="h-[380px]">
                    <ResponsiveContainer width="100%" height="100%">
                      <BarChart data={vehicles} margin={{ top: 5, right: 20, bottom: 5, left: 0 }}
                        barCategoryGap="20%">
                        <CartesianGrid strokeDasharray="3 3" stroke="#1e293b" vertical={false} />
                        <XAxis dataKey="hour" stroke="#64748b" tick={{ fill: "#64748b", fontSize: 11 }} tickLine={false} />
                        <YAxis stroke="#64748b" tick={{ fill: "#64748b", fontSize: 11 }} tickLine={false} axisLine={false}
                          label={{ value: "So xe (tb)", angle: -90, position: "insideLeft", fill: "#64748b", fontSize: 11 }} />
                        <Tooltip content={<VehicleTooltip />} cursor={{ fill: "rgba(148,163,184,0.05)" }} />
                        <Legend wrapperStyle={{ paddingTop: 16, fontSize: 12, color: "#94a3b8" }} />
                        <Bar dataKey="motorbikes" name="Xe may"  stackId="a" fill="#06b6d4" radius={[0,0,0,0]} />
                        <Bar dataKey="cars"       name="O to"    stackId="a" fill="#8b5cf6" radius={[0,0,0,0]} />
                        <Bar dataKey="buses"      name="Xe buyt" stackId="a" fill="#f59e0b" radius={[0,0,0,0]} />
                        <Bar dataKey="trucks"     name="Xe tai"  stackId="a" fill="#ef4444" radius={[4,4,0,0]} />
                      </BarChart>
                    </ResponsiveContainer>
                  </div>
                </div>

                {/* Summary cards */}
                <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                  {[
                    { key: "motorbikes", label: "Xe may",  color: "#06b6d4", icon: <Activity size={18} /> },
                    { key: "cars",       label: "O to",    color: "#8b5cf6", icon: <Car size={18} /> },
                    { key: "buses",      label: "Xe buyt", color: "#f59e0b", icon: <Bus size={18} /> },
                    { key: "trucks",     label: "Xe tai",  color: "#ef4444", icon: <Truck size={18} /> },
                  ].map(({ key, label, color, icon }) => {
                    const avg = vehicles.reduce((s, r) => s + (r as any)[key], 0) / 24;
                    const peak = Math.max(...vehicles.map(r => (r as any)[key]));
                    const peakHour = vehicles.find(r => (r as any)[key] === peak)?.hour ?? "--";
                    return (
                      <div key={key} className="bg-slate-900 border border-slate-800 p-4 rounded-xl">
                        <div className="flex items-center gap-2 mb-3" style={{ color }}>
                          {icon}
                          <span className="text-sm font-medium text-slate-300">{label}</span>
                        </div>
                        <p className="text-2xl font-bold text-white">{avg.toFixed(1)}</p>
                        <p className="text-xs text-slate-500 mt-1">tb / gio</p>
                        <p className="text-xs text-slate-500 mt-2">
                          Dinh: <span className="text-white">{peak.toFixed(0)}</span> xe luc {peakHour}
                        </p>
                      </div>
                    );
                  })}
                </div>

                {/* Data table */}
                <div className="bg-slate-900 border border-slate-800 rounded-xl overflow-hidden">
                  <div className="px-5 py-3 border-b border-slate-800">
                    <h3 className="text-sm font-medium text-white">Du lieu chi tiet theo gio</h3>
                  </div>
                  <div className="overflow-x-auto">
                    <table className="w-full text-sm">
                      <thead>
                        <tr className="border-b border-slate-800">
                          <th className="text-left px-4 py-2.5 text-slate-500 font-medium">Gio</th>
                          <th className="text-right px-4 py-2.5 text-cyan-500 font-medium">Xe may</th>
                          <th className="text-right px-4 py-2.5 text-purple-400 font-medium">O to</th>
                          <th className="text-right px-4 py-2.5 text-amber-400 font-medium">Xe buyt</th>
                          <th className="text-right px-4 py-2.5 text-red-400 font-medium">Xe tai</th>
                          <th className="text-right px-4 py-2.5 text-slate-400 font-medium">Tong</th>
                          <th className="text-right px-4 py-2.5 text-slate-400 font-medium">Chu ky tb</th>
                          <th className="text-right px-4 py-2.5 text-slate-500 font-medium">Mau</th>
                        </tr>
                      </thead>
                      <tbody>
                        {vehicles.map((row, i) => {
                          const total = row.cars + row.motorbikes + row.buses + row.trucks;
                          const isRush = [7,8,9,17,18,19].includes(i);
                          return (
                            <tr key={row.hour}
                              className={`border-b border-slate-800/50 transition-colors hover:bg-slate-800/30 ${
                                isRush ? "bg-purple-900/10" : ""
                              }`}>
                              <td className="px-4 py-2.5 text-slate-300 font-medium">
                                {row.hour}
                                {isRush && (
                                  <span className="ml-2 text-xs px-1.5 py-0.5 rounded bg-purple-900/40 text-purple-400">Cao diem</span>
                                )}
                              </td>
                              <td className="px-4 py-2.5 text-right text-cyan-400">{row.motorbikes.toFixed(1)}</td>
                              <td className="px-4 py-2.5 text-right text-purple-400">{row.cars.toFixed(1)}</td>
                              <td className="px-4 py-2.5 text-right text-amber-400">{row.buses.toFixed(1)}</td>
                              <td className="px-4 py-2.5 text-right text-red-400">{row.trucks.toFixed(1)}</td>
                              <td className="px-4 py-2.5 text-right text-white font-semibold">{total.toFixed(0)}</td>
                              <td className="px-4 py-2.5 text-right text-slate-400">{row.avgCycleTime.toFixed(1)}s</td>
                              <td className="px-4 py-2.5 text-right text-slate-500 text-xs">{row.sampleCount}</td>
                            </tr>
                          );
                        })}
                      </tbody>
                    </table>
                  </div>
                </div>
              </>
            ) : (
              <div className="text-center py-12 text-slate-500">Khong co du lieu</div>
            )}
          </div>
        )}

      </div>
    </div>
  );
}

// ── Shared helper components ───────────────────────────────────────────────────
function LoadingBox() {
  return (
    <div className="h-[300px] flex items-center justify-center bg-slate-900/50 rounded-xl border border-slate-800">
      <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-purple-500" />
    </div>
  );
}

function ErrorBox({ message, hint }: { message: string; hint?: string }) {
  return (
    <div className="bg-red-500/10 border border-red-500/30 text-red-400 p-6 rounded-xl text-center">
      <Activity className="mx-auto mb-2 opacity-50" size={32} />
      <p className="font-medium">{message}</p>
      {hint && <p className="text-xs mt-2 text-red-400/60">{hint}</p>}
    </div>
  );
}
