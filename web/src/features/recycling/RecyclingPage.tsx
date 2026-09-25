import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { fetchFacilities } from './recyclingApi';
import CreateFacilityForm from './CreateFacilityForm';
import UserMenu from '../auth/UserMenu';
import type { FacilityListItem, FacilityStatus } from '../../types/recycling';

export default function RecyclingPage() {
  const [showForm, setShowForm] = useState(false);

  const { data, isLoading, isError, error, refetch, isFetching } = useQuery({
    queryKey: ['facilities'],
    queryFn: fetchFacilities,
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
          <div className="flex items-center gap-4">
            <Link to="/" className="text-sm text-slate-300 hover:text-white transition">
              ← Back
            </Link>
            <UserMenu />
          </div>
        </div>
      </header>

      <main className="max-w-7xl mx-auto px-6 py-12">
        <div className="flex items-end justify-between mb-8">
          <div>
            <h1 className="text-3xl font-bold tracking-tight">Recycling Facilities</h1>
            <p className="text-sm text-slate-400 mt-1">
              Facilities, intake history, and recovery metrics.
            </p>
          </div>
          <div className="flex items-center gap-3">
            <button
              type="button"
              onClick={() => refetch()}
              disabled={isFetching}
              className="px-4 py-2 rounded-lg border border-slate-700 text-sm text-slate-200 hover:bg-slate-900 transition disabled:opacity-50"
            >
              {isFetching ? 'Refreshing…' : 'Refresh'}
            </button>
            <button
              type="button"
              onClick={() => setShowForm((v) => !v)}
              className="px-4 py-2 rounded-lg bg-emerald-500 text-slate-950 font-medium text-sm hover:bg-emerald-400 transition"
            >
              {showForm ? 'Cancel' : '+ New facility'}
            </button>
          </div>
        </div>

        {showForm && (
          <CreateFacilityForm
            onCreated={() => setShowForm(false)}
            onCancel={() => setShowForm(false)}
          />
        )}

        {isLoading && <LoadingState />}
        {isError && <ErrorState error={error} />}
        {data && data.length === 0 && !showForm && <EmptyState />}
        {data && data.length > 0 && <FacilitiesGrid items={data} />}

        {data && (
          <div className="mt-6 text-xs text-slate-500">
            {data.length} facilit{data.length === 1 ? 'y' : 'ies'} total
          </div>
        )}
      </main>
    </div>
  );
}

function LoadingState() {
  return (
    <div className="rounded-xl border border-slate-800 bg-slate-900/50 p-12 text-center">
      <div className="text-slate-400 text-sm">Loading facilities…</div>
    </div>
  );
}

function ErrorState({ error }: { error: unknown }) {
  const message = error instanceof Error ? error.message : 'Unknown error';
  return (
    <div className="rounded-xl border border-red-500/30 bg-red-500/5 p-6">
      <div className="text-red-400 font-medium">Failed to load facilities</div>
      <div className="text-sm text-slate-400 mt-2">{message}</div>
    </div>
  );
}

function EmptyState() {
  return (
    <div className="rounded-xl border border-slate-800 bg-slate-900/50 p-12 text-center">
      <div className="text-slate-300">No recycling facilities yet</div>
      <div className="text-sm text-slate-500 mt-2">
        Click "New facility" above to add the first one.
      </div>
    </div>
  );
}

function FacilitiesGrid({ items }: { items: FacilityListItem[] }) {
  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
      {items.map((f) => (
        <FacilityCard key={f.id} facility={f} />
      ))}
    </div>
  );
}

function FacilityCard({ facility }: { facility: FacilityListItem }) {
  return (
    <Link
      to={`/recycling/${facility.id}`}
      className="block rounded-xl border border-slate-800 bg-slate-900/50 p-6 hover:border-emerald-500/40 transition"
    >
      <div className="flex items-start justify-between gap-4">
        <div>
          <div className="text-lg font-semibold text-slate-100">{facility.name}</div>
          <div className="text-xs text-slate-500 font-mono mt-0.5">
            {facility.latitude.toFixed(4)}, {facility.longitude.toFixed(4)}
          </div>
        </div>
        <StatusBadge status={facility.status} />
      </div>

      <div className="mt-5 grid grid-cols-2 gap-4 text-sm">
        <Metric
          label="Recycled"
          value={`${(facility.metrics.recyclingRate * 100).toFixed(1)}%`}
          tone="emerald"
        />
        <Metric
          label="CO₂ saved"
          value={`${facility.metrics.co2SavedKilograms.toLocaleString(undefined, { maximumFractionDigits: 0 })} kg`}
          tone="default"
        />
        <Metric
          label="Received"
          value={`${facility.metrics.receivedKilograms.toLocaleString(undefined, { maximumFractionDigits: 0 })} kg`}
          tone="default"
        />
        <Metric
          label="Batches"
          value={facility.intakeCount.toString()}
          tone="default"
        />
      </div>
    </Link>
  );
}

function Metric({
  label,
  value,
  tone,
}: {
  label: string;
  value: string;
  tone: 'default' | 'emerald';
}) {
  return (
    <div>
      <div className="text-xs text-slate-500">{label}</div>
      <div
        className={
          'text-base font-semibold mt-0.5 ' +
          (tone === 'emerald' ? 'text-emerald-400' : 'text-slate-200')
        }
      >
        {value}
      </div>
    </div>
  );
}

function StatusBadge({ status }: { status: FacilityStatus }) {
  const styles: Record<FacilityStatus, string> = {
    Online: 'bg-emerald-500/10 text-emerald-400 border-emerald-500/30',
    Offline: 'bg-slate-500/10 text-slate-400 border-slate-500/30',
    Maintenance: 'bg-amber-500/10 text-amber-400 border-amber-500/30',
    Decommissioned: 'bg-red-500/10 text-red-400 border-red-500/30',
  };
  return (
    <span
      className={
        'inline-block px-2 py-0.5 rounded-full border text-xs font-medium ' + styles[status]
      }
    >
      {status}
    </span>
  );
}
