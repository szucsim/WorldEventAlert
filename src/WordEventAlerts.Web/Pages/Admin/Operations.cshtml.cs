using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WordEventAlerts.Web.Services;

namespace WordEventAlerts.Web.Pages.Admin;

/// <summary>
/// Provides admin-facing operational workflow for delivery attempt inspection and replay.
/// </summary>
public sealed class OperationsModel : PageModel
{
    private readonly IAlertsApiClient _alertsApiClient;
    private readonly ILogger<OperationsModel> _logger;

    public OperationsModel(IAlertsApiClient alertsApiClient, ILogger<OperationsModel> logger)
    {
        _alertsApiClient = alertsApiClient;
        _logger = logger;
    }

    /// <summary>
    /// Gets dead-letter attempts loaded from the API.
    /// </summary>
    public IReadOnlyCollection<DeliveryAttemptDto> DeadLetters { get; private set; } = Array.Empty<DeliveryAttemptDto>();

    /// <summary>
    /// Gets attempts loaded by correlation filter.
    /// </summary>
    public IReadOnlyCollection<DeliveryAttemptDto> AttemptsByCorrelation { get; private set; } = Array.Empty<DeliveryAttemptDto>();

    /// <summary>
    /// Gets the attempt returned by ID lookup.
    /// </summary>
    public DeliveryAttemptDto? AttemptById { get; private set; }

    /// <summary>
    /// Gets replay response details.
    /// </summary>
    public ReplayDeliveryAttemptResponseDto? ReplayResult { get; private set; }

    /// <summary>
    /// Gets a user-facing status message.
    /// </summary>
    public string? StatusMessage { get; private set; }

    /// <summary>
    /// Gets a user-facing error message.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    [BindProperty]
    public string CorrelationFilterInput { get; set; } = string.Empty;

    [BindProperty]
    public string AttemptIdInput { get; set; } = string.Empty;

    [BindProperty]
    public string ReplayAttemptIdInput { get; set; } = string.Empty;

    /// <summary>
    /// Handles the first page render.
    /// </summary>
    public void OnGet()
    {
    }

    /// <summary>
    /// Loads dead-letter attempts.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The current page result.</returns>
    public async Task<IActionResult> OnPostLoadDeadLettersAsync(CancellationToken cancellationToken)
    {
        var correlationId = CreateCorrelationId();

        try
        {
            _logger.LogInformation("AdminDeadLettersLoadRequested CorrelationId={CorrelationId}", correlationId);
            DeadLetters = await _alertsApiClient.ListDeadLettersAsync(correlationId, cancellationToken);
            StatusMessage = $"Loaded {DeadLetters.Count} dead-letter attempt(s).";
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "AdminDeadLettersLoadFailed CorrelationId={CorrelationId}", correlationId);
            ErrorMessage = exception.Message;
        }

        return Page();
    }

    /// <summary>
    /// Searches attempts by correlation identifier.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The current page result.</returns>
    public async Task<IActionResult> OnPostSearchByCorrelationAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(CorrelationFilterInput))
        {
            ErrorMessage = "Correlation filter cannot be empty.";
            return Page();
        }

        var correlationId = CreateCorrelationId();

        try
        {
            _logger.LogInformation(
                "AdminCorrelationSearchRequested CorrelationId={CorrelationId} Filter={Filter}",
                correlationId,
                CorrelationFilterInput);

            AttemptsByCorrelation = await _alertsApiClient.ListAttemptsByCorrelationIdAsync(
                CorrelationFilterInput.Trim(),
                correlationId,
                cancellationToken);

            StatusMessage = $"Loaded {AttemptsByCorrelation.Count} attempt(s) for correlation {CorrelationFilterInput}.";
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "AdminCorrelationSearchFailed CorrelationId={CorrelationId}", correlationId);
            ErrorMessage = exception.Message;
        }

        return Page();
    }

    /// <summary>
    /// Retrieves a specific attempt by identifier.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The current page result.</returns>
    public async Task<IActionResult> OnPostGetAttemptByIdAsync(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(AttemptIdInput, out var attemptId))
        {
            ErrorMessage = "Attempt ID must be a valid GUID.";
            return Page();
        }

        var correlationId = CreateCorrelationId();

        try
        {
            _logger.LogInformation(
                "AdminAttemptLookupRequested CorrelationId={CorrelationId} AttemptId={AttemptId}",
                correlationId,
                attemptId);

            AttemptById = await _alertsApiClient.GetAttemptByIdAsync(attemptId, correlationId, cancellationToken);
            StatusMessage = AttemptById is null
                ? $"Attempt {attemptId} was not found."
                : $"Attempt {attemptId} loaded.";
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "AdminAttemptLookupFailed CorrelationId={CorrelationId}", correlationId);
            ErrorMessage = exception.Message;
        }

        return Page();
    }

    /// <summary>
    /// Replays a delivery attempt by identifier.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The current page result.</returns>
    public async Task<IActionResult> OnPostReplayAttemptAsync(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(ReplayAttemptIdInput, out var attemptId))
        {
            ErrorMessage = "Replay Attempt ID must be a valid GUID.";
            return Page();
        }

        var correlationId = CreateCorrelationId();

        try
        {
            _logger.LogInformation(
                "AdminReplayRequested CorrelationId={CorrelationId} AttemptId={AttemptId}",
                correlationId,
                attemptId);

            ReplayResult = await _alertsApiClient.ReplayAttemptAsync(attemptId, correlationId, cancellationToken);
            StatusMessage = ReplayResult is null
                ? $"Replay source attempt {attemptId} was not found."
                : $"Replay created with attempt {ReplayResult.ReplayAttemptId}.";
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "AdminReplayFailed CorrelationId={CorrelationId}", correlationId);
            ErrorMessage = exception.Message;
        }

        return Page();
    }

    private static string CreateCorrelationId()
    {
        return Guid.NewGuid().ToString("N");
    }
}
