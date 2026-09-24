import { apiClient } from '../../lib/apiClient';
import type {
  PagedResult,
  StationListQuery,
  StationListItem,
} from '../../types/station';

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
