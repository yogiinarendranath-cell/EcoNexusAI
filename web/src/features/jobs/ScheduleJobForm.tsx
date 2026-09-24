import { useState, type FormEvent } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { AxiosError } from 'axios';
import { scheduleJob } from './jobsApi';
import { fetchVehicles } from '../vehicles/vehiclesApi';
import { fetchStations } from '../stations/stationsApi';

type Props = {
  onCreated: () => void;
  onCancel: () => void;
};

export default function ScheduleJobForm({ onCreated, onCancel }: Props) {
  const queryClient = useQueryClient();

  const [vehicleId, setVehicleId] = useState('');
  const [scheduledFor, setScheduledFor] = useState('');
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
      scheduleJob({
        vehicleId,
        scheduledFor: new Date(scheduledFor).toISOString(),
        stops: selectedStations.map((stationId) => ({ stationId })),
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['jobs'] });
      onCreated();
    },
    onError: (err) => {
      if (err instanceof AxiosError && err.response?.status === 409) {
        setError(
          (err.response.data as { detail?: string })?.detail ??
            'Conflict scheduling job.',
        );
      } else if (err instanceof AxiosError && err.response?.status === 404) {
        setError(
          (err.response.data as { detail?: string })?.detail ??
            'A referenced vehicle or station was not found.',
        );
      } else {
        setError('Could not schedule job. Check the API connection.');
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
    if (!scheduledFor) return setError('Pick a scheduled time.');
    if (selectedStations.length === 0) return setError('Pick at least one station.');

    mutation.mutate();
  }

  const activeVehicles = (vehiclesQuery.data ?? []).filter((v) => v.isActive);
  const stations = stationsQuery.data?.items ?? [];

  return (
    <div className="rounded-xl border border-slate-800 bg-slate-900/50 p-6 mb-8">
      <h2 className="text-lg font-semibold tracking-tight mb-4">Schedule job</h2>

      {error && (
        <div className="mb-4 rounded-lg border border-red-500/30 bg-red-500/10 px-4 py-3 text-sm text-red-300">
          {error}
        </div>
      )}

      <form onSubmit={handleSubmit} className="space-y-6">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label htmlFor="vehicle" className="block text-sm font-medium text-slate-300 mb-2">
              Vehicle
            </label>
            <select
              id="vehicle"
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

          <div>
            <label htmlFor="scheduled" className="block text-sm font-medium text-slate-300 mb-2">
              Scheduled for
            </label>
            <input
              id="scheduled"
              type="datetime-local"
              required
              value={scheduledFor}
              onChange={(e) => setScheduledFor(e.target.value)}
              disabled={mutation.isPending}
              className="w-full rounded-lg border border-slate-700 bg-slate-950 px-4 py-2.5 text-slate-100 focus:outline-none focus:ring-2 focus:ring-emerald-500/40 focus:border-emerald-500/40 disabled:opacity-50"
            />
          </div>
        </div>

        <div>
          <div className="text-sm font-medium text-slate-300 mb-2">
            Stations ({selectedStations.length} selected)
          </div>
          <div className="max-h-64 overflow-y-auto rounded-lg border border-slate-800 bg-slate-950 p-2">
            {stationsQuery.isLoading && (
              <div className="text-sm text-slate-500 p-3">Loading stations…</div>
            )}
            {stations.map((s) => {
              const checked = selectedStations.includes(s.id);
              const order = selectedStations.indexOf(s.id);
              return (
                <label
                  key={s.id}
                  className="flex items-center gap-3 p-2 rounded-md hover:bg-slate-900 cursor-pointer"
                >
                  <input
                    type="checkbox"
                    checked={checked}
                    onChange={() => toggleStation(s.id)}
                    className="accent-emerald-500"
                  />
                  <span className="font-mono text-xs text-slate-500 w-10">
                    {checked ? order + 1 : '—'}
                  </span>
                  <span className="font-mono text-slate-200 text-sm">{s.code}</span>
                  <span className="text-slate-400 text-xs">{s.primaryCategory}</span>
                </label>
              );
            })}
          </div>
          <div className="text-xs text-slate-500 mt-2">
            Stops are assigned sequence numbers in the order you select them.
          </div>
        </div>

        <div className="flex gap-3">
          <button
            type="submit"
            disabled={mutation.isPending}
            className="rounded-lg bg-emerald-500 px-6 py-2.5 font-medium text-slate-950 hover:bg-emerald-400 transition disabled:opacity-50"
          >
            {mutation.isPending ? 'Scheduling…' : 'Schedule job'}
          </button>
          <button
            type="button"
            onClick={onCancel}
            disabled={mutation.isPending}
            className="px-6 py-2.5 rounded-lg border border-slate-700 text-sm text-slate-300 hover:bg-slate-900 transition disabled:opacity-50"
          >
            Cancel
          </button>
        </div>
      </form>
    </div>
  );
}
