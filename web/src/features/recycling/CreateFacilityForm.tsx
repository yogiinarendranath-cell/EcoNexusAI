import { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { createFacility } from './recyclingApi';

interface Props {
  onCreated: () => void;
  onCancel: () => void;
}

export default function CreateFacilityForm({ onCreated, onCancel }: Props) {
  const qc = useQueryClient();
  const [name, setName] = useState('');
  const [latitude, setLatitude] = useState('18.5204');
  const [longitude, setLongitude] = useState('73.8567');
  const [capacity, setCapacity] = useState('20000');

  const mutation = useMutation({
    mutationFn: createFacility,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['facilities'] });
      onCreated();
    },
  });

  const submit = (e: React.FormEvent) => {
    e.preventDefault();
    mutation.mutate({
      name: name.trim(),
      latitude: Number(latitude),
      longitude: Number(longitude),
      dailyCapacityKilograms: Number(capacity),
    });
  };

  return (
    <form
      onSubmit={submit}
      className="rounded-xl border border-slate-800 bg-slate-900/50 p-6 mb-6 space-y-4"
    >
      <h2 className="text-lg font-semibold">New recycling facility</h2>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <label className="block">
          <span className="text-sm text-slate-400">Name</span>
          <input
            required
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="Pune Central Recycling"
            className="mt-1 w-full rounded-lg bg-slate-950 border border-slate-800 px-3 py-2 text-slate-100 focus:outline-none focus:border-emerald-500"
          />
        </label>

        <label className="block">
          <span className="text-sm text-slate-400">Daily capacity (kg)</span>
          <input
            required
            type="number"
            min={1}
            value={capacity}
            onChange={(e) => setCapacity(e.target.value)}
            className="mt-1 w-full rounded-lg bg-slate-950 border border-slate-800 px-3 py-2 text-slate-100 focus:outline-none focus:border-emerald-500"
          />
        </label>

        <label className="block">
          <span className="text-sm text-slate-400">Latitude</span>
          <input
            required
            type="number"
            step="any"
            min={-90}
            max={90}
            value={latitude}
            onChange={(e) => setLatitude(e.target.value)}
            className="mt-1 w-full rounded-lg bg-slate-950 border border-slate-800 px-3 py-2 text-slate-100 focus:outline-none focus:border-emerald-500"
          />
        </label>

        <label className="block">
          <span className="text-sm text-slate-400">Longitude</span>
          <input
            required
            type="number"
            step="any"
            min={-180}
            max={180}
            value={longitude}
            onChange={(e) => setLongitude(e.target.value)}
            className="mt-1 w-full rounded-lg bg-slate-950 border border-slate-800 px-3 py-2 text-slate-100 focus:outline-none focus:border-emerald-500"
          />
        </label>
      </div>

      {mutation.isError && (
        <div className="rounded-lg border border-red-500/30 bg-red-500/5 px-4 py-3 text-sm text-red-400">
          Failed to create facility. The name may already exist.
        </div>
      )}

      <div className="flex items-center gap-3 pt-2">
        <button
          type="submit"
          disabled={mutation.isPending}
          className="px-4 py-2 rounded-lg bg-emerald-500 text-slate-950 font-medium text-sm hover:bg-emerald-400 transition disabled:opacity-50"
        >
          {mutation.isPending ? 'Creating…' : 'Create facility'}
        </button>
        <button
          type="button"
          onClick={onCancel}
          className="px-4 py-2 rounded-lg border border-slate-700 text-sm text-slate-300 hover:bg-slate-900 transition"
        >
          Cancel
        </button>
      </div>
    </form>
  );
}
