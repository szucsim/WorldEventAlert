using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using WordEventAlerts.Core.Abstractions.Repositories;
using WordEventAlerts.Core.Domain;

namespace WordEventAlerts.Tests.Integration;

public sealed class AdminApiTests
{
    [Fact]
    public async Task AdminDeadLettersEndpoint_ShouldReturnOnlyDeadLetterAttempts()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        await SeedAttemptAsync(factory, CreateAttempt(DeliveryOutcome.DeadLettered, "corr-admin-deadletter", 1));
        await SeedAttemptAsync(factory, CreateAttempt(DeliveryOutcome.Succeeded, "corr-admin-deadletter", 2));

        var response = await client.GetAsync("/api/v1/admin/delivery-attempts/dead-letters");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Array, json.RootElement.ValueKind);
        Assert.All(json.RootElement.EnumerateArray(), item =>
        {
            Assert.Equal((int)DeliveryOutcome.DeadLettered, item.GetProperty("outcome").GetInt32());
        });
    }

    [Fact]
    public async Task AdminCorrelationEndpoint_ShouldFilterAttemptsByCorrelationId()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        const string correlationId = "corr-admin-filter";

        await SeedAttemptAsync(factory, CreateAttempt(DeliveryOutcome.FailedTransient, correlationId, 1));
        await SeedAttemptAsync(factory, CreateAttempt(DeliveryOutcome.Succeeded, correlationId, 2));
        await SeedAttemptAsync(factory, CreateAttempt(DeliveryOutcome.Succeeded, "corr-other", 1));

        var response = await client.GetAsync($"/api/v1/admin/delivery-attempts/correlation/{correlationId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Array, json.RootElement.ValueKind);
        Assert.Equal(2, json.RootElement.GetArrayLength());
        Assert.All(json.RootElement.EnumerateArray(), item =>
        {
            Assert.Equal(correlationId, item.GetProperty("correlationId").GetString());
        });
    }

    [Fact]
    public async Task AdminReplayEndpoint_ShouldCreateReplayAttempt_AndExposeItById()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var original = CreateAttempt(DeliveryOutcome.DeadLettered, "corr-admin-replay", 1);
        await SeedAttemptAsync(factory, original);

        var replayResponse = await client.PostAsync($"/api/v1/admin/delivery-attempts/{original.DeliveryAttemptId}/replay", content: null);
        Assert.Equal(HttpStatusCode.OK, replayResponse.StatusCode);

        var replayJson = JsonDocument.Parse(await replayResponse.Content.ReadAsStringAsync());
        var replayAttemptIdText = replayJson.RootElement.GetProperty("replayAttemptId").GetString();

        Assert.True(Guid.TryParse(replayAttemptIdText, out var replayAttemptId));
        Assert.NotEqual(original.DeliveryAttemptId, replayAttemptId);

        var getByIdResponse = await client.GetAsync($"/api/v1/admin/delivery-attempts/{replayAttemptId}");
        Assert.Equal(HttpStatusCode.OK, getByIdResponse.StatusCode);

        var attemptJson = JsonDocument.Parse(await getByIdResponse.Content.ReadAsStringAsync());
        Assert.Equal(replayAttemptId, attemptJson.RootElement.GetProperty("deliveryAttemptId").GetGuid());
    }

    private static async Task SeedAttemptAsync(WebApplicationFactory<Program> factory, DeliveryAttempt attempt)
    {
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDeliveryAttemptRepository>();
        await repository.SaveAsync(attempt);
    }

    private static DeliveryAttempt CreateAttempt(DeliveryOutcome outcome, string correlationId, int attemptNumber)
    {
        return new DeliveryAttempt(
            deliveryAttemptId: Guid.NewGuid(),
            eventId: Guid.NewGuid(),
            ruleId: Guid.NewGuid(),
            userId: Guid.NewGuid(),
            channelType: NotificationChannelType.Email,
            destination: "alerts@example.com",
            attemptNumber: attemptNumber,
            outcome: outcome,
            attemptedAtUtc: DateTimeOffset.UtcNow.AddSeconds(attemptNumber),
            failureReason: outcome == DeliveryOutcome.Succeeded ? null : "seeded failure",
            correlationId: correlationId);
    }
}
