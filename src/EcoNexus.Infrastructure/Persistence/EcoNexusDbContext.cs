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
    public DbSet<RecyclingFacility> RecyclingFacilities => Set<RecyclingFacility>();
    public DbSet<CitizenProfile> CitizenProfiles => Set<CitizenProfile>();
    public DbSet<Reward> Rewards => Set<Reward>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // 1. Apply ASP.NET Core Identity's configurations first.
        base.OnModelCreating(builder);

        // 2. Apply our own IEntityTypeConfiguration<T> classes.
        builder.ApplyConfigurationsFromAssembly(typeof(EcoNexusDbContext).Assembly);

        // 3. Fix client-assigned Guid Ids.
        //
        //    Our domain Entity base class assigns Id = Guid.NewGuid() in its
        //    parameterless constructor. EF Core's default convention is that a
        //    Guid primary key which is NOT the CLR default (Guid.Empty) at the
        //    time an entity is attached is assumed to already exist in the
        //    database, and therefore is tracked as Modified (UPDATE) rather
        //    than Added (INSERT).
        //
        //    ValueGeneratedNever() tells EF: "the application assigns this Id,
        //    and a new instance with this Id is a new row - INSERT it."
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(Entity).IsAssignableFrom(entityType.ClrType)
                && entityType.FindProperty(nameof(Entity.Id)) is not null)
            {
                builder.Entity(entityType.ClrType)
                    .Property(nameof(Entity.Id))
                    .ValueGeneratedNever();
            }
        }
    }

    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        var aggregatesWithEvents = ChangeTracker
            .Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = aggregatesWithEvents
            .SelectMany(a => a.DomainEvents)
            .ToList();

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
