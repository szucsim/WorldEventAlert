namespace WordEventAlerts.Web.Configuration;

/// <summary>
/// Configures connectivity to the alerts API from the Razor UI.
/// </summary>
public sealed class AlertsApiOptions
{
    /// <summary>
    /// Configuration section name for alerts API settings.
    /// </summary>
    public const string SectionName = "AlertsApi";

    /// <summary>
    /// Gets or sets the base URL for the alerts API.
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:5239";
}
