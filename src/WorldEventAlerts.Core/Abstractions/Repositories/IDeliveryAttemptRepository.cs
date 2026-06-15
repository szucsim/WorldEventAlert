using WorldEventAlerts.Core.Domain;

namespace WorldEventAlerts.Core.Abstractions.Repositories;

/// <summary>
/// Provides persistence and query operations for notification delivery attempts.
/// </summary>
public interface IDeliveryAttemptRepository
{
    /// <summary>
    /// Persists a delivery attempt record.
    /// </summary>
    /// <param name="deliveryAttempt">The delivery attempt to persist.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task SaveAsync(DeliveryAttempt deliveryAttempt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a delivery attempt by its identifier.
    /// </summary>
    /// <param name="deliveryAttemptId">The delivery attempt identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The matching delivery attempt, or null when not found.</returns>
    Task<DeliveryAttempt?> GetByDeliveryAttemptIdAsync(
        Guid deliveryAttemptId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists delivery attempts for a world event identifier.
    /// </summary>
    /// <param name="eventId">The world event identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A read-only collection of matching delivery attempts.</returns>
    Task<IReadOnlyCollection<DeliveryAttempt>> ListByEventIdAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists delivery attempts for a correlation identifier.
    /// </summary>
    /// <param name="correlationId">The correlation identifier shared across related operations.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A read-only collection of matching delivery attempts.</returns>
    Task<IReadOnlyCollection<DeliveryAttempt>> ListByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists delivery attempts in dead-letter state.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A read-only collection of dead-lettered delivery attempts.</returns>
    Task<IReadOnlyCollection<DeliveryAttempt>> ListDeadLettersAsync(CancellationToken cancellationToken = default);
}

