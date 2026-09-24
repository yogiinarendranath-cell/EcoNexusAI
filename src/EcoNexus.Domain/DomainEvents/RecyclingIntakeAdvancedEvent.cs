using EcoNexus.Domain.Abstractions;
using EcoNexus.Domain.Enums;

namespace EcoNexus.Domain.DomainEvents;

/// <summary>
/// Raised when an intake batch advances from one stage to the next.
/// </summary>
public sealed record RecyclingIntakeAdvancedEvent(
    Guid FacilityId,
    Guid IntakeId,
    IntakeStage FromStage,
    IntakeStage ToStage,
    DateTimeOffset AdvancedAt) : IDomainEvent;
