using WorldEventAlerts.Core.Abstractions.Notifications;
using WorldEventAlerts.Core.Domain;

namespace WorldEventAlerts.Infrastructure.Notifications.Slack;

/// <summary>
/// Slack channel strategy that validates HTTPS webhook destinations and simulates successful sends.
/// </summary>
public sealed class SlackNotificationChannel : INotificationChannel
{
    private static readonly TimeSpan SimulatedLatency = TimeSpan.FromMilliseconds(35);

    /// <inheritdoc />
    public NotificationChannelType ChannelType => NotificationChannelType.Slack;

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task<NotificationSendResult> SendAsync(
        NotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        cancellationToken.ThrowIfCancellationRequested();
        ValidateDestination(request.Destination);

        await Task.Delay(SimulatedLatency, cancellationToken);

        var destination = request.Destination.Trim().ToLowerInvariant();
        var message = request.Message.Trim().ToLowerInvariant();

        if (destination.Contains("simulate-transient", StringComparison.Ordinal)
            || message.Contains("simulate-transient", StringComparison.Ordinal))
        {
            return NotificationSendResult.FailedTransient(
                errorCode: "SLACK_SIMULATED_TRANSIENT",
                errorMessage: "Simulated transient Slack webhook throttling.");
        }

        if (destination.Contains("simulate-permanent", StringComparison.Ordinal)
            || message.Contains("simulate-permanent", StringComparison.Ordinal))
        {
            return NotificationSendResult.FailedPermanent(
                errorCode: "SLACK_SIMULATED_PERMANENT",
                errorMessage: "Simulated permanent Slack webhook authorization failure.");
        }

        return NotificationSendResult.Succeeded();
    }
}

