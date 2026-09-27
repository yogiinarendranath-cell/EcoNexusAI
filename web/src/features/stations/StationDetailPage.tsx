import { useQuery } from "@tanstack/react-query";
import { Link, useParams } from "react-router-dom";
import { fetchStationDetail, fetchStationForecast, fetchStationReadings } from "./stationsApi";
import ForecastChart from "./ForecastChart";
import UserMenu from "../auth/UserMenu";

const WINDOW_HOURS = 24;

export default function StationDetailPage() {
    const { id } = useParams<{ id: string }>();

    const stationQuery = useQuery({
        queryKey: ["station", id],
        queryFn: () => fetchStationDetail(id!),
        enabled: !!id,
    });

    const readingsQuery = useQuery({
        queryKey: ["station", id, "readings", WINDOW_HOURS],
        queryFn: () => fetchStationReadings(id!, WINDOW_HOURS, 200),
        enabled: !!id,
    });

    const forecastQuery = useQuery({
        queryKey: ["station", id, "forecast", WINDOW_HOURS],
        queryFn: () => fetchStationForecast(id!, WINDOW_HOURS),
        enabled: !!id,
    });

    const isLoading = stationQuery.isLoading || readingsQuery.isLoading || forecastQuery.isLoading;
    const isError = stationQuery.isError || readingsQuery.isError || forecastQuery.isError;

    return (
        <div className="min-h-screen bg-slate-950 text-slate-100 font-sans">
            <header className="border-b border-slate-800">
                <div className="max-w-7xl mx-auto px-6 py-5 flex items-center justify-between">
                    <Link to="/stations" className="flex items-center gap-3">
                        <div className="w-9 h-9 rounded-lg bg-emerald-500 flex items-center justify-center font-bold text-slate-950">
                            EN
                        </div>
                        <div>
                            <div className="text-lg font-semibold tracking-tight">EcoNexus AI</div>
                            <div className="text-xs text-slate-400">Station Detail</div>
                        </div>
                    </Link>
                    <div className="flex items-center gap-4">
                        <Link to="/stations" className="text-sm text-slate-300 hover:text-white transition">
                            ← All stations
                        </Link>
                        <UserMenu />
                    </div>
                </div>
            </header>

            <main className="max-w-7xl mx-auto px-6 py-12">
                {isLoading && <LoadingState />}
                {isError && <ErrorState />}

                {stationQuery.data && (
                    <>
                        <StationHeader station={stationQuery.data} />

                        {forecastQuery.data && readingsQuery.data && (
                            <div className="mt-8">
                                <ForecastChart
                                    readings={readingsQuery.data}
                                    forecast={forecastQuery.data}
                                />
                            </div>
                        )}

                        {readingsQuery.data && readingsQuery.data.length === 0 && (
                            <div className="mt-8 rounded-xl border border-slate-800 bg-slate-900/50 p-12 text-center">
                                <div className="text-slate-300">No readings in the last {WINDOW_HOURS}h</div>
                                <div className="text-sm text-slate-500 mt-2">
                                    Start the IoT simulator (`dotnet run --project src/EcoNexus.Worker`) to
                                    generate data.
                                </div>
                            </div>
                        )}
                    </>
                )}
            </main>
        </div>
    );
}

function StationHeader({ station }: { station: import("../../types/station").StationListItem }) {
    return (
        <div>
            <div className="flex items-end justify-between">
                <div>
                    <h1 className="text-3xl font-bold tracking-tight font-mono">{station.code}</h1>
                    <p className="text-sm text-slate-400 mt-1">
                        {station.primaryCategory} · {station.latitude.toFixed(4)}, {station.longitude.toFixed(4)}
                    </p>
                </div>
                <div className="text-right">
                    <div className="text-xs text-slate-500">Current fill</div>
                    <div className={"text-2xl font-semibold " + (station.isCritical ? "text-red-400" : "text-emerald-400")}>
                        {station.currentFillPercent.toFixed(1)}%
                    </div>
                </div>
            </div>
        </div>
    );
}

function LoadingState() {
    return (
        <div className="rounded-xl border border-slate-800 bg-slate-900/50 p-12 text-center">
            <div className="text-slate-400 text-sm">Loading station data…</div>
        </div>
    );
}

function ErrorState() {
    return (
        <div className="rounded-xl border border-red-500/30 bg-red-500/5 p-6">
            <div className="text-red-400 font-medium">Failed to load station</div>
            <div className="text-sm text-slate-400 mt-2">
                Verify the API is running and the station exists.
            </div>
        </div>
    );
}

