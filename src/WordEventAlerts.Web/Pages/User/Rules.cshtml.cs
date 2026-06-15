using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WordEventAlerts.Core.Domain;
using WordEventAlerts.Web.Services;

namespace WordEventAlerts.Web.Pages.User;

/// <summary>
/// Provides user-facing rule and subscription management workflow.
/// </summary>
public sealed class RulesModel : PageModel
{
    private readonly IAlertsApiClient _alertsApiClient;
    private readonly ILogger<RulesModel> _logger;

    public RulesModel(IAlertsApiClient alertsApiClient, ILogger<RulesModel> logger)
    {
        _alertsApiClient = alertsApiClient;
        _logger = logger;
    }

    /// <summary>
    /// Gets available event categories for rule configuration.
    /// </summary>
    public IReadOnlyCollection<WorldEventCategory> AvailableCategories { get; } = Enum.GetValues<WorldEventCategory>();

    /// <summary>
    /// Gets available notification channels for subscription configuration.
    /// </summary>
    public IReadOnlyCollection<NotificationChannelType> AvailableChannels { get; } = Enum.GetValues<NotificationChannelType>();

    /// <summary>
    /// Gets loaded rules for the selected user.
    /// </summary>
    public IReadOnlyCollection<AlertRuleDto> Rules { get; private set; } = Array.Empty<AlertRuleDto>();

    /// <summary>
    /// Gets loaded subscriptions for the selected rule.
    /// </summary>
    public IReadOnlyCollection<ChannelSubscriptionDto> Subscriptions { get; private set; } = Array.Empty<ChannelSubscriptionDto>();

    /// <summary>
    /// Gets a user-facing status message.
    /// </summary>
    public string? StatusMessage { get; private set; }

    /// <summary>
    /// Gets a user-facing error message.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    [BindProperty]
    public string UserIdInput { get; set; } = Guid.NewGuid().ToString();

    [BindProperty]
    public string RuleIdInput { get; set; } = string.Empty;

    [BindProperty]
    public bool RuleIsEnabled { get; set; } = true;

    [BindProperty]
    public int? MinimumSeverityInput { get; set; }

    [BindProperty]
    public List<string> SelectedCategoryNames { get; set; } = [];

    [BindProperty]
    public string RegionsCsv { get; set; } = string.Empty;

    [BindProperty]
    public string KeywordsCsv { get; set; } = string.Empty;

    [BindProperty]
    public string SubscriptionRuleIdInput { get; set; } = string.Empty;

    [BindProperty]
    public string SubscriptionIdInput { get; set; } = string.Empty;

    [BindProperty]
    public string SubscriptionUserIdInput { get; set; } = string.Empty;

    [BindProperty]
    public NotificationChannelType ChannelTypeInput { get; set; } = NotificationChannelType.Email;

    [BindProperty]
    public string DestinationInput { get; set; } = string.Empty;

    [BindProperty]
    public bool SubscriptionEnabled { get; set; } = true;

    [BindProperty]
    public int PriorityInput { get; set; }

    [BindProperty]
    public int? MaxRetryAttemptsInput { get; set; } = 3;

    /// <summary>
    /// Initializes default values for first page render.
    /// </summary>
    public void OnGet()
    {
        if (string.IsNullOrWhiteSpace(SubscriptionUserIdInput))
        {
            SubscriptionUserIdInput = UserIdInput;
        }
    }

    /// <summary>
    /// Loads rules for the entered user identifier.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The current page result.</returns>
    public async Task<IActionResult> OnPostLoadRulesAsync(CancellationToken cancellationToken)
    {
        if (!TryReadGuid(UserIdInput, out var userId))
        {
            ErrorMessage = "User ID must be a valid GUID.";
            return Page();
        }

        var correlationId = CreateCorrelationId();

        try
        {
            _logger.LogInformation(
                "UserRulesLoadRequested CorrelationId={CorrelationId} UserId={UserId}",
                correlationId,
                userId);

            Rules = await _alertsApiClient.ListRulesByUserAsync(userId, correlationId, cancellationToken);
            StatusMessage = $"Loaded {Rules.Count} rule(s) for user {userId}.";
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "UserRulesLoadFailed CorrelationId={CorrelationId}", correlationId);
            ErrorMessage = exception.Message;
        }

