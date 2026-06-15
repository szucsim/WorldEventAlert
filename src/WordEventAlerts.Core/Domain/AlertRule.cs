namespace WordEventAlerts.Core.Domain;

public sealed class AlertRule
{
    public AlertRule(
        Guid ruleId,
        Guid userId,
        bool isEnabled,
        IEnumerable<WorldEventCategory>? categories,
        int? minimumSeverity,
        IEnumerable<string>? regions,
        IEnumerable<string>? keywords)
    {
        if (ruleId == Guid.Empty)
        {
            throw new ArgumentException("Rule ID must be a non-empty GUID.", nameof(ruleId));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID must be a non-empty GUID.", nameof(userId));
        }

        if (minimumSeverity is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumSeverity), "Minimum severity must be between 0 and 100.");
        }

        RuleId = ruleId;
        UserId = userId;
        IsEnabled = isEnabled;
        Categories = (categories ?? []).ToHashSet();
        MinimumSeverity = minimumSeverity;
        Regions = NormalizeValues(regions, toUpper: true);
        Keywords = NormalizeValues(keywords, toUpper: false);
    }

    public Guid RuleId { get; }

    public Guid UserId { get; }

    public bool IsEnabled { get; private set; }

    public IReadOnlySet<WorldEventCategory> Categories { get; }

    public int? MinimumSeverity { get; }

    public IReadOnlySet<string> Regions { get; }

    public IReadOnlySet<string> Keywords { get; }

    public bool Matches(WorldEvent worldEvent)
    {
        ArgumentNullException.ThrowIfNull(worldEvent);

        if (!IsEnabled)
        {
            return false;
        }

        if (Categories.Count > 0 && !Categories.Contains(worldEvent.Category))
        {
            return false;
        }

        if (MinimumSeverity.HasValue && worldEvent.SeverityScore < MinimumSeverity.Value)
        {
            return false;
        }

        if (Regions.Count > 0 && !worldEvent.Regions.Any(region => Regions.Contains(region)))
        {
            return false;
        }

        if (Keywords.Count > 0)
        {
            var keywordMatchFromTags = worldEvent.Keywords.Any(keyword => Keywords.Contains(keyword));

            if (keywordMatchFromTags)
            {
                return true;
            }

            var searchText = $"{worldEvent.Headline} {worldEvent.Summary}".ToLowerInvariant();
            var keywordMatchFromText = Keywords.Any(searchText.Contains);

            if (!keywordMatchFromText)
            {
                return false;
            }
        }

        return true;
    }

    public void Enable() => IsEnabled = true;

    public void Disable() => IsEnabled = false;

    private static IReadOnlySet<string> NormalizeValues(IEnumerable<string>? values, bool toUpper)
    {
        var normalized = (values ?? [])
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => toUpper ? value.Trim().ToUpperInvariant() : value.Trim().ToLowerInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return normalized;
    }
}
