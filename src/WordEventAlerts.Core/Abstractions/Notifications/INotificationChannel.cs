using WordEventAlerts.Core.Domain;

namespace WordEventAlerts.Core.Abstractions.Notifications;

/// <summary>
/// Defines a pluggable notification channel strategy for validating and sending messages.
/// </summary>
public interface INotificationChannel
{
    /// <summary>
    /// Gets the channel type handled by this strategy.
    /// </summary>
    NotificationChannelType ChannelType { get; }

    /// <summary>
    /// Validates the destination format used by this channel.
    /// </summary>
    /// <param name="destination">The destination value to validate.</param>
    /// <exception cref="ArgumentException">Thrown when the destination format is invalid.</exception>
    void ValidateDestination(string destination);

    /// <summary>
    /// Sends a notification request through the channel implementation.
    /// </summary>
    /// <param name="request">The request payload to send.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The send result indicating success or failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
    Task<NotificationSendResult> SendAsync(
        NotificationRequest request,
        CancellationToken cancellationToken = default);
}
