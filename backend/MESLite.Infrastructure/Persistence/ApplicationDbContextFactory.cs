using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MESLite.Infrastructure.Persistence;

/// <summary>
/// Design-time factory so `dotnet ef` can build the context without spinning up the API host.
/// The connection string here is only used by tooling (e.g. migrations scaffolding), not at runtime.
/// </summary>
public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("MESLITE_DESIGN_CONNECTION")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=MESLite;Trusted_Connection=True;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString, sql => sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.GetName().Name))
            .Options;

        return new ApplicationDbContext(options);
    }
}
