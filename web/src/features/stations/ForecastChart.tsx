import {
    CartesianGrid,
    ComposedChart,
    Line,
    ReferenceLine,
    ResponsiveContainer,
    Tooltip,
    XAxis,
    YAxis,
} from "recharts";
import type { Forecast, StationReading } from "../../types/forecast";

type Props = {
    readings: StationReading[];
    forecast: Forecast;
};

type Point = {
    /** Milliseconds since epoch — used as the numeric x-axis value. */
    t: number;
    /** Actual fill level at time t, or null for forecast points. */
    actual: number | null;
    /** Forecast fill level at time t, or null for actual points. */
    forecast: number | null;
};

const MS_PER_HOUR = 3_600_000;

export default function ForecastChart({ readings, forecast }: Props) {
    const now = Date.now();

    const actualPoints: Point[] = readings.map((r) => ({
        t: new Date(r.recordedAt).getTime(),
        actual: r.fillLevelPercent,
        forecast: null,
    }));

    const lastReading = actualPoints[actualPoints.length - 1];

    // Anchor the forecast line on the last real reading so the two
    // series connect visually. From there, project forward to the
    // predicted overflow time (or +12h if no overflow is predicted,
    // so the user still sees a trend line).
    const overflowAt = forecast.predictedOverflowAt
        ? new Date(forecast.predictedOverflowAt).getTime()
        : now + 12 * MS_PER_HOUR;

    const forecastPoints: Point[] = lastReading
        ? [
              { t: lastReading.t, actual: null, forecast: lastReading.actual },
              { t: overflowAt, actual: null, forecast: 100 },
          ]
        : [];

    const data: Point[] = [...actualPoints, ...forecastPoints];

    // Compute the visible x-domain so both series fit.
    const xMin = Math.min(...data.map((p) => p.t));
    const xMax = Math.max(...data.map((p) => p.t));

    return (
        <div className="rounded-xl border border-slate-800 bg-slate-900/40 p-6">
            <div className="flex items-baseline justify-between mb-4">
                <div>
                    <h2 className="text-lg font-semibold text-slate-100">Fill-level forecast</h2>
                    <p className="text-xs text-slate-400 mt-1">
                        Past readings (solid) and predicted trajectory (dashed).
                    </p>
                </div>
                <ForecastSummary forecast={forecast} />
            </div>

            <div style={{ width: "100%", height: 320 }}>
                <ResponsiveContainer>
                    <ComposedChart data={data} margin={{ top: 10, right: 30, left: 0, bottom: 20 }}>
                        <CartesianGrid stroke="#1e293b" strokeDasharray="3 3" />
                        <XAxis
                            type="number"
                            dataKey="t"
                            domain={[xMin, xMax]}
                            tickFormatter={formatHour}
                            stroke="#64748b"
                            style={{ fontSize: 11 }}
                            label={{ value: "Time", position: "insideBottom", offset: -8, fill: "#64748b", fontSize: 11 }}
                        />
                        <YAxis
                            type="number"
                            domain={[0, 100]}
                            stroke="#64748b"
                            style={{ fontSize: 11 }}
                            label={{ value: "Fill %", angle: -90, position: "insideLeft", fill: "#64748b", fontSize: 11 }}
                        />
                        <Tooltip content={<CustomTooltip />} />

                        {/* Overflow marker */}
                        {forecast.isOverflowPredicted && forecast.predictedOverflowAt && (
                            <ReferenceLine
                                x={new Date(forecast.predictedOverflowAt).getTime()}
                                stroke="#ef4444"
                                strokeDasharray="4 4"
                                label={{ value: "Overflow", position: "top", fill: "#f87171", fontSize: 11 }}
                            />
                        )}

                        {/* 90% critical threshold */}
                        <ReferenceLine
                            y={90}
                            stroke="#f59e0b"
                            strokeDasharray="2 4"
                            label={{ value: "90% critical", position: "right", fill: "#fbbf24", fontSize: 10 }}
                        />

                        {/* Actual readings */}
                        <Line
                            type="monotone"
                            dataKey="actual"
                            stroke="#10b981"
                            strokeWidth={2}
                            dot={{ r: 3, fill: "#10b981" }}
                            activeDot={{ r: 5 }}
                            isAnimationActive={false}
                            connectNulls={false}
                        />

                        {/* Forecast trajectory */}
                        <Line
                            type="monotone"
                            dataKey="forecast"
                            stroke="#38bdf8"
                            strokeWidth={2}
                            strokeDasharray="6 4"
                            dot={{ r: 3, fill: "#38bdf8" }}
                            isAnimationActive={false}
                            connectNulls={true}
                        />
                    </ComposedChart>
                </ResponsiveContainer>
            </div>
        </div>
    );
}

function ForecastSummary({ forecast }: { forecast: Forecast }) {
    if (forecast.method === "InsufficientData") {
        return (
            <div className="text-right">
                <div className="text-xs text-amber-400 font-medium">
                    Insufficient data
                </div>
                <div className="text-xs text-slate-500 mt-1">
                    {forecast.sampleSize} reading{forecast.sampleSize === 1 ? "" : "s"} in window
                </div>
            </div>
        );
    }

    if (!forecast.isOverflowPredicted) {
        return (
            <div className="text-right">
                <div className="text-xs text-emerald-400 font-medium">No overflow predicted</div>
                <div className="text-xs text-slate-500 mt-1">
                    {forecast.fillRatePercentPerHour <= 0
                        ? "Fill level flat or declining"
                        : "Stable"}{" "}
                    · {Math.round(forecast.confidence * 100)}% confidence
                </div>
            </div>
        );
    }

    const hours = forecast.hoursUntilOverflow ?? 0;
    const label = formatDuration(hours);

    return (
        <div className="text-right">
            <div className="text-xs text-slate-400">Predicted overflow in</div>
            <div className="text-lg font-semibold text-red-400">{label}</div>
            <div className="text-xs text-slate-500 mt-1">
                {Math.round(forecast.confidence * 100)}% confidence · {forecast.sampleSize} readings
            </div>
        </div>
    );
}

function CustomTooltip({ active, payload }: { active?: boolean; payload?: Array<{ payload: Point }> }) {
    if (!active || !payload || payload.length === 0) return null;
    const p = payload[0].payload;
    const value = p.actual ?? p.forecast;
    if (value === null || value === undefined) return null;

    return (
        <div className="rounded-lg border border-slate-700 bg-slate-900 px-3 py-2 text-xs shadow-lg">
            <div className="text-slate-400">{new Date(p.t).toLocaleString()}</div>
            <div className="text-slate-100 font-mono mt-1">
                {value.toFixed(1)}%{" "}
                <span className="text-slate-500">
                    ({p.actual !== null ? "actual" : "forecast"})
                </span>
            </div>
        </div>
    );
}

function formatHour(ms: number): string {
    const d = new Date(ms);
    return d.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
}

function formatDuration(hours: number): string {
    if (hours < 1) return `${Math.round(hours * 60)} min`;
    if (hours < 48) return `${hours.toFixed(1)} h`;
    return `${(hours / 24).toFixed(1)} d`;
}

