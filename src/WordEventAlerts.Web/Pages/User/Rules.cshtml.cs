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
    private static readonly Guid DemoUserId = Guid.Parse("8ee9ea2e-0a64-4ebb-ac45-fc2bba623a83");

    private readonly IAlertsApiClient _alertsApiClient;
    private readonly ILogger<RulesModel> _logger;

    /// <summary>
    /// Initializes the user rules page model with API access and logging dependencies.
    /// </summary>
    /// <param name="alertsApiClient">Typed client used for rules and subscription API operations.</param>
    /// <param name="logger">Logger used for workflow telemetry and error diagnostics.</param>
    /// <exception cref="ArgumentNullException">Thrown when any dependency is null.</exception>
    public RulesModel(IAlertsApiClient alertsApiClient, ILogger<RulesModel> logger)
    {
        _alertsApiClient = alertsApiClient ?? throw new ArgumentNullException(nameof(alertsApiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the fixed demo user identifier used for the Phase 1 UI workflow.
    /// </summary>
    public Guid EffectiveUserId => DemoUserId;

    /// <summary>
    /// Gets available event categories for rule configuration.
    /// </summary>
    public IReadOnlyCollection<WorldEventCategory> AvailableCategories { get; } = Enum.GetValues<WorldEventCategory>();

    /// <summary>
    /// Gets available notification channels for subscription configuration.
    /// </summary>
    public IReadOnlyCollection<NotificationChannelType> AvailableChannels { get; } = Enum.GetValues<NotificationChannelType>();

    /// <summary>
    /// Gets available severity options for rule configuration.
    /// </summary>
    public IReadOnlyCollection<int> AvailableSeverityValues { get; } = Enumerable.Range(0, 11).Select(index => index * 10).ToArray();

    /// <summary>
    /// Gets loaded rules for the selected user.
    /// </summary>
    public IReadOnlyCollection<AlertRuleDto> Rules { get; private set; } = Array.Empty<AlertRuleDto>();

    /// <summary>
    /// Gets loaded subscriptions for the selected user.
    /// </summary>
    public IReadOnlyCollection<ChannelSubscriptionDto> Subscriptions { get; private set; } = Array.Empty<ChannelSubscriptionDto>();

    /// <summary>
    /// Gets subscriptions grouped by channel type for streamlined review.
    /// </summary>
    public IReadOnlyCollection<SubscriptionChannelGroup> GroupedSubscriptions { get; private set; } = Array.Empty<SubscriptionChannelGroup>();

    /// <summary>
    /// Gets a value indicating whether the rule form is editing an existing rule.
    /// </summary>
    public bool IsEditingRule => TryReadGuid(EditingRuleIdInput, out _);

    /// <summary>
    /// Gets a value indicating whether the subscription form is editing an existing subscription.
    /// </summary>
    public bool IsEditingSubscription => TryReadGuid(EditingSubscriptionIdInput, out _);

    /// <summary>
    /// Gets a user-facing status message.
    /// </summary>
    public string? StatusMessage { get; private set; }

    /// <summary>
    /// Gets a user-facing error message.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    [BindProperty]
    public string RuleNameInput { get; set; } = string.Empty;

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
    public string SelectedRuleIdInput { get; set; } = string.Empty;

    [BindProperty]
    public string EditingRuleIdInput { get; set; } = string.Empty;

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

    [BindProperty]
    public string EditingSubscriptionIdInput { get; set; } = string.Empty;

    /// <summary>
    /// Initializes default values for first page render.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when initial data loading has finished.</returns>
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var correlationId = CreateCorrelationId();

        try
        {
            await LoadPageDataAsync(correlationId, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "UserRulesInitialLoadFailed CorrelationId={CorrelationId}", correlationId);
            ErrorMessage = exception.Message;
        }
    }

    /// <summary>
    /// Loads rules for the entered user identifier.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The current page result.</returns>
    public async Task<IActionResult> OnPostLoadRulesAsync(CancellationToken cancellationToken)
    {
        var correlationId = CreateCorrelationId();

        try
        {
            _logger.LogInformation(
                "UserRulesLoadRequested CorrelationId={CorrelationId} UserId={UserId}",
                correlationId,
                DemoUserId);

            await LoadPageDataAsync(correlationId, cancellationToken);
            StatusMessage = $"Loaded {Rules.Count} rule(s).";
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
        var correlationId = CreateCorrelationId();

        if (string.IsNullOrWhiteSpace(RuleNameInput))
        {
            ErrorMessage = "Rule name is required.";
            await LoadPageDataAsync(correlationId, cancellationToken);
            return Page();
        }

        var isEditingRule = TryReadGuid(EditingRuleIdInput, out var editingRuleId);
        var ruleId = isEditingRule ? editingRuleId : Guid.NewGuid();
        var categories = ParseCategories(SelectedCategoryNames);

        try
        {
            _logger.LogInformation(
                "UserRuleUpsertRequested CorrelationId={CorrelationId} RuleId={RuleId} UserId={UserId}",
                correlationId,
                ruleId,
                DemoUserId);

            var request = new UpsertAlertRuleRequestDto
            {
                UserId = DemoUserId,
                Name = RuleNameInput.Trim(),
                IsEnabled = RuleIsEnabled,
                MinimumSeverity = MinimumSeverityInput,
                Categories = categories,
                Regions = ParseCsv(RegionsCsv),
                Keywords = ParseCsv(KeywordsCsv)
            };

            _ = await _alertsApiClient.UpsertRuleAsync(ruleId, request, correlationId, cancellationToken);
            await LoadPageDataAsync(correlationId, cancellationToken);
            SelectedRuleIdInput = ruleId.ToString();

            // Always return to create mode after save so users can quickly add the next item.
            ResetRuleForm();

            StatusMessage = isEditingRule
                ? $"Rule '{request.Name}' updated successfully."
                : $"Rule '{request.Name}' created successfully.";
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "UserRuleUpsertFailed CorrelationId={CorrelationId}", correlationId);
            ErrorMessage = exception.Message;
        }

        return Page();
    }

    /// <summary>
    /// Loads a selected rule into form edit mode.
    /// </summary>
    /// <param name="ruleId">The rule identifier to load.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The current page result.</returns>
    public async Task<IActionResult> OnPostEditRuleAsync(Guid ruleId, CancellationToken cancellationToken)
    {
        var correlationId = CreateCorrelationId();

        try
        {
            await LoadPageDataAsync(correlationId, cancellationToken);
            var rule = Rules.FirstOrDefault(item => item.RuleId == ruleId);

            if (rule is null)
            {
                ErrorMessage = $"Rule {ruleId} was not found.";
                return Page();
            }

            ApplyRuleToForm(rule);
            SelectedRuleIdInput = rule.RuleId.ToString();
            EditingRuleIdInput = rule.RuleId.ToString();
            StatusMessage = $"Rule '{rule.Name}' loaded for editing.";
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "UserRuleEditLoadFailed CorrelationId={CorrelationId} RuleId={RuleId}", correlationId, ruleId);
            ErrorMessage = exception.Message;
        }

        return Page();
    }

    /// <summary>
    /// Deletes a rule and any associated subscriptions.
    /// </summary>
    /// <param name="ruleId">The rule identifier to delete.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The current page result.</returns>
    public async Task<IActionResult> OnPostDeleteRuleAsync(Guid ruleId, CancellationToken cancellationToken)
    {
        var correlationId = CreateCorrelationId();

        try
        {
            var wasDeleted = await _alertsApiClient.DeleteRuleAsync(ruleId, correlationId, cancellationToken);
            await LoadPageDataAsync(correlationId, cancellationToken);

            if (wasDeleted)
            {
                if (string.Equals(EditingRuleIdInput, ruleId.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    ResetRuleForm();
                }

                StatusMessage = $"Rule {ruleId} deleted successfully.";
            }
            else
            {
                ErrorMessage = $"Rule {ruleId} was not found.";
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "UserRuleDeleteFailed CorrelationId={CorrelationId} RuleId={RuleId}", correlationId, ruleId);
            ErrorMessage = exception.Message;
        }

        return Page();
    }

    /// <summary>
    /// Clears the current rule edit state and resets the rule form.
    /// </summary>
    /// <returns>The current page result.</returns>
    public async Task<IActionResult> OnPostClearRuleEditAsync(CancellationToken cancellationToken)
    {
        var correlationId = CreateCorrelationId();
        ResetRuleForm();
        await LoadPageDataAsync(correlationId, cancellationToken);
        StatusMessage = "Rule edit form reset.";
        return Page();
    }

    /// <summary>
    /// Loads subscriptions configured for a rule.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The current page result.</returns>
    public async Task<IActionResult> OnPostLoadSubscriptionsAsync(CancellationToken cancellationToken)
    {
        var correlationId = CreateCorrelationId();

        try
        {
            await LoadRulesAsync(correlationId, cancellationToken);
            await LoadSubscriptionsAsync(correlationId, cancellationToken);

            _logger.LogInformation(
                "UserSubscriptionsLoadRequested CorrelationId={CorrelationId} UserId={UserId}",
                correlationId,
                DemoUserId);

            StatusMessage = $"Loaded {Subscriptions.Count} subscription(s).";
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
        var correlationId = CreateCorrelationId();

        try
        {
            await LoadPageDataAsync(correlationId, cancellationToken);

            if (!TryReadGuid(SelectedRuleIdInput, out var ruleId))
            {
                ErrorMessage = "Please select a rule first.";
                return Page();
            }

            if (string.IsNullOrWhiteSpace(DestinationInput))
            {
                ErrorMessage = "Destination is required for a subscription.";
                return Page();
            }

            if (!TryValidateDestinationForChannel(ChannelTypeInput, DestinationInput, out var destinationValidationError))
            {
                ErrorMessage = destinationValidationError;
                return Page();
            }

            var isEditingSubscription = TryReadGuid(EditingSubscriptionIdInput, out var editingSubscriptionId);
            var subscriptionId = isEditingSubscription ? editingSubscriptionId : Guid.NewGuid();

            _logger.LogInformation(
                "UserSubscriptionUpsertRequested CorrelationId={CorrelationId} RuleId={RuleId} SubscriptionId={SubscriptionId}",
                correlationId,
                ruleId,
                subscriptionId);

            var request = new UpsertChannelSubscriptionRequestDto
            {
                UserId = DemoUserId,
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

            await LoadSubscriptionsAsync(correlationId, cancellationToken);

            // Always return to create mode after save so users can quickly add the next item.
            ResetSubscriptionForm();
            StatusMessage = isEditingSubscription
                ? $"Subscription {subscriptionId} updated successfully."
                : $"Subscription created successfully for rule {ruleId}.";
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "UserSubscriptionUpsertFailed CorrelationId={CorrelationId}", correlationId);
            ErrorMessage = exception.Message;
        }

        return Page();
    }

    /// <summary>
    /// Loads a selected subscription into form edit mode.
    /// </summary>
    /// <param name="subscriptionId">The subscription identifier to load.</param>
    /// <param name="ruleId">The rule identifier associated to the subscription.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The current page result.</returns>
    public async Task<IActionResult> OnPostEditSubscriptionAsync(Guid subscriptionId, Guid ruleId, CancellationToken cancellationToken)
    {
        var correlationId = CreateCorrelationId();

        try
        {
            await LoadPageDataAsync(correlationId, cancellationToken);
            var subscription = Subscriptions.FirstOrDefault(item => item.SubscriptionId == subscriptionId && item.RuleId == ruleId);

            if (subscription is null)
            {
                ErrorMessage = $"Subscription {subscriptionId} was not found.";
                return Page();
            }

            ApplySubscriptionToForm(subscription);
            EditingSubscriptionIdInput = subscription.SubscriptionId.ToString();
            StatusMessage = $"Subscription {subscription.SubscriptionId} loaded for editing.";
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "UserSubscriptionEditLoadFailed CorrelationId={CorrelationId} SubscriptionId={SubscriptionId}", correlationId, subscriptionId);
            ErrorMessage = exception.Message;
        }

        return Page();
    }

    /// <summary>
    /// Deletes a subscription from the selected rule.
    /// </summary>
    /// <param name="subscriptionId">The subscription identifier to delete.</param>
    /// <param name="ruleId">The rule identifier associated to the subscription.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The current page result.</returns>
    public async Task<IActionResult> OnPostDeleteSubscriptionAsync(Guid subscriptionId, Guid ruleId, CancellationToken cancellationToken)
    {
        var correlationId = CreateCorrelationId();

        try
        {
            await LoadRulesAsync(correlationId, cancellationToken);

            var wasDeleted = await _alertsApiClient.DeleteSubscriptionAsync(ruleId, subscriptionId, correlationId, cancellationToken);
            await LoadSubscriptionsAsync(correlationId, cancellationToken);

            if (wasDeleted)
            {
                if (string.Equals(EditingSubscriptionIdInput, subscriptionId.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    ResetSubscriptionForm();
                }

                StatusMessage = $"Subscription {subscriptionId} deleted successfully.";
            }
            else
            {
                ErrorMessage = $"Subscription {subscriptionId} was not found.";
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "UserSubscriptionDeleteFailed CorrelationId={CorrelationId} SubscriptionId={SubscriptionId}", correlationId, subscriptionId);
            ErrorMessage = exception.Message;
        }

        return Page();
    }

    /// <summary>
    /// Clears the current subscription edit state and resets the subscription form.
    /// </summary>
    /// <returns>The current page result.</returns>
    public async Task<IActionResult> OnPostClearSubscriptionEditAsync(CancellationToken cancellationToken)
    {
        var correlationId = CreateCorrelationId();
        ResetSubscriptionForm();
        await LoadPageDataAsync(correlationId, cancellationToken);
        StatusMessage = "Subscription edit form reset.";
        return Page();
    }

    private static bool TryReadGuid(string? input, out Guid id)
    {
        return Guid.TryParse(input, out id);
    }

    private async Task LoadPageDataAsync(string correlationId, CancellationToken cancellationToken)
    {
        await LoadRulesAsync(correlationId, cancellationToken);
        await LoadSubscriptionsAsync(correlationId, cancellationToken);
    }

    private async Task LoadRulesAsync(string correlationId, CancellationToken cancellationToken)
    {
        Rules = await _alertsApiClient.ListRulesByUserAsync(DemoUserId, correlationId, cancellationToken);

        if (Rules.Count == 0)
        {
            SelectedRuleIdInput = string.Empty;
            return;
        }

        if (!TryReadGuid(SelectedRuleIdInput, out var selectedRuleId)
            || !Rules.Any(rule => rule.RuleId == selectedRuleId))
        {
            SelectedRuleIdInput = Rules.First().RuleId.ToString();
        }

        if (TryReadGuid(EditingRuleIdInput, out var editingRuleId)
            && !Rules.Any(rule => rule.RuleId == editingRuleId))
        {
            EditingRuleIdInput = string.Empty;
        }
    }

    private async Task LoadSubscriptionsAsync(string correlationId, CancellationToken cancellationToken)
    {
        Subscriptions = await _alertsApiClient.ListSubscriptionsByUserAsync(DemoUserId, correlationId, cancellationToken);
        GroupedSubscriptions = BuildGroupedSubscriptions(Subscriptions);

        if (Subscriptions.Count == 0)
        {
            EditingSubscriptionIdInput = string.Empty;
            return;
        }

        if (TryReadGuid(EditingSubscriptionIdInput, out var editingSubscriptionId)
            && !Subscriptions.Any(subscription => subscription.SubscriptionId == editingSubscriptionId))
        {
            EditingSubscriptionIdInput = string.Empty;
        }
    }

    private void ApplyRuleToForm(AlertRuleDto rule)
    {
        RuleNameInput = rule.Name;
        RuleIsEnabled = rule.IsEnabled;
        MinimumSeverityInput = rule.MinimumSeverity;
        RegionsCsv = string.Join(", ", rule.Regions);
        KeywordsCsv = string.Join(", ", rule.Keywords);
        SelectedCategoryNames = rule.Categories.Select(category => category.ToString()).ToList();
    }

    private void ApplySubscriptionToForm(ChannelSubscriptionDto subscription)
    {
        SelectedRuleIdInput = subscription.RuleId.ToString();
        ChannelTypeInput = subscription.ChannelType;
        DestinationInput = subscription.Destination;
        SubscriptionEnabled = subscription.IsEnabled;
        PriorityInput = subscription.Priority;
        MaxRetryAttemptsInput = subscription.MaxRetryAttempts;
    }

    private void ResetRuleForm()
    {
        EditingRuleIdInput = string.Empty;
        RuleNameInput = string.Empty;
        RuleIsEnabled = true;
        MinimumSeverityInput = null;
        SelectedCategoryNames = [];
        RegionsCsv = string.Empty;
        KeywordsCsv = string.Empty;
    }

    private void ResetSubscriptionForm()
    {
        EditingSubscriptionIdInput = string.Empty;
        ChannelTypeInput = NotificationChannelType.Email;
        DestinationInput = string.Empty;
        SubscriptionEnabled = true;
        PriorityInput = 0;
        MaxRetryAttemptsInput = 3;
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

    private static bool TryValidateDestinationForChannel(
        NotificationChannelType channelType,
        string destination,
        out string? errorMessage)
    {
        var candidate = destination.Trim();

        if (channelType == NotificationChannelType.Email)
        {
            var isValidEmail = candidate.Contains('@', StringComparison.Ordinal)
                && !candidate.StartsWith('@')
                && !candidate.EndsWith('@');

            if (!isValidEmail)
            {
                errorMessage = "Email destination must be a valid email-like address.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        if (channelType == NotificationChannelType.Slack)
        {
            if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri))
            {
                errorMessage = "Slack destination must be an absolute URI.";
                return false;
            }

            if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = "Slack destination must use HTTPS.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        errorMessage = "Unsupported channel type.";
        return false;
    }

    private static IReadOnlyCollection<SubscriptionChannelGroup> BuildGroupedSubscriptions(
        IReadOnlyCollection<ChannelSubscriptionDto> subscriptions)
    {
        return subscriptions
            .OrderBy(subscription => subscription.ChannelType)
            .ThenBy(subscription => subscription.Priority)
            .ThenBy(subscription => subscription.SubscriptionId)
            .GroupBy(subscription => subscription.ChannelType)
            .Select(group => new SubscriptionChannelGroup
            {
                ChannelType = group.Key,
                Items = group.ToArray()
            })
            .ToArray();
    }
}

/// <summary>
/// Represents grouped subscriptions for a specific notification channel.
/// </summary>
public sealed class SubscriptionChannelGroup
{
    /// <summary>
    /// Gets the channel type represented by this group.
    /// </summary>
    public NotificationChannelType ChannelType { get; init; }

    /// <summary>
    /// Gets grouped subscriptions for the channel.
    /// </summary>
    public IReadOnlyCollection<ChannelSubscriptionDto> Items { get; init; } = Array.Empty<ChannelSubscriptionDto>();
}
