namespace WorldEventAlerts.Core.Domain;

public sealed class ChannelSubscription
{
    public ChannelSubscription(
        Guid subscriptionId,
        Guid userId,
        Guid ruleId,
        NotificationChannelType channelType,
        string destination,
        bool isEnabled,
        int priority,
        int? maxRetryAttempts)
    {
        if (subscriptionId == Guid.Empty)
        {
            throw new ArgumentException("Subscription ID must be a non-empty GUID.", nameof(subscriptionId));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID must be a non-empty GUID.", nameof(userId));
        }

        if (ruleId == Guid.Empty)
        {
            throw new ArgumentException("Rule ID must be a non-empty GUID.", nameof(ruleId));
        }

        if (string.IsNullOrWhiteSpace(destination))
        {
            throw new ArgumentException("Destination is required.", nameof(destination));
        }

        if (priority < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(priority), "Priority cannot be negative.");
        }

        if (maxRetryAttempts <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRetryAttempts), "Max retry attempts must be greater than zero.");
        }

        SubscriptionId = subscriptionId;
        UserId = userId;
        RuleId = ruleId;
        ChannelType = channelType;
        Destination = destination.Trim();
        IsEnabled = isEnabled;
        Priority = priority;
        MaxRetryAttempts = maxRetryAttempts;
    }

    public Guid SubscriptionId { get; }

    public Guid UserId { get; }

    public Guid RuleId { get; }

    public NotificationChannelType ChannelType { get; }

    public string Destination { get; }

    public bool IsEnabled { get; private set; }

    public int Priority { get; }

    public int? MaxRetryAttempts { get; }

    public void Enable() => IsEnabled = true;

    public void Disable() => IsEnabled = false;
}

