using WordEventAlerts.Core.Domain;

namespace WordEventAlerts.Core.Abstractions.Repositories;

/// <summary>
/// Provides storage operations for normalized world event records.
/// </summary>
public interface IWorldEventRepository
{
    /// <summary>
    /// Persists a world event record.
    /// </summary>
    /// <param name="worldEvent">The event to persist.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task SaveAsync(WorldEvent worldEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a world event by its internal event identifier.
    /// </summary>
    /// <param name="eventId">The event identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The matching world event, or null when not found.</returns>
    Task<WorldEvent?> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists world events using skip/take pagination.
    /// </summary>
    /// <param name="skip">Number of records to skip.</param>
    /// <param name="take">Maximum number of records to return.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A paged read-only collection of world events.</returns>
    Task<IReadOnlyCollection<WorldEvent>> ListAsync(int skip, int take, CancellationToken cancellationToken = default);
}
