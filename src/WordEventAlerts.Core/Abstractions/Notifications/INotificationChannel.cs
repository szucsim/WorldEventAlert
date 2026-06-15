using WordEventAlerts.Core.Domain;

namespace WordEventAlerts.Core.Abstractions.Notifications;

public interface INotificationChannel
{
    NotificationChannelType ChannelType { get; }

    void ValidateDestination(string destination);

    Task<NotificationSendResult> SendAsync(
        NotificationRequest request,
        CancellationToken cancellationToken = default);
}
