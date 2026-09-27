import { apiClient } from '../../lib/apiClient';
import type {
  PagedResult,
  StationListQuery,
  StationListItem,
} from '../../types/station';
import type { Forecast, StationReading } from '../../types/forecast';

/**
 * Fetches a page of stations from the API.
 *
 * The API endpoint is GET /api/v1/stations with query parameters
 * for filter / sort / paging. We build the query string only from
 * the keys the caller provided, so undefined values don't leak into
 * the URL.
 */
export async function fetchStations(
  query: StationListQuery = {},
): Promise<PagedResult<StationListItem>> {
  const params = new URLSearchParams();

  if (query.status) params.set('status', query.status);
  if (query.category) params.set('category', query.category);
  if (query.criticalOnly) params.set('criticalOnly', 'true');
  if (query.sortBy) params.set('sortBy', query.sortBy);
  if (query.sortDesc !== undefined) params.set('sortDesc', String(query.sortDesc));
  if (query.page) params.set('page', String(query.page));
  if (query.pageSize) params.set('pageSize', String(query.pageSize));

  const qs = params.toString();
  const url = qs ? `/v1/stations?${qs}` : '/v1/stations';

  const { data } = await apiClient.get<PagedResult<StationListItem>>(url);
  return data;
}

/**
 * Fetches detailed metadata for one station.
 * Backend: GET /api/v1/stations/{id}
 */
export async function fetchStationDetail(id: string): Promise<StationListItem> {
    const { data } = await apiClient.get<StationListItem>(`/v1/stations/${id}`);
    return data;
}

/**
 * Fetches the most recent readings for one station, ordered ascending by time.
 * Backend: GET /api/v1/stations/{id}/readings?windowHours=N&limit=M
 */
export async function fetchStationReadings(
    id: string,
    windowHours = 24,
    limit = 200,
): Promise<StationReading[]> {
    const params = new URLSearchParams();
    params.set("windowHours", String(windowHours));
    params.set("limit", String(limit));

    const { data } = await apiClient.get<StationReading[]>(
        `/v1/stations/${id}/readings?${params.toString()}`,
    );
    return data;
}

/**
 * Fetches the fill-level forecast for one station.
 * Backend: GET /api/v1/stations/{id}/forecast?windowHours=N
 */
export async function fetchStationForecast(
    id: string,
    windowHours = 24,
): Promise<Forecast> {
    const params = new URLSearchParams();
    params.set("windowHours", String(windowHours));

    const { data } = await apiClient.get<Forecast>(
        `/v1/stations/${id}/forecast?${params.toString()}`,
    );
    return data;
}

