using WordEventAlerts.Core.Domain;

namespace WordEventAlerts.Core.Abstractions.Repositories;

public interface IDeliveryAttemptRepository
{
    Task SaveAsync(DeliveryAttempt deliveryAttempt, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<DeliveryAttempt>> ListByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<DeliveryAttempt>> ListDeadLettersAsync(CancellationToken cancellationToken = default);
}
