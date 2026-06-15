using WordEventAlerts.Core.Domain;

namespace WordEventAlerts.Core.Abstractions.Notifications;

/// <summary>
/// Dispatches notifications through channel strategies and persists delivery attempt outcomes.
/// </summary>
public interface INotificationDispatcher
{
    /// <summary>
    /// Dispatches a notification request and stores the resulting delivery attempt.
    /// </summary>
    /// <param name="request">The notification request to dispatch.</param>
    /// <param name="attemptNumber">The 1-based attempt number used for retry tracking.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The persisted delivery attempt record.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="attemptNumber"/> is less than 1.</exception>
    Task<DeliveryAttempt> DispatchAsync(
        NotificationRequest request,
        int attemptNumber,
        CancellationToken cancellationToken = default);
}
