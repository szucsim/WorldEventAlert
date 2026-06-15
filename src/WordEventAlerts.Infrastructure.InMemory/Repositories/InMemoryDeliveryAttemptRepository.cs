using System.Collections.Concurrent;
using WordEventAlerts.Core.Abstractions.Repositories;
using WordEventAlerts.Core.Domain;

namespace WordEventAlerts.Infrastructure.InMemory.Repositories;

/// <summary>
/// In-memory implementation of delivery attempt persistence and query operations.
/// </summary>
public sealed class InMemoryDeliveryAttemptRepository : IDeliveryAttemptRepository
{
    private readonly ConcurrentDictionary<Guid, DeliveryAttempt> _attempts = new();

    /// <inheritdoc />
    public Task SaveAsync(DeliveryAttempt deliveryAttempt, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(deliveryAttempt);

        cancellationToken.ThrowIfCancellationRequested();
        _attempts[deliveryAttempt.DeliveryAttemptId] = deliveryAttempt;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<DeliveryAttempt?> GetByDeliveryAttemptIdAsync(
        Guid deliveryAttemptId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _attempts.TryGetValue(deliveryAttemptId, out var attempt);
        return Task.FromResult(attempt);
    }

    /// <inheritdoc />
    public Task<IReadOnlyCollection<DeliveryAttempt>> ListByEventIdAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = _attempts.Values
            .Where(attempt => attempt.EventId == eventId)
            .OrderBy(attempt => attempt.AttemptedAtUtc)
            .ThenBy(attempt => attempt.AttemptNumber)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<DeliveryAttempt>>(result);
    }

    /// <inheritdoc />
    public Task<IReadOnlyCollection<DeliveryAttempt>> ListByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(correlationId))
        {
            throw new ArgumentException("Correlation ID is required.", nameof(correlationId));
        }

        var result = _attempts.Values
            .Where(attempt => string.Equals(attempt.CorrelationId, correlationId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(attempt => attempt.AttemptedAtUtc)
            .ThenBy(attempt => attempt.AttemptNumber)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<DeliveryAttempt>>(result);
    }

    /// <inheritdoc />
    public Task<IReadOnlyCollection<DeliveryAttempt>> ListDeadLettersAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = _attempts.Values
            .Where(attempt => attempt.Outcome == DeliveryOutcome.DeadLettered)
            .OrderByDescending(attempt => attempt.AttemptedAtUtc)
            .ThenByDescending(attempt => attempt.AttemptNumber)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<DeliveryAttempt>>(result);
    }
}
