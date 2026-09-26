using System.Diagnostics.Metrics;

namespace EcoNexus.Infrastructure.Observability;

/// <summary>
/// Central definition of all custom business metrics emitted by EcoNexus.
///
/// Instruments live on a single meter named "EcoNexus.Business". The
/// OpenTelemetry pipeline (Program.cs) subscribes to that meter name via
/// AddMeter(EcoNexusMeters.BusinessMeterName), which is what turns these
/// counter increments into exported metrics.
///
/// Metric names are dotted and lowercase, matching OpenTelemetry semantic
/// conventions (e.g. "http.server.request.duration"). Every counter has a
/// stable name — changing one is a breaking change for any dashboard that
/// consumes it, so treat these as public API.
/// </summary>
public sealed class EcoNexusMeters
{
    /// <summary>
    /// Meter name that the OTel pipeline subscribes to. Do not change
    /// without also updating Program.cs's AddMeter call.
    /// </summary>
    public const string BusinessMeterName = "EcoNexus.Business";

    public EcoNexusMeters(IMeterFactory meterFactory)
    {
        ArgumentNullException.ThrowIfNull(meterFactory);

        var meter = meterFactory.Create(BusinessMeterName);

        StationReadingsRecorded = meter.CreateCounter<long>(
            name: "econexus.station.reading_recorded",
            unit: "{reading}",
            description: "Number of sensor readings recorded across all waste stations.");

        StationCriticalFillReached = meter.CreateCounter<long>(
            name: "econexus.station.critical_fill_reached",
            unit: "{event}",
            description: "Number of times a station crossed into critical fill level (>= 90%).");

        StationCollected = meter.CreateCounter<long>(
            name: "econexus.station.collected",
            unit: "{event}",
            description: "Number of times a waste station was collected/emptied.");

        CitizenReportsFiled = meter.CreateCounter<long>(
            name: "econexus.citizen.report_filed",
            unit: "{report}",
            description: "Number of citizen reports filed against waste stations.");

        RecyclingIntakesRecorded = meter.CreateCounter<long>(
            name: "econexus.recycling.intake_recorded",
            unit: "{intake}",
            description: "Number of recycling facility intake batches recorded.");

        RecyclingIntakesAdvanced = meter.CreateCounter<long>(
            name: "econexus.recycling.intake_advanced",
            unit: "{advance}",
            description: "Number of times a recycling intake was advanced through its processing stages.");

        GreenPointsEarned = meter.CreateCounter<long>(
            name: "econexus.green_points.earned",
            unit: "{point}",
            description: "Total green points earned by citizens across all sources.");
    }

    public Counter<long> StationReadingsRecorded { get; }
    public Counter<long> StationCriticalFillReached { get; }
    public Counter<long> StationCollected { get; }
    public Counter<long> CitizenReportsFiled { get; }
    public Counter<long> RecyclingIntakesRecorded { get; }
    public Counter<long> RecyclingIntakesAdvanced { get; }
    public Counter<long> GreenPointsEarned { get; }
}
