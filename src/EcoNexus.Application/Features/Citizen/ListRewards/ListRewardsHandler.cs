using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Contracts.Citizen;
using MediatR;

namespace EcoNexus.Application.Features.Citizen.ListRewards;

internal sealed class ListRewardsHandler
    : IRequestHandler<ListRewardsQuery, IReadOnlyList<RewardResponse>>
{
    private readonly IRewardRepository _repository;

    public ListRewardsHandler(IRewardRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<RewardResponse>> Handle(
        ListRewardsQuery request,
        CancellationToken cancellationToken)
    {
        var rewards = await _repository.GetActiveAsync(cancellationToken);

        return rewards
            .Select(r => new RewardResponse(
                r.Id,
                r.Name,
                r.Description,
                r.CostInPoints,
                r.IsActive,
                r.CreatedAt))
            .ToList();
    }
}
