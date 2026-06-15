using Microsoft.Extensions.DependencyInjection;
using WordEventAlerts.Core.Abstractions.Repositories;
using WordEventAlerts.Infrastructure.InMemory.Repositories;

namespace WordEventAlerts.Infrastructure.InMemory.DependencyInjection;

public static class ServiceCollectionExtensions
{
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
