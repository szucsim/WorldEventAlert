# Step 15 - Admin Outcome UX and User Subscription Grouping

## Commit

- Hash: `<pending>`
- Message: `<pending>`

## Objective

- Fix mock ingestion counters so matched events are not incorrectly reported as dispatched/failed.
- Improve admin alert visibility with delivery outcome context and better table ergonomics.
- Change user subscription loading from rule-scoped to user-wide grouped-by-channel view.
- Preserve loaded rule/subscription lists across create and edit interactions.

## Delivered

- Corrected event ingestion counters in API:
  - `DispatchedNotifications` now increments only when delivery outcome is `Succeeded`.
  - `FailedNotifications` now increments for non-success outcomes and exceptions.
- Added delivery-attempt repository query by event ID (`ListByEventIdAsync`) and in-memory implementation.
- Enriched `/api/v1/admin/alerts` response with per-event delivery summary fields:
  - total attempts, succeeded/transient/permanent/dead-letter counts
  - last outcome, last failure reason, last attempted timestamp
- Added user-wide subscriptions endpoint:
  - `GET /api/v1/alert-rules/subscriptions?userId=...`
- Updated Web typed client contract/implementation:
  - added user-wide subscription query method
  - extended admin alert DTO with outcome summary fields
- Updated User panel behavior:
  - Load Subscriptions now fetches all subscriptions for the demo user
  - subscriptions rendered grouped by channel
  - subscription rows include associated rule name
  - create/edit/cancel handlers retain loaded rules/subscriptions instead of clearing the lists
- Updated Admin panel alerts table:
  - sticky header with scrollable body
  - last outcome badge and attempt breakdown columns
  - last failure reason column

## Function Documentation Added

- XML docs were added/kept current for new/updated API and page-model methods:
  - `src/WordEventAlerts.Core/Abstractions/Repositories/IDeliveryAttemptRepository.cs`
  - `src/WordEventAlerts.Api/Endpoints/AlertRuleEndpoints.cs`
  - `src/WordEventAlerts.Web/Pages/User/Rules.cshtml.cs`

## Logging Added

- Added dedicated subscription user-list event:
  - `SubscriptionListByUser`
- Existing structured event logging preserved for correlation:
  - `CorrelationId`

## Validation

- Build + tests: `dotnet test .\WordEventAlerts.slnx` succeeded.
- Totals: 31 tests passed, 0 failed.

## Notes

- Admin alert outcome summary is derived from persisted delivery attempts by event ID.
- User panel subscription grouping supports mixed-rule subscriptions without requiring rule-level list reloads.