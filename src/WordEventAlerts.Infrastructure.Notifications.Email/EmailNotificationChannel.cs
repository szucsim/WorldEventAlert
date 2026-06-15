using WordEventAlerts.Core.Abstractions.Notifications;
using WordEventAlerts.Core.Domain;

namespace WordEventAlerts.Infrastructure.Notifications.Email;

/// <summary>
/// Email channel strategy that validates email-like destinations and simulates successful sends.
/// </summary>
public sealed class EmailNotificationChannel : INotificationChannel
{
    private static readonly TimeSpan SimulatedLatency = TimeSpan.FromMilliseconds(40);

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
    public async Task<NotificationSendResult> SendAsync(
        NotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        cancellationToken.ThrowIfCancellationRequested();
        ValidateDestination(request.Destination);

        await Task.Delay(SimulatedLatency, cancellationToken);

        var destination = request.Destination.Trim().ToLowerInvariant();
        var subject = request.Subject.Trim().ToLowerInvariant();

        if (destination.Contains("simulate-transient", StringComparison.Ordinal)
            || subject.Contains("simulate-transient", StringComparison.Ordinal))
        {
            return NotificationSendResult.FailedTransient(
                errorCode: "EMAIL_SIMULATED_TRANSIENT",
                errorMessage: "Simulated transient SMTP timeout. Retry can succeed later.");
        }

        if (destination.Contains("simulate-permanent", StringComparison.Ordinal)
            || subject.Contains("simulate-permanent", StringComparison.Ordinal))
        {
            return NotificationSendResult.FailedPermanent(
                errorCode: "EMAIL_SIMULATED_PERMANENT",
                errorMessage: "Simulated permanent recipient rejection.");
        }

        return NotificationSendResult.Succeeded();
    }
}
