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
                Name = "Breaking News Rule",
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
                Name = "Market Movement Rule",
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

    [Fact]
    public async Task DeleteRule_ShouldRemoveRule_AndCascadeSubscriptions()
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
                Name = "Delete Rule Flow",
                IsEnabled = true,
                Categories = [WorldEventCategory.NaturalDisaster],
                MinimumSeverity = 50,
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

        var deleteRuleResponse = await client.DeleteAsync($"/api/v1/alert-rules/{ruleId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteRuleResponse.StatusCode);

        var getRuleResponse = await client.GetAsync($"/api/v1/alert-rules/{ruleId}");
        Assert.Equal(HttpStatusCode.NotFound, getRuleResponse.StatusCode);

        var listSubscriptionsResponse = await client.GetAsync($"/api/v1/alert-rules/{ruleId}/subscriptions");
        Assert.Equal(HttpStatusCode.OK, listSubscriptionsResponse.StatusCode);

        var subscriptionsJson = JsonDocument.Parse(await listSubscriptionsResponse.Content.ReadAsStringAsync());
        Assert.Equal(0, subscriptionsJson.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task DeleteSubscription_ShouldRemoveOnlyRequestedSubscription()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var userId = Guid.NewGuid();
        var ruleId = Guid.NewGuid();
        var firstSubscriptionId = Guid.NewGuid();
        var secondSubscriptionId = Guid.NewGuid();

        var upsertRuleResponse = await client.PutAsJsonAsync(
            $"/api/v1/alert-rules/{ruleId}",
            new UpsertAlertRuleRequest
            {
                UserId = userId,
                Name = "Delete Subscription Flow",
                IsEnabled = true,
                Categories = [WorldEventCategory.BreakingNews],
                MinimumSeverity = 10,
                Regions = ["US"],
                Keywords = ["weather"]
            });

        Assert.Equal(HttpStatusCode.OK, upsertRuleResponse.StatusCode);

        var firstSubscriptionResponse = await client.PutAsJsonAsync(
            $"/api/v1/alert-rules/{ruleId}/subscriptions/{firstSubscriptionId}",
            new UpsertChannelSubscriptionRequest
            {
                UserId = userId,
                ChannelType = NotificationChannelType.Email,
                Destination = "alerts@example.com",
                IsEnabled = true,
                Priority = 0,
                MaxRetryAttempts = 3
            });

        Assert.Equal(HttpStatusCode.OK, firstSubscriptionResponse.StatusCode);

        var secondSubscriptionResponse = await client.PutAsJsonAsync(
            $"/api/v1/alert-rules/{ruleId}/subscriptions/{secondSubscriptionId}",
            new UpsertChannelSubscriptionRequest
            {
                UserId = userId,
                ChannelType = NotificationChannelType.Slack,
                Destination = "https://hooks.slack.local/services/alpha",
                IsEnabled = true,
                Priority = 1,
                MaxRetryAttempts = 3
            });

        Assert.Equal(HttpStatusCode.OK, secondSubscriptionResponse.StatusCode);

        var deleteSubscriptionResponse = await client.DeleteAsync($"/api/v1/alert-rules/{ruleId}/subscriptions/{firstSubscriptionId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteSubscriptionResponse.StatusCode);

        var listSubscriptionsResponse = await client.GetAsync($"/api/v1/alert-rules/{ruleId}/subscriptions");
        Assert.Equal(HttpStatusCode.OK, listSubscriptionsResponse.StatusCode);

        var subscriptionsJson = JsonDocument.Parse(await listSubscriptionsResponse.Content.ReadAsStringAsync());
        Assert.Equal(1, subscriptionsJson.RootElement.GetArrayLength());
        Assert.Equal(secondSubscriptionId, subscriptionsJson.RootElement[0].GetProperty("subscriptionId").GetGuid());

        var deleteMissingResponse = await client.DeleteAsync($"/api/v1/alert-rules/{ruleId}/subscriptions/{firstSubscriptionId}");
        Assert.Equal(HttpStatusCode.NotFound, deleteMissingResponse.StatusCode);
    }

    [Fact]
    public async Task ListSubscriptionsByUser_ShouldReturnSubscriptionsAcrossRules()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var userId = Guid.NewGuid();
        var firstRuleId = Guid.NewGuid();
        var secondRuleId = Guid.NewGuid();
        var firstSubscriptionId = Guid.NewGuid();
        var secondSubscriptionId = Guid.NewGuid();

        await client.PutAsJsonAsync(
            $"/api/v1/alert-rules/{firstRuleId}",
            new UpsertAlertRuleRequest
            {
                UserId = userId,
                Name = "User-wide listing rule 1",
                IsEnabled = true,
                Categories = [WorldEventCategory.BreakingNews],
                MinimumSeverity = 10,
                Regions = ["US"],
                Keywords = ["market"]
            });

        await client.PutAsJsonAsync(
            $"/api/v1/alert-rules/{secondRuleId}",
            new UpsertAlertRuleRequest
            {
                UserId = userId,
                Name = "User-wide listing rule 2",
                IsEnabled = true,
                Categories = [WorldEventCategory.MarketMovement],
                MinimumSeverity = 20,
                Regions = ["EU"],
                Keywords = ["rates"]
            });

        await client.PutAsJsonAsync(
            $"/api/v1/alert-rules/{firstRuleId}/subscriptions/{firstSubscriptionId}",
            new UpsertChannelSubscriptionRequest
            {
                UserId = userId,
                ChannelType = NotificationChannelType.Email,
                Destination = "alerts@example.com",
                IsEnabled = true,
                Priority = 0,
                MaxRetryAttempts = 3
            });

        await client.PutAsJsonAsync(
            $"/api/v1/alert-rules/{secondRuleId}/subscriptions/{secondSubscriptionId}",
            new UpsertChannelSubscriptionRequest
            {
                UserId = userId,
                ChannelType = NotificationChannelType.Slack,
                Destination = "https://hooks.slack.local/services/beta",
                IsEnabled = true,
                Priority = 1,
                MaxRetryAttempts = 3
            });

        var listResponse = await client.GetAsync($"/api/v1/alert-rules/subscriptions?userId={userId}");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var listJson = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
        Assert.Equal(2, listJson.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task Ingestion_ShouldCountFailedNotifications_ForSimulatedChannelFailure()
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
                Name = "Simulated Failure Counter Rule",
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
                Destination = "simulate-transient@example.com",
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
        Assert.Equal(0, payload.DispatchedNotifications);
        Assert.Equal(1, payload.FailedNotifications);
    }
}
