using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Test locally: http://localhost:5239
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

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
