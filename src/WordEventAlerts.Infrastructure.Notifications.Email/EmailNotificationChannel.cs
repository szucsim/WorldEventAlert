using WordEventAlerts.Core.Abstractions.Notifications;
using WordEventAlerts.Core.Domain;

namespace WordEventAlerts.Infrastructure.Notifications.Email;

public sealed class EmailNotificationChannel : INotificationChannel
{
    public NotificationChannelType ChannelType => NotificationChannelType.Email;

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
