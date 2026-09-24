using EcoNexus.Domain.Abstractions;
using EcoNexus.Domain.Enums;

namespace EcoNexus.Domain.DomainEvents;

/// <summary>
/// Raised when a new intake batch is recorded at a recycling facility.
/// </summary>
public sealed record RecyclingIntakeRecordedEvent(
    Guid FacilityId,
    string FacilityName,
    Guid IntakeId,
    WasteCategory Material,
    double WeightKilograms,
    DateTimeOffset RecordedAt) : IDomainEvent;
