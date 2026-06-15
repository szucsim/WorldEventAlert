using WorldEventAlerts.Core.Abstractions.Matching;
using WorldEventAlerts.Core.Abstractions.Repositories;
using WorldEventAlerts.Core.Domain;

namespace WorldEventAlerts.Core.Services;

/// <summary>
/// Default alert matching engine that evaluates a world event against enabled rules.
/// </summary>
public sealed class AlertMatchingEngine : IAlertMatchingEngine
{
    private readonly IAlertRuleRepository _alertRuleRepository;
    private readonly IUserPreferenceRepository _userPreferenceRepository;

    /// <summary>
    /// Initializes matching dependencies.
    /// </summary>
    /// <param name="alertRuleRepository">Repository for loading enabled rules.</param>
    /// <param name="userPreferenceRepository">Repository for loading channel subscriptions.</param>
    /// <exception cref="ArgumentNullException">Thrown when any dependency is null.</exception>
    public AlertMatchingEngine(
        IAlertRuleRepository alertRuleRepository,
        IUserPreferenceRepository userPreferenceRepository)
    {
        _alertRuleRepository = alertRuleRepository ?? throw new ArgumentNullException(nameof(alertRuleRepository));
        _userPreferenceRepository = userPreferenceRepository ?? throw new ArgumentNullException(nameof(userPreferenceRepository));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<AlertMatch>> MatchAsync(
        WorldEvent worldEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(worldEvent);
        cancellationToken.ThrowIfCancellationRequested();

        var enabledRules = await _alertRuleRepository.ListEnabledAsync(cancellationToken);
        var matches = new List<AlertMatch>();

        foreach (var rule in enabledRules)
        {
            if (!rule.Matches(worldEvent))
            {
                continue;
            }

            var subscriptions = await _userPreferenceRepository.ListByRuleAsync(rule.RuleId, cancellationToken);
            var enabledSubscriptions = subscriptions
                .Where(subscription => subscription.IsEnabled)
                .OrderBy(subscription => subscription.Priority)
                .ThenBy(subscription => subscription.SubscriptionId)
                .ToArray();

            matches.Add(new AlertMatch(rule, enabledSubscriptions));
        }

        return matches;
    }
}

