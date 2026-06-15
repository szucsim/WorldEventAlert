using WorldEventAlerts.Core.Abstractions.Notifications;
using WorldEventAlerts.Core.Abstractions.Repositories;
using WorldEventAlerts.Core.Domain;
using WorldEventAlerts.Infrastructure.Observability.Logging;

namespace WorldEventAlerts.Api.Endpoints;

/// <summary>
/// Maps admin operational API endpoints for delivery visibility and replay.
/// </summary>
public static class AdminEndpoints
{
    private const string AdminRoutePrefix = "/api/v1/admin";

    /// <summary>
    /// Registers admin operational routes.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The same route builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="endpoints"/> is null.</exception>
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup(AdminRoutePrefix).WithTags("Admin");

        group.MapGet("/delivery-attempts/dead-letters", ListDeadLettersAsync)
            .WithName("AdminListDeadLetterDeliveryAttempts")
            .WithSummary("Lists dead-letter delivery attempts.")
            .WithDescription("Returns delivery attempts with dead-letter outcome for operational triage.");

        group.MapGet("/delivery-attempts/correlation/{correlationId}", ListByCorrelationIdAsync)
            .WithName("AdminListDeliveryAttemptsByCorrelation")
            .WithSummary("Lists delivery attempts for a correlation ID.")
            .WithDescription("Returns all delivery attempts linked to the provided correlation identifier.");

        group.MapGet("/delivery-attempts/{deliveryAttemptId:guid}", GetByIdAsync)
            .WithName("AdminGetDeliveryAttemptById")
            .WithSummary("Gets a delivery attempt by ID.")
            .WithDescription("Returns the requested delivery attempt record when it exists.");

        group.MapGet("/alerts", ListAlertsAsync)
            .WithName("AdminListAlerts")
            .WithSummary("Lists ingested alerts.")
            .WithDescription("Returns ingested world events for admin operational review.");

        group.MapPost("/delivery-attempts/{deliveryAttemptId:guid}/replay", ReplayDeliveryAttemptAsync)
            .WithName("AdminReplayDeliveryAttempt")
            .WithSummary("Replays a previous delivery attempt.")
            .WithDescription("Creates a new notification dispatch attempt based on an existing delivery attempt.");

