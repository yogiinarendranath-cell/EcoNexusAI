using EcoNexus.Contracts.Citizen;
using MediatR;

namespace EcoNexus.Application.Features.Citizen.RecordStationVisit;

/// <summary>Command: log a station visit and award green points.</summary>
public sealed record RecordStationVisitCommand(Guid UserId, RecordStationVisitRequest Request)
    : IRequest<RecordStationVisitResponse>;