        return Page();
    }

    /// <summary>
    /// Creates or updates a rule using the current form values.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The current page result.</returns>
    public async Task<IActionResult> OnPostUpsertRuleAsync(CancellationToken cancellationToken)
    {
        if (!TryReadGuid(UserIdInput, out var userId))
        {
            ErrorMessage = "User ID must be a valid GUID.";
            return Page();
        }

        if (!TryReadOrCreateGuid(RuleIdInput, out var ruleId))
        {
            ErrorMessage = "Rule ID must be empty or a valid GUID.";
            return Page();
        }

        RuleIdInput = ruleId.ToString();
        SubscriptionRuleIdInput = ruleId.ToString();
        SubscriptionUserIdInput = userId.ToString();
        var categories = ParseCategories(SelectedCategoryNames);
        var correlationId = CreateCorrelationId();

        try
        {
            _logger.LogInformation(
                "UserRuleUpsertRequested CorrelationId={CorrelationId} RuleId={RuleId} UserId={UserId}",
                correlationId,
                ruleId,
                userId);

            var request = new UpsertAlertRuleRequestDto
            {
                UserId = userId,
                IsEnabled = RuleIsEnabled,
                MinimumSeverity = MinimumSeverityInput,
                Categories = categories,
                Regions = ParseCsv(RegionsCsv),
                Keywords = ParseCsv(KeywordsCsv)
            };

            _ = await _alertsApiClient.UpsertRuleAsync(ruleId, request, correlationId, cancellationToken);
            Rules = await _alertsApiClient.ListRulesByUserAsync(userId, correlationId, cancellationToken);
            StatusMessage = $"Rule {ruleId} saved successfully.";
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "UserRuleUpsertFailed CorrelationId={CorrelationId}", correlationId);
            ErrorMessage = exception.Message;
        }

        return Page();
    }

    /// <summary>
    /// Loads subscriptions configured for a rule.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The current page result.</returns>
    public async Task<IActionResult> OnPostLoadSubscriptionsAsync(CancellationToken cancellationToken)
    {
        if (!TryReadGuid(SubscriptionRuleIdInput, out var ruleId))
        {
            ErrorMessage = "Subscription Rule ID must be a valid GUID.";
            return Page();
        }

        var correlationId = CreateCorrelationId();

        try
        {
            _logger.LogInformation(
                "UserSubscriptionsLoadRequested CorrelationId={CorrelationId} RuleId={RuleId}",
                correlationId,
                ruleId);

            Subscriptions = await _alertsApiClient.ListSubscriptionsByRuleAsync(ruleId, correlationId, cancellationToken);
            StatusMessage = $"Loaded {Subscriptions.Count} subscription(s) for rule {ruleId}.";
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "UserSubscriptionsLoadFailed CorrelationId={CorrelationId}", correlationId);
            ErrorMessage = exception.Message;
        }

        return Page();
    }

    /// <summary>
    /// Creates or updates a subscription using the current form values.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The current page result.</returns>
    public async Task<IActionResult> OnPostUpsertSubscriptionAsync(CancellationToken cancellationToken)
    {
        if (!TryReadGuid(SubscriptionRuleIdInput, out var ruleId))
        {
            ErrorMessage = "Subscription Rule ID must be a valid GUID.";
            return Page();
        }

        if (!TryReadGuid(SubscriptionUserIdInput, out var userId))
        {
            ErrorMessage = "Subscription User ID must be a valid GUID.";
            return Page();
        }

        if (!TryReadOrCreateGuid(SubscriptionIdInput, out var subscriptionId))
        {
            ErrorMessage = "Subscription ID must be empty or a valid GUID.";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(DestinationInput))
        {
            ErrorMessage = "Destination is required for a subscription.";
            return Page();
        }

        SubscriptionIdInput = subscriptionId.ToString();
        var correlationId = CreateCorrelationId();

        try
        {
            _logger.LogInformation(
                "UserSubscriptionUpsertRequested CorrelationId={CorrelationId} RuleId={RuleId} SubscriptionId={SubscriptionId}",
                correlationId,
                ruleId,
                subscriptionId);

            var request = new UpsertChannelSubscriptionRequestDto
            {
                UserId = userId,
                ChannelType = ChannelTypeInput,
                Destination = DestinationInput.Trim(),
                IsEnabled = SubscriptionEnabled,
                Priority = PriorityInput,
                MaxRetryAttempts = MaxRetryAttemptsInput
            };

            _ = await _alertsApiClient.UpsertSubscriptionAsync(
                ruleId,
                subscriptionId,
                request,
                correlationId,
                cancellationToken);

            Subscriptions = await _alertsApiClient.ListSubscriptionsByRuleAsync(ruleId, correlationId, cancellationToken);
            StatusMessage = $"Subscription {subscriptionId} saved successfully.";
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "UserSubscriptionUpsertFailed CorrelationId={CorrelationId}", correlationId);
            ErrorMessage = exception.Message;
        }

        return Page();
    }

    private static bool TryReadGuid(string? input, out Guid id)
    {
        return Guid.TryParse(input, out id);
    }

    private static bool TryReadOrCreateGuid(string? input, out Guid id)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            id = Guid.NewGuid();
            return true;
        }

        return Guid.TryParse(input, out id);
    }

    private static IReadOnlyCollection<string> ParseCsv(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Array.Empty<string>();
        }

        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyCollection<WorldEventCategory> ParseCategories(IEnumerable<string> selectedCategoryNames)
    {
        var categories = new List<WorldEventCategory>();

        foreach (var categoryName in selectedCategoryNames)
        {
            if (Enum.TryParse<WorldEventCategory>(categoryName, ignoreCase: true, out var parsed))
            {
                categories.Add(parsed);
            }
        }

        return categories.Distinct().ToArray();
    }

    private static string CreateCorrelationId()
    {
        return Guid.NewGuid().ToString("N");
    }
}
