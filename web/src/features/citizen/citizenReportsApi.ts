import { apiClient } from '../../lib/apiClient';

/** Request body for POST /api/v1/citizen/reports. */
export interface FileReportRequest {
  stationId: string;
  reportType: CitizenReportType;
  description: string;
  photoUrl: string | null;
}

/** Response from POST /api/v1/citizen/reports. */
export interface FileReportResponse {
  id: string;
  stationId: string;
  reportType: CitizenReportType;
  status: ReportStatus;
  filedAt: string;
}

/** A single report as returned by GET /api/v1/citizen/reports/mine. */
export interface CitizenReportDto {
  id: string;
  stationId: string;
  reportType: CitizenReportType;
  description: string;
  photoUrl: string | null;
  status: ReportStatus;
  filedAt: string;
  acknowledgedAt: string | null;
  resolvedAt: string | null;
  resolutionNote: string | null;
}

export type CitizenReportType =
  | 'OverflowingBin'
  | 'DamagedStation'
  | 'IllegalDumping'
  | 'MissedCollection'
  | 'Other';

export type ReportStatus = 'New' | 'Acknowledged' | 'Resolved' | 'Rejected';

export async function fileReport(payload: FileReportRequest): Promise<FileReportResponse> {
  const { data } = await apiClient.post<FileReportResponse>('/v1/citizen/reports', payload);
  return data;
}

export async function fetchMyReports(): Promise<CitizenReportDto[]> {
  const { data } = await apiClient.get<CitizenReportDto[]>('/v1/citizen/reports/mine');
  return data;
}
