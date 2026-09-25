import { useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { fetchStations } from '../stations/stationsApi';
import {
  fileReport,
  type CitizenReportType,
} from './citizenReportsApi';

const REPORT_TYPES: { value: CitizenReportType; label: string; icon: string }[] = [
  { value: 'OverflowingBin', label: 'Overflowing bin', icon: '🗑️' },
  { value: 'DamagedStation', label: 'Damaged station', icon: '🔧' },
  { value: 'IllegalDumping', label: 'Illegal dumping', icon: '⚠️' },
  { value: 'MissedCollection', label: 'Missed collection', icon: '🚛' },
  { value: 'Other', label: 'Something else', icon: '❓' },
];

interface LocationState {
  stationId?: string;
  stationCode?: string;
}

export default function CitizenReport() {
  const qc = useQueryClient();
  const location = useLocation();
  const initial = (location.state as LocationState | null) ?? {};

  const [stationId, setStationId] = useState(initial.stationId ?? '');
  const [reportType, setReportType] = useState<CitizenReportType>('OverflowingBin');
  const [description, setDescription] = useState('');
  const [photoUrl, setPhotoUrl] = useState('');
  const [submitted, setSubmitted] = useState(false);

  const { data: stations } = useQuery({
    queryKey: ['stations', 'for-report'],
    queryFn: () => fetchStations({ pageSize: 100 }),
  });

  const mutation = useMutation({
    mutationFn: fileReport,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['citizen', 'reports'] });
      setSubmitted(true);
    },
  });

  const submit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!stationId) return;
    mutation.mutate({
      stationId,
      reportType,
      description: description.trim(),
      photoUrl: photoUrl.trim() || null,
    });
  };

  if (submitted) {
    return (
      <div className="max-w-md mx-auto px-5 pt-8 pb-4">
        <div className="rounded-2xl border border-emerald-500/30 bg-emerald-500/5 p-8 text-center">
          <div className="text-4xl mb-3">✓</div>
          <h1 className="text-xl font-bold text-emerald-300">Report submitted</h1>
          <p className="text-sm text-slate-400 mt-2">
            Operations will review it shortly. You&apos;ll earn green points when it&apos;s verified.
          </p>
          <div className="flex items-center justify-center gap-3 mt-6">
            <button
              type="button"
              onClick={() => {
                setSubmitted(false);
                setDescription('');
                setPhotoUrl('');
                setStationId('');
              }}
              className="px-4 py-2 rounded-lg border border-slate-700 text-sm text-slate-200 hover:bg-slate-900 transition"
            >
              File another
            </button>
            <Link
              to="/app/points"
              className="px-4 py-2 rounded-lg bg-emerald-500 text-slate-950 font-medium text-sm hover:bg-emerald-400 transition"
            >
              View points
            </Link>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-md mx-auto px-5 pt-8 pb-4">
      <h1 className="text-2xl font-bold tracking-tight mb-1">Report an Issue</h1>
      <p className="text-sm text-slate-400 mb-6">
        Help us keep stations clean and working.
      </p>

      <form onSubmit={submit} className="space-y-5">
        {/* Station picker */}
        <div>
          <label className="block text-sm text-slate-300 mb-2">
            Station
          </label>
          <select
            required
            value={stationId}
            onChange={(e) => setStationId(e.target.value)}
            className="w-full rounded-lg border border-slate-700 bg-slate-950 px-4 py-2.5 text-slate-100 focus:outline-none focus:border-emerald-500"
          >
            <option value="">Select a station…</option>
            {stations?.items.map((s) => (
              <option key={s.id} value={s.id}>
                {s.code} — {s.primaryCategory}
              </option>
            ))}
          </select>
        </div>

        {/* Report type chips */}
        <div>
          <label className="block text-sm text-slate-300 mb-2">
            What&apos;s wrong?
          </label>
          <div className="grid grid-cols-2 gap-2">
            {REPORT_TYPES.map((t) => {
              const active = reportType === t.value;
              return (
                <button
                  key={t.value}
                  type="button"
                  onClick={() => setReportType(t.value)}
                  className={
                    'rounded-lg border px-3 py-2.5 text-sm text-left transition ' +
                    (active
                      ? 'border-emerald-500/60 bg-emerald-500/10 text-emerald-300'
                      : 'border-slate-800 bg-slate-900/50 text-slate-300 hover:border-slate-700')
                  }
                >
                  <span className="mr-2">{t.icon}</span>
                  {t.label}
                </button>
              );
            })}
          </div>
        </div>

        {/* Description */}
        <div>
          <label htmlFor="description" className="block text-sm text-slate-300 mb-2">
            Description
          </label>
          <textarea
            id="description"
            required
            rows={4}
            maxLength={2000}
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            placeholder="Bin has been full for two days, lid is broken…"
            className="w-full rounded-lg border border-slate-700 bg-slate-950 px-4 py-2.5 text-slate-100 placeholder-slate-600 focus:outline-none focus:border-emerald-500 resize-none"
          />
          <div className="text-xs text-slate-500 mt-1 text-right">
            {description.length} / 2000
          </div>
        </div>

        {/* Photo URL */}
        <div>
          <label htmlFor="photoUrl" className="block text-sm text-slate-300 mb-2">
            Photo URL <span className="text-slate-500">(optional)</span>
          </label>
          <input
            id="photoUrl"
            type="url"
            maxLength={2048}
            value={photoUrl}
            onChange={(e) => setPhotoUrl(e.target.value)}
            placeholder="https://…"
            className="w-full rounded-lg border border-slate-700 bg-slate-950 px-4 py-2.5 text-slate-100 placeholder-slate-600 focus:outline-none focus:border-emerald-500"
          />
        </div>

        {mutation.isError && (
          <div className="rounded-lg border border-red-500/30 bg-red-500/5 px-4 py-3 text-sm text-red-400">
            {mutation.error instanceof Error
              ? mutation.error.message
              : 'Failed to submit report.'}
          </div>
        )}

        <button
          type="submit"
          disabled={mutation.isPending || !stationId || description.trim().length === 0}
          className="w-full rounded-lg bg-emerald-500 px-4 py-3 font-medium text-slate-950 hover:bg-emerald-400 transition disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {mutation.isPending ? 'Submitting…' : 'Submit report'}
        </button>
      </form>
    </div>
  );
}
