import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { fetchMyProfile } from './citizenApi';
import { useAuthStore } from '../auth/authStore';

export default function CitizenHome() {
  const user = useAuthStore((s) => s.user);

  const { data: profile, isLoading, isError, error, refetch, isFetching } = useQuery({
    queryKey: ['citizen', 'profile'],
    queryFn: fetchMyProfile,
  });

  return (
    <div className="max-w-md mx-auto px-5 pt-8 pb-4">
      {/* Greeting */}
      <div className="mb-6">
        <div className="text-xs uppercase tracking-wide text-emerald-400">
          EcoNexus AI
        </div>
        <h1 className="text-2xl font-bold mt-1">
          Hello, {user?.displayName ?? 'Citizen'} 👋
        </h1>
        <p className="text-sm text-slate-400 mt-1">
          Track your recycling impact and green points.
        </p>
      </div>

      {isLoading && <LoadingCard />}
      {isError && <ErrorCard error={error} onRetry={() => refetch()} retrying={isFetching} />}

      {profile && (
        <>
          {/* Points card */}
          <div className="rounded-2xl border border-emerald-500/30 bg-emerald-500/5 p-6 mb-4">
            <div className="text-xs text-emerald-400 font-medium uppercase tracking-wide">
              Green Points
            </div>
            <div className="text-5xl font-bold mt-2 text-emerald-300">
              {profile.greenPointsBalance}
            </div>
            <div className="text-xs text-slate-400 mt-2">
              {profile.currentStreakDays > 0
                ? `${profile.currentStreakDays}-day streak 🔥`
                : 'Visit a station to start your streak'}
            </div>
          </div>

          {/* Stats row */}
          <div className="grid grid-cols-2 gap-3 mb-6">
            <StatCard label="Total earned" value={profile.totalEarned.toString()} />
            <StatCard label="Total redeemed" value={profile.totalRedeemed.toString()} />
            <StatCard label="Transactions" value={profile.totalTransactions.toString()} />
            <StatCard
              label="Member since"
              value={new Date(profile.createdAt).toLocaleDateString(undefined, {
                month: 'short',
                year: 'numeric',
              })}
            />
          </div>

          {/* Quick actions */}
          <div className="text-xs uppercase tracking-wide text-slate-500 mb-3">
            Quick actions
          </div>
          <div className="space-y-2">
            <QuickAction
              to="/app/stations"
              icon="📍"
              title="Find nearby stations"
              subtitle="See fill levels and start a visit"
            />
            <QuickAction
              to="/app/report"
              icon="📸"
              title="Report an issue"
              subtitle="Overflowing bin, damaged station, illegal dumping"
            />
            <QuickAction
              to="/app/points"
              icon="🎁"
              title="Redeem rewards"
              subtitle="Turn green points into real-world impact"
            />
          </div>
        </>
      )}

      <div className="mt-8 text-xs text-slate-500 text-center">
        <Link to="/" className="hover:text-slate-300 transition">
          ← Back to EcoNexus
        </Link>
      </div>
    </div>
  );
}

function LoadingCard() {
  return (
    <div className="rounded-2xl border border-slate-800 bg-slate-900/50 p-12 text-center">
      <div className="text-slate-400 text-sm">Loading your profile…</div>
    </div>
  );
}

function ErrorCard({
  error,
  onRetry,
  retrying,
}: {
  error: unknown;
  onRetry: () => void;
  retrying: boolean;
}) {
  const message = error instanceof Error ? error.message : 'Unknown error';
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

function StatCard({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-xl border border-slate-800 bg-slate-900/50 p-4">
      <div className="text-xs text-slate-500">{label}</div>
      <div className="text-lg font-semibold mt-1 text-slate-100">{value}</div>
    </div>
  );
}

function QuickAction({
  to,
  icon,
  title,
  subtitle,
}: {
  to: string;
  icon: string;
  title: string;
  subtitle: string;
}) {
  return (
    <Link
      to={to}
      className="flex items-center gap-4 rounded-xl border border-slate-800 bg-slate-900/50 p-4 hover:border-emerald-500/40 transition"
    >
      <div className="text-2xl w-10 text-center">{icon}</div>
      <div className="flex-1">
        <div className="text-sm font-medium text-slate-100">{title}</div>
        <div className="text-xs text-slate-500 mt-0.5">{subtitle}</div>
      </div>
      <div className="text-slate-500">→</div>
    </Link>
  );
}
