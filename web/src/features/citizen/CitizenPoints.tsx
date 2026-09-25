import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  fetchMyPointHistory,
  fetchMyProfile,
  fetchRewards,
  redeemReward,
} from './citizenApi';
import type { PointTransaction, Reward } from '../../types/citizen';

export default function CitizenPoints() {
  const [redeemingId, setRedeemingId] = useState<string | null>(null);
  const qc = useQueryClient();

  const profileQuery = useQuery({
    queryKey: ['citizen', 'profile'],
    queryFn: fetchMyProfile,
  });

  const historyQuery = useQuery({
    queryKey: ['citizen', 'points', 'history'],
    queryFn: () => fetchMyPointHistory(1, 50),
  });

  const rewardsQuery = useQuery({
    queryKey: ['citizen', 'rewards'],
    queryFn: fetchRewards,
  });

  const redeem = useMutation({
    mutationFn: (rewardId: string) => redeemReward(rewardId),
    onMutate: (rewardId) => setRedeemingId(rewardId),
    onSettled: () => setRedeemingId(null),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['citizen', 'profile'] });
      qc.invalidateQueries({ queryKey: ['citizen', 'points', 'history'] });
      qc.invalidateQueries({ queryKey: ['citizen', 'rewards'] });
    },
  });

  return (
    <div className="max-w-md mx-auto px-5 pt-8 pb-4">
      <div className="mb-6">
        <div className="text-xs uppercase tracking-wide text-emerald-400">
          EcoNexus AI
        </div>
        <h1 className="text-2xl font-bold mt-1">Points & rewards</h1>
        <p className="text-sm text-slate-400 mt-1">
          Earn by recycling. Redeem for real-world impact.
        </p>
      </div>

      {/* Balance card */}
      {profileQuery.isLoading && <LoadingCard label="Loading your balance…" />}
      {profileQuery.isError && (
        <ErrorCard
          message="Failed to load balance"
          onRetry={() => profileQuery.refetch()}
          retrying={profileQuery.isFetching}
        />
      )}
      {profileQuery.data && (
        <div className="rounded-2xl border border-emerald-500/30 bg-emerald-500/5 p-6 mb-6">
          <div className="text-xs text-emerald-400 font-medium uppercase tracking-wide">
            Available balance
          </div>
          <div className="text-5xl font-bold mt-2 text-emerald-300">
            {profileQuery.data.greenPointsBalance}
          </div>
          <div className="text-xs text-slate-400 mt-2">
            {profileQuery.data.currentStreakDays > 0
              ? `${profileQuery.data.currentStreakDays}-day streak 🔥`
              : 'Visit a station to start your streak'}
          </div>
        </div>
      )}

      {/* Rewards */}
      <div className="text-xs uppercase tracking-wide text-slate-500 mb-3">
        Rewards
      </div>

      {rewardsQuery.isLoading && <LoadingCard label="Loading rewards…" />}
      {rewardsQuery.isError && (
        <ErrorCard
          message="Failed to load rewards"
          onRetry={() => rewardsQuery.refetch()}
          retrying={rewardsQuery.isFetching}
        />
      )}
      {rewardsQuery.data && rewardsQuery.data.length === 0 && (
        <div className="rounded-2xl border border-slate-800 bg-slate-900/50 p-6 text-center text-sm text-slate-500">
          No rewards available yet.
        </div>
      )}
      {rewardsQuery.data && rewardsQuery.data.length > 0 && (
        <div className="space-y-2 mb-8">
          {rewardsQuery.data.map((reward: Reward) => (
            <RewardCard
              key={reward.id}
              reward={reward}
              canAfford={
                (profileQuery.data?.greenPointsBalance ?? 0) >= reward.costInPoints
              }
              isRedeeming={redeemingId === reward.id}
              onRedeem={() => redeem.mutate(reward.id)}
            />
          ))}
        </div>
      )}

      {/* Recent transactions */}
      <div className="text-xs uppercase tracking-wide text-slate-500 mb-3">
        Recent activity
      </div>

      {historyQuery.isLoading && <LoadingCard label="Loading history…" />}
      {historyQuery.isError && (
        <ErrorCard
          message="Failed to load history"
          onRetry={() => historyQuery.refetch()}
          retrying={historyQuery.isFetching}
        />
      )}
      {historyQuery.data && historyQuery.data.items.length === 0 && (
        <div className="rounded-2xl border border-slate-800 bg-slate-900/50 p-6 text-center text-sm text-slate-500">
          No transactions yet.
        </div>
      )}
      {historyQuery.data && historyQuery.data.items.length > 0 && (
        <div className="space-y-1.5">
          {historyQuery.data.items.map((tx: PointTransaction) => (
            <TransactionRow key={tx.id} transaction={tx} />
          ))}
        </div>
      )}

      <div className="mt-8 text-xs text-slate-500 text-center">
        <Link to="/app" className="hover:text-slate-300 transition">
          ← Back to home
        </Link>
      </div>
    </div>
  );
}

function RewardCard({
  reward,
  canAfford,
  isRedeeming,
  onRedeem,
}: {
  reward: Reward;
  canAfford: boolean;
  isRedeeming: boolean;
  onRedeem: () => void;
}) {
  return (
    <div className="rounded-xl border border-slate-800 bg-slate-900/50 p-4 flex items-center gap-4">
      <div className="flex-1 min-w-0">
        <div className="text-sm font-medium text-slate-100 truncate">
          {reward.name}
        </div>
        <div className="text-xs text-slate-500 mt-0.5 line-clamp-2">
          {reward.description}
        </div>
        <div className="text-xs text-emerald-400 font-medium mt-1">
          {reward.costInPoints.toLocaleString()} pts
        </div>
      </div>
      <button
        type="button"
        onClick={onRedeem}
        disabled={!canAfford || isRedeeming || !reward.isActive}
        className={
          'px-3 py-2 rounded-lg text-xs font-medium transition whitespace-nowrap ' +
          (canAfford && reward.isActive
            ? 'bg-emerald-500 text-slate-950 hover:bg-emerald-400'
            : 'bg-slate-800 text-slate-500 cursor-not-allowed')
        }
      >
        {isRedeeming ? 'Redeeming…' : 'Redeem'}
      </button>
    </div>
  );
}

function TransactionRow({ transaction }: { transaction: PointTransaction }) {
  const isPositive = transaction.signedDelta > 0;
  return (
    <div className="rounded-xl border border-slate-800 bg-slate-900/50 px-4 py-3 flex items-center gap-3">
      <div
        className={
          'text-sm font-mono font-medium w-14 text-right ' +
          (isPositive ? 'text-emerald-400' : 'text-red-400')
        }
      >
        {isPositive ? '+' : ''}
        {transaction.signedDelta}
      </div>
      <div className="flex-1 min-w-0">
        <div className="text-xs text-slate-300 truncate">
          {transaction.description}
        </div>
        <div className="text-[10px] text-slate-500 mt-0.5">
          {new Date(transaction.occurredAt).toLocaleString()}
        </div>
      </div>
    </div>
  );
}

function LoadingCard({ label }: { label: string }) {
  return (
    <div className="rounded-2xl border border-slate-800 bg-slate-900/50 p-8 text-center">
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
      <div className="text-red-400 font-medium">{message}</div>
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
