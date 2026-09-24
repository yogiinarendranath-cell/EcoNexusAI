/**
 * Mirrors EcoNexus.Contracts.Stations.StationListItemResponse
 * and EcoNexus.Contracts.Stations.StationDetailResponse.
 *
 * Keep these in sync with the backend contracts. If the backend
 * changes, update here and the TypeScript compiler will surface
 * every place that needs a fix.
 */

export type WasteCategory =
  | 'Organic'
  | 'Plastic'
  | 'Paper'
  | 'Glass'
  | 'Metal'
  | 'EWaste'
  | 'General'
  | 'Hazardous';

export type StationStatus =
  | 'Online'
  | 'Offline'
  | 'Maintenance'
  | 'Decommissioned';

export type StationListItem = {
  id: string;
  code: string;
  latitude: number;
  longitude: number;
  capacityKilograms: number;
  currentFillPercent: number;
  primaryCategory: WasteCategory;
  status: StationStatus;
  lastUpdatedAt: string;
  lastCollectedAt: string | null;
};

export type StationDetail = StationListItem;

export type StationListQuery = {
  status?: StationStatus;
  category?: WasteCategory;
  criticalOnly?: boolean;
  sortBy?: 'code' | 'filllevel' | 'lastupdated';
  sortDesc?: boolean;
  page?: number;
  pageSize?: number;
};

export type PagedResult<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
};
