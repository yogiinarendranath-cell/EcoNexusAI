using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Common.Exceptions;
using EcoNexus.Contracts.Citizen;
using MediatR;

namespace EcoNexus.Application.Features.Citizen.RedeemReward;

internal sealed class RedeemRewardHandler
    : IRequestHandler<RedeemRewardCommand, RedeemRewardResponse>
{
    private readonly ICitizenProfileRepository _profileRepository;
    private readonly IRewardRepository _rewardRepository;

    public RedeemRewardHandler(
        ICitizenProfileRepository profileRepository,
        IRewardRepository rewardRepository)
    {
        _profileRepository = profileRepository;
        _rewardRepository = rewardRepository;
    }

    public async Task<RedeemRewardResponse> Handle(
        RedeemRewardCommand request,
        CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        if (profile is null)
        {
            throw new NotFoundException(
                $"Citizen profile for user '{request.UserId}' was not found.");
        }

        var reward = await _rewardRepository.GetByIdAsync(request.RewardId, cancellationToken);

        if (reward is null)
        {
            throw new NotFoundException(
                $"Reward '{request.RewardId}' was not found.");
        }

        var occurredAt = DateTimeOffset.UtcNow;

        EcoNexus.Domain.Entities.GreenPointTransaction tx;
        try
        {
            tx = profile.RedeemReward(reward, occurredAt);
        }
        catch (InvalidOperationException ex)
        {
            // Insufficient balance or inactive reward → HTTP 409.
            throw new ConflictException(ex.Message);
        }

        await _profileRepository.SaveChangesAsync(cancellationToken);

        return new RedeemRewardResponse(
            profile.Id,
            reward.Id,
            reward.Name,
            reward.CostInPoints,
            profile.GreenPointsBalance,
            occurredAt);
    }
}
