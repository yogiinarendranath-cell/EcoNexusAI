using EcoNexus.Application.Abstractions.Dispatching;
using EcoNexus.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EcoNexus.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by `dotnet ef` commands (migrations, etc.) so the
/// tooling can create a DbContext without running the full application host.
///
/// The design-time context uses a no-op event dispatcher: migrations build
/// the model, they never execute business operations that raise domain events.
/// </summary>
public sealed class EcoNexusDbContextFactory : IDesignTimeDbContextFactory<EcoNexusDbContext>
{
    private const string DefaultConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=EcoNexus;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

    public EcoNexusDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<EcoNexusDbContext>();
        optionsBuilder.UseSqlServer(DefaultConnectionString);

        return new EcoNexusDbContext(optionsBuilder.Options, new NoOpDomainEventDispatcher());
    }

    /// <summary>
    /// Does nothing. Exists only so the design-time DbContext can be constructed
    /// without a MediatR container.
    /// </summary>
    private sealed class NoOpDomainEventDispatcher : IDomainEventDispatcher
    {
        public Task DispatchAsync(
            IReadOnlyCollection<IDomainEvent> domainEvents,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
