using EcoNexus.Domain.Entities;
using EcoNexus.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EcoNexus.Infrastructure.Persistence;

/// <summary>
/// The EF Core DbContext for EcoNexus AI.
/// Inherits from IdentityDbContext so ASP.NET Core Identity tables (users,
/// roles, claims, etc.) are managed by the same context as our domain entities.
/// This ensures a single transaction boundary and one migration history.
/// </summary>
public sealed class EcoNexusDbContext
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public EcoNexusDbContext(DbContextOptions<EcoNexusDbContext> options)
        : base(options)
    {
    }

    // ---- Aggregate roots ----

    public DbSet<WasteStation> WasteStations => Set<WasteStation>();
    public DbSet<CollectionVehicle> CollectionVehicles => Set<CollectionVehicle>();
    public DbSet<CollectionJob> CollectionJobs => Set<CollectionJob>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<CitizenReport> CitizenReports => Set<CitizenReport>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // 1. Apply ASP.NET Core Identity's configurations first.
        base.OnModelCreating(builder);

        // 2. Apply our own IEntityTypeConfiguration<T> classes.
        builder.ApplyConfigurationsFromAssembly(typeof(EcoNexusDbContext).Assembly);
    }
}
