using System.Collections.Concurrent;
using WordEventAlerts.Core.Abstractions.Repositories;
using WordEventAlerts.Core.Domain;

namespace WordEventAlerts.Infrastructure.InMemory.Repositories;

/// <summary>
/// In-memory implementation of alert rule persistence operations.
/// </summary>
public sealed class InMemoryAlertRuleRepository : IAlertRuleRepository
{
    private readonly ConcurrentDictionary<Guid, AlertRule> _rules = new();

    /// <inheritdoc />
    public Task UpsertAsync(AlertRule rule, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rule);

        cancellationToken.ThrowIfCancellationRequested();
        _rules[rule.RuleId] = rule;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<AlertRule?> GetByRuleIdAsync(Guid ruleId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _rules.TryGetValue(ruleId, out var rule);
        return Task.FromResult(rule);
    }

    /// <inheritdoc />
    public Task<IReadOnlyCollection<AlertRule>> ListByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = _rules.Values
            .Where(rule => rule.UserId == userId)
            .OrderBy(rule => rule.Name)
            .ThenBy(rule => rule.RuleId)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<AlertRule>>(result);
    }

    /// <inheritdoc />
    public Task<IReadOnlyCollection<AlertRule>> ListEnabledAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = _rules.Values
            .Where(rule => rule.IsEnabled)
            .OrderBy(rule => rule.Name)
            .ThenBy(rule => rule.RuleId)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<AlertRule>>(result);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(Guid ruleId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var wasDeleted = _rules.TryRemove(ruleId, out _);
        return Task.FromResult(wasDeleted);
    }
}
