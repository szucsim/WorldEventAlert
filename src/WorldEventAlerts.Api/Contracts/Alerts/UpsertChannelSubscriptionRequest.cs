using WorldEventAlerts.Core.Domain;

namespace WorldEventAlerts.Api.Contracts.Alerts;

/// <summary>
/// Request payload for inserting or updating a channel subscription for a rule.
/// </summary>
public sealed class UpsertChannelSubscriptionRequest
{
    public required Guid UserId { get; init; }

    public NotificationChannelType ChannelType { get; init; }

    public required string Destination { get; init; }

    public bool IsEnabled { get; init; } = true;

    public int Priority { get; init; }

    public int? MaxRetryAttempts { get; init; } = 3;
}

