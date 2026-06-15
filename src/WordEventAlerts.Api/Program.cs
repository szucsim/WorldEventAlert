using Scalar.AspNetCore;
using WordEventAlerts.Core.Abstractions.Notifications;
using WordEventAlerts.Core.Services;
using WordEventAlerts.Infrastructure.InMemory.DependencyInjection;
using WordEventAlerts.Infrastructure.Notifications.Email;
using WordEventAlerts.Infrastructure.Notifications.Slack;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Test locally: http://localhost:5239
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddInMemoryRepositories();
builder.Services.AddSingleton<INotificationChannel, EmailNotificationChannel>();
builder.Services.AddSingleton<INotificationChannel, SlackNotificationChannel>();
builder.Services.AddSingleton<IChannelRegistry, NotificationChannelRegistry>();
builder.Services.AddScoped<INotificationDispatcher, NotificationDispatcher>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/api/health", () =>
    Results.Ok(new
    {
        status = "ok",
        service = "WordEventAlerts.Api",
        utc = DateTimeOffset.UtcNow
    }))
    .WithName("GetHealth")
    .WithSummary("Gets API health status.")
    .WithDescription("Returns a lightweight health payload for local validation and CI smoke checks.");

app.Run();
