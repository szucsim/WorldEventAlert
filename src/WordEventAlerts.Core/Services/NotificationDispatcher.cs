using WordEventAlerts.Core.Abstractions.Notifications;
using WordEventAlerts.Core.Abstractions.Repositories;
using WordEventAlerts.Core.Domain;

namespace WordEventAlerts.Core.Services;

public sealed class NotificationDispatcher : INotificationDispatcher
{
    private readonly IChannelRegistry _channelRegistry;
    private readonly IDeliveryAttemptRepository _deliveryAttemptRepository;

    public NotificationDispatcher(
        IChannelRegistry channelRegistry,
        IDeliveryAttemptRepository deliveryAttemptRepository)
    {
        _channelRegistry = channelRegistry ?? throw new ArgumentNullException(nameof(channelRegistry));
        _deliveryAttemptRepository = deliveryAttemptRepository ?? throw new ArgumentNullException(nameof(deliveryAttemptRepository));
    }

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
        channel.ValidateDestination(request.Destination);

        var result = await channel.SendAsync(request, cancellationToken);
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
