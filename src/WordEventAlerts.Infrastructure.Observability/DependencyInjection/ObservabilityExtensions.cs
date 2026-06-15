using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using WordEventAlerts.Infrastructure.Observability.Middleware;

namespace WordEventAlerts.Infrastructure.Observability.DependencyInjection;

public static class ObservabilityExtensions
{
    public static IServiceCollection AddObservabilityFoundation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }

    public static IApplicationBuilder UseObservabilityFoundation(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<RequestCorrelationMiddleware>();
    }
}
