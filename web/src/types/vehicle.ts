/**
 * Mirrors EcoNexus.Contracts.CollectionVehicles.CollectionVehicleResponse.
 */
export type CollectionVehicle = {
  id: string;
  registrationNumber: string;
  capacityKilograms: number;
  isActive: boolean;
};

export type CreateCollectionVehicleRequest = {
  registrationNumber: string;
  capacityKilograms: number;
};
