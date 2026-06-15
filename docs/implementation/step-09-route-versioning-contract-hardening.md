# Step 09 - Route Versioning Contract Hardening

## Commit

- Hash: `<pending>`
- Message: `<pending>`

## Objective

- Remove client-editable schema versioning from event ingestion payload.
- Enforce API versioning at route level using `/api/v1/...` prefix.

## Delivered

- Removed `SchemaVersion` from ingest request contract.
- Locked event schema version assignment to server-side `v1` in ingestion pipeline.
- Updated all mapped API routes to `/api/v1/...`:
  - health endpoint
  - events endpoints
  - alert-rules endpoints
- Updated integration tests to target versioned routes.
- Updated architecture documentation to clarify server-managed schema versioning.

## Function Documentation Added

- No new public methods were introduced in this step.
- Existing XML function documentation remains intact and compliant.

## Logging Added

- No new log event types introduced.
- Existing structured logging now runs under versioned route paths.

## Validation

- Build: passed
- Unit tests: passed (5/5)
- Integration tests: passed (13/13)

## Notes

- This addresses a contract-safety flaw by eliminating payload-based schema control vectors.
