import { Link, useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { fetchMyProfile } from './citizenApi';
import { useAuthStore } from '../auth/authStore';

export default function CitizenProfile() {
  const user = useAuthStore((s) => s.user);
  const clear = useAuthStore((s) => s.clear);
  const navigate = useNavigate();

  const { data: profile, isLoading, isError, error, refetch, isFetching } = useQuery({
    queryKey: ['citizen', 'profile'],
    queryFn: fetchMyProfile,
  });

  const handleSignOut = () => {
    clear();
    navigate('/login', { replace: true });
  };

  return (
    <div className="max-w-md mx-auto px-5 pt-8 pb-4">
      <div className="mb-6">
        <div className="text-xs uppercase tracking-wide text-emerald-400">
          EcoNexus AI
        </div>
        <h1 className="text-2xl font-bold mt-1">Your profile</h1>
        <p className="text-sm text-slate-400 mt-1">
          Account details and recycling stats.
        </p>
      </div>

      {isLoading && <LoadingCard label="Loading your profile…" />}
      {isError && (
        <ErrorCard
          message={error instanceof Error ? error.message : 'Failed to load profile'}
          onRetry={() => refetch()}
          retrying={isFetching}
        />
      )}

      {profile && (
        <>
          {/* Identity card */}
          <div className="rounded-2xl border border-slate-800 bg-slate-900/50 p-6 mb-4">
            <div className="flex items-center gap-4">
              <div className="w-14 h-14 rounded-full bg-emerald-500 flex items-center justify-center text-2xl font-bold text-slate-950">
                {(profile.displayName ?? user?.displayName ?? '?').charAt(0).toUpperCase()}
              </div>
              <div className="flex-1 min-w-0">
                <div className="text-lg font-semibold text-slate-100 truncate">
                  {profile.displayName ?? user?.displayName ?? 'Citizen'}
                </div>
                <div className="text-xs text-slate-500 truncate">
                  {user?.email ?? '—'}
                </div>
              </div>
            </div>
          </div>

          {/* Stats */}
          <div className="grid grid-cols-2 gap-3 mb-4">
            <StatCard label="Green points" value={profile.greenPointsBalance.toString()} />
            <StatCard
              label="Streak"
              value={
                profile.currentStreakDays > 0
                  ? `${profile.currentStreakDays} days 🔥`
                  : '—'
              }
            />
            <StatCard label="Total earned" value={profile.totalEarned.toString()} />
            <StatCard label="Total redeemed" value={profile.totalRedeemed.toString()} />
            <StatCard
              label="Transactions"
              value={profile.totalTransactions.toString()}
            />
            <StatCard
              label="Member since"
              value={new Date(profile.createdAt).toLocaleDateString(undefined, {
                month: 'short',
                year: 'numeric',
              })}
            />
          </div>

          {/* Home location */}
          <div className="rounded-2xl border border-slate-800 bg-slate-900/50 p-6 mb-6">
            <div className="text-xs uppercase tracking-wide text-slate-500 mb-2">
              Home location
            </div>
            {profile.homeAddress || profile.homeLatitude != null ? (
              <div className="text-sm text-slate-300">
                {profile.homeAddress ?? 'Coordinates set'}
                {profile.homeLatitude != null && profile.homeLongitude != null && (
                  <div className="text-xs text-slate-500 font-mono mt-1">
                    {profile.homeLatitude.toFixed(4)}, {profile.homeLongitude.toFixed(4)}
                  </div>
                )}
              </div>
            ) : (
              <div className="text-sm text-slate-500">
                Not set — add one to get better nearby recommendations.
              </div>
            )}
          </div>

          {/* Actions */}
          <div className="space-y-2">
            <Link
              to="/app/points"
              className="flex items-center gap-4 rounded-xl border border-slate-800 bg-slate-900/50 p-4 hover:border-emerald-500/40 transition"
            >
              <div className="text-2xl w-10 text-center">⭐</div>
              <div className="flex-1">
                <div className="text-sm font-medium text-slate-100">
                  View your points
                </div>
                <div className="text-xs text-slate-500 mt-0.5">
                  History and available rewards
                </div>
              </div>
              <div className="text-slate-500">→</div>
            </Link>

            <button
              type="button"
              onClick={handleSignOut}
              className="w-full flex items-center gap-4 rounded-xl border border-red-500/30 bg-red-500/5 p-4 hover:bg-red-500/10 transition"
            >
              <div className="text-2xl w-10 text-center">🚪</div>
              <div className="flex-1 text-left">
                <div className="text-sm font-medium text-red-400">Sign out</div>
                <div className="text-xs text-slate-500 mt-0.5">
                  End your session on this device
                </div>
              </div>
            </button>
          </div>
        </>
      )}

      <div className="mt-8 text-xs text-slate-500 text-center">
        <Link to="/app" className="hover:text-slate-300 transition">
          ← Back to home
        </Link>
      </div>
    </div>
  );
}

function StatCard({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-xl border border-slate-800 bg-slate-900/50 p-4">
      <div className="text-xs text-slate-500">{label}</div>
      <div className="text-lg font-semibold mt-1 text-slate-100">{value}</div>
    </div>
  );
}

function LoadingCard({ label }: { label: string }) {
  return (
    <div className="rounded-2xl border border-slate-800 bg-slate-900/50 p-12 text-center">
      <div className="text-slate-400 text-sm">{label}</div>
    </div>
  );
}

function ErrorCard({
  message,
  onRetry,
  retrying,
}: {
  message: string;
  onRetry: () => void;
  retrying: boolean;
}) {
  return (
    <div className="rounded-2xl border border-red-500/30 bg-red-500/5 p-6">
      <div className="text-red-400 font-medium">Failed to load profile</div>
      <div className="text-sm text-slate-400 mt-2">{message}</div>
      <button
        type="button"
        onClick={onRetry}
        disabled={retrying}
        className="mt-4 px-4 py-2 rounded-lg bg-slate-800 text-slate-100 text-sm hover:bg-slate-700 transition disabled:opacity-50"
      >
        {retrying ? 'Retrying…' : 'Retry'}
      </button>
    </div>
  );
}
