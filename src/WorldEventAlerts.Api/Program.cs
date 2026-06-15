using Scalar.AspNetCore;
using WorldEventAlerts.Api.Endpoints;
using WorldEventAlerts.Core.Abstractions.Matching;
using WorldEventAlerts.Core.Abstractions.Notifications;
using WorldEventAlerts.Core.Services;
using WorldEventAlerts.Infrastructure.InMemory.DependencyInjection;
using WorldEventAlerts.Infrastructure.Notifications.Email;
using WorldEventAlerts.Infrastructure.Notifications.Slack;
using WorldEventAlerts.Infrastructure.Observability.DependencyInjection;
using WorldEventAlerts.Infrastructure.Observability.Logging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Test locally: http://localhost:5239
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddObservabilityFoundation();
builder.Services.AddInMemoryRepositories();
builder.Services.AddSingleton<INotificationChannel, EmailNotificationChannel>();
builder.Services.AddSingleton<INotificationChannel, SlackNotificationChannel>();
builder.Services.AddSingleton<IChannelRegistry, NotificationChannelRegistry>();
builder.Services.AddScoped<IAlertMatchingEngine, AlertMatchingEngine>();
builder.Services.AddScoped<INotificationDispatcher, NotificationDispatcher>();

var app = builder.Build();
app.UseObservabilityFoundation();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/api/v1/health", (HttpContext httpContext, ILoggerFactory loggerFactory) =>
{
    var correlationId = httpContext.GetCorrelationId();
    loggerFactory
        .CreateLogger("HealthEndpoint")
        .LogInformation("HealthCheckRequested CorrelationId={CorrelationId}", correlationId);

    return Results.Ok(new
    {
        status = "ok",
        service = "WorldEventAlerts.Api",
        correlationId,
        utc = DateTimeOffset.UtcNow
    });
})
    .WithName("GetHealth")
    .WithSummary("Gets API health status.")
    .WithDescription("Returns a lightweight health payload for local validation and CI smoke checks.");

app.MapEventIngestionEndpoints();
app.MapAlertRuleEndpoints();
app.MapAdminEndpoints();

app.Run();

public partial class Program;

