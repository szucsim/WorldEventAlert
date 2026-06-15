using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WordEventAlerts.Infrastructure.Observability.Logging;

namespace WordEventAlerts.Infrastructure.Observability.Middleware;

/// <summary>
/// Middleware that ensures correlation IDs are propagated and request lifecycle events are logged.
/// </summary>
public sealed class RequestCorrelationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestCorrelationMiddleware> _logger;

    /// <summary>
    /// Initializes middleware dependencies.
    /// </summary>
    /// <param name="next">The next pipeline delegate.</param>
    /// <param name="logger">Logger used for request lifecycle events.</param>
    /// <exception cref="ArgumentNullException">Thrown when any dependency is null.</exception>
    public RequestCorrelationMiddleware(RequestDelegate next, ILogger<RequestCorrelationMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Processes an HTTP request by applying correlation metadata and structured lifecycle logging.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <returns>A task representing middleware completion.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="httpContext"/> is null.</exception>
    public async Task InvokeAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var correlationId = ResolveCorrelationId(httpContext);
        httpContext.TraceIdentifier = correlationId;
        httpContext.Items[ObservabilityConstants.CorrelationItemKey] = correlationId;
        httpContext.Response.Headers[ObservabilityConstants.CorrelationHeaderName] = correlationId;

        var scopeValues = new Dictionary<string, object>
        {
            [ObservabilityConstants.ScopeKeys.CorrelationId] = correlationId,
            [ObservabilityConstants.ScopeKeys.RequestMethod] = httpContext.Request.Method,
            [ObservabilityConstants.ScopeKeys.RequestPath] = httpContext.Request.Path.Value ?? string.Empty
        };

        using var _ = _logger.BeginScope(scopeValues);
        _logger.LogInformation(
            "{LogEvent} {RequestMethod} {RequestPath}",
            ObservabilityConstants.LogEvents.RequestStarted,
            httpContext.Request.Method,
            httpContext.Request.Path.Value);

        try
        {
            await _next(httpContext);
            _logger.LogInformation(
                "{LogEvent} {RequestMethod} {RequestPath} {StatusCode}",
                ObservabilityConstants.LogEvents.RequestCompleted,
                httpContext.Request.Method,
                httpContext.Request.Path.Value,
                httpContext.Response.StatusCode);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "{LogEvent} {RequestMethod} {RequestPath}",
                ObservabilityConstants.LogEvents.RequestFailed,
                httpContext.Request.Method,
                httpContext.Request.Path.Value);

            throw;
        }
    }

    private static string ResolveCorrelationId(HttpContext httpContext)
    {
        var incoming = httpContext.Request.Headers[ObservabilityConstants.CorrelationHeaderName].ToString();

        if (!string.IsNullOrWhiteSpace(incoming))
        {
            return incoming.Trim();
        }

        return Guid.NewGuid().ToString("N");
    }
}
