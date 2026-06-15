using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace WorldEventAlerts.Web.Services;

/// <summary>
/// Default implementation of API operations used by Razor Pages workflows.
/// </summary>
public sealed class AlertsApiClient : IAlertsApiClient
{
    private const string CorrelationHeaderName = "X-Correlation-Id";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<AlertsApiClient> _logger;

    /// <summary>
    /// Creates a new typed API client instance.
    /// </summary>
    /// <param name="httpClient">The configured HTTP client.</param>
    /// <param name="logger">The logger for API call telemetry.</param>
    public AlertsApiClient(HttpClient httpClient, ILogger<AlertsApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        if (_httpClient.BaseAddress is null)
        {
            throw new InvalidOperationException("AlertsApiClient requires HttpClient.BaseAddress to be configured.");
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<AlertRuleDto>> ListRulesByUserAsync(
        Guid userId,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/alert-rules?userId={userId}");
        AddCorrelationHeader(request, correlationId);
        var result = await SendAsync<List<AlertRuleDto>>(request, correlationId, cancellationToken);
        return result is null ? Array.Empty<AlertRuleDto>() : result;
    }

    /// <inheritdoc />
    public Task<AlertRuleDto?> UpsertRuleAsync(
        Guid ruleId,
        UpsertAlertRuleRequestDto request,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var message = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/alert-rules/{ruleId}")
        {
            Content = JsonContent.Create(request)
        };

        AddCorrelationHeader(message, correlationId);
        return SendAsync<AlertRuleDto>(message, correlationId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteRuleAsync(
        Guid ruleId,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/alert-rules/{ruleId}");
        AddCorrelationHeader(request, correlationId);
        var response = await SendAsync<HttpResponseMessage>(request, correlationId, cancellationToken, rawResponse: true, allowNotFound: true);
        return response?.StatusCode == HttpStatusCode.NoContent;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<ChannelSubscriptionDto>> ListSubscriptionsByRuleAsync(
        Guid ruleId,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/alert-rules/{ruleId}/subscriptions");
        AddCorrelationHeader(request, correlationId);
        var result = await SendAsync<List<ChannelSubscriptionDto>>(request, correlationId, cancellationToken);
        return result is null ? Array.Empty<ChannelSubscriptionDto>() : result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<ChannelSubscriptionDto>> ListSubscriptionsByUserAsync(
        Guid userId,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/alert-rules/subscriptions?userId={userId}");
        AddCorrelationHeader(request, correlationId);
        var result = await SendAsync<List<ChannelSubscriptionDto>>(request, correlationId, cancellationToken);
        return result is null ? Array.Empty<ChannelSubscriptionDto>() : result;
    }

    /// <inheritdoc />
    public Task<ChannelSubscriptionDto?> UpsertSubscriptionAsync(
        Guid ruleId,
        Guid subscriptionId,
        UpsertChannelSubscriptionRequestDto request,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var message = new HttpRequestMessage(
            HttpMethod.Put,
            $"/api/v1/alert-rules/{ruleId}/subscriptions/{subscriptionId}")
        {
            Content = JsonContent.Create(request)
        };

        AddCorrelationHeader(message, correlationId);
        return SendAsync<ChannelSubscriptionDto>(message, correlationId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteSubscriptionAsync(
        Guid ruleId,
        Guid subscriptionId,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/alert-rules/{ruleId}/subscriptions/{subscriptionId}");
        AddCorrelationHeader(request, correlationId);
        var response = await SendAsync<HttpResponseMessage>(request, correlationId, cancellationToken, rawResponse: true, allowNotFound: true);
        return response?.StatusCode == HttpStatusCode.NoContent;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<DeliveryAttemptDto>> ListDeadLettersAsync(
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/delivery-attempts/dead-letters");
        AddCorrelationHeader(request, correlationId);
        var result = await SendAsync<List<DeliveryAttemptDto>>(request, correlationId, cancellationToken);
        return result is null ? Array.Empty<DeliveryAttemptDto>() : result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<DeliveryAttemptDto>> ListAttemptsByCorrelationIdAsync(
        string filterCorrelationId,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var encodedCorrelationId = Uri.EscapeDataString(filterCorrelationId);
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/admin/delivery-attempts/correlation/{encodedCorrelationId}");

        AddCorrelationHeader(request, correlationId);
        var result = await SendAsync<List<DeliveryAttemptDto>>(request, correlationId, cancellationToken);
        return result is null ? Array.Empty<DeliveryAttemptDto>() : result;
    }

    /// <inheritdoc />
    public Task<DeliveryAttemptDto?> GetAttemptByIdAsync(
        Guid deliveryAttemptId,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/admin/delivery-attempts/{deliveryAttemptId}");
        AddCorrelationHeader(request, correlationId);
        return SendAsync<DeliveryAttemptDto>(request, correlationId, cancellationToken, allowNotFound: true);
    }

    /// <inheritdoc />
    public Task<ReplayDeliveryAttemptResponseDto?> ReplayAttemptAsync(
        Guid deliveryAttemptId,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/delivery-attempts/{deliveryAttemptId}/replay");
        AddCorrelationHeader(request, correlationId);
        return SendAsync<ReplayDeliveryAttemptResponseDto>(request, correlationId, cancellationToken, allowNotFound: true);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<AlertEventDto>> ListAlertsAsync(
        string correlationId,
        int skip = 0,
        int take = 200,
        CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/admin/alerts?skip={skip}&take={take}");
        AddCorrelationHeader(request, correlationId);
        var result = await SendAsync<List<AlertEventDto>>(request, correlationId, cancellationToken);
        return result is null ? Array.Empty<AlertEventDto>() : result;
    }

    /// <inheritdoc />
    public Task<IngestWorldEventResponseDto?> IngestEventAsync(
        IngestWorldEventRequestDto request,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, "/api/v1/events")
        {
            Content = JsonContent.Create(request)
        };

        AddCorrelationHeader(message, correlationId);
        return SendAsync<IngestWorldEventResponseDto>(message, correlationId, cancellationToken);
    }

    private static void AddCorrelationHeader(HttpRequestMessage request, string correlationId)
    {
        request.Headers.Remove(CorrelationHeaderName);
        request.Headers.Add(CorrelationHeaderName, correlationId);
    }

    private async Task<TResponse?> SendAsync<TResponse>(
        HttpRequestMessage request,
        string correlationId,
        CancellationToken cancellationToken,
        bool allowNotFound = false,
        bool rawResponse = false)
    {
        var requestPath = GetRequestPath(request.RequestUri);

        _logger.LogInformation(
            "WebApiRequestStarted CorrelationId={CorrelationId} Method={Method} Path={Path}",
            correlationId,
            request.Method,
            requestPath);

        HttpResponseMessage response;

        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            throw new InvalidOperationException(
                $"Unable to reach Alerts API at '{_httpClient.BaseAddress}'. Ensure the API is running and AlertsApi:BaseUrl is correct.",
                exception);
        }

        using (response)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogInformation(
                "WebApiRequestCompleted CorrelationId={CorrelationId} Method={Method} Path={Path} StatusCode={StatusCode}",
                correlationId,
                request.Method,
                requestPath,
                (int)response.StatusCode);

            if (allowNotFound && response.StatusCode == HttpStatusCode.NotFound)
            {
                return default;
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"API call failed for {request.Method} {requestPath}: {(int)response.StatusCode} {responseBody}");
            }

            if (rawResponse)
            {
                return (TResponse)(object)new HttpResponseMessage(response.StatusCode);
            }

            if (string.IsNullOrWhiteSpace(responseBody))
            {
                return default;
            }

            return JsonSerializer.Deserialize<TResponse>(responseBody, SerializerOptions);
        }
    }

    private static string GetRequestPath(Uri? uri)
    {
        if (uri is null)
        {
            return "<null>";
        }

        return uri.IsAbsoluteUri
            ? uri.PathAndQuery
            : uri.OriginalString;
    }
}

