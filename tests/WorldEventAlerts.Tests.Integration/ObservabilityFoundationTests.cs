using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using WorldEventAlerts.Api.Contracts.Alerts;
using WorldEventAlerts.Api.Contracts.Events;
using WorldEventAlerts.Core.Domain;
using WorldEventAlerts.Infrastructure.Observability.Logging;

namespace WorldEventAlerts.Tests.Integration;

public sealed class ObservabilityFoundationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ObservabilityFoundationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthEndpoint_ShouldGenerateCorrelationId_WhenHeaderMissing()
    {
        var response = await _client.GetAsync("/api/v1/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues(ObservabilityConstants.CorrelationHeaderName, out var headerValues));

        var correlationId = headerValues.Single();
        Assert.False(string.IsNullOrWhiteSpace(correlationId));

        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(correlationId, payload.RootElement.GetProperty("correlationId").GetString());
    }

    [Fact]
    public async Task HealthEndpoint_ShouldPropagateCorrelationId_WhenHeaderProvided()
    {
        const string inboundCorrelationId = "corr-test-123";

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/health");
        request.Headers.Add(ObservabilityConstants.CorrelationHeaderName, inboundCorrelationId);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues(ObservabilityConstants.CorrelationHeaderName, out var headerValues));
        Assert.Equal(inboundCorrelationId, headerValues.Single());

        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(inboundCorrelationId, payload.RootElement.GetProperty("correlationId").GetString());
    }

    [Fact]
    public async Task IngestionFlow_ShouldPropagateProvidedCorrelationId_IntoEventAndDeliveryLookup()
    {
        await ConfigureMatchingRuleAsync();

        var inboundCorrelationId = Guid.NewGuid().ToString("N");

        using var ingestRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/events")
        {
            Content = JsonContent.Create(new IngestWorldEventRequest
            {
                SourceEventId = Guid.NewGuid().ToString("N"),
                SourceSystem = "observability-test-source",
                Category = WorldEventCategory.BreakingNews,
                SeverityScore = 91,
                Headline = "Correlation propagation check",
                Summary = "Validate propagation over end-to-end flow.",
                Regions = ["US"],
                Keywords = ["storm"]
            })
        };

        ingestRequest.Headers.Add(ObservabilityConstants.CorrelationHeaderName, inboundCorrelationId);

        var ingestResponse = await _client.SendAsync(ingestRequest);
        Assert.Equal(HttpStatusCode.Accepted, ingestResponse.StatusCode);
        Assert.True(ingestResponse.Headers.TryGetValues(ObservabilityConstants.CorrelationHeaderName, out var ingestHeaderValues));
        Assert.Equal(inboundCorrelationId, ingestHeaderValues.Single());

        var ingestPayload = await ingestResponse.Content.ReadFromJsonAsync<IngestWorldEventResponse>();
        Assert.NotNull(ingestPayload);
        Assert.Equal(inboundCorrelationId, ingestPayload.CorrelationId);
        Assert.True(ingestPayload.DispatchedNotifications >= 1);

        var readEventResponse = await _client.GetAsync($"/api/v1/events/{ingestPayload.EventId}");
        Assert.Equal(HttpStatusCode.OK, readEventResponse.StatusCode);
        var readEventJson = JsonDocument.Parse(await readEventResponse.Content.ReadAsStringAsync());
        Assert.Equal(inboundCorrelationId, readEventJson.RootElement.GetProperty("correlationId").GetString());

        var attemptsResponse = await _client.GetAsync($"/api/v1/admin/delivery-attempts/correlation/{inboundCorrelationId}");
        Assert.Equal(HttpStatusCode.OK, attemptsResponse.StatusCode);

        var attemptsJson = JsonDocument.Parse(await attemptsResponse.Content.ReadAsStringAsync());
        Assert.True(attemptsJson.RootElement.GetArrayLength() >= 1);
        Assert.All(attemptsJson.RootElement.EnumerateArray(), attempt =>
        {
            Assert.Equal(inboundCorrelationId, attempt.GetProperty("correlationId").GetString());
        });
    }

    [Fact]
    public async Task IngestionFlow_ShouldGenerateCorrelationId_WhenMissing_AndSupportAdminTrace()
    {
        await ConfigureMatchingRuleAsync();

        var ingestResponse = await _client.PostAsJsonAsync(
            "/api/v1/events",
            new IngestWorldEventRequest
            {
                SourceEventId = Guid.NewGuid().ToString("N"),
                SourceSystem = "observability-test-source",
                Category = WorldEventCategory.BreakingNews,
                SeverityScore = 93,
                Headline = "Generated correlation check",
                Summary = "Validate generated correlation traceability.",
                Regions = ["US"],
                Keywords = ["storm"]
            });

        Assert.Equal(HttpStatusCode.Accepted, ingestResponse.StatusCode);
        Assert.True(ingestResponse.Headers.TryGetValues(ObservabilityConstants.CorrelationHeaderName, out var ingestHeaderValues));

        var generatedCorrelationId = ingestHeaderValues.Single();
        Assert.False(string.IsNullOrWhiteSpace(generatedCorrelationId));

        var ingestPayload = await ingestResponse.Content.ReadFromJsonAsync<IngestWorldEventResponse>();
        Assert.NotNull(ingestPayload);
        Assert.Equal(generatedCorrelationId, ingestPayload.CorrelationId);

        var attemptsResponse = await _client.GetAsync($"/api/v1/admin/delivery-attempts/correlation/{generatedCorrelationId}");
        Assert.Equal(HttpStatusCode.OK, attemptsResponse.StatusCode);

        var attemptsJson = JsonDocument.Parse(await attemptsResponse.Content.ReadAsStringAsync());
        Assert.True(attemptsJson.RootElement.GetArrayLength() >= 1);
        Assert.All(attemptsJson.RootElement.EnumerateArray(), attempt =>
        {
            Assert.Equal(generatedCorrelationId, attempt.GetProperty("correlationId").GetString());
        });
    }

    private async Task ConfigureMatchingRuleAsync()
    {
        var userId = Guid.NewGuid();
        var ruleId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();

        var upsertRuleResponse = await _client.PutAsJsonAsync(
            $"/api/v1/alert-rules/{ruleId}",
            new UpsertAlertRuleRequest
            {
                UserId = userId,
                Name = "Observability Matching Rule",
                IsEnabled = true,
                Categories = [WorldEventCategory.BreakingNews],
                MinimumSeverity = 70,
                Regions = ["US"],
                Keywords = ["storm"]
            });

        Assert.Equal(HttpStatusCode.OK, upsertRuleResponse.StatusCode);

        var upsertSubscriptionResponse = await _client.PutAsJsonAsync(
            $"/api/v1/alert-rules/{ruleId}/subscriptions/{subscriptionId}",
            new UpsertChannelSubscriptionRequest
            {
                UserId = userId,
                ChannelType = NotificationChannelType.Email,
                Destination = "alerts@example.com",
                IsEnabled = true,
                Priority = 0,
                MaxRetryAttempts = 3
            });

        Assert.Equal(HttpStatusCode.OK, upsertSubscriptionResponse.StatusCode);
    }
}

