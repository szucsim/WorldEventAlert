using WordEventAlerts.Core.Domain;

namespace WordEventAlerts.Core.Abstractions.Repositories;

public interface IUserPreferenceRepository
{
    Task UpsertSubscriptionAsync(ChannelSubscription subscription, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ChannelSubscription>> ListByRuleAsync(Guid ruleId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ChannelSubscription>> ListByUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
