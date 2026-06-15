using WorldEventAlerts.Core.Abstractions.Notifications;
using WorldEventAlerts.Core.Domain;

namespace WorldEventAlerts.Core.Services;

/// <summary>
/// Default in-memory registry that resolves notification channels by channel type.
/// </summary>
public sealed class NotificationChannelRegistry : IChannelRegistry
{
    private readonly IReadOnlyDictionary<NotificationChannelType, INotificationChannel> _channels;

    /// <summary>
    /// Initializes a registry using the provided channel implementations.
    /// </summary>
    /// <param name="channels">Registered channel strategies.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="channels"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when duplicate channel types are registered.</exception>
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

    /// <summary>
    /// Resolves the channel strategy for the specified channel type.
    /// </summary>
    /// <param name="channelType">The channel type to resolve.</param>
    /// <returns>The matching notification channel implementation.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when no channel is registered for the channel type.</exception>
    public INotificationChannel Resolve(NotificationChannelType channelType)
    {
        if (_channels.TryGetValue(channelType, out var channel))
        {
            return channel;
        }

        throw new KeyNotFoundException($"No notification channel registered for {channelType}.");
    }
}

