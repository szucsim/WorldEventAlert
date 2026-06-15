using Microsoft.Extensions.DependencyInjection;
using WorldEventAlerts.Core.Abstractions.Repositories;
using WorldEventAlerts.Infrastructure.InMemory.Repositories;

namespace WorldEventAlerts.Infrastructure.InMemory.DependencyInjection;

/// <summary>
/// Extension methods for registering in-memory repository implementations.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all in-memory repository adapters used by the application.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <returns>The same service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddInMemoryRepositories(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IWorldEventRepository, InMemoryWorldEventRepository>();
        services.AddSingleton<IAlertRuleRepository, InMemoryAlertRuleRepository>();
        services.AddSingleton<IUserPreferenceRepository, InMemoryUserPreferenceRepository>();
        services.AddSingleton<IDeliveryAttemptRepository, InMemoryDeliveryAttemptRepository>();

        return services;
    }
}

