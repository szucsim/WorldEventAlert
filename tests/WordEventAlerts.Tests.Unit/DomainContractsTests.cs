using WordEventAlerts.Core.Domain;

namespace WordEventAlerts.Tests.Unit;

public sealed class DomainContractsTests
{
    [Fact]
    public void WorldEvent_ShouldThrow_WhenSeverityIsOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateWorldEvent(severityScore: 101));
    }

    [Fact]
    public void AlertRule_ShouldMatch_WhenAllFiltersPass()
    {
        var rule = CreateRule(
            categories: [WorldEventCategory.MarketMovement],
            minimumSeverity: 80,
            regions: ["US"],
            keywords: ["inflation"]);

        var worldEvent = CreateWorldEvent(
            category: WorldEventCategory.MarketMovement,
            severityScore: 85,
            regions: ["US", "CA"],
            keywords: ["inflation"]);

        var isMatch = rule.Matches(worldEvent);

        Assert.True(isMatch);
    }

    [Fact]
    public void AlertRule_ShouldNotMatch_WhenRuleIsDisabled()
    {
        var rule = CreateRule(isEnabled: false, categories: [WorldEventCategory.BreakingNews]);
        var worldEvent = CreateWorldEvent(category: WorldEventCategory.BreakingNews);

        var isMatch = rule.Matches(worldEvent);

        Assert.False(isMatch);
    }

    [Fact]
    public void AlertRule_ShouldNotMatch_WhenSeverityIsBelowMinimum()
    {
        var rule = CreateRule(minimumSeverity: 90);
        var worldEvent = CreateWorldEvent(severityScore: 70);

        var isMatch = rule.Matches(worldEvent);

        Assert.False(isMatch);
    }

    [Fact]
    public void AlertRule_ShouldMatch_WhenKeywordAppearsInHeadline()
    {
        var rule = CreateRule(keywords: ["earthquake"]);
        var worldEvent = CreateWorldEvent(
            headline: "Major earthquake reported in coastal region",
            summary: "Emergency agencies are responding.",
            keywords: []);

        var isMatch = rule.Matches(worldEvent);

        Assert.True(isMatch);
    }

    private static WorldEvent CreateWorldEvent(
        WorldEventCategory category = WorldEventCategory.BreakingNews,
        int severityScore = 50,
        string headline = "Breaking event",
        string summary = "Important summary",
        IEnumerable<string>? regions = null,
        IEnumerable<string>? keywords = null)
    {
        return new WorldEvent(
            eventId: Guid.NewGuid(),
            sourceEventId: "source-1",
            sourceSystem: "test-feed",
            category: category,
            severityScore: severityScore,
            headline: headline,
            summary: summary,
            regions: regions ?? ["US"],
            keywords: keywords ?? ["alert"],
            occurredAtUtc: DateTimeOffset.UtcNow.AddMinutes(-1),
            ingestedAtUtc: DateTimeOffset.UtcNow,
            schemaVersion: "v1",
            correlationId: Guid.NewGuid().ToString("N"));
    }

    private static AlertRule CreateRule(
        bool isEnabled = true,
        IEnumerable<WorldEventCategory>? categories = null,
        int? minimumSeverity = null,
        IEnumerable<string>? regions = null,
        IEnumerable<string>? keywords = null)
    {
        return new AlertRule(
            ruleId: Guid.NewGuid(),
            userId: Guid.NewGuid(),
            isEnabled: isEnabled,
            categories: categories ?? [],
            minimumSeverity: minimumSeverity,
            regions: regions ?? [],
            keywords: keywords ?? []);
    }
}
