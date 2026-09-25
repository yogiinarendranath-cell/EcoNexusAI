using EcoNexus.Contracts.Citizen;
using EcoNexus.Contracts.Common;
using MediatR;

namespace EcoNexus.Application.Features.Citizen.ListPointTransactions;

/// <summary>Query: paginated green-points history for the current citizen.</summary>
public sealed record ListPointTransactionsQuery(Guid UserId, PointHistoryQuery Parameters)
    : IRequest<PagedResult<PointTransactionResponse>>;
