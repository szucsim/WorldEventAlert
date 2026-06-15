namespace WorldEventAlerts.Core.Domain;

public sealed class DeliveryAttempt
{
    public DeliveryAttempt(
        Guid deliveryAttemptId,
        Guid eventId,
        Guid ruleId,
        Guid userId,
        NotificationChannelType channelType,
        string destination,
        int attemptNumber,
        DeliveryOutcome outcome,
        DateTimeOffset attemptedAtUtc,
        string? failureReason,
        string correlationId)
    {
        if (deliveryAttemptId == Guid.Empty)
        {
            throw new ArgumentException("Delivery attempt ID must be a non-empty GUID.", nameof(deliveryAttemptId));
        }

        if (eventId == Guid.Empty)
        {
            throw new ArgumentException("Event ID must be a non-empty GUID.", nameof(eventId));
        }

        if (ruleId == Guid.Empty)
        {
            throw new ArgumentException("Rule ID must be a non-empty GUID.", nameof(ruleId));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID must be a non-empty GUID.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(destination))
        {
            throw new ArgumentException("Destination is required.", nameof(destination));
        }

        if (attemptNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(attemptNumber), "Attempt number must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(correlationId))
        {
            throw new ArgumentException("Correlation ID is required.", nameof(correlationId));
        }

        DeliveryAttemptId = deliveryAttemptId;
        EventId = eventId;
        RuleId = ruleId;
        UserId = userId;
        ChannelType = channelType;
        Destination = destination.Trim();
        AttemptNumber = attemptNumber;
        Outcome = outcome;
        AttemptedAtUtc = attemptedAtUtc;
        FailureReason = string.IsNullOrWhiteSpace(failureReason) ? null : failureReason.Trim();
        CorrelationId = correlationId.Trim();
    }

    public Guid DeliveryAttemptId { get; }

    public Guid EventId { get; }

    public Guid RuleId { get; }

    public Guid UserId { get; }

    public NotificationChannelType ChannelType { get; }

    public string Destination { get; }

    public int AttemptNumber { get; }

    public DeliveryOutcome Outcome { get; }

    public DateTimeOffset AttemptedAtUtc { get; }

    public string? FailureReason { get; }

    public string CorrelationId { get; }
}

