using WordEventAlerts.Core.Domain;

namespace WordEventAlerts.Api.Contracts.Events;

/// <summary>
/// Request payload for ingesting a normalized world event.
/// API/schema versioning is controlled by the route prefix and not by request payload fields.
/// </summary>
public sealed class IngestWorldEventRequest
{
    public Guid? EventId { get; init; }

    public required string SourceEventId { get; init; }

    public required string SourceSystem { get; init; }

    public WorldEventCategory Category { get; init; } = WorldEventCategory.Other;

    public int SeverityScore { get; init; }

    public required string Headline { get; init; }

    public required string Summary { get; init; }

    public IReadOnlyCollection<string> Regions { get; init; } = [];

    public IReadOnlyCollection<string> Keywords { get; init; } = [];

    public DateTimeOffset? OccurredAtUtc { get; init; }
}
