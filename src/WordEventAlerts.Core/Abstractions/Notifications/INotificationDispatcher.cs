using WordEventAlerts.Core.Domain;

namespace WordEventAlerts.Core.Abstractions.Notifications;

public interface INotificationDispatcher
{
    Task<DeliveryAttempt> DispatchAsync(
        NotificationRequest request,
        int attemptNumber,
        CancellationToken cancellationToken = default);
}
