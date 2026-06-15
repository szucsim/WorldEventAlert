using WordEventAlerts.Api.Contracts.Alerts;
using WordEventAlerts.Core.Abstractions.Repositories;
using WordEventAlerts.Core.Domain;
using WordEventAlerts.Infrastructure.Observability.Logging;

namespace WordEventAlerts.Api.Endpoints;

/// <summary>
/// Maps alert rule and channel subscription management API endpoints.
/// </summary>
public static class AlertRuleEndpoints
{
    /// <summary>
    /// Registers alert rule and subscription management routes.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The same route builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="endpoints"/> is null.</exception>
    public static IEndpointRouteBuilder MapAlertRuleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/api/alert-rules").WithTags("AlertRules");

        group.MapPut("/{ruleId:guid}", UpsertAlertRuleAsync)
            .WithName("UpsertAlertRule")
            .WithSummary("Creates or updates an alert rule.")
            .WithDescription("Creates or updates a user's alert rule including category, severity, region, and keyword filters.");

        group.MapGet("/{ruleId:guid}", GetAlertRuleAsync)
            .WithName("GetAlertRuleById")
            .WithSummary("Gets an alert rule by identifier.")
            .WithDescription("Returns the configured alert rule when it exists.");

        group.MapGet("", ListAlertRulesByUserAsync)
            .WithName("ListAlertRulesByUser")
            .WithSummary("Lists alert rules for a user.")
            .WithDescription("Returns all alert rules for the specified user ID.");

        group.MapPut("/{ruleId:guid}/subscriptions/{subscriptionId:guid}", UpsertSubscriptionAsync)
            .WithName("UpsertAlertRuleSubscription")
            .WithSummary("Creates or updates a channel subscription for a rule.")
            .WithDescription("Creates or updates notification channel settings for a rule.");

        group.MapGet("/{ruleId:guid}/subscriptions", ListSubscriptionsByRuleAsync)
            .WithName("ListSubscriptionsByRule")
            .WithSummary("Lists subscriptions configured for a rule.")
            .WithDescription("Returns all channel subscriptions for the provided rule ID.");

        return endpoints;
    }

    private static async Task<IResult> UpsertAlertRuleAsync(
        Guid ruleId,
        UpsertAlertRuleRequest request,
        HttpContext httpContext,
        IAlertRuleRepository alertRuleRepository,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("AlertRuleWrite");
        var correlationId = httpContext.GetCorrelationId();

        var rule = new AlertRule(
            ruleId: ruleId,
            userId: request.UserId,
            isEnabled: request.IsEnabled,
            categories: request.Categories,
            minimumSeverity: request.MinimumSeverity,
            regions: request.Regions,
            keywords: request.Keywords);

        await alertRuleRepository.UpsertAsync(rule, cancellationToken);

        logger.LogInformation(
            "{LogEvent} CorrelationId={CorrelationId} RuleId={RuleId} UserId={UserId} IsEnabled={IsEnabled}",
            ObservabilityConstants.LogEvents.AlertRuleUpserted,
            correlationId,
            rule.RuleId,
            rule.UserId,
            rule.IsEnabled);

        return Results.Ok(new
        {
            rule.RuleId,
            rule.UserId,
            rule.IsEnabled,
            Categories = rule.Categories,
            rule.MinimumSeverity,
            Regions = rule.Regions,
            Keywords = rule.Keywords
        });
    }

    private static async Task<IResult> GetAlertRuleAsync(
        Guid ruleId,
        HttpContext httpContext,
        IAlertRuleRepository alertRuleRepository,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("AlertRuleRead");
        var correlationId = httpContext.GetCorrelationId();
        var rule = await alertRuleRepository.GetByRuleIdAsync(ruleId, cancellationToken);

        if (rule is null)
        {
            return Results.NotFound();
        }

        logger.LogInformation(
            "{LogEvent} CorrelationId={CorrelationId} RuleId={RuleId} UserId={UserId}",
            ObservabilityConstants.LogEvents.AlertRuleRead,
            correlationId,
            rule.RuleId,
            rule.UserId);

        return Results.Ok(new
        {
            rule.RuleId,
            rule.UserId,
            rule.IsEnabled,
            Categories = rule.Categories,
            rule.MinimumSeverity,
            Regions = rule.Regions,
            Keywords = rule.Keywords
        });
    }

    private static async Task<IResult> ListAlertRulesByUserAsync(
        Guid userId,
        HttpContext httpContext,
        IAlertRuleRepository alertRuleRepository,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("AlertRuleList");
        var correlationId = httpContext.GetCorrelationId();
        var rules = await alertRuleRepository.ListByUserAsync(userId, cancellationToken);

        logger.LogInformation(
            "{LogEvent} CorrelationId={CorrelationId} UserId={UserId} Count={Count}",
            ObservabilityConstants.LogEvents.AlertRuleListByUser,
            correlationId,
            userId,
            rules.Count);

        return Results.Ok(rules.Select(rule => new
        {
            rule.RuleId,
            rule.UserId,
            rule.IsEnabled,
            Categories = rule.Categories,
            rule.MinimumSeverity,
            Regions = rule.Regions,
            Keywords = rule.Keywords
        }));
    }

    private static async Task<IResult> UpsertSubscriptionAsync(
        Guid ruleId,
        Guid subscriptionId,
        UpsertChannelSubscriptionRequest request,
        HttpContext httpContext,
        IUserPreferenceRepository userPreferenceRepository,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("AlertSubscriptionWrite");
        var correlationId = httpContext.GetCorrelationId();

        var subscription = new ChannelSubscription(
            subscriptionId: subscriptionId,
            userId: request.UserId,
            ruleId: ruleId,
            channelType: request.ChannelType,
            destination: request.Destination,
            isEnabled: request.IsEnabled,
            priority: request.Priority,
            maxRetryAttempts: request.MaxRetryAttempts);

        await userPreferenceRepository.UpsertSubscriptionAsync(subscription, cancellationToken);

        logger.LogInformation(
            "{LogEvent} CorrelationId={CorrelationId} RuleId={RuleId} UserId={UserId} SubscriptionId={SubscriptionId} ChannelType={ChannelType}",
            ObservabilityConstants.LogEvents.SubscriptionUpserted,
            correlationId,
            ruleId,
            request.UserId,
            subscriptionId,
            request.ChannelType);

        return Results.Ok(new
        {
            subscription.SubscriptionId,
            subscription.RuleId,
            subscription.UserId,
            subscription.ChannelType,
            subscription.Destination,
            subscription.IsEnabled,
            subscription.Priority,
            subscription.MaxRetryAttempts
        });
    }

    private static async Task<IResult> ListSubscriptionsByRuleAsync(
        Guid ruleId,
        HttpContext httpContext,
        IUserPreferenceRepository userPreferenceRepository,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("AlertSubscriptionList");
        var correlationId = httpContext.GetCorrelationId();
        var subscriptions = await userPreferenceRepository.ListByRuleAsync(ruleId, cancellationToken);

        logger.LogInformation(
            "{LogEvent} CorrelationId={CorrelationId} RuleId={RuleId} Count={Count}",
            ObservabilityConstants.LogEvents.SubscriptionListByRule,
            correlationId,
            ruleId,
            subscriptions.Count);

        return Results.Ok(subscriptions.Select(subscription => new
        {
            subscription.SubscriptionId,
            subscription.RuleId,
            subscription.UserId,
            subscription.ChannelType,
            subscription.Destination,
            subscription.IsEnabled,
            subscription.Priority,
            subscription.MaxRetryAttempts
        }));
    }
}
