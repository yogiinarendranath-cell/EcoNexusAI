/** Metrics snapshot returned by the recycling API. */
export interface RecyclingMetrics {
  totalBatches: number;
  recoveredBatches: number;
  receivedKilograms: number;
  recoveredKilograms: number;
  landfilledKilograms: number;
  recyclingRate: number;
  landfillDiversion: number;
  co2SavedKilograms: number;
}

/** Lifecycle stage of a single intake batch. */
export type IntakeStage =
  | 'Received'
  | 'Sorted'
  | 'Processed'
  | 'Recovered'
  | 'Landfilled';

/** Operational status of a recycling facility. */
export type FacilityStatus =
  | 'Online'
  | 'Offline'
  | 'Maintenance'
  | 'Decommissioned';

/** Waste material categories accepted by facilities. */
export type WasteMaterial =
  | 'Organic'
  | 'Plastic'
  | 'Paper'
  | 'Glass'
  | 'Metal'
  | 'EWaste'
  | 'General'
  | 'Hazardous';

/** Lightweight facility view used by the list page. */
export interface FacilityListItem {
  id: string;
  name: string;
  latitude: number;
  longitude: number;
  dailyCapacityKilograms: number;
  status: FacilityStatus;
  intakeCount: number;
  metrics: RecyclingMetrics;
  lastUpdatedAt: string;
}

/** A single intake batch. */
export interface Intake {
  id: string;
  material: WasteMaterial;
  weightKilograms: number;
  stage: IntakeStage;
  recordedAt: string;
  stageUpdatedAt: string | null;
}

/** Full facility detail including intake history and metrics. */
export interface FacilityDetail {
  id: string;
  name: string;
  latitude: number;
  longitude: number;
  dailyCapacityKilograms: number;
  status: FacilityStatus;
  metrics: RecyclingMetrics;
  intakes: Intake[];
  createdAt: string;
  lastUpdatedAt: string;
}

/** Request body for creating a facility. */
export interface CreateFacilityRequest {
  name: string;
  latitude: number;
  longitude: number;
  dailyCapacityKilograms: number;
}

/** Response returned after a facility is created. */
export interface CreateFacilityResponse {
  id: string;
  name: string;
}

/** Request body for recording an intake batch. */
export interface RecordIntakeRequest {
  material: WasteMaterial;
  weightKilograms: number;
  recordedAt: string;
}

/** Response returned after an intake is recorded. */
export interface RecordIntakeResponse {
  facilityId: string;
  intakeId: string;
  material: WasteMaterial;
  weightKilograms: number;
  stage: IntakeStage;
  recordedAt: string;
}

/** Request body for advancing an intake to a new stage. */
export interface AdvanceIntakeRequest {
  nextStage: IntakeStage;
  advancedAt: string;
}

/** Response returned after advancing an intake. */
export interface AdvanceIntakeResponse {
  facilityId: string;
  intakeId: string;
  previousStage: IntakeStage;
  currentStage: IntakeStage;
  advancedAt: string;
}
