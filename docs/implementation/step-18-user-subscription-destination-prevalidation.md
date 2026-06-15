# Step 18 - User Subscription Destination Prevalidation

## Commit

- Hash: `<pending>`
- Message: `<pending>`

## Objective

- Validate subscription destination format in the User panel before API submission, not only for empty values.
- Ensure invalid email destinations are rejected at creation/update time.

## Delivered

- Added pre-submit channel-aware destination validation to user subscription upsert flow.
- Added email-like format check (`@` present and not at start/end) in User flow.
- Added Slack destination checks in User flow for parity with backend channel validation:
  - absolute URI required
  - HTTPS scheme required
- Validation now runs before sending `UpsertSubscriptionAsync`, returning a clear user-facing error message when invalid.

## Function Documentation Added

- Existing XML docs were kept current in modified file:
  - `src/WorldEventAlerts.Web/Pages/User/Rules.cshtml.cs`

## Logging Added

- No new structured logging events were required for this validation-only change.

## Validation

- Build + tests: `dotnet test .\WorldEventAlerts.slnx --nologo -v minimal` succeeded.
- Totals: 31 tests passed, 0 failed.

## Notes

- This change prevents avoidable API round-trips for malformed destinations and aligns UI behavior with channel adapter constraints.
