using WorldEventAlerts.Core.Domain;
using WorldEventAlerts.Infrastructure.InMemory.Repositories;

namespace WorldEventAlerts.Tests.Integration;

public sealed class InMemoryRepositoryTests
{
    [Fact]
    public async Task WorldEventRepository_ShouldSaveAndRetrieveEvent()
    {
        var repository = new InMemoryWorldEventRepository();
        var worldEvent = CreateWorldEvent();

        await repository.SaveAsync(worldEvent);
        var stored = await repository.GetByEventIdAsync(worldEvent.EventId);

        Assert.NotNull(stored);
        Assert.Equal(worldEvent.EventId, stored.EventId);
    }

    [Fact]
    public async Task WorldEventRepository_ShouldSupportPagination()
    {
        var repository = new InMemoryWorldEventRepository();

        await repository.SaveAsync(CreateWorldEvent(occurredAtUtc: DateTimeOffset.UtcNow.AddMinutes(-3)));
        await repository.SaveAsync(CreateWorldEvent(occurredAtUtc: DateTimeOffset.UtcNow.AddMinutes(-2)));
        await repository.SaveAsync(CreateWorldEvent(occurredAtUtc: DateTimeOffset.UtcNow.AddMinutes(-1)));

        var page = await repository.ListAsync(skip: 1, take: 1);

        Assert.Single(page);
    }

    [Fact]
    public async Task AlertRuleRepository_ShouldListEnabledRules()
    {
        var repository = new InMemoryAlertRuleRepository();
        var enabledRule = CreateRule(isEnabled: true);
        var disabledRule = CreateRule(isEnabled: false);

        await repository.UpsertAsync(enabledRule);
        await repository.UpsertAsync(disabledRule);

        var enabledRules = await repository.ListEnabledAsync();

        Assert.Single(enabledRules);
        Assert.Equal(enabledRule.RuleId, enabledRules.Single().RuleId);
    }

    [Fact]
    public async Task UserPreferenceRepository_ShouldListByUserAndRule()
    {
        var repository = new InMemoryUserPreferenceRepository();
        var userId = Guid.NewGuid();
        var ruleId = Guid.NewGuid();
        var matching = CreateSubscription(userId, ruleId, priority: 0);
        var differentRule = CreateSubscription(userId, Guid.NewGuid(), priority: 1);

        await repository.UpsertSubscriptionAsync(matching);
        await repository.UpsertSubscriptionAsync(differentRule);

        var byRule = await repository.ListByRuleAsync(ruleId);
        var byUser = await repository.ListByUserAsync(userId);

        Assert.Single(byRule);
        Assert.Equal(2, byUser.Count);
    }

    [Fact]
    public async Task DeliveryAttemptRepository_ShouldFilterDeadLettersAndCorrelation()
    {
        var repository = new InMemoryDeliveryAttemptRepository();
        var correlationId = Guid.NewGuid().ToString("N");
        var deadLetter = CreateDeliveryAttempt(correlationId, DeliveryOutcome.DeadLettered, attemptNumber: 2);
        var success = CreateDeliveryAttempt(correlationId, DeliveryOutcome.Succeeded, attemptNumber: 1);
        var unrelated = CreateDeliveryAttempt(Guid.NewGuid().ToString("N"), DeliveryOutcome.DeadLettered, attemptNumber: 1);

        await repository.SaveAsync(deadLetter);
        await repository.SaveAsync(success);
        await repository.SaveAsync(unrelated);

        var byCorrelation = await repository.ListByCorrelationIdAsync(correlationId);
        var deadLetters = await repository.ListDeadLettersAsync();

        Assert.Equal(2, byCorrelation.Count);
        Assert.Equal(2, deadLetters.Count);
        Assert.Contains(deadLetter, deadLetters);
    }

    private static WorldEvent CreateWorldEvent(
        WorldEventCategory category = WorldEventCategory.BreakingNews,
        int severity = 60,
        DateTimeOffset? occurredAtUtc = null)
    {
        var occurred = occurredAtUtc ?? DateTimeOffset.UtcNow.AddMinutes(-5);
        return new WorldEvent(
            eventId: Guid.NewGuid(),
            sourceEventId: Guid.NewGuid().ToString("N"),
            sourceSystem: "integration-test-feed",
            category: category,
            severityScore: severity,
            headline: "Test headline",
            summary: "Test summary",
            regions: ["US"],
            keywords: ["test"],
            occurredAtUtc: occurred,
            ingestedAtUtc: occurred.AddSeconds(30),
            schemaVersion: "v1",
            correlationId: Guid.NewGuid().ToString("N"));
    }

    private static AlertRule CreateRule(bool isEnabled)
    {
        return new AlertRule(
            ruleId: Guid.NewGuid(),
            userId: Guid.NewGuid(),
            name: $"Rule-{Guid.NewGuid():N}",
            isEnabled: isEnabled,
            categories: [WorldEventCategory.BreakingNews],
            minimumSeverity: 50,
            regions: ["US"],
            keywords: ["test"]);
    }

    private static ChannelSubscription CreateSubscription(Guid userId, Guid ruleId, int priority)
    {
        return new ChannelSubscription(
            subscriptionId: Guid.NewGuid(),
            userId: userId,
            ruleId: ruleId,
            channelType: NotificationChannelType.Email,
            destination: "alerts@example.com",
            isEnabled: true,
            priority: priority,
            maxRetryAttempts: 3);
    }

    private static DeliveryAttempt CreateDeliveryAttempt(string correlationId, DeliveryOutcome outcome, int attemptNumber)
    {
        return new DeliveryAttempt(
            deliveryAttemptId: Guid.NewGuid(),
            eventId: Guid.NewGuid(),
            ruleId: Guid.NewGuid(),
            userId: Guid.NewGuid(),
            channelType: NotificationChannelType.Slack,
            destination: "https://hooks.slack.local/test",
            attemptNumber: attemptNumber,
            outcome: outcome,
            attemptedAtUtc: DateTimeOffset.UtcNow.AddSeconds(attemptNumber),
            failureReason: outcome is DeliveryOutcome.Succeeded ? null : "simulated",
            correlationId: correlationId);
    }
}

