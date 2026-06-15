# Step 13 - Manual Testing Hardening and Rule Naming

## Commit

- Hash: `<pending>`
- Message: `<pending>`

## Objective

- Fix manual testing failures in Web UI caused by relative URI logging in the typed API client.
- Simplify user workflow UX by removing manual ID entry and using a fixed demo user.
- Introduce human-readable alert rule names to support rule selection in UI.

## Delivered

- Fixed `AlertsApiClient` logging/error formatting to safely handle relative request URIs.
- Added explicit connection-failure translation in `AlertsApiClient` for unreachable API hosts.
- Added startup guard in `AlertsApiClient` for missing `HttpClient.BaseAddress`.
- Added graceful initial page-load error handling in user rules page model when API is unavailable.
- Aligned Web default/development `AlertsApi:BaseUrl` to API launch profile URL (`http://localhost:5239`).
- Added `Name` to `AlertRule` domain model and validation.
- Added `Name` to alert rule upsert API contract and endpoint response projections.
- Updated Web DTOs to include rule names.
- Simplified User Rules UI flow:
  - fixed demo user ID (no manual user ID input)
  - rule creation auto-generates rule ID
  - subscription creation auto-generates subscription ID
  - subscription uses rule dropdown selection by rule name
  - severity uses selection dropdown
- Updated repository sort order for user-facing rule lists to name-first ordering.
- Updated unit/integration tests for the new required rule name contract.

## Function Documentation Added

- Existing XML docs were kept current in modified public-facing files:
  - `src/WordEventAlerts.Web/Services/AlertsApiClient.cs`
  - `src/WordEventAlerts.Web/Pages/User/Rules.cshtml.cs`
  - `src/WordEventAlerts.Core/Domain/AlertRule.cs`
  - `src/WordEventAlerts.Api/Contracts/Alerts/UpsertAlertRuleRequest.cs`

## Logging Added

- Hardened request path logging in Web typed client:
  - `WebApiRequestStarted`
  - `WebApiRequestCompleted`
- Logging now safely supports relative request URIs while preserving correlation IDs.

## Validation

- Build: `dotnet build WordEventAlerts.slnx /p:UseAppHost=false` succeeded.
- Unit tests: `dotnet test tests/WordEventAlerts.Tests.Unit/WordEventAlerts.Tests.Unit.csproj /p:UseAppHost=false` succeeded (5/5).
- Integration tests: `dotnet test tests/WordEventAlerts.Tests.Integration/WordEventAlerts.Tests.Integration.csproj /p:UseAppHost=false` succeeded (18/18).

## Notes

- The user workflow currently operates against a fixed demo user ID until authentication and real user context are introduced.
