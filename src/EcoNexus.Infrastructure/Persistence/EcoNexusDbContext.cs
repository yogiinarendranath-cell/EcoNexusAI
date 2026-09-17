using EcoNexus.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EcoNexus.Infrastructure.Persistence;

/// <summary>
/// The EF Core DbContext for EcoNexus AI.
/// Exposes the aggregate roots as DbSets and applies all
/// IEntityTypeConfiguration implementations found in this assembly.
/// </summary>
public sealed class EcoNexusDbContext : DbContext
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        // Apply all IEntityTypeConfiguration<T> in this assembly.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EcoNexusDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
