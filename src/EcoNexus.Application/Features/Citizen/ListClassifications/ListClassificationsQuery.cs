using EcoNexus.Contracts.Citizen;
using MediatR;

namespace EcoNexus.Application.Features.Citizen.ListClassifications;

/// <summary>
/// Query: list every AI waste classification performed by the current
/// citizen, newest first.
/// </summary>
public sealed record ListClassificationsQuery(Guid UserId)
    : IRequest<IReadOnlyList<ClassificationListItemResponse>>;
