using WorldEventAlerts.Core.Domain;

namespace WorldEventAlerts.Core.Abstractions.Matching;

/// <summary>
/// Evaluates world events against enabled alert rules and returns matching rule/subscription pairs.
/// </summary>
public interface IAlertMatchingEngine
{
    /// <summary>
    /// Matches a world event against active rules and their enabled channel subscriptions.
    /// </summary>
    /// <param name="worldEvent">The normalized world event to evaluate.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A read-only collection of matched rules and their enabled subscriptions.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="worldEvent"/> is null.</exception>
    Task<IReadOnlyCollection<AlertMatch>> MatchAsync(
        WorldEvent worldEvent,
        CancellationToken cancellationToken = default);
}

