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
        public const string EventId = "EventId";
        public const string RuleId = "RuleId";
        public const string UserId = "UserId";
        public const string ChannelType = "ChannelType";
        public const string DeliveryAttemptId = "DeliveryAttemptId";
        public const string AttemptNumber = "AttemptNumber";
        public const string Outcome = "Outcome";
        public const string FailureClassification = "FailureClassification";
    }

    public static class LogEvents
    {
        public const string RequestStarted = "RequestStarted";
        public const string RequestCompleted = "RequestCompleted";
        public const string RequestFailed = "RequestFailed";
        public const string EventIngested = "EventIngested";
        public const string EventRead = "EventRead";
        public const string RuleMatched = "RuleMatched";
        public const string RuleNotMatched = "RuleNotMatched";
        public const string DeliveryAttempted = "DeliveryAttempted";
        public const string DeliverySucceeded = "DeliverySucceeded";
        public const string DeliveryFailed = "DeliveryFailed";
        public const string DeliveryDeadLettered = "DeliveryDeadLettered";
        public const string AlertRuleUpserted = "AlertRuleUpserted";
        public const string AlertRuleRead = "AlertRuleRead";
        public const string AlertRuleListByUser = "AlertRuleListByUser";
        public const string SubscriptionUpserted = "SubscriptionUpserted";
        public const string SubscriptionListByRule = "SubscriptionListByRule";
        public const string AdminDeadLettersListed = "AdminDeadLettersListed";
        public const string AdminDeliveriesByCorrelationListed = "AdminDeliveriesByCorrelationListed";
        public const string AdminDeliveryAttemptRead = "AdminDeliveryAttemptRead";
        public const string AdminReplayRequested = "AdminReplayRequested";
        public const string AdminReplaySucceeded = "AdminReplaySucceeded";
        public const string AdminReplayFailed = "AdminReplayFailed";
    }
}
