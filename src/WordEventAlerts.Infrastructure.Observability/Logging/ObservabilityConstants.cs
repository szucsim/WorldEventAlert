namespace WordEventAlerts.Infrastructure.Observability.Logging;

public static class ObservabilityConstants
{
    public const string CorrelationHeaderName = "X-Correlation-Id";
    public const string CorrelationItemKey = "CorrelationId";

    public static class ScopeKeys
    {
        public const string CorrelationId = "CorrelationId";
        public const string RequestMethod = "RequestMethod";
        public const string RequestPath = "RequestPath";
    }

    public static class LogEvents
    {
        public const string RequestStarted = "RequestStarted";
        public const string RequestCompleted = "RequestCompleted";
        public const string RequestFailed = "RequestFailed";
    }
}
