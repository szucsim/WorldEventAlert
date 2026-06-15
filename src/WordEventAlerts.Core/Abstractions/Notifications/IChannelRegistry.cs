using WordEventAlerts.Core.Domain;

namespace WordEventAlerts.Core.Abstractions.Notifications;

/// <summary>
/// Resolves notification channel strategies by channel type.
/// </summary>
public interface IChannelRegistry
{
    /// <summary>
    /// Resolves a registered notification channel implementation.
    /// </summary>
    /// <param name="channelType">The channel type to resolve.</param>
    /// <returns>The resolved channel strategy.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when no channel is registered for the type.</exception>
    INotificationChannel Resolve(NotificationChannelType channelType);
}
