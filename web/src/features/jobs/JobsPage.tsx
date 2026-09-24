import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { fetchJobs } from './jobsApi';
import ScheduleJobForm from './ScheduleJobForm';
import UserMenu from '../auth/UserMenu';
import type { CollectionJob, JobStatus } from '../../types/job';

export default function JobsPage() {
  const [showForm, setShowForm] = useState(false);

  const { data, isLoading, isError, error, refetch, isFetching } = useQuery({
    queryKey: ['jobs'],
    queryFn: fetchJobs,
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
            <h1 className="text-3xl font-bold tracking-tight">Collection Jobs</h1>
            <p className="text-sm text-slate-400 mt-1">
              Scheduled collection runs and their stops.
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
              {showForm ? 'Cancel' : '+ Schedule job'}
            </button>
          </div>
        </div>

        {showForm && (
          <ScheduleJobForm
            onCreated={() => setShowForm(false)}
            onCancel={() => setShowForm(false)}
          />
        )}

        {isLoading && <LoadingState />}
        {isError && <ErrorState error={error} />}
        {data && data.length === 0 && !showForm && <EmptyState />}
        {data && data.length > 0 && <JobsTable items={data} />}

        {data && (
          <div className="mt-6 text-xs text-slate-500">
            {data.length} job{data.length === 1 ? '' : 's'} total
          </div>
        )}
      </main>
    </div>
  );
}

function LoadingState() {
  return (
    <div className="rounded-xl border border-slate-800 bg-slate-900/50 p-12 text-center">
      <div className="text-slate-400 text-sm">Loading jobs…</div>
    </div>
  );
}

function ErrorState({ error }: { error: unknown }) {
  const message = error instanceof Error ? error.message : 'Unknown error';
  return (
    <div className="rounded-xl border border-red-500/30 bg-red-500/5 p-6">
      <div className="text-red-400 font-medium">Failed to load jobs</div>
      <div className="text-sm text-slate-400 mt-2">{message}</div>
    </div>
  );
}

function EmptyState() {
  return (
    <div className="rounded-xl border border-slate-800 bg-slate-900/50 p-12 text-center">
      <div className="text-slate-300">No collection jobs yet</div>
      <div className="text-sm text-slate-500 mt-2">
        Click "Schedule job" above to create the first one.
      </div>
    </div>
  );
}

function JobsTable({ items }: { items: CollectionJob[] }) {
  return (
    <div className="overflow-hidden rounded-xl border border-slate-800">
      <table className="w-full text-sm">
        <thead className="bg-slate-900/70 text-slate-400">
          <tr>
            <th className="text-left px-4 py-3 font-medium">Scheduled</th>
            <th className="text-left px-4 py-3 font-medium">Status</th>
            <th className="text-right px-4 py-3 font-medium">Stops</th>
            <th className="text-right px-4 py-3 font-medium">Collected</th>
            <th className="text-right px-4 py-3 font-medium">Vehicle</th>
            <th className="px-4 py-3"></th>
          </tr>
        </thead>
        <tbody className="divide-y divide-slate-800">
          {items.map((j) => (
            <tr key={j.id} className="hover:bg-slate-900/40 transition">
              <td className="px-4 py-3 text-slate-200 font-mono text-xs">
                {new Date(j.scheduledFor).toLocaleString()}
              </td>
              <td className="px-4 py-3">
                <StatusBadge status={j.status} />
              </td>
              <td className="px-4 py-3 text-right text-slate-300">{j.stops.length}</td>
              <td className="px-4 py-3 text-right text-slate-300">
                {j.totalCollectedKilograms.toLocaleString()} kg
              </td>
              <td className="px-4 py-3 text-right text-slate-500 font-mono text-xs">
                {j.vehicleId.slice(0, 8)}…
              </td>
              <td className="px-4 py-3 text-right">
                <Link
                  to={`/jobs/${j.id}`}
                  className="text-emerald-400 hover:text-emerald-300 text-xs font-medium"
                >
                  View →
                </Link>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
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
        'inline-block px-2 py-0.5 rounded-full border text-xs font-medium ' +
        styles[status]
      }
    >
      {status}
    </span>
  );
}
