using WordEventAlerts.Core.Domain;

namespace WordEventAlerts.Api.Contracts.Alerts;

/// <summary>
/// Request payload for inserting or updating an alert rule.
/// </summary>
public sealed class UpsertAlertRuleRequest
{
    public required Guid UserId { get; init; }

    public required string Name { get; init; }

    public bool IsEnabled { get; init; } = true;

    public IReadOnlyCollection<WorldEventCategory> Categories { get; init; } = [];

    public int? MinimumSeverity { get; init; }

    public IReadOnlyCollection<string> Regions { get; init; } = [];

    public IReadOnlyCollection<string> Keywords { get; init; } = [];
}
