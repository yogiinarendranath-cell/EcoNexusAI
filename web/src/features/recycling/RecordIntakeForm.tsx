import { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { recordIntake } from './recyclingApi';
import type { WasteMaterial } from '../../types/recycling';

interface Props {
  facilityId: string;
  onCreated: () => void;
  onCancel: () => void;
}

const MATERIALS: WasteMaterial[] = [
  'Organic',
  'Plastic',
  'Paper',
  'Glass',
  'Metal',
  'EWaste',
  'General',
  'Hazardous',
];

export default function RecordIntakeForm({ facilityId, onCreated, onCancel }: Props) {
  const qc = useQueryClient();
  const [material, setMaterial] = useState<WasteMaterial>('Plastic');
  const [weight, setWeight] = useState('100');

  const mutation = useMutation({
    mutationFn: (payload: { material: WasteMaterial; weightKilograms: number; recordedAt: string }) =>
      recordIntake(facilityId, payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['facility', facilityId] });
      qc.invalidateQueries({ queryKey: ['facilities'] });
      onCreated();
    },
  });

  const submit = (e: React.FormEvent) => {
    e.preventDefault();
    mutation.mutate({
      material,
      weightKilograms: Number(weight),
      recordedAt: new Date().toISOString(),
    });
  };

  return (
    <form
      onSubmit={submit}
      className="rounded-xl border border-slate-800 bg-slate-900/50 p-6 mb-6 space-y-4"
    >
      <h2 className="text-lg font-semibold">Record intake batch</h2>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <label className="block">
          <span className="text-sm text-slate-400">Material</span>
          <select
            value={material}
            onChange={(e) => setMaterial(e.target.value as WasteMaterial)}
            className="mt-1 w-full rounded-lg bg-slate-950 border border-slate-800 px-3 py-2 text-slate-100 focus:outline-none focus:border-emerald-500"
          >
            {MATERIALS.map((m) => (
              <option key={m} value={m}>
                {m}
              </option>
            ))}
          </select>
        </label>

        <label className="block">
          <span className="text-sm text-slate-400">Weight (kg)</span>
          <input
            required
            type="number"
            min={1}
            step="any"
            value={weight}
            onChange={(e) => setWeight(e.target.value)}
            className="mt-1 w-full rounded-lg bg-slate-950 border border-slate-800 px-3 py-2 text-slate-100 focus:outline-none focus:border-emerald-500"
          />
        </label>
      </div>

      {mutation.isError && (
        <div className="rounded-lg border border-red-500/30 bg-red-500/5 px-4 py-3 text-sm text-red-400">
          Failed to record intake.
        </div>
      )}

      <div className="flex items-center gap-3 pt-2">
        <button
          type="submit"
          disabled={mutation.isPending}
          className="px-4 py-2 rounded-lg bg-emerald-500 text-slate-950 font-medium text-sm hover:bg-emerald-400 transition disabled:opacity-50"
        >
          {mutation.isPending ? 'Recording…' : 'Record intake'}
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
