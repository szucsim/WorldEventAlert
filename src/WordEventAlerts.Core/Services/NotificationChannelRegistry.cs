using WordEventAlerts.Core.Abstractions.Notifications;
using WordEventAlerts.Core.Domain;

namespace WordEventAlerts.Core.Services;

public sealed class NotificationChannelRegistry : IChannelRegistry
{
    private readonly IReadOnlyDictionary<NotificationChannelType, INotificationChannel> _channels;

    public NotificationChannelRegistry(IEnumerable<INotificationChannel> channels)
    {
        ArgumentNullException.ThrowIfNull(channels);

        var channelMap = new Dictionary<NotificationChannelType, INotificationChannel>();

        foreach (var channel in channels)
        {
            if (!channelMap.TryAdd(channel.ChannelType, channel))
            {
                throw new InvalidOperationException($"Duplicate channel registration detected for {channel.ChannelType}.");
            }
        }

        _channels = channelMap;
    }

    public INotificationChannel Resolve(NotificationChannelType channelType)
    {
        if (_channels.TryGetValue(channelType, out var channel))
        {
            return channel;
        }

        throw new KeyNotFoundException($"No notification channel registered for {channelType}.");
    }
}
