namespace WorldEventAlerts.Core.Domain;

/// <summary>
/// Represents a successful rule match and the enabled subscriptions that should be considered for dispatch.
/// </summary>
public sealed class AlertMatch
{
    /// <summary>
    /// Initializes a match result.
    /// </summary>
    /// <param name="rule">The matched alert rule.</param>
    /// <param name="subscriptions">Enabled channel subscriptions for the matched rule.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public AlertMatch(AlertRule rule, IReadOnlyCollection<ChannelSubscription> subscriptions)
    {
        Rule = rule ?? throw new ArgumentNullException(nameof(rule));
        Subscriptions = subscriptions ?? throw new ArgumentNullException(nameof(subscriptions));
    }

    /// <summary>
    /// Gets the matched alert rule.
    /// </summary>
    public AlertRule Rule { get; }

    /// <summary>
    /// Gets enabled channel subscriptions associated with the matched rule.
    /// </summary>
    public IReadOnlyCollection<ChannelSubscription> Subscriptions { get; }
}

