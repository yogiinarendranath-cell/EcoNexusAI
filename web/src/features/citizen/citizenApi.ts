import { apiClient } from '../../lib/apiClient';
import type {
  CitizenProfile,
  PagedResult,
  PointTransaction,
  RecordVisitResponse,
  RedeemRewardResponse,
  Reward,
} from '../../types/citizen';

export async function fetchMyProfile(): Promise<CitizenProfile> {
  const { data } = await apiClient.get<CitizenProfile>('/v1/citizen/profile');
  return data;
}

export async function fetchMyPointHistory(
  page = 1,
  pageSize = 20,
): Promise<PagedResult<PointTransaction>> {
  const { data } = await apiClient.get<PagedResult<PointTransaction>>(
    '/v1/citizen/points/history',
    { params: { page, pageSize } },
  );
  return data;
}

export async function recordStationVisit(
  stationId: string,
  visitDate: string,
): Promise<RecordVisitResponse> {
  const { data } = await apiClient.post<RecordVisitResponse>(
    '/v1/citizen/visits',
    { stationId, visitDate },
  );
  return data;
}

export async function fetchRewards(): Promise<Reward[]> {
  const { data } = await apiClient.get<Reward[]>('/v1/citizen/rewards');
  return data;
}

export async function redeemReward(rewardId: string): Promise<RedeemRewardResponse> {
  const { data } = await apiClient.post<RedeemRewardResponse>(
    `/v1/citizen/rewards/${rewardId}/redeem`,
    null,
  );
  return data;
}
