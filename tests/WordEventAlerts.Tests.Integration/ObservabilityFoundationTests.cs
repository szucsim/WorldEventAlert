using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using WordEventAlerts.Infrastructure.Observability.Logging;

namespace WordEventAlerts.Tests.Integration;

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
}
