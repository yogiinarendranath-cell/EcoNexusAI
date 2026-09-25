import { useMutation, useQueryClient } from '@tanstack/react-query';
import { advanceIntake } from './recyclingApi';
import type { Intake, IntakeStage } from '../../types/recycling';

const STAGE_STYLES: Record<IntakeStage, string> = {
  Received: 'bg-sky-500/10 text-sky-400 border-sky-500/30',
  Sorted: 'bg-indigo-500/10 text-indigo-400 border-indigo-500/30',
  Processed: 'bg-amber-500/10 text-amber-400 border-amber-500/30',
  Recovered: 'bg-emerald-500/10 text-emerald-400 border-emerald-500/30',
  Landfilled: 'bg-slate-500/10 text-slate-400 border-slate-500/30',
};

const NEXT_STAGE: Partial<Record<IntakeStage, IntakeStage>> = {
  Received: 'Sorted',
  Sorted: 'Processed',
  Processed: 'Recovered',
};

interface Props {
  facilityId: string;
  intake: Intake;
}

export default function IntakeRow({ facilityId, intake }: Props) {
  const qc = useQueryClient();

  const mutation = useMutation({
    mutationFn: (nextStage: IntakeStage) =>
      advanceIntake(facilityId, intake.id, {
        nextStage,
        advancedAt: new Date().toISOString(),
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['facility', facilityId] });
      qc.invalidateQueries({ queryKey: ['facilities'] });
    },
  });

  const next = NEXT_STAGE[intake.stage];
  const isTerminal = !next;

  return (
    <tr className="hover:bg-slate-900/40 transition">
      <td className="px-4 py-3 text-slate-200">{intake.material}</td>
      <td className="px-4 py-3 text-right text-slate-300">
        {intake.weightKilograms.toLocaleString()} kg
      </td>
      <td className="px-4 py-3">
        <span
          className={
            'inline-block px-2 py-0.5 rounded-full border text-xs font-medium ' +
            STAGE_STYLES[intake.stage]
          }
        >
          {intake.stage}
        </span>
      </td>
      <td className="px-4 py-3 text-slate-400 font-mono text-xs">
        {new Date(intake.recordedAt).toLocaleString()}
      </td>
      <td className="px-4 py-3 text-right">
        {isTerminal ? (
          <span className="text-xs text-slate-500">—</span>
        ) : (
          <button
            type="button"
            onClick={() => mutation.mutate(next!)}
            disabled={mutation.isPending}
            className="px-3 py-1 rounded-lg bg-sky-500 text-slate-950 font-medium text-xs hover:bg-sky-400 transition disabled:opacity-50"
          >
            {mutation.isPending ? 'Advancing…' : `Advance → ${next}`}
          </button>
        )}
      </td>
    </tr>
  );
}
