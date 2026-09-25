import { apiClient } from '../../lib/apiClient';
import type {
  ClassificationListItem,
  ClassifyWasteRequest,
  ClassifyWasteResponse,
} from '../../types/citizen';

export async function classifyWaste(
  imageUrl: string,
): Promise<ClassifyWasteResponse> {
  const payload: ClassifyWasteRequest = { imageUrl };
  const { data } = await apiClient.post<ClassifyWasteResponse>(
    '/v1/citizen/classify',
    payload,
  );
  return data;
}

export async function fetchClassifications(): Promise<ClassificationListItem[]> {
  const { data } = await apiClient.get<ClassificationListItem[]>(
    '/v1/citizen/classifications',
  );
  return data;
}
