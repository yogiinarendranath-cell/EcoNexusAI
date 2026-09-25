namespace EcoNexus.Contracts.Citizen;

/// <summary>Pagination parameters for the citizen's points history.</summary>
public sealed record PointHistoryQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
