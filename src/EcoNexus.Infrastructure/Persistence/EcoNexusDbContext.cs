using EcoNexus.Application.Abstractions.Dispatching;
using EcoNexus.Domain.Abstractions;
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
///
/// After a successful save, any domain events raised by aggregate roots are
/// gathered and dispatched through <see cref="IDomainEventDispatcher"/>.
/// </summary>
public sealed class EcoNexusDbContext
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    private readonly IDomainEventDispatcher _dispatcher;

    public EcoNexusDbContext(
        DbContextOptions<EcoNexusDbContext> options,
        IDomainEventDispatcher dispatcher)
        : base(options)
    {
        _dispatcher = dispatcher;
    }

    // ---- Aggregate roots ----

    public DbSet<WasteStation> WasteStations => Set<WasteStation>();
    public DbSet<CollectionVehicle> CollectionVehicles => Set<CollectionVehicle>();
    public DbSet<CollectionJob> CollectionJobs => Set<CollectionJob>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<CitizenReport> CitizenReports => Set<CitizenReport>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // 1. Apply ASP.NET Core Identity's configurations first.
        base.OnModelCreating(builder);

        // 2. Apply our own IEntityTypeConfiguration<T> classes.
        builder.ApplyConfigurationsFromAssembly(typeof(EcoNexusDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        // Gather events BEFORE saving (in case we want to use them post-save
        // for optimistic-concurrency decisions later). Take a snapshot now.
        var aggregatesWithEvents = ChangeTracker
            .Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = aggregatesWithEvents
            .SelectMany(a => a.DomainEvents)
            .ToList();

        // Clear events immediately so a failing save doesn't re-dispatch later.
        foreach (var aggregate in aggregatesWithEvents)
        {
            aggregate.ClearDomainEvents();
        }

        var result = await base.SaveChangesAsync(cancellationToken);

        if (domainEvents.Count > 0)
        {
            await _dispatcher.DispatchAsync(domainEvents, cancellationToken);
        }

        return result;
    }
}
