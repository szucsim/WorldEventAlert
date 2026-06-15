using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using WordEventAlerts.Api.Contracts.Alerts;
using WordEventAlerts.Api.Contracts.Events;
using WordEventAlerts.Core.Domain;

namespace WordEventAlerts.Tests.Integration;

public sealed class AlertApiFlowTests
{
    [Fact]
    public async Task Ingestion_ShouldMatchRule_AndDispatchNotification()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var userId = Guid.NewGuid();
        var ruleId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();

        var upsertRuleResponse = await client.PutAsJsonAsync(
            $"/api/v1/alert-rules/{ruleId}",
            new UpsertAlertRuleRequest
            {
                UserId = userId,
                IsEnabled = true,
                Categories = [WorldEventCategory.BreakingNews],
                MinimumSeverity = 70,
                Regions = ["US"],
                Keywords = ["storm"]
            });

        Assert.Equal(HttpStatusCode.OK, upsertRuleResponse.StatusCode);

        var upsertSubscriptionResponse = await client.PutAsJsonAsync(
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

        var ingestResponse = await client.PostAsJsonAsync(
            "/api/v1/events",
            new IngestWorldEventRequest
            {
                SourceEventId = Guid.NewGuid().ToString("N"),
                SourceSystem = "integration-test-source",
                Category = WorldEventCategory.BreakingNews,
                SeverityScore = 95,
                Headline = "Major storm warning issued",
                Summary = "Storm risk increased in the region.",
                Regions = ["US"],
                Keywords = ["storm"]
            });

        Assert.Equal(HttpStatusCode.Accepted, ingestResponse.StatusCode);

        var payload = await ingestResponse.Content.ReadFromJsonAsync<IngestWorldEventResponse>();
        Assert.NotNull(payload);
        Assert.Equal(1, payload.MatchedRules);
        Assert.Equal(1, payload.DispatchedNotifications);
        Assert.Equal(0, payload.FailedNotifications);

        var getEventResponse = await client.GetAsync($"/api/v1/events/{payload.EventId}");
        Assert.Equal(HttpStatusCode.OK, getEventResponse.StatusCode);
    }

    [Fact]
    public async Task AlertRules_ShouldListByUser()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var userId = Guid.NewGuid();
        var ruleId = Guid.NewGuid();

        var upsertRuleResponse = await client.PutAsJsonAsync(
            $"/api/v1/alert-rules/{ruleId}",
            new UpsertAlertRuleRequest
            {
                UserId = userId,
                IsEnabled = true,
                Categories = [WorldEventCategory.MarketMovement],
                MinimumSeverity = 60,
                Regions = ["US"],
                Keywords = ["inflation"]
            });

        Assert.Equal(HttpStatusCode.OK, upsertRuleResponse.StatusCode);

        var listResponse = await client.GetAsync($"/api/v1/alert-rules?userId={userId}");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var json = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Array, json.RootElement.ValueKind);
        Assert.True(json.RootElement.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task Ingestion_ShouldReturnNoMatches_WhenNoRuleMatches()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var ingestResponse = await client.PostAsJsonAsync(
            "/api/v1/events",
            new IngestWorldEventRequest
            {
                SourceEventId = Guid.NewGuid().ToString("N"),
                SourceSystem = "integration-test-source",
                Category = WorldEventCategory.Other,
                SeverityScore = 10,
                Headline = "Informational update",
                Summary = "No action required.",
                Regions = ["AQ"],
                Keywords = ["informational"]
            });

        Assert.Equal(HttpStatusCode.Accepted, ingestResponse.StatusCode);

        var payload = await ingestResponse.Content.ReadFromJsonAsync<IngestWorldEventResponse>();
        Assert.NotNull(payload);
        Assert.Equal(0, payload.MatchedRules);
        Assert.Equal(0, payload.DispatchedNotifications);
        Assert.Equal(0, payload.FailedNotifications);
    }
}
