using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Common.Exceptions;
using EcoNexus.Contracts.Citizen;
using EcoNexus.Contracts.Common;
using MediatR;

namespace EcoNexus.Application.Features.Citizen.ListPointTransactions;

internal sealed class ListPointTransactionsHandler
    : IRequestHandler<ListPointTransactionsQuery, PagedResult<PointTransactionResponse>>
{
    private readonly ICitizenProfileRepository _repository;

    public ListPointTransactionsHandler(ICitizenProfileRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResult<PointTransactionResponse>> Handle(
        ListPointTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByUserIdAsync(request.UserId, cancellationToken);

        if (profile is null)
        {
            throw new NotFoundException(
                $"Citizen profile for user '{request.UserId}' was not found.");
        }

        var p = request.Parameters;
        var page = p.Page < 1 ? 1 : p.Page;
        var pageSize = p.PageSize is < 1 or > 100 ? 20 : p.PageSize;

        var all = profile.Transactions
            .OrderByDescending(t => t.OccurredAt)
            .ToList();

        var totalCount = all.Count;

        var items = all
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new PointTransactionResponse(
                t.Id,
                t.SignedDelta,
                t.Source.ToString(),
                t.Reason.ToString(),
                t.Description,
                t.RelatedEntityId,
                t.OccurredAt))
            .ToList();

        return new PagedResult<PointTransactionResponse>(items, page, pageSize, totalCount);
    }
}
