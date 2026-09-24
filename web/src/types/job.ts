/**
 * Mirrors EcoNexus.Contracts.CollectionJobs.*
 */
export type JobStatus = 'Scheduled' | 'InProgress' | 'Completed' | 'Cancelled';

export type RouteStop = {
  id: string;
  stationId: string;
  sequence: number;
  collectedWeightKilograms: number | null;
  completedAt: string | null;
};

export type CollectionJob = {
  id: string;
  vehicleId: string;
  status: JobStatus;
  scheduledFor: string;
  startedAt: string | null;
  completedAt: string | null;
  stops: RouteStop[];
  totalCollectedKilograms: number;
};

export type ScheduleJobStop = { stationId: string };

export type ScheduleJobRequest = {
  vehicleId: string;
  scheduledFor: string;
  stops: ScheduleJobStop[];
};

export type PreviewRouteRequest = {
  vehicleId: string;
  candidateStationIds: string[];
};

export type PreviewRouteStop = {
  stationId: string;
  stationCode: string;
  sequence: number;
  fillLevelPercent: number;
  estimatedWeightKilograms: number;
};

export type PreviewRouteResponse = {
  vehicleId: string;
  vehicleCapacityKilograms: number;
  stops: PreviewRouteStop[];
  totalEstimatedWeightKilograms: number;
  skippedCount: number;
};
