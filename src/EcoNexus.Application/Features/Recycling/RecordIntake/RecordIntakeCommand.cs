using EcoNexus.Contracts.Recycling;
using MediatR;

namespace EcoNexus.Application.Features.Recycling.RecordIntake;

/// <summary>Command: record a new intake batch at a facility.</summary>
public sealed record RecordIntakeCommand(Guid FacilityId, RecordIntakeRequest Request)
    : IRequest<RecordIntakeResponse>;
