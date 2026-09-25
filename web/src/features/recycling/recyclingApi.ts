import { apiClient } from '../../lib/apiClient';
import type {
  AdvanceIntakeRequest,
  AdvanceIntakeResponse,
  CreateFacilityRequest,
  CreateFacilityResponse,
  FacilityDetail,
  FacilityListItem,
  RecordIntakeRequest,
  RecordIntakeResponse,
} from '../../types/recycling';

export async function fetchFacilities(): Promise<FacilityListItem[]> {
  const { data } = await apiClient.get<FacilityListItem[]>('/v1/recycling-facilities');
  return data;
}

export async function fetchFacilityById(id: string): Promise<FacilityDetail> {
  const { data } = await apiClient.get<FacilityDetail>(`/v1/recycling-facilities/${id}`);
  return data;
}

export async function createFacility(
  payload: CreateFacilityRequest,
): Promise<CreateFacilityResponse> {
  const { data } = await apiClient.post<CreateFacilityResponse>(
    '/v1/recycling-facilities',
    payload,
  );
  return data;
}

export async function recordIntake(
  facilityId: string,
  payload: RecordIntakeRequest,
): Promise<RecordIntakeResponse> {
  const { data } = await apiClient.post<RecordIntakeResponse>(
    `/v1/recycling-facilities/${facilityId}/intakes`,
    payload,
  );
  return data;
}

export async function advanceIntake(
  facilityId: string,
  intakeId: string,
  payload: AdvanceIntakeRequest,
): Promise<AdvanceIntakeResponse> {
  const { data } = await apiClient.post<AdvanceIntakeResponse>(
    `/v1/recycling-facilities/${facilityId}/intakes/${intakeId}/advance`,
    payload,
  );
  return data;
}
