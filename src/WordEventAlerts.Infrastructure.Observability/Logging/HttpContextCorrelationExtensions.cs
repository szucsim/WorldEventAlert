using Microsoft.AspNetCore.Http;

namespace WordEventAlerts.Infrastructure.Observability.Logging;

public static class HttpContextCorrelationExtensions
{
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
