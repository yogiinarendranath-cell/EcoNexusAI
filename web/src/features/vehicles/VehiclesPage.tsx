import { useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { AxiosError } from 'axios';
import { fetchVehicles, createVehicle } from './vehiclesApi';
import UserMenu from '../auth/UserMenu';
import type { CollectionVehicle } from '../../types/vehicle';

export default function VehiclesPage() {
  const queryClient = useQueryClient();
  const [showForm, setShowForm] = useState(false);

  const { data, isLoading, isError, error, refetch, isFetching } = useQuery({
    queryKey: ['vehicles'],
    queryFn: fetchVehicles,
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
            <h1 className="text-3xl font-bold tracking-tight">Collection Vehicles</h1>
            <p className="text-sm text-slate-400 mt-1">
              Fleet vehicles available for collection routes.
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
              {showForm ? 'Cancel' : '+ Add vehicle'}
            </button>
          </div>
        </div>

        {showForm && (
          <AddVehicleForm
            onCreated={() => {
              setShowForm(false);
              queryClient.invalidateQueries({ queryKey: ['vehicles'] });
            }}
            onCancel={() => setShowForm(false)}
          />
        )}

        {isLoading && <LoadingState />}
        {isError && <ErrorState error={error} />}
        {data && data.length === 0 && !showForm && <EmptyState />}
        {data && data.length > 0 && <VehiclesTable items={data} />}

        {data && (
          <div className="mt-6 text-xs text-slate-500">
            {data.length} vehicle{data.length === 1 ? '' : 's'} total
          </div>
        )}
      </main>
    </div>
  );
}

function LoadingState() {
  return (
    <div className="rounded-xl border border-slate-800 bg-slate-900/50 p-12 text-center">
      <div className="text-slate-400 text-sm">Loading vehicles…</div>
    </div>
  );
}

function ErrorState({ error }: { error: unknown }) {
  const message = error instanceof Error ? error.message : 'Unknown error';
  return (
    <div className="rounded-xl border border-red-500/30 bg-red-500/5 p-6">
      <div className="text-red-400 font-medium">Failed to load vehicles</div>
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
      <div className="text-slate-300">No vehicles yet</div>
      <div className="text-sm text-slate-500 mt-2">
        Click "Add vehicle" above to register your first one.
      </div>
    </div>
  );
}

function VehiclesTable({ items }: { items: CollectionVehicle[] }) {
  return (
    <div className="overflow-hidden rounded-xl border border-slate-800">
      <table className="w-full text-sm">
        <thead className="bg-slate-900/70 text-slate-400">
          <tr>
            <th className="text-left px-4 py-3 font-medium">Registration</th>
            <th className="text-right px-4 py-3 font-medium">Capacity</th>
            <th className="text-left px-4 py-3 font-medium">Status</th>
            <th className="text-left px-4 py-3 font-medium font-mono text-xs">ID</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-slate-800">
          {items.map((v) => (
            <tr key={v.id} className="hover:bg-slate-900/40 transition">
              <td className="px-4 py-3 font-mono text-slate-100">{v.registrationNumber}</td>
              <td className="px-4 py-3 text-right text-slate-300">
                {v.capacityKilograms.toLocaleString()} kg
              </td>
              <td className="px-4 py-3">
                <ActiveBadge isActive={v.isActive} />
              </td>
              <td className="px-4 py-3 text-slate-500 font-mono text-xs">{v.id}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function ActiveBadge({ isActive }: { isActive: boolean }) {
  const cls = isActive
    ? 'bg-emerald-500/10 text-emerald-400 border-emerald-500/30'
    : 'bg-slate-500/10 text-slate-400 border-slate-500/30';
  const label = isActive ? 'Active' : 'Inactive';
  return (
    <span className={'inline-block px-2 py-0.5 rounded-full border text-xs font-medium ' + cls}>
      {label}
    </span>
  );
}

type AddVehicleFormProps = {
  onCreated: () => void;
  onCancel: () => void;
};

function AddVehicleForm({ onCreated, onCancel }: AddVehicleFormProps) {
  const [registrationNumber, setRegistrationNumber] = useState('');
  const [capacityKilograms, setCapacityKilograms] = useState('');
  const [error, setError] = useState<string | null>(null);

  const mutation = useMutation({
    mutationFn: () =>
      createVehicle({
        registrationNumber: registrationNumber.trim(),
        capacityKilograms: Number(capacityKilograms),
      }),
    onSuccess: () => onCreated(),
    onError: (err) => {
      if (err instanceof AxiosError && err.response?.status === 409) {
        const detail =
          (err.response.data as { detail?: string } | undefined)?.detail ??
          'A vehicle with that registration number already exists.';
        setError(detail);
      } else if (err instanceof AxiosError && err.response?.status === 400) {
        setError('Invalid input. Check the registration number and capacity.');
      } else {
        setError('Could not create vehicle. Check the API connection.');
      }
    },
  });

  function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setError(null);
    mutation.mutate();
  }

  return (
    <div className="rounded-xl border border-slate-800 bg-slate-900/50 p-6 mb-8">
      <h2 className="text-lg font-semibold tracking-tight mb-4">Add vehicle</h2>

      {error && (
        <div className="mb-4 rounded-lg border border-red-500/30 bg-red-500/10 px-4 py-3 text-sm text-red-300">
          {error}
        </div>
      )}

      <form onSubmit={handleSubmit} className="grid grid-cols-1 md:grid-cols-3 gap-4 items-end">
        <div className="md:col-span-1">
          <label htmlFor="reg" className="block text-sm font-medium text-slate-300 mb-2">
            Registration number
          </label>
          <input
            id="reg"
            type="text"
            required
            value={registrationNumber}
            onChange={(e) => setRegistrationNumber(e.target.value)}
            disabled={mutation.isPending}
            className="w-full rounded-lg border border-slate-700 bg-slate-950 px-4 py-2.5 text-slate-100 placeholder-slate-600 focus:outline-none focus:ring-2 focus:ring-emerald-500/40 focus:border-emerald-500/40 disabled:opacity-50 transition"
            placeholder="MH-12-AB-1234"
          />
        </div>

        <div className="md:col-span-1">
          <label htmlFor="cap" className="block text-sm font-medium text-slate-300 mb-2">
            Capacity (kg)
          </label>
          <input
            id="cap"
            type="number"
            required
            min="1"
            step="1"
            value={capacityKilograms}
            onChange={(e) => setCapacityKilograms(e.target.value)}
            disabled={mutation.isPending}
            className="w-full rounded-lg border border-slate-700 bg-slate-950 px-4 py-2.5 text-slate-100 placeholder-slate-600 focus:outline-none focus:ring-2 focus:ring-emerald-500/40 focus:border-emerald-500/40 disabled:opacity-50 transition"
            placeholder="5000"
          />
        </div>

        <div className="md:col-span-1 flex gap-2">
          <button
            type="submit"
            disabled={mutation.isPending}
            className="flex-1 rounded-lg bg-emerald-500 px-4 py-2.5 font-medium text-slate-950 hover:bg-emerald-400 transition disabled:opacity-50"
          >
            {mutation.isPending ? 'Creating…' : 'Create'}
          </button>
          <button
            type="button"
            onClick={onCancel}
            disabled={mutation.isPending}
            className="px-4 py-2.5 rounded-lg border border-slate-700 text-sm text-slate-300 hover:bg-slate-900 transition disabled:opacity-50"
          >
            Cancel
          </button>
        </div>
      </form>
    </div>
  );
}
