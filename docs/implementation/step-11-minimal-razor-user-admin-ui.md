# Step 11 - Minimal Razor User and Admin UI

## Commit

- Hash: `<pending>`
- Message: `<pending>`

## Objective

- Implement Milestone 6 from Phase 1 plan: minimal Razor UI for user and admin workflows.
- Keep thin delivery-layer behavior by calling existing `/api/v1/...` endpoints.
- Maintain observability through correlation-aware API requests and structured logs in the Web layer.

## Delivered

- Added typed API client for Web -> API interactions with correlation header propagation.
- Added User workflow page for:
  - upsert alert rules
  - list rules by user
  - upsert subscriptions
  - list subscriptions by rule
- Added Admin workflow page for:
  - list dead-letter attempts
  - list attempts by correlation ID
  - get attempt by delivery attempt ID
  - replay attempt
- Wired Web app startup for API base URL configuration.
- Updated layout and responsive styling to provide a purpose-built workflow UI.

## Function Documentation Added

- XML documentation added for new public Web-layer contracts and page models:
  - `src/WorldEventAlerts.Web/Configuration/AlertsApiOptions.cs`
  - `src/WorldEventAlerts.Web/Services/IAlertsApiClient.cs`
  - `src/WorldEventAlerts.Web/Services/AlertsApiClient.cs`
  - `src/WorldEventAlerts.Web/Pages/User/Rules.cshtml.cs`
  - `src/WorldEventAlerts.Web/Pages/Admin/Operations.cshtml.cs`

## Logging Added

- Added structured logs in Web typed client:
  - `WebApiRequestStarted`
  - `WebApiRequestCompleted`
- Added workflow operation logs in Razor Page models for user and admin actions.

## Validation

- Build: `dotnet build WorldEventAlerts.slnx /p:UseAppHost=false` succeeded.
- Unit tests: `dotnet test tests/WorldEventAlerts.Tests.Unit/WorldEventAlerts.Tests.Unit.csproj /p:UseAppHost=false` succeeded (5/5).
- Integration tests: `dotnet test tests/WorldEventAlerts.Tests.Integration/WorldEventAlerts.Tests.Integration.csproj /p:UseAppHost=false` succeeded (16/16).

## Notes

- Web API base URL is configurable via `AlertsApi:BaseUrl` in Web appsettings.
- The UI intentionally remains thin; business logic and state orchestration stay in API/Core layers.

