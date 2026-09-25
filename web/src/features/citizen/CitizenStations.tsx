import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { fetchMyProfile } from './citizenApi';
import { fetchStations } from '../stations/stationsApi';
import type { StationListItem } from '../../types/station';

const DEFAULT_REFERENCE = { lat: 18.5204, lng: 73.8567 }; // Pune center

/**
 * Haversine distance in meters between two points on Earth.
 * Standard formula, good enough for sub-100km proximity within a city.
 */
function haversineMeters(
  lat1: number,
  lng1: number,
  lat2: number,
  lng2: number,
): number {
  const R = 6_371_000; // Earth radius in meters
  const toRad = (deg: number) => (deg * Math.PI) / 180;

  const dLat = toRad(lat2 - lat1);
  const dLng = toRad(lng2 - lng1);

  const a =
    Math.sin(dLat / 2) ** 2 +
    Math.cos(toRad(lat1)) * Math.cos(toRad(lat2)) * Math.sin(dLng / 2) ** 2;

  return 2 * R * Math.asin(Math.sqrt(a));
}

function formatDistance(meters: number): string {
  if (meters < 1000) return `${Math.round(meters)} m`;
  return `${(meters / 1000).toFixed(1)} km`;
}

export default function CitizenStations() {
  const { data: profile } = useQuery({
    queryKey: ['citizen', 'profile'],
    queryFn: fetchMyProfile,
  });

  const { data: stations, isLoading, isError, error, refetch, isFetching } = useQuery({
    queryKey: ['stations', 'citizen-list'],
    queryFn: () => fetchStations({ pageSize: 50 }),
  });

  // Reference point: profile home if set, else Pune center
  const refLat = profile?.homeLatitude ?? DEFAULT_REFERENCE.lat;
  const refLng = profile?.homeLongitude ?? DEFAULT_REFERENCE.lng;

  const sorted = stations?.items
    .map((s) => ({
      station: s,
      distanceMeters: haversineMeters(refLat, refLng, s.latitude, s.longitude),
    }))
    .sort((a, b) => a.distanceMeters - b.distanceMeters) ?? [];

  return (
    <div className="max-w-md mx-auto px-5 pt-8 pb-4">
      <div className="flex items-end justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Nearby Stations</h1>
          <p className="text-sm text-slate-400 mt-1">
            {profile?.homeLatitude
              ? 'Sorted by distance from your home'
              : 'Sorted by distance from central Pune'}
          </p>
        </div>
        <button
          type="button"
          onClick={() => refetch()}
          disabled={isFetching}
          className="px-3 py-1.5 rounded-lg border border-slate-700 text-xs text-slate-200 hover:bg-slate-900 transition disabled:opacity-50"
        >
          {isFetching ? '…' : 'Refresh'}
        </button>
      </div>

      {isLoading && (
        <div className="rounded-xl border border-slate-800 bg-slate-900/50 p-12 text-center">
          <div className="text-slate-400 text-sm">Loading stations…</div>
        </div>
      )}

      {isError && (
        <div className="rounded-xl border border-red-500/30 bg-red-500/5 p-6">
          <div className="text-red-400 font-medium">Failed to load stations</div>
          <div className="text-sm text-slate-400 mt-2">
            {error instanceof Error ? error.message : 'Unknown error'}
          </div>
        </div>
      )}

      {!isLoading && !isError && sorted.length === 0 && (
        <div className="rounded-xl border border-slate-800 bg-slate-900/50 p-12 text-center">
          <div className="text-slate-300">No stations found</div>
          <div className="text-sm text-slate-500 mt-2">
            Try again later or report a missing station.
          </div>
        </div>
      )}

      <div className="space-y-2">
        {sorted.map(({ station, distanceMeters }) => (
          <StationRow
            key={station.id}
            station={station}
            distanceMeters={distanceMeters}
          />
        ))}
      </div>
    </div>
  );
}

function StationRow({
  station,
  distanceMeters,
}: {
  station: StationListItem;
  distanceMeters: number;
}) {
  const fillTone =
    station.currentFillPercent >= 90
      ? 'critical'
      : station.currentFillPercent >= 70
      ? 'high'
      : 'normal';

  return (
    <Link
      to={`/app/report`}
      state={{ stationId: station.id, stationCode: station.code }}
      className="block rounded-xl border border-slate-800 bg-slate-900/50 p-4 hover:border-emerald-500/40 transition"
    >
      <div className="flex items-center justify-between">
        <div>
          <div className="text-base font-semibold text-slate-100 font-mono">
            {station.code}
          </div>
          <div className="text-xs text-slate-500 mt-0.5">
            {station.primaryCategory} · {formatDistance(distanceMeters)}
          </div>
        </div>
        <div className="text-right">
          <div className="text-xs text-slate-500">Fill</div>
          <div
            className={
              'text-lg font-semibold ' +
              (fillTone === 'critical'
                ? 'text-red-400'
                : fillTone === 'high'
                ? 'text-amber-400'
                : 'text-emerald-400')
            }
          >
            {station.currentFillPercent.toFixed(0)}%
          </div>
        </div>
      </div>
    </Link>
  );
}
