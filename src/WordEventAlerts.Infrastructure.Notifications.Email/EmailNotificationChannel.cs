using WordEventAlerts.Core.Abstractions.Notifications;
using WordEventAlerts.Core.Domain;

namespace WordEventAlerts.Infrastructure.Notifications.Email;

/// <summary>
/// Email channel strategy that validates email-like destinations and simulates successful sends.
/// </summary>
public sealed class EmailNotificationChannel : INotificationChannel
{
    /// <inheritdoc />
    public NotificationChannelType ChannelType => NotificationChannelType.Email;

    /// <inheritdoc />
    public void ValidateDestination(string destination)
    {
        if (string.IsNullOrWhiteSpace(destination))
        {
            throw new ArgumentException("Email destination is required.", nameof(destination));
        }

        var candidate = destination.Trim();

        if (!candidate.Contains('@', StringComparison.Ordinal) || candidate.StartsWith('@') || candidate.EndsWith('@'))
        {
            throw new ArgumentException("Email destination must be a valid email-like address.", nameof(destination));
        }
    }

    /// <inheritdoc />
    public Task<NotificationSendResult> SendAsync(
        NotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        cancellationToken.ThrowIfCancellationRequested();
        ValidateDestination(request.Destination);

        return Task.FromResult(NotificationSendResult.Succeeded());
    }
}
