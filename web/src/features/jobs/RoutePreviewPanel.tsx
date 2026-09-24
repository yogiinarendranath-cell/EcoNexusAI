import type { PreviewRouteResponse } from '../../types/job';

type Props = {
  result: PreviewRouteResponse;
};

export default function RoutePreviewPanel({ result }: Props) {
  const capacityUsedPct =
    result.vehicleCapacityKilograms > 0
      ? (result.totalEstimatedWeightKilograms / result.vehicleCapacityKilograms) * 100
      : 0;

  if (result.stops.length === 0) {
    return (
      <div className="mt-6 rounded-lg border border-amber-500/30 bg-amber-500/5 p-5">
        <div className="text-amber-300 font-medium">No eligible stops</div>
        <div className="text-sm text-slate-400 mt-2">
          None of the selected stations meet the minimum fill threshold (60%).
          Give them a reading above 60% first.
        </div>
      </div>
    );
  }

  return (
    <div className="mt-6">
      <div className="flex items-baseline justify-between mb-3">
        <h3 className="text-md font-semibold text-slate-100">Optimized route</h3>
        <div className="text-xs text-slate-400">
          {result.stops.length} stop{result.stops.length === 1 ? '' : 's'} ·{' '}
          <span className="font-mono text-sky-300">
            {result.totalEstimatedWeightKilograms.toLocaleString()} kg
          </span>{' '}
          of{' '}
          <span className="font-mono">
            {result.vehicleCapacityKilograms.toLocaleString()} kg
          </span>{' '}
          ({capacityUsedPct.toFixed(0)}%)
        </div>
      </div>

      {result.skippedCount > 0 && (
        <div className="mb-3 rounded-lg border border-amber-500/20 bg-amber-500/5 px-3 py-2 text-xs text-amber-300">
          {result.skippedCount} station{result.skippedCount === 1 ? '' : 's'} could not be
          found and were skipped.
        </div>
      )}

      <ol className="space-y-2">
        {result.stops.map((stop) => (
          <li
            key={stop.stationId}
            className="flex items-center gap-4 rounded-lg border border-slate-800 bg-slate-900/40 p-3"
          >
            <div className="w-9 h-9 rounded-full bg-sky-500 text-slate-950 flex items-center justify-center font-semibold text-sm shrink-0">
              {stop.sequence}
            </div>
            <div className="flex-1 min-w-0">
              <div className="flex items-baseline gap-3">
                <span className="font-mono text-slate-100 font-medium">{stop.stationCode}</span>
                <span className="text-xs text-slate-500">
                  fill {stop.fillLevelPercent.toFixed(0)}%
                </span>
              </div>
              <div className="text-xs text-slate-500 mt-1 font-mono truncate">
                {stop.stationId}
              </div>
            </div>
            <div className="text-right shrink-0">
              <div className="text-slate-200 font-mono text-sm">
                {stop.estimatedWeightKilograms.toLocaleString()} kg
              </div>
              <div className="text-xs text-slate-500">est. weight</div>
            </div>
          </li>
        ))}
      </ol>
    </div>
  );
}
