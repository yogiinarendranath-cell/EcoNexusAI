import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { fetchFacilityById } from './recyclingApi';
import RecordIntakeForm from './RecordIntakeForm';
import IntakeRow from './IntakeRow';
import UserMenu from '../auth/UserMenu';

export default function FacilityDetailPage() {
  const { id } = useParams<{ id: string }>();
  const [showForm, setShowForm] = useState(false);

  const { data, isLoading, isError, error, refetch, isFetching } = useQuery({
    queryKey: ['facility', id],
    queryFn: () => fetchFacilityById(id!),
    enabled: !!id,
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
            <Link to="/recycling" className="text-sm text-slate-300 hover:text-white transition">
              ← All facilities
            </Link>
            <UserMenu />
          </div>
        </div>
      </header>

      <main className="max-w-7xl mx-auto px-6 py-12">
        {isLoading && <LoadingState />}
        {isError && <ErrorState error={error} />}

        {data && (
          <>
            <div className="flex items-end justify-between mb-8">
              <div>
                <h1 className="text-3xl font-bold tracking-tight">{data.name}</h1>
                <p className="text-sm text-slate-400 mt-1 font-mono">
                  {data.latitude.toFixed(4)}, {data.longitude.toFixed(4)}
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
                  {showForm ? 'Cancel' : '+ Record intake'}
                </button>
              </div>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-4 gap-6 mb-8">
              <MetricCard label="Recycled" value={`${(data.metrics.recyclingRate * 100).toFixed(1)}%`} tone="emerald" />
              <MetricCard label="Landfill diversion" value={`${(data.metrics.landfillDiversion * 100).toFixed(1)}%`} tone="default" />
              <MetricCard label="CO₂ saved" value={`${data.metrics.co2SavedKilograms.toLocaleString(undefined, { maximumFractionDigits: 0 })} kg`} tone="emerald" />
              <MetricCard label="Batches" value={`${data.metrics.totalBatches}`} tone="default" />
            </div>

            {showForm && id && (
              <RecordIntakeForm
                facilityId={id}
                onCreated={() => setShowForm(false)}
                onCancel={() => setShowForm(false)}
              />
            )}

            {data.intakes.length === 0 && (
              <div className="rounded-xl border border-slate-800 bg-slate-900/50 p-12 text-center">
                <div className="text-slate-300">No intake batches recorded yet</div>
                <div className="text-sm text-slate-500 mt-2">
                  Click "Record intake" above to add the first one.
                </div>
              </div>
            )}

            {data.intakes.length > 0 && (
              <div className="overflow-hidden rounded-xl border border-slate-800">
                <table className="w-full text-sm">
                  <thead className="bg-slate-900/70 text-slate-400">
                    <tr>
                      <th className="text-left px-4 py-3 font-medium">Material</th>
                      <th className="text-right px-4 py-3 font-medium">Weight</th>
                      <th className="text-left px-4 py-3 font-medium">Stage</th>
                      <th className="text-left px-4 py-3 font-medium">Recorded</th>
                      <th className="px-4 py-3"></th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-800">
                    {data.intakes.map((i) => (
                      <IntakeRow key={i.id} facilityId={data.id} intake={i} />
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </>
        )}
      </main>
    </div>
  );
}

function LoadingState() {
  return (
    <div className="rounded-xl border border-slate-800 bg-slate-900/50 p-12 text-center">
      <div className="text-slate-400 text-sm">Loading facility…</div>
    </div>
  );
}

function ErrorState({ error }: { error: unknown }) {
  const message = error instanceof Error ? error.message : 'Unknown error';
  return (
    <div className="rounded-xl border border-red-500/30 bg-red-500/5 p-6">
      <div className="text-red-400 font-medium">Failed to load facility</div>
      <div className="text-sm text-slate-400 mt-2">{message}</div>
    </div>
  );
}

function MetricCard({
  label,
  value,
  tone,
}: {
  label: string;
  value: string;
  tone: 'default' | 'emerald';
}) {
  return (
    <div className="rounded-xl border border-slate-800 bg-slate-900/50 p-6">
      <div className="text-sm text-slate-400">{label}</div>
      <div
        className={
          'text-2xl font-semibold mt-2 ' +
          (tone === 'emerald' ? 'text-emerald-400' : 'text-slate-100')
        }
      >
        {value}
      </div>
    </div>
  );
}
