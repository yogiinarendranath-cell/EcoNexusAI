using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EcoNexus.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by `dotnet ef` commands (migrations, etc.) so the
/// tooling can create a DbContext without running the full application host.
/// </summary>
public sealed class EcoNexusDbContextFactory : IDesignTimeDbContextFactory<EcoNexusDbContext>
{
    private const string DefaultConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=EcoNexus;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

    public EcoNexusDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<EcoNexusDbContext>();
        optionsBuilder.UseSqlServer(DefaultConnectionString);

        return new EcoNexusDbContext(optionsBuilder.Options);
    }
}
