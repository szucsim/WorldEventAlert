using WordEventAlerts.Core.Abstractions.Notifications;
using WordEventAlerts.Core.Domain;

namespace WordEventAlerts.Infrastructure.Notifications.Slack;

public sealed class SlackNotificationChannel : INotificationChannel
{
    public NotificationChannelType ChannelType => NotificationChannelType.Slack;

    public void ValidateDestination(string destination)
    {
        if (string.IsNullOrWhiteSpace(destination))
        {
            throw new ArgumentException("Slack destination is required.", nameof(destination));
        }

        if (!Uri.TryCreate(destination.Trim(), UriKind.Absolute, out var uri))
        {
            throw new ArgumentException("Slack destination must be an absolute URI.", nameof(destination));
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Slack destination must use HTTPS.", nameof(destination));
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
