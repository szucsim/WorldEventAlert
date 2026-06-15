using System.Collections.Concurrent;
using WordEventAlerts.Core.Abstractions.Repositories;
using WordEventAlerts.Core.Domain;

namespace WordEventAlerts.Infrastructure.InMemory.Repositories;

/// <summary>
/// In-memory implementation of user channel subscription persistence operations.
/// </summary>
public sealed class InMemoryUserPreferenceRepository : IUserPreferenceRepository
{
    private readonly ConcurrentDictionary<Guid, ChannelSubscription> _subscriptions = new();

    /// <inheritdoc />
    public Task UpsertSubscriptionAsync(ChannelSubscription subscription, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subscription);

        cancellationToken.ThrowIfCancellationRequested();
        _subscriptions[subscription.SubscriptionId] = subscription;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyCollection<ChannelSubscription>> ListByRuleAsync(
        Guid ruleId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = _subscriptions.Values
            .Where(subscription => subscription.RuleId == ruleId)
            .OrderBy(subscription => subscription.Priority)
            .ThenBy(subscription => subscription.SubscriptionId)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<ChannelSubscription>>(result);
    }

    /// <inheritdoc />
    public Task<IReadOnlyCollection<ChannelSubscription>> ListByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = _subscriptions.Values
            .Where(subscription => subscription.UserId == userId)
            .OrderBy(subscription => subscription.Priority)
            .ThenBy(subscription => subscription.SubscriptionId)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<ChannelSubscription>>(result);
    }
}
