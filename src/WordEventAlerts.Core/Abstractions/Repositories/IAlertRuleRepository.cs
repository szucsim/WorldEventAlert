using WordEventAlerts.Core.Domain;

namespace WordEventAlerts.Core.Abstractions.Repositories;

/// <summary>
/// Provides persistence operations for user alert rules.
/// </summary>
public interface IAlertRuleRepository
{
    /// <summary>
    /// Inserts or updates an alert rule.
    /// </summary>
    /// <param name="rule">The alert rule to persist.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task UpsertAsync(AlertRule rule, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an alert rule by its rule identifier.
    /// </summary>
    /// <param name="ruleId">The rule identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The matching alert rule, or null when not found.</returns>
    Task<AlertRule?> GetByRuleIdAsync(Guid ruleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all alert rules for a given user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A read-only collection of the user's alert rules.</returns>
    Task<IReadOnlyCollection<AlertRule>> ListByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all currently enabled alert rules.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A read-only collection of enabled alert rules.</returns>
    Task<IReadOnlyCollection<AlertRule>> ListEnabledAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an alert rule by its rule identifier.
    /// </summary>
    /// <param name="ruleId">The rule identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>True when the rule was deleted; otherwise false.</returns>
    Task<bool> DeleteAsync(Guid ruleId, CancellationToken cancellationToken = default);
}
