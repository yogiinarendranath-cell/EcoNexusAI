namespace EcoNexus.Infrastructure.Identity;

/// <summary>
/// Canonical role names for EcoNexus AI. Referenced by authorization policies
/// and by the role seed at startup. Never change an existing value — roles
/// are persisted in AspNetRoles and referenced by user accounts.
/// </summary>
public static class EcoNexusRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string CityAdmin = "CityAdmin";
    public const string OperationsManager = "OperationsManager";
    public const string CollectionManager = "CollectionManager";
    public const string Driver = "Driver";
    public const string RecyclingManager = "RecyclingManager";
    public const string Citizen = "Citizen";

    /// <summary>
    /// All roles that must exist in the system. Used by the role seeder.
    /// </summary>
    public static IReadOnlyList<string> All { get; } = new[]
    {
        SuperAdmin,
        CityAdmin,
        OperationsManager,
        CollectionManager,
        Driver,
        RecyclingManager,
        Citizen
    };
}
