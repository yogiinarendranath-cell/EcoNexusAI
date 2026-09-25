/** Metrics-free profile data as returned by GET /api/v1/citizen/profile. */
export interface CitizenProfile {
  id: string;
  userId: string;
  displayName: string;
  homeAddress: string | null;
  homeLatitude: number | null;
  homeLongitude: number | null;
  greenPointsBalance: number;
  currentStreakDays: number;
  lastVisitDate: string | null;
  totalTransactions: number;
  totalEarned: number;
  totalRedeemed: number;
  createdAt: string;
  lastUpdatedAt: string;
}

/** A single green points ledger entry. */
export interface PointTransaction {
  id: string;
  signedDelta: number;
  source: string;
  reason: string;
  description: string;
  relatedEntityId: string | null;
  occurredAt: string;
}

/** Paginated envelope from the API. */
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

/** A redeemable reward from the catalog. */
export interface Reward {
  id: string;
  name: string;
  description: string;
  costInPoints: number;
  isActive: boolean;
  createdAt: string;
}

/** Response after logging a station visit. */
export interface RecordVisitResponse {
  citizenProfileId: string;
  transactionId: string;
  stationId: string;
  pointsAwarded: number;
  newBalance: number;
  currentStreakDays: number;
  occurredAt: string;
}

/** Response after redeeming a reward. */
export interface RedeemRewardResponse {
  citizenProfileId: string;
  rewardId: string;
  rewardName: string;
  pointsSpent: number;
  newBalance: number;
  redeemedAt: string;
}