        return endpoints;
    }

    private static async Task<IResult> ListDeadLettersAsync(
        HttpContext httpContext,
        IDeliveryAttemptRepository deliveryAttemptRepository,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("AdminDeliveries");
        var correlationId = httpContext.GetCorrelationId();
        var deadLetters = await deliveryAttemptRepository.ListDeadLettersAsync(cancellationToken);

        logger.LogInformation(
            "{LogEvent} CorrelationId={CorrelationId} Count={Count}",
            ObservabilityConstants.LogEvents.AdminDeadLettersListed,
            correlationId,
            deadLetters.Count);

        return Results.Ok(deadLetters.Select(ToResponse));
    }

    private static async Task<IResult> ListByCorrelationIdAsync(
        string correlationId,
        HttpContext httpContext,
        IDeliveryAttemptRepository deliveryAttemptRepository,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("AdminDeliveries");
        var requestCorrelationId = httpContext.GetCorrelationId();
        var attempts = await deliveryAttemptRepository.ListByCorrelationIdAsync(correlationId, cancellationToken);

        logger.LogInformation(
            "{LogEvent} CorrelationId={CorrelationId} FilterCorrelationId={FilterCorrelationId} Count={Count}",
            ObservabilityConstants.LogEvents.AdminDeliveriesByCorrelationListed,
            requestCorrelationId,
            correlationId,
            attempts.Count);

        return Results.Ok(attempts.Select(ToResponse));
    }

    private static async Task<IResult> GetByIdAsync(
        Guid deliveryAttemptId,
        HttpContext httpContext,
        IDeliveryAttemptRepository deliveryAttemptRepository,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("AdminDeliveries");
        var correlationId = httpContext.GetCorrelationId();
        var attempt = await deliveryAttemptRepository.GetByDeliveryAttemptIdAsync(deliveryAttemptId, cancellationToken);

        if (attempt is null)
        {
            return Results.NotFound();
        }

        logger.LogInformation(
            "{LogEvent} CorrelationId={CorrelationId} DeliveryAttemptId={DeliveryAttemptId}",
            ObservabilityConstants.LogEvents.AdminDeliveryAttemptRead,
            correlationId,
            deliveryAttemptId);

        return Results.Ok(ToResponse(attempt));
    }

    private static async Task<IResult> ListAlertsAsync(
        HttpContext httpContext,
        IWorldEventRepository worldEventRepository,
        IDeliveryAttemptRepository deliveryAttemptRepository,
        ILoggerFactory loggerFactory,
        int skip = 0,
        int take = 200,
        CancellationToken cancellationToken = default)
    {
        var logger = loggerFactory.CreateLogger("AdminAlerts");
        var correlationId = httpContext.GetCorrelationId();
        var safeTake = Math.Clamp(take, 1, 500);
        var alerts = await worldEventRepository.ListAsync(skip, safeTake, cancellationToken);

        logger.LogInformation(
            "{LogEvent} CorrelationId={CorrelationId} Skip={Skip} Take={Take} Count={Count}",
            ObservabilityConstants.LogEvents.AdminAlertsListed,
            correlationId,
            skip,
            safeTake,
            alerts.Count);

        var response = new List<object>(alerts.Count);

        foreach (var alert in alerts)
        {
            var attempts = await deliveryAttemptRepository.ListByEventIdAsync(alert.EventId, cancellationToken);
            var lastAttempt = attempts
                .OrderByDescending(item => item.AttemptedAtUtc)
                .ThenByDescending(item => item.AttemptNumber)
                .FirstOrDefault();

            response.Add(new
            {
                alert.EventId,
                alert.SourceEventId,
                alert.SourceSystem,
                alert.Category,
                alert.SeverityScore,
                alert.Headline,
                alert.Summary,
                Regions = alert.Regions,
                Keywords = alert.Keywords,
                alert.OccurredAtUtc,
                alert.IngestedAtUtc,
                alert.SchemaVersion,
                alert.CorrelationId,
                TotalAttempts = attempts.Count,
                SucceededAttempts = attempts.Count(item => item.Outcome == DeliveryOutcome.Succeeded),
                FailedTransientAttempts = attempts.Count(item => item.Outcome == DeliveryOutcome.FailedTransient),
                FailedPermanentAttempts = attempts.Count(item => item.Outcome == DeliveryOutcome.FailedPermanent),
                DeadLetteredAttempts = attempts.Count(item => item.Outcome == DeliveryOutcome.DeadLettered),
                LastOutcome = lastAttempt?.Outcome,
                LastFailureReason = lastAttempt?.FailureReason,
                LastAttemptedAtUtc = lastAttempt?.AttemptedAtUtc
            });
        }

        return Results.Ok(response);
    }

    private static async Task<IResult> ReplayDeliveryAttemptAsync(
        Guid deliveryAttemptId,
        HttpContext httpContext,
        IDeliveryAttemptRepository deliveryAttemptRepository,
        INotificationDispatcher notificationDispatcher,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("AdminReplay");
        var correlationId = httpContext.GetCorrelationId();
        var sourceAttempt = await deliveryAttemptRepository.GetByDeliveryAttemptIdAsync(deliveryAttemptId, cancellationToken);

        if (sourceAttempt is null)
        {
            return Results.NotFound();
        }

        logger.LogInformation(
            "{LogEvent} CorrelationId={CorrelationId} DeliveryAttemptId={DeliveryAttemptId}",
            ObservabilityConstants.LogEvents.AdminReplayRequested,
            correlationId,
            deliveryAttemptId);

        var replayRequest = new NotificationRequest(
            eventId: sourceAttempt.EventId,
            ruleId: sourceAttempt.RuleId,
            userId: sourceAttempt.UserId,
            channelType: sourceAttempt.ChannelType,
            destination: sourceAttempt.Destination,
            subject: $"Replay for event {sourceAttempt.EventId}",
            message: $"Replay initiated by admin for attempt {sourceAttempt.DeliveryAttemptId}.",
            correlationId: correlationId);

        try
        {
            var replayAttempt = await notificationDispatcher.DispatchAsync(
                replayRequest,
                sourceAttempt.AttemptNumber + 1,
                cancellationToken);

            logger.LogInformation(
                "{LogEvent} CorrelationId={CorrelationId} SourceAttemptId={SourceAttemptId} ReplayAttemptId={ReplayAttemptId} Outcome={Outcome}",
                ObservabilityConstants.LogEvents.AdminReplaySucceeded,
                correlationId,
                sourceAttempt.DeliveryAttemptId,
                replayAttempt.DeliveryAttemptId,
                replayAttempt.Outcome);

            return Results.Ok(new
            {
                sourceAttemptId = sourceAttempt.DeliveryAttemptId,
                replayAttemptId = replayAttempt.DeliveryAttemptId,
                attemptNumber = replayAttempt.AttemptNumber,
                outcome = replayAttempt.Outcome,
                correlationId = replayAttempt.CorrelationId
            });
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "{LogEvent} CorrelationId={CorrelationId} SourceAttemptId={SourceAttemptId}",
                ObservabilityConstants.LogEvents.AdminReplayFailed,
                correlationId,
                sourceAttempt.DeliveryAttemptId);

            throw;
        }
    }

    private static object ToResponse(DeliveryAttempt attempt)
    {
        return new
        {
            attempt.DeliveryAttemptId,
            attempt.EventId,
            attempt.RuleId,
            attempt.UserId,
            attempt.ChannelType,
            attempt.Destination,
            attempt.AttemptNumber,
            attempt.Outcome,
            attempt.AttemptedAtUtc,
            attempt.FailureReason,
            attempt.CorrelationId
        };
    }
}

