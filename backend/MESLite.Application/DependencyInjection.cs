using System.Reflection;
using FluentValidation;
using MediatR;
using MESLite.Application.Common.Behaviours;
using MESLite.Application.Common.Interfaces;
using MESLite.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MESLite.Application;

/// <summary>Wires up MediatR, FluentValidation and Application services.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));

        services.AddScoped<IOeeCalculationService, OeeCalculationService>();
        services.AddScoped<IAiAnalyticsService, AiAnalyticsService>();
        services.AddScoped<IPredictiveMaintenanceService, PredictiveMaintenanceService>();

        return services;
    }
}
