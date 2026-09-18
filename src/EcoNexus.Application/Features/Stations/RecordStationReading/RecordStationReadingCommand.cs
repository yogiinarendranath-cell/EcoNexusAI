using EcoNexus.Contracts.Stations;
using MediatR;

namespace EcoNexus.Application.Features.Stations.RecordStationReading;

public sealed record RecordStationReadingCommand(Guid StationId, RecordReadingRequest Request)
    : IRequest<RecordReadingResponse>;
