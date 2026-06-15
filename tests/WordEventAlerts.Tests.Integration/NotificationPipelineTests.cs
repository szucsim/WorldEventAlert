using WordEventAlerts.Core.Abstractions.Notifications;
using WordEventAlerts.Core.Domain;
using WordEventAlerts.Core.Services;
using WordEventAlerts.Infrastructure.InMemory.Repositories;
using WordEventAlerts.Infrastructure.Notifications.Email;
using WordEventAlerts.Infrastructure.Notifications.Slack;

namespace WordEventAlerts.Tests.Integration;

public sealed class NotificationPipelineTests
{
    [Fact]
    public async Task DispatchAsync_ShouldPersistSucceededAttempt_ForEmailChannel()
    {
        var correlationId = Guid.NewGuid().ToString("N");
        var deliveryRepository = new InMemoryDeliveryAttemptRepository();
        var registry = new NotificationChannelRegistry(new INotificationChannel[]
        {
            new EmailNotificationChannel(),
            new SlackNotificationChannel()
        });
        var dispatcher = new NotificationDispatcher(registry, deliveryRepository);

        var request = new NotificationRequest(
            eventId: Guid.NewGuid(),
            ruleId: Guid.NewGuid(),
            userId: Guid.NewGuid(),
            channelType: NotificationChannelType.Email,
            destination: "alerts@example.com",
            subject: "Market alert",
            message: "Severity threshold exceeded.",
            correlationId: correlationId);

        var attempt = await dispatcher.DispatchAsync(request, attemptNumber: 1);
        var persisted = await deliveryRepository.ListByCorrelationIdAsync(correlationId);

        Assert.Equal(DeliveryOutcome.Succeeded, attempt.Outcome);
        Assert.Single(persisted);
        Assert.Equal(NotificationChannelType.Email, persisted.Single().ChannelType);
    }

    [Fact]
    public async Task DispatchAsync_ShouldThrow_ForInvalidSlackDestination()
    {
        var correlationId = Guid.NewGuid().ToString("N");
        var deliveryRepository = new InMemoryDeliveryAttemptRepository();
        var registry = new NotificationChannelRegistry(new INotificationChannel[]
        {
            new EmailNotificationChannel(),
            new SlackNotificationChannel()
        });
        var dispatcher = new NotificationDispatcher(registry, deliveryRepository);

        var request = new NotificationRequest(
            eventId: Guid.NewGuid(),
            ruleId: Guid.NewGuid(),
            userId: Guid.NewGuid(),
            channelType: NotificationChannelType.Slack,
            destination: "http://hooks.slack.local/test",
            subject: "Disaster alert",
            message: "Major event detected.",
            correlationId: correlationId);

        await Assert.ThrowsAsync<ArgumentException>(() => dispatcher.DispatchAsync(request, attemptNumber: 1));

        var persisted = await deliveryRepository.ListByCorrelationIdAsync(correlationId);
        Assert.Empty(persisted);
    }

    [Fact]
    public void NotificationChannelRegistry_ShouldThrow_WhenDuplicateChannelTypesRegistered()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new NotificationChannelRegistry(new INotificationChannel[]
            {
                new EmailNotificationChannel(),
                new EmailNotificationChannel()
            }));
    }
}
