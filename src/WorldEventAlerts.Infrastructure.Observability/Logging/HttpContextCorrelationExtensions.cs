using Microsoft.AspNetCore.Http;

namespace WorldEventAlerts.Infrastructure.Observability.Logging;

/// <summary>
/// Helper extensions for reading correlation metadata from <see cref="HttpContext"/>.
/// </summary>
public static class HttpContextCorrelationExtensions
{
    /// <summary>
    /// Gets the current request correlation identifier from context items when available.
    /// </summary>
    /// <param name="httpContext">The HTTP context containing request metadata.</param>
    /// <returns>The resolved correlation identifier for the request.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="httpContext"/> is null.</exception>
    public static string GetCorrelationId(this HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (httpContext.Items.TryGetValue(ObservabilityConstants.CorrelationItemKey, out var value)
            && value is string correlationId
            && !string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId;
        }

        return httpContext.TraceIdentifier;
    }
}

