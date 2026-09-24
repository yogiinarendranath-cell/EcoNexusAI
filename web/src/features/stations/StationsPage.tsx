import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { fetchStations } from './stationsApi';
import type { StationListItem } from '../../types/station';

export default function StationsPage() {
  const { data, isLoading, isError, error, refetch, isFetching } = useQuery({
    queryKey: ['stations', { page: 1, pageSize: 50 }],
    queryFn: () => fetchStations({ page: 1, pageSize: 50, sortBy: 'code' }),
  });

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 font-sans">
      <header className="border-b border-slate-800">
        <div className="max-w-7xl mx-auto px-6 py-5 flex items-center justify-between">
          <Link to="/" className="flex items-center gap-3">
            <div className="w-9 h-9 rounded-lg bg-emerald-500 flex items-center justify-center font-bold text-slate-950">
              EN
            </div>
            <div>
              <div className="text-lg font-semibold tracking-tight">EcoNexus AI</div>
              <div className="text-xs text-slate-400">Smart Waste & Recycling Network</div>
            </div>
          </Link>
          <Link to="/" className="text-sm text-slate-300 hover:text-white transition">
            ← Back
          </Link>
        </div>
      </header>

      <main className="max-w-7xl mx-auto px-6 py-12">
        <div className="flex items-end justify-between mb-8">
          <div>
            <h1 className="text-3xl font-bold tracking-tight">Smart Stations</h1>
            <p className="text-sm text-slate-400 mt-1">
              Live view of waste stations from the EcoNexus network.
            </p>
          </div>
          <button
            type="button"
            onClick={() => refetch()}
            disabled={isFetching}
            className="px-4 py-2 rounded-lg border border-slate-700 text-sm text-slate-200 hover:bg-slate-900 transition disabled:opacity-50"
          >
            {isFetching ? 'Refreshing…' : 'Refresh'}
          </button>
        </div>

        {isLoading && <LoadingState />}
        {isError && <ErrorState error={error} />}
        {data && data.items.length === 0 && <EmptyState />}
        {data && data.items.length > 0 && <StationsTable items={data.items} />}

        {data && (
          <div className="mt-6 text-xs text-slate-500">
            {data.totalCount} total · showing {data.items.length} · page {data.page} of {data.totalPages}
          </div>
        )}
      </main>
    </div>
  );
}

function LoadingState() {
  return (
    <div className="rounded-xl border border-slate-800 bg-slate-900/50 p-12 text-center">
      <div className="text-slate-400 text-sm">Loading stations…</div>
    </div>
  );
}

function ErrorState({ error }: { error: unknown }) {
  const message = error instanceof Error ? error.message : 'Unknown error';
  return (
    <div className="rounded-xl border border-red-500/30 bg-red-500/5 p-6">
      <div className="text-red-400 font-medium">Failed to load stations</div>
      <div className="text-sm text-slate-400 mt-2">{message}</div>
      <div className="text-xs text-slate-500 mt-2">
        Make sure the API is running on port 5067 and reachable via the Vite proxy.
      </div>
    </div>
  );
}

function EmptyState() {
  return (
    <div className="rounded-xl border border-slate-800 bg-slate-900/50 p-12 text-center">
      <div className="text-slate-300">No stations yet</div>
      <div className="text-sm text-slate-500 mt-2">
        Create some stations via the API (POST /api/v1/stations) or wait for the IoT
        simulator to seed them.
      </div>
    </div>
  );
}

function StationsTable({ items }: { items: StationListItem[] }) {
  return (
    <div className="overflow-hidden rounded-xl border border-slate-800">
      <table className="w-full text-sm">
        <thead className="bg-slate-900/70 text-slate-400">
          <tr>
            <th className="text-left px-4 py-3 font-medium">Code</th>
            <th className="text-left px-4 py-3 font-medium">Category</th>
            <th className="text-left px-4 py-3 font-medium">Status</th>
            <th className="text-right px-4 py-3 font-medium">Fill %</th>
            <th className="text-right px-4 py-3 font-medium">Capacity</th>
            <th className="text-right px-4 py-3 font-medium">Location</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-slate-800">
          {items.map((s) => {
            const isCritical = s.currentFillPercent >= 90;
            return (
              <tr key={s.id} className="hover:bg-slate-900/40 transition">
                <td className="px-4 py-3 font-mono text-slate-100">{s.code}</td>
                <td className="px-4 py-3 text-slate-300">{s.primaryCategory}</td>
                <td className="px-4 py-3">
                  <StatusBadge status={s.status} />
                </td>
                <td className={'px-4 py-3 text-right font-medium ' + (isCritical ? 'text-red-400' : 'text-slate-200')}>
                  {s.currentFillPercent.toFixed(1)}%
                </td>
                <td className="px-4 py-3 text-right text-slate-400">
                  {s.capacityKilograms} kg
                </td>
                <td className="px-4 py-3 text-right text-slate-500 font-mono text-xs">
                  {s.latitude.toFixed(4)}, {s.longitude.toFixed(4)}
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}

function StatusBadge({ status }: { status: string }) {
  const tone =
    status === 'Online'
      ? 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20'
      : status === 'Maintenance'
        ? 'bg-amber-500/10 text-amber-400 border-amber-500/20'
        : status === 'Offline'
          ? 'bg-slate-500/10 text-slate-400 border-slate-500/20'
          : 'bg-red-500/10 text-red-400 border-red-500/20';
  return (
    <span className={'inline-block px-2 py-0.5 rounded-full border text-xs ' + tone}>
      {status}
    </span>
  );
}
