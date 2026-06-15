using System.Collections.Concurrent;
using WordEventAlerts.Core.Abstractions.Repositories;
using WordEventAlerts.Core.Domain;

namespace WordEventAlerts.Infrastructure.InMemory.Repositories;

public sealed class InMemoryAlertRuleRepository : IAlertRuleRepository
{
    private readonly ConcurrentDictionary<Guid, AlertRule> _rules = new();

    public Task UpsertAsync(AlertRule rule, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rule);

        cancellationToken.ThrowIfCancellationRequested();
        _rules[rule.RuleId] = rule;
        return Task.CompletedTask;
    }

    public Task<AlertRule?> GetByRuleIdAsync(Guid ruleId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _rules.TryGetValue(ruleId, out var rule);
        return Task.FromResult(rule);
    }

    public Task<IReadOnlyCollection<AlertRule>> ListByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = _rules.Values
            .Where(rule => rule.UserId == userId)
            .OrderBy(rule => rule.RuleId)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<AlertRule>>(result);
    }

    public Task<IReadOnlyCollection<AlertRule>> ListEnabledAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = _rules.Values
            .Where(rule => rule.IsEnabled)
            .OrderBy(rule => rule.RuleId)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<AlertRule>>(result);
    }
}
