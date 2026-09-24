using EcoNexus.Contracts.Recycling;
using MediatR;

namespace EcoNexus.Application.Features.Recycling.CreateFacility;

/// <summary>Command: create a new recycling facility.</summary>
public sealed record CreateFacilityCommand(CreateFacilityRequest Request)
    : IRequest<CreateFacilityResponse>;
