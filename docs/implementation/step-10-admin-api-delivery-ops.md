# Step 10 - Admin API Delivery Operations

## Commit

- Hash: `<pending>`
- Message: `<pending>`

## Objective

- Continue original plan by introducing Admin API operations for delivery observability and replay.
- Keep structured logging, correlation flow, and XML function documentation standards.

## Delivered

- Added admin endpoints under `/api/v1/admin`:
  - list dead-letter delivery attempts
  - list attempts by correlation ID
  - get delivery attempt by ID
  - replay delivery attempt
- Extended delivery attempt repository contract with get-by-id support.
- Implemented get-by-id in in-memory repository.
- Added admin-specific structured log event constants.
- Added integration tests for admin operations.

## Function Documentation Added

- Public API XML documentation added in:
  - `src/WordEventAlerts.Api/Endpoints/AdminEndpoints.cs`
- Existing XML documentation updated to remain accurate in:
  - `src/WordEventAlerts.Core/Abstractions/Repositories/IDeliveryAttemptRepository.cs`

## Logging Added

- New structured log events:
  - `AdminDeadLettersListed`
  - `AdminDeliveriesByCorrelationListed`
  - `AdminDeliveryAttemptRead`
  - `AdminReplayRequested`
  - `AdminReplaySucceeded`
  - `AdminReplayFailed`

## Validation

- Build: `dotnet build WordEventAlerts.slnx /p:UseAppHost=false` succeeded.
- Unit tests: `dotnet test tests/WordEventAlerts.Tests.Unit/WordEventAlerts.Tests.Unit.csproj /p:UseAppHost=false` succeeded (5/5).
- Integration tests: `dotnet test tests/WordEventAlerts.Tests.Integration/WordEventAlerts.Tests.Integration.csproj /p:UseAppHost=false` succeeded (16/16).

## Notes

- Replay currently performs a direct dispatch retry using the original destination/channel.
