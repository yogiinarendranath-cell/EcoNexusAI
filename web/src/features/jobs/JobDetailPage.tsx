import { Link, useParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { fetchJobById } from './jobsApi';
import UserMenu from '../auth/UserMenu';
import type { JobStatus, RouteStop } from '../../types/job';

export default function JobDetailPage() {
  const { id } = useParams<{ id: string }>();

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ['job', id],
    queryFn: () => fetchJobById(id!),
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
            <Link to="/jobs" className="text-sm text-slate-300 hover:text-white transition">
              ← All jobs
            </Link>
            <UserMenu />
          </div>
        </div>
      </header>

      <main className="max-w-5xl mx-auto px-6 py-12">
        {isLoading && <LoadingState />}
        {isError && <ErrorState error={error} />}

        {data && (
          <>
            <div className="mb-8">
              <div className="text-xs text-slate-500 font-mono mb-2">{data.id}</div>
              <h1 className="text-3xl font-bold tracking-tight">Collection Job</h1>
              <p className="text-sm text-slate-400 mt-1">
                Scheduled for {new Date(data.scheduledFor).toLocaleString()}
              </p>
            </div>

            <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-8">
              <InfoCard label="Status" value={<StatusBadge status={data.status} />} />
              <InfoCard label="Stops" value={String(data.stops.length)} />
              <InfoCard
                label="Collected"
                value={`${data.totalCollectedKilograms.toLocaleString()} kg`}
              />
              <InfoCard
                label="Vehicle"
                value={<span className="font-mono text-xs">{data.vehicleId.slice(0, 8)}…</span>}
              />
            </div>

            <h2 className="text-lg font-semibold mb-4">Route</h2>
            <ol className="space-y-3">
              {data.stops
                .slice()
                .sort((a, b) => a.sequence - b.sequence)
                .map((stop) => (
                  <StopRow key={stop.id} stop={stop} />
                ))}
            </ol>
          </>
        )}
      </main>
    </div>
  );
}

function InfoCard({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div className="rounded-xl border border-slate-800 bg-slate-900/50 p-4">
      <div className="text-xs text-slate-500 mb-2">{label}</div>
      <div className="text-lg font-medium text-slate-100">{value}</div>
    </div>
  );
}

function StatusBadge({ status }: { status: JobStatus }) {
  const styles: Record<JobStatus, string> = {
    Scheduled:  'bg-sky-500/10 text-sky-400 border-sky-500/30',
    InProgress: 'bg-amber-500/10 text-amber-400 border-amber-500/30',
    Completed:  'bg-emerald-500/10 text-emerald-400 border-emerald-500/30',
    Cancelled:  'bg-slate-500/10 text-slate-400 border-slate-500/30',
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

function StopRow({ stop }: { stop: RouteStop }) {
  const done = stop.completedAt !== null;
  return (
    <li className="flex items-center gap-4 rounded-xl border border-slate-800 bg-slate-900/30 p-4">
      <div
        className={
          'w-10 h-10 rounded-full flex items-center justify-center font-semibold text-sm ' +
          (done ? 'bg-emerald-500 text-slate-950' : 'bg-slate-800 text-slate-400')
        }
      >
        {stop.sequence}
      </div>
      <div className="flex-1">
        <div className="font-mono text-xs text-slate-500">{stop.stationId}</div>
        <div className="text-xs text-slate-400 mt-1">
          {done
            ? `Collected ${stop.collectedWeightKilograms ?? 0} kg at ${new Date(stop.completedAt!).toLocaleTimeString()}`
            : 'Pending'}
        </div>
      </div>
    </li>
  );
}

function LoadingState() {
  return (
    <div className="rounded-xl border border-slate-800 bg-slate-900/50 p-12 text-center">
      <div className="text-slate-400 text-sm">Loading job…</div>
    </div>
  );
}

function ErrorState({ error }: { error: unknown }) {
  const message = error instanceof Error ? error.message : 'Unknown error';
  return (
    <div className="rounded-xl border border-red-500/30 bg-red-500/5 p-6">
      <div className="text-red-400 font-medium">Failed to load job</div>
      <div className="text-sm text-slate-400 mt-2">{message}</div>
    </div>
  );
}
