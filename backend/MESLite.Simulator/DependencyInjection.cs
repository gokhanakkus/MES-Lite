using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MESLite.Simulator;

public static class DependencyInjection
{
    public static IServiceCollection AddSimulator(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SimulatorOptions>(configuration.GetSection(SimulatorOptions.SectionName));
        services.AddHostedService<ProductionSimulatorService>();
        return services;
    }
}
