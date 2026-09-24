import { apiClient } from '../../lib/apiClient';
import type {
  CollectionJob,
  PreviewRouteRequest,
  PreviewRouteResponse,
  ScheduleJobRequest,
} from '../../types/job';

export async function fetchJobs(): Promise<CollectionJob[]> {
  const { data } = await apiClient.get<CollectionJob[]>('/v1/collection-jobs');
  return data;
}

export async function fetchJobById(id: string): Promise<CollectionJob> {
  const { data } = await apiClient.get<CollectionJob>(`/v1/collection-jobs/${id}`);
  return data;
}

export async function scheduleJob(payload: ScheduleJobRequest): Promise<CollectionJob> {
  const { data } = await apiClient.post<CollectionJob>('/v1/collection-jobs', payload);
  return data;
}

export async function previewRoute(
  payload: PreviewRouteRequest,
): Promise<PreviewRouteResponse> {
  const { data } = await apiClient.post<PreviewRouteResponse>(
    '/v1/collection-jobs/preview-route',
    payload,
  );
  return data;
}
