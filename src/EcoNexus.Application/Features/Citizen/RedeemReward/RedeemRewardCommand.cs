using EcoNexus.Contracts.Citizen;
using MediatR;

namespace EcoNexus.Application.Features.Citizen.RedeemReward;

/// <summary>Command: redeem a reward with the citizen's green points.</summary>
public sealed record RedeemRewardCommand(Guid UserId, Guid RewardId)
    : IRequest<RedeemRewardResponse>;
