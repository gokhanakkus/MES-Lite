using MESLite.Application.Common.Interfaces;
using MESLite.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MESLite.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // SQL Server is the default. Set "Database:Provider" to "Sqlite" to run with zero external
        // dependencies (handy where SQL Server / LocalDB isn't available). Both share the same model.
        var provider = configuration["Database:Provider"] ?? "SqlServer";
        var migrationsAssembly = typeof(ApplicationDbContext).Assembly.GetName().Name;

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                var sqlite = configuration.GetConnectionString("SqliteConnection") ?? "Data Source=meslite.db";
                options.UseSqlite(sqlite);
            }
            else
            {
                var connectionString = configuration.GetConnectionString("DefaultConnection")
                    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not configured.");
                options.UseSqlServer(connectionString, sql =>
                {
                    sql.MigrationsAssembly(migrationsAssembly);
                    sql.EnableRetryOnFailure();
                });
            }
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        return services;
    }
}
