using EcoNexus.Contracts.Stations;
using MediatR;

namespace EcoNexus.Application.Features.Stations.GetStationById;

/// <summary>Query: fetch a single station by id.</summary>
public sealed record GetStationByIdQuery(Guid Id) : IRequest<StationDetailResponse?>;
