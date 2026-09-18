namespace EcoNexus.Contracts.Stations;

/// <summary>
/// Query parameters for GET /api/v1/stations.
/// All values are optional except page and pageSize (defaulted).
/// </summary>
public sealed record StationListQuery(
    int Page = 1,
    int PageSize = 20,
    string? Status = null,
    string? Category = null,
    bool CriticalOnly = false,
    string? SortBy = "code",
    bool SortDesc = false);
