using WordEventAlerts.Core.Domain;

namespace WordEventAlerts.Core.Abstractions.Repositories;

/// <summary>
/// Provides persistence operations for user channel subscriptions.
/// </summary>
public interface IUserPreferenceRepository
{
    /// <summary>
    /// Inserts or updates a channel subscription.
    /// </summary>
    /// <param name="subscription">The subscription to persist.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task UpsertSubscriptionAsync(ChannelSubscription subscription, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists channel subscriptions configured for a rule.
    /// </summary>
    /// <param name="ruleId">The rule identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A read-only collection of channel subscriptions for the rule.</returns>
    Task<IReadOnlyCollection<ChannelSubscription>> ListByRuleAsync(Guid ruleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists channel subscriptions configured for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A read-only collection of channel subscriptions for the user.</returns>
    Task<IReadOnlyCollection<ChannelSubscription>> ListByUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
