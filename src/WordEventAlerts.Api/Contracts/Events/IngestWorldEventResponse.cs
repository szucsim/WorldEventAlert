namespace WordEventAlerts.Api.Contracts.Events;

/// <summary>
/// Response payload summarizing ingestion, matching, and dispatch outcomes.
/// </summary>
public sealed class IngestWorldEventResponse
{
    public required Guid EventId { get; init; }

    public required string CorrelationId { get; init; }

    public int MatchedRules { get; init; }

    public int DispatchedNotifications { get; init; }

    public int FailedNotifications { get; init; }

    public required DateTimeOffset IngestedAtUtc { get; init; }
}
