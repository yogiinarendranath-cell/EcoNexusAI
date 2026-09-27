/**
 * Mirrors the forecast + readings responses from the EcoNexus API.
 * Keep in sync with:
 *   EcoNexus.Contracts.Stations.ForecastStationFillLevelResponse
 *   EcoNexus.Contracts.Stations.StationReadingResponse
 */

export type ForecastMethod = "LinearRegression" | "InsufficientData";

export type Forecast = {
    stationId: string;
    currentFillPercent: number;
    fillRatePercentPerHour: number;
    predictedOverflowAt: string | null;
    hoursUntilOverflow: number | null;
    confidence: number;
    method: ForecastMethod;
    sampleSize: number;
    isOverflowPredicted: boolean;
};

export type StationReading = {
    id: string;
    stationId: string;
    fillLevelPercent: number;
    temperatureCelsius: number;
    batteryPercent: number;
    recordedAt: string;
};

