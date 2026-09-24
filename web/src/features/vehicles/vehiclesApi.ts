import { apiClient } from '../../lib/apiClient';
import type {
  CollectionVehicle,
  CreateCollectionVehicleRequest,
} from '../../types/vehicle';

export async function fetchVehicles(): Promise<CollectionVehicle[]> {
  const { data } = await apiClient.get<CollectionVehicle[]>('/v1/collection-vehicles');
  return data;
}

export async function createVehicle(
  payload: CreateCollectionVehicleRequest,
): Promise<CollectionVehicle> {
  const { data } = await apiClient.post<CollectionVehicle>(
    '/v1/collection-vehicles',
    payload,
  );
  return data;
}
