using WordEventAlerts.Core.Domain;

namespace WordEventAlerts.Core.Abstractions.Notifications;

public interface IChannelRegistry
{
    INotificationChannel Resolve(NotificationChannelType channelType);
}
