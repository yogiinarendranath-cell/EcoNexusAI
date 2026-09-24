using EcoNexus.Contracts.Recycling;
using MediatR;

namespace EcoNexus.Application.Features.Recycling.AdvanceIntake;

/// <summary>Command: advance an intake batch to a new lifecycle stage.</summary>
public sealed record AdvanceIntakeCommand(
    Guid FacilityId,
    Guid IntakeId,
    AdvanceIntakeRequest Request) : IRequest<AdvanceIntakeResponse>;
