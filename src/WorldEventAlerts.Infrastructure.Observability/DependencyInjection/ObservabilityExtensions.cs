using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using WorldEventAlerts.Infrastructure.Observability.Middleware;

namespace WorldEventAlerts.Infrastructure.Observability.DependencyInjection;

/// <summary>
/// Extension methods for registering and enabling observability middleware.
/// </summary>
public static class ObservabilityExtensions
{
    /// <summary>
    /// Registers observability-related services for the application.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <returns>The same service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddObservabilityFoundation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }

    /// <summary>
    /// Adds observability middleware to the application request pipeline.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The same application builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="app"/> is null.</exception>
    public static IApplicationBuilder UseObservabilityFoundation(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<RequestCorrelationMiddleware>();
    }
}

