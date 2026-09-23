using MediatR;

namespace EcoNexus.Application.Features.Stations.DeleteStation;

/// <summary>
/// Command: delete a waste station. Idempotent — deleting a non-existent
/// station succeeds silently (no error).
/// </summary>
public sealed record DeleteStationCommand(Guid StationId) : IRequest;
