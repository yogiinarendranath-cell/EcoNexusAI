using EcoNexus.Contracts.Citizen;
using MediatR;

namespace EcoNexus.Application.Features.Citizen.ListRewards;

/// <summary>Query: list currently active rewards available for redemption.</summary>
public sealed record ListRewardsQuery
    : IRequest<IReadOnlyList<RewardResponse>>;
