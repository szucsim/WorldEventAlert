using WorldEventAlerts.Core.Abstractions.Notifications;
using WorldEventAlerts.Core.Abstractions.Repositories;
using WorldEventAlerts.Core.Domain;

namespace WorldEventAlerts.Core.Services;

/// <summary>
/// Dispatches notification requests through channel strategies and records delivery attempts.
/// </summary>
public sealed class NotificationDispatcher : INotificationDispatcher
{
    private readonly IChannelRegistry _channelRegistry;
    private readonly IDeliveryAttemptRepository _deliveryAttemptRepository;

    /// <summary>
    /// Initializes a dispatcher with channel resolution and delivery persistence dependencies.
    /// </summary>
    /// <param name="channelRegistry">Registry used to resolve channel strategies.</param>
    /// <param name="deliveryAttemptRepository">Repository used to persist dispatch outcomes.</param>
    /// <exception cref="ArgumentNullException">Thrown when any dependency is null.</exception>
    public NotificationDispatcher(
        IChannelRegistry channelRegistry,
        IDeliveryAttemptRepository deliveryAttemptRepository)
    {
        _channelRegistry = channelRegistry ?? throw new ArgumentNullException(nameof(channelRegistry));
        _deliveryAttemptRepository = deliveryAttemptRepository ?? throw new ArgumentNullException(nameof(deliveryAttemptRepository));
    }

    /// <summary>
    /// Dispatches a notification request and stores the resulting delivery attempt.
    /// </summary>
    /// <param name="request">Notification payload and routing metadata.</param>
    /// <param name="attemptNumber">The 1-based attempt number used for retry tracking.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The persisted delivery attempt record.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="attemptNumber"/> is less than 1.</exception>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested before or during dispatch.</exception>
    public async Task<DeliveryAttempt> DispatchAsync(
        NotificationRequest request,
        int attemptNumber,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (attemptNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(attemptNumber), "Attempt number must be greater than zero.");
        }

        var channel = _channelRegistry.Resolve(request.ChannelType);
        NotificationSendResult result;

        try
        {
            channel.ValidateDestination(request.Destination);
            result = await channel.SendAsync(request, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            // Treat channel-level exceptions as permanent delivery failures and persist them for admin triage.
            result = NotificationSendResult.FailedPermanent(
                errorCode: "CHANNEL_DISPATCH_EXCEPTION",
                errorMessage: exception.Message);
        }

        var outcome = MapOutcome(result);

        var deliveryAttempt = new DeliveryAttempt(
            deliveryAttemptId: Guid.NewGuid(),
            eventId: request.EventId,
            ruleId: request.RuleId,
            userId: request.UserId,
            channelType: request.ChannelType,
            destination: request.Destination,
            attemptNumber: attemptNumber,
            outcome: outcome,
            attemptedAtUtc: DateTimeOffset.UtcNow,
            failureReason: result.IsSuccess ? null : result.ErrorMessage,
            correlationId: request.CorrelationId);

        await _deliveryAttemptRepository.SaveAsync(deliveryAttempt, cancellationToken);
        return deliveryAttempt;
    }

    private static DeliveryOutcome MapOutcome(NotificationSendResult result)
    {
        if (result.IsSuccess)
        {
            return DeliveryOutcome.Succeeded;
        }

        return result.IsTransientFailure
            ? DeliveryOutcome.FailedTransient
            : DeliveryOutcome.FailedPermanent;
    }
}

