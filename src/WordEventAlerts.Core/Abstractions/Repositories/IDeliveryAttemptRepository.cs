using WordEventAlerts.Core.Domain;

namespace WordEventAlerts.Core.Abstractions.Repositories;

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
