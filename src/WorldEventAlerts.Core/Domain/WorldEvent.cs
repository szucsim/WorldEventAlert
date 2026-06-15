namespace WorldEventAlerts.Core.Domain;

public sealed class WorldEvent
{
    public WorldEvent(
        Guid eventId,
        string sourceEventId,
        string sourceSystem,
        WorldEventCategory category,
        int severityScore,
        string headline,
        string summary,
        IEnumerable<string>? regions,
        IEnumerable<string>? keywords,
        DateTimeOffset occurredAtUtc,
        DateTimeOffset ingestedAtUtc,
        string schemaVersion,
        string correlationId)
    {
        if (eventId == Guid.Empty)
        {
            throw new ArgumentException("Event ID must be a non-empty GUID.", nameof(eventId));
        }

        if (string.IsNullOrWhiteSpace(sourceEventId))
        {
            throw new ArgumentException("Source event ID is required.", nameof(sourceEventId));
        }

        if (string.IsNullOrWhiteSpace(sourceSystem))
        {
            throw new ArgumentException("Source system is required.", nameof(sourceSystem));
        }

        if (severityScore < 0 || severityScore > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(severityScore), "Severity score must be between 0 and 100.");
        }

        if (string.IsNullOrWhiteSpace(headline))
        {
            throw new ArgumentException("Headline is required.", nameof(headline));
        }

        if (string.IsNullOrWhiteSpace(summary))
        {
            throw new ArgumentException("Summary is required.", nameof(summary));
        }

        if (ingestedAtUtc < occurredAtUtc)
        {
            throw new ArgumentException("Ingested time cannot be earlier than occurred time.", nameof(ingestedAtUtc));
        }

        if (string.IsNullOrWhiteSpace(schemaVersion))
        {
            throw new ArgumentException("Schema version is required.", nameof(schemaVersion));
        }

        if (string.IsNullOrWhiteSpace(correlationId))
        {
            throw new ArgumentException("Correlation ID is required.", nameof(correlationId));
        }

        EventId = eventId;
        SourceEventId = sourceEventId.Trim();
        SourceSystem = sourceSystem.Trim();
        Category = category;
        SeverityScore = severityScore;
        Headline = headline.Trim();
        Summary = summary.Trim();
        Regions = NormalizeValues(regions, toUpper: true);
        Keywords = NormalizeValues(keywords, toUpper: false);
        OccurredAtUtc = occurredAtUtc;
        IngestedAtUtc = ingestedAtUtc;
        SchemaVersion = schemaVersion.Trim();
        CorrelationId = correlationId.Trim();
    }

    public Guid EventId { get; }

    public string SourceEventId { get; }

    public string SourceSystem { get; }

    public WorldEventCategory Category { get; }

    public int SeverityScore { get; }

    public string Headline { get; }

    public string Summary { get; }

    public IReadOnlySet<string> Regions { get; }

    public IReadOnlySet<string> Keywords { get; }

    public DateTimeOffset OccurredAtUtc { get; }

    public DateTimeOffset IngestedAtUtc { get; }

    public string SchemaVersion { get; }

    public string CorrelationId { get; }

    private static IReadOnlySet<string> NormalizeValues(IEnumerable<string>? values, bool toUpper)
    {
        var normalized = (values ?? [])
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => toUpper ? value.Trim().ToUpperInvariant() : value.Trim().ToLowerInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return normalized;
    }
}

