using WordEventAlerts.Core.Domain;

namespace WordEventAlerts.Web.Services;

/// <summary>
/// Provides typed operations for interacting with alerting API endpoints.
/// </summary>
public interface IAlertsApiClient
{
    /// <summary>
    /// Lists alert rules for the provided user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="correlationId">The request correlation identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The list of alert rules for the user.</returns>
    Task<IReadOnlyCollection<AlertRuleDto>> ListRulesByUserAsync(
        Guid userId,
        string correlationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates an alert rule.
    /// </summary>
    /// <param name="ruleId">The alert rule identifier.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="correlationId">The request correlation identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The persisted rule payload returned by the API.</returns>
    Task<AlertRuleDto?> UpsertRuleAsync(
        Guid ruleId,
        UpsertAlertRuleRequestDto request,
        string correlationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists channel subscriptions configured for a rule.
    /// </summary>
    /// <param name="ruleId">The rule identifier.</param>
    /// <param name="correlationId">The request correlation identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The list of channel subscriptions.</returns>
    Task<IReadOnlyCollection<ChannelSubscriptionDto>> ListSubscriptionsByRuleAsync(
        Guid ruleId,
        string correlationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates a channel subscription for a rule.
    /// </summary>
    /// <param name="ruleId">The rule identifier.</param>
    /// <param name="subscriptionId">The subscription identifier.</param>
    /// <param name="request">The request payload.</param>
    /// <param name="correlationId">The request correlation identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The persisted channel subscription payload returned by the API.</returns>
    Task<ChannelSubscriptionDto?> UpsertSubscriptionAsync(
        Guid ruleId,
        Guid subscriptionId,
        UpsertChannelSubscriptionRequestDto request,
        string correlationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists dead-letter delivery attempts.
    /// </summary>
    /// <param name="correlationId">The request correlation identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The dead-letter delivery attempts.</returns>
    Task<IReadOnlyCollection<DeliveryAttemptDto>> ListDeadLettersAsync(
        string correlationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists delivery attempts for a correlation identifier.
    /// </summary>
    /// <param name="filterCorrelationId">The correlation identifier to filter by.</param>
    /// <param name="correlationId">The request correlation identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The matching delivery attempts.</returns>
    Task<IReadOnlyCollection<DeliveryAttemptDto>> ListAttemptsByCorrelationIdAsync(
        string filterCorrelationId,
        string correlationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a delivery attempt by identifier.
    /// </summary>
    /// <param name="deliveryAttemptId">The delivery attempt identifier.</param>
    /// <param name="correlationId">The request correlation identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The matching delivery attempt when found; otherwise null.</returns>
    Task<DeliveryAttemptDto?> GetAttemptByIdAsync(
        Guid deliveryAttemptId,
        string correlationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Replays a previously persisted delivery attempt.
    /// </summary>
    /// <param name="deliveryAttemptId">The source delivery attempt identifier.</param>
    /// <param name="correlationId">The request correlation identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Replay details when the source attempt exists; otherwise null.</returns>
    Task<ReplayDeliveryAttemptResponseDto?> ReplayAttemptAsync(
        Guid deliveryAttemptId,
        string correlationId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents an alert rule projection returned by API endpoints.
/// </summary>
public sealed class AlertRuleDto
{
    public Guid RuleId { get; init; }

    public Guid UserId { get; init; }

    public bool IsEnabled { get; init; }

    public IReadOnlyCollection<WorldEventCategory> Categories { get; init; } = Array.Empty<WorldEventCategory>();

    public int? MinimumSeverity { get; init; }

    public IReadOnlyCollection<string> Regions { get; init; } = Array.Empty<string>();

    public IReadOnlyCollection<string> Keywords { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Request payload used to upsert alert rules via the API.
/// </summary>
public sealed class UpsertAlertRuleRequestDto
{
    public Guid UserId { get; init; }

    public bool IsEnabled { get; init; } = true;

    public IReadOnlyCollection<WorldEventCategory> Categories { get; init; } = Array.Empty<WorldEventCategory>();

    public int? MinimumSeverity { get; init; }

    public IReadOnlyCollection<string> Regions { get; init; } = Array.Empty<string>();

    public IReadOnlyCollection<string> Keywords { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Represents a channel subscription projection returned by API endpoints.
/// </summary>
public sealed class ChannelSubscriptionDto
{
    public Guid SubscriptionId { get; init; }

    public Guid RuleId { get; init; }

    public Guid UserId { get; init; }

    public NotificationChannelType ChannelType { get; init; }

    public string Destination { get; init; } = string.Empty;

    public bool IsEnabled { get; init; }

    public int Priority { get; init; }

    public int? MaxRetryAttempts { get; init; }
}

/// <summary>
/// Request payload used to upsert channel subscriptions via the API.
/// </summary>
public sealed class UpsertChannelSubscriptionRequestDto
{
    public Guid UserId { get; init; }

    public NotificationChannelType ChannelType { get; init; }

    public string Destination { get; init; } = string.Empty;

    public bool IsEnabled { get; init; } = true;

    public int Priority { get; init; }

    public int? MaxRetryAttempts { get; init; } = 3;
}

/// <summary>
/// Represents a delivery attempt projection returned by admin endpoints.
/// </summary>
public sealed class DeliveryAttemptDto
{
    public Guid DeliveryAttemptId { get; init; }

    public Guid EventId { get; init; }

    public Guid RuleId { get; init; }

    public Guid UserId { get; init; }

    public NotificationChannelType ChannelType { get; init; }

    public string Destination { get; init; } = string.Empty;

    public int AttemptNumber { get; init; }

    public DeliveryOutcome Outcome { get; init; }

    public DateTimeOffset AttemptedAtUtc { get; init; }

    public string? FailureReason { get; init; }

    public string CorrelationId { get; init; } = string.Empty;
}

/// <summary>
/// Represents replay outcome data returned by admin replay endpoint.
/// </summary>
public sealed class ReplayDeliveryAttemptResponseDto
{
    public Guid SourceAttemptId { get; init; }

    public Guid ReplayAttemptId { get; init; }

    public int AttemptNumber { get; init; }

    public DeliveryOutcome Outcome { get; init; }

    public string CorrelationId { get; init; } = string.Empty;
}
