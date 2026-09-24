import { useState, type FormEvent } from 'react';
import { useMutation, useQuery } from '@tanstack/react-query';
import { AxiosError } from 'axios';
import { previewRoute } from './jobsApi';
import { fetchVehicles } from '../vehicles/vehiclesApi';
import { fetchStations } from '../stations/stationsApi';
import RoutePreviewPanel from './RoutePreviewPanel';
import type { PreviewRouteResponse } from '../../types/job';

type Props = {
  onClose: () => void;
};

export default function PreviewRouteForm({ onClose }: Props) {
  const [vehicleId, setVehicleId] = useState('');
  const [selectedStations, setSelectedStations] = useState<string[]>([]);
  const [error, setError] = useState<string | null>(null);

  const vehiclesQuery = useQuery({
    queryKey: ['vehicles'],
    queryFn: fetchVehicles,
  });

  const stationsQuery = useQuery({
    queryKey: ['stations', { page: 1, pageSize: 50 }],
    queryFn: () => fetchStations({ page: 1, pageSize: 50, sortBy: 'code' }),
  });

  const mutation = useMutation({
    mutationFn: () =>
      previewRoute({
        vehicleId,
        candidateStationIds: selectedStations,
      }),
    onError: (err) => {
      if (err instanceof AxiosError && err.response?.status === 404) {
        setError(
          (err.response.data as { detail?: string })?.detail ??
            'Vehicle not found.',
        );
      } else {
        setError('Could not preview route. Check the API connection.');
      }
    },
  });

  function toggleStation(id: string) {
    setSelectedStations((prev) =>
      prev.includes(id) ? prev.filter((x) => x !== id) : [...prev, id],
    );
  }

  function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setError(null);

    if (!vehicleId) return setError('Pick a vehicle.');
    if (selectedStations.length === 0) return setError('Pick at least one station.');

    mutation.mutate();
  }

  const activeVehicles = (vehiclesQuery.data ?? []).filter((v) => v.isActive);
  const stations = stationsQuery.data?.items ?? [];

  return (
    <div className="rounded-xl border border-sky-500/30 bg-sky-500/5 p-6 mb-8">
      <div className="flex items-start justify-between mb-4">
        <div>
          <h2 className="text-lg font-semibold tracking-tight">Preview route</h2>
          <p className="text-xs text-slate-400 mt-1">
            Read-only simulation. No collection job is created.
          </p>
        </div>
        <button
          type="button"
          onClick={onClose}
          className="text-slate-400 hover:text-slate-200 text-sm transition"
        >
          ✕ Close
        </button>
      </div>

      {error && (
        <div className="mb-4 rounded-lg border border-red-500/30 bg-red-500/10 px-4 py-3 text-sm text-red-300">
          {error}
        </div>
      )}

      <form onSubmit={handleSubmit} className="space-y-6">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label htmlFor="pv-vehicle" className="block text-sm font-medium text-slate-300 mb-2">
              Vehicle
            </label>
            <select
              id="pv-vehicle"
              required
              value={vehicleId}
              onChange={(e) => setVehicleId(e.target.value)}
              disabled={mutation.isPending || vehiclesQuery.isLoading}
              className="w-full rounded-lg border border-slate-700 bg-slate-950 px-4 py-2.5 text-slate-100 focus:outline-none focus:ring-2 focus:ring-emerald-500/40 focus:border-emerald-500/40 disabled:opacity-50"
            >
              <option value="">
                {vehiclesQuery.isLoading ? 'Loading…' : '— Select a vehicle —'}
              </option>
              {activeVehicles.map((v) => (
                <option key={v.id} value={v.id}>
                  {v.registrationNumber} ({v.capacityKilograms} kg)
                </option>
              ))}
            </select>
          </div>
        </div>

        <div>
          <div className="text-sm font-medium text-slate-300 mb-2">
            Candidate stations ({selectedStations.length} selected)
          </div>
          <div className="max-h-64 overflow-y-auto rounded-lg border border-slate-800 bg-slate-950 p-2">
            {stationsQuery.isLoading && (
              <div className="text-sm text-slate-500 p-3">Loading stations…</div>
            )}
            {stations.map((s) => (
              <label
                key={s.id}
                className="flex items-center gap-3 p-2 rounded-md hover:bg-slate-900 cursor-pointer"
              >
                <input
                  type="checkbox"
                  checked={selectedStations.includes(s.id)}
                  onChange={() => toggleStation(s.id)}
                  className="accent-emerald-500"
                />
                <span className="font-mono text-slate-200 text-sm w-20">{s.code}</span>
                <span className="text-slate-400 text-xs flex-1">{s.primaryCategory}</span>
                <span
                  className={
                    'font-mono text-xs ' +
                    (s.currentFillPercent >= 60
                      ? 'text-emerald-400'
                      : 'text-slate-500')
                  }
                >
                  {s.currentFillPercent.toFixed(0)}%
                </span>
              </label>
            ))}
          </div>
        </div>

        <div className="flex gap-3">
          <button
            type="submit"
            disabled={mutation.isPending}
            className="rounded-lg bg-sky-500 px-6 py-2.5 font-medium text-slate-950 hover:bg-sky-400 transition disabled:opacity-50"
          >
            {mutation.isPending ? 'Optimizing…' : 'Preview route'}
          </button>
          <button
            type="button"
            onClick={onClose}
            disabled={mutation.isPending}
            className="px-6 py-2.5 rounded-lg border border-slate-700 text-sm text-slate-300 hover:bg-slate-900 transition disabled:opacity-50"
          >
            Cancel
          </button>
        </div>
      </form>

      {mutation.data && <RoutePreviewPanel result={mutation.data as PreviewRouteResponse} />}
    </div>
  );
}
