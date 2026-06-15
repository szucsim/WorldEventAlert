using WordEventAlerts.Api.Contracts.Events;
using WordEventAlerts.Core.Abstractions.Matching;
using WordEventAlerts.Core.Abstractions.Notifications;
using WordEventAlerts.Core.Abstractions.Repositories;
using WordEventAlerts.Core.Domain;
using WordEventAlerts.Infrastructure.Observability.Logging;

namespace WordEventAlerts.Api.Endpoints;

/// <summary>
/// Maps event ingestion and event lookup API endpoints.
/// </summary>
public static class EventIngestionEndpoints
{
    private const string ApiVersion = "v1";

    private const string EventsRoutePrefix = "/api/v1/events";

    /// <summary>
    /// Registers event ingestion and event query routes.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The same route builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="endpoints"/> is null.</exception>
    public static IEndpointRouteBuilder MapEventIngestionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup(EventsRoutePrefix).WithTags("Events");

        group.MapPost("", IngestEventAsync)
            .WithName("IngestWorldEvent")
            .WithSummary("Ingests a normalized world event and triggers matching/dispatch.")
            .WithDescription("Stores the event, matches enabled alert rules, and dispatches matching channel notifications.");

        group.MapGet("/{eventId:guid}", GetEventByIdAsync)
            .WithName("GetWorldEventById")
            .WithSummary("Gets a world event by internal identifier.")
            .WithDescription("Returns the normalized world event payload for the provided event ID.");

        return endpoints;
    }

    private static async Task<IResult> IngestEventAsync(
        IngestWorldEventRequest request,
        HttpContext httpContext,
        IWorldEventRepository worldEventRepository,
        IAlertMatchingEngine alertMatchingEngine,
        INotificationDispatcher notificationDispatcher,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("EventIngestion");
        var correlationId = httpContext.GetCorrelationId();
        var now = DateTimeOffset.UtcNow;

        var worldEvent = new WorldEvent(
            eventId: request.EventId ?? Guid.NewGuid(),
            sourceEventId: request.SourceEventId,
            sourceSystem: request.SourceSystem,
            category: request.Category,
            severityScore: request.SeverityScore,
            headline: request.Headline,
            summary: request.Summary,
            regions: request.Regions,
            keywords: request.Keywords,
            occurredAtUtc: request.OccurredAtUtc ?? now,
            ingestedAtUtc: now,
            schemaVersion: ApiVersion,
            correlationId: correlationId);

        await worldEventRepository.SaveAsync(worldEvent, cancellationToken);
        logger.LogInformation(
            "{LogEvent} CorrelationId={CorrelationId} EventId={EventId} Category={Category} SeverityScore={SeverityScore}",
            ObservabilityConstants.LogEvents.EventIngested,
            correlationId,
            worldEvent.EventId,
            worldEvent.Category,
            worldEvent.SeverityScore);

        var matches = await alertMatchingEngine.MatchAsync(worldEvent, cancellationToken);
        var dispatchedNotifications = 0;
        var failedNotifications = 0;

        if (matches.Count == 0)
        {
            logger.LogInformation(
                "{LogEvent} CorrelationId={CorrelationId} EventId={EventId}",
                ObservabilityConstants.LogEvents.RuleNotMatched,
                correlationId,
                worldEvent.EventId);
        }

        foreach (var match in matches)
        {
            logger.LogInformation(
                "{LogEvent} CorrelationId={CorrelationId} EventId={EventId} RuleId={RuleId} SubscriptionCount={SubscriptionCount}",
                ObservabilityConstants.LogEvents.RuleMatched,
                correlationId,
                worldEvent.EventId,
                match.Rule.RuleId,
                match.Subscriptions.Count);

            foreach (var subscription in match.Subscriptions)
            {
                var notificationRequest = new NotificationRequest(
                    eventId: worldEvent.EventId,
                    ruleId: match.Rule.RuleId,
                    userId: match.Rule.UserId,
                    channelType: subscription.ChannelType,
                    destination: subscription.Destination,
                    subject: $"{worldEvent.Category}: {worldEvent.Headline}",
                    message: worldEvent.Summary,
                    correlationId: correlationId);

                logger.LogInformation(
                    "{LogEvent} CorrelationId={CorrelationId} EventId={EventId} RuleId={RuleId} UserId={UserId} ChannelType={ChannelType}",
                    ObservabilityConstants.LogEvents.DeliveryAttempted,
                    correlationId,
                    worldEvent.EventId,
                    match.Rule.RuleId,
                    match.Rule.UserId,
                    subscription.ChannelType);

                try
                {
                    var attempt = await notificationDispatcher.DispatchAsync(
                        notificationRequest,
                        attemptNumber: 1,
                        cancellationToken: cancellationToken);

                    dispatchedNotifications++;

                    logger.LogInformation(
                        "{LogEvent} CorrelationId={CorrelationId} EventId={EventId} RuleId={RuleId} UserId={UserId} ChannelType={ChannelType} Outcome={Outcome}",
                        attempt.Outcome == DeliveryOutcome.Succeeded
                            ? ObservabilityConstants.LogEvents.DeliverySucceeded
                            : ObservabilityConstants.LogEvents.DeliveryFailed,
                        correlationId,
                        worldEvent.EventId,
                        match.Rule.RuleId,
                        match.Rule.UserId,
                        subscription.ChannelType,
                        attempt.Outcome);
                }
                catch (Exception exception)
                {
                    failedNotifications++;
                    logger.LogError(
                        exception,
                        "{LogEvent} CorrelationId={CorrelationId} EventId={EventId} RuleId={RuleId} UserId={UserId} ChannelType={ChannelType}",
                        ObservabilityConstants.LogEvents.DeliveryFailed,
                        correlationId,
                        worldEvent.EventId,
                        match.Rule.RuleId,
                        match.Rule.UserId,
                        subscription.ChannelType);
                }
            }
        }

        var response = new IngestWorldEventResponse
        {
            EventId = worldEvent.EventId,
            CorrelationId = correlationId,
            MatchedRules = matches.Count,
            DispatchedNotifications = dispatchedNotifications,
            FailedNotifications = failedNotifications,
            IngestedAtUtc = worldEvent.IngestedAtUtc
        };

        return Results.Accepted($"{EventsRoutePrefix}/{worldEvent.EventId}", response);
    }

    private static async Task<IResult> GetEventByIdAsync(
        Guid eventId,
        HttpContext httpContext,
        IWorldEventRepository worldEventRepository,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("EventRead");
        var correlationId = httpContext.GetCorrelationId();
        var worldEvent = await worldEventRepository.GetByEventIdAsync(eventId, cancellationToken);

        if (worldEvent is null)
        {
            return Results.NotFound();
        }

        logger.LogInformation(
            "{LogEvent} CorrelationId={CorrelationId} EventId={EventId}",
            ObservabilityConstants.LogEvents.EventRead,
            correlationId,
            worldEvent.EventId);

        return Results.Ok(new
        {
            worldEvent.EventId,
            worldEvent.SourceEventId,
            worldEvent.SourceSystem,
            worldEvent.Category,
            worldEvent.SeverityScore,
            worldEvent.Headline,
            worldEvent.Summary,
            Regions = worldEvent.Regions,
            Keywords = worldEvent.Keywords,
            worldEvent.OccurredAtUtc,
            worldEvent.IngestedAtUtc,
            worldEvent.SchemaVersion,
            worldEvent.CorrelationId
        });
    }
}
