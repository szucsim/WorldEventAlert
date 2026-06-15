namespace WordEventAlerts.Core.Domain;

public sealed class NotificationRequest
{
    public NotificationRequest(
        Guid eventId,
        Guid ruleId,
        Guid userId,
        NotificationChannelType channelType,
        string destination,
        string subject,
        string message,
        string correlationId)
    {
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

        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new ArgumentException("Subject is required.", nameof(subject));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Message is required.", nameof(message));
        }

        if (string.IsNullOrWhiteSpace(correlationId))
        {
            throw new ArgumentException("Correlation ID is required.", nameof(correlationId));
        }

        EventId = eventId;
        RuleId = ruleId;
        UserId = userId;
        ChannelType = channelType;
        Destination = destination.Trim();
        Subject = subject.Trim();
        Message = message.Trim();
        CorrelationId = correlationId.Trim();
    }

    public Guid EventId { get; }

    public Guid RuleId { get; }

    public Guid UserId { get; }

    public NotificationChannelType ChannelType { get; }

    public string Destination { get; }

    public string Subject { get; }

    public string Message { get; }

    public string CorrelationId { get; }
}
