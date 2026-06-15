using WordEventAlerts.Core.Domain;

namespace WordEventAlerts.Core.Abstractions.Repositories;

public interface IAlertRuleRepository
{
    Task UpsertAsync(AlertRule rule, CancellationToken cancellationToken = default);

    Task<AlertRule?> GetByRuleIdAsync(Guid ruleId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AlertRule>> ListByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AlertRule>> ListEnabledAsync(CancellationToken cancellationToken = default);
}
