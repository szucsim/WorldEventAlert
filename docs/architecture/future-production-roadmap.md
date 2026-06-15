# Future Production Roadmap

This roadmap defines planned production upgrades. It is intentionally planning-only and does not represent implemented behavior in the current MVP.

## Scope and Intent

- Preserve current MVP behavior while preparing for production reliability, security, and scalability.
- Sequence changes so each track can be independently validated and released.
- Keep architectural boundaries (Core abstractions, infrastructure adapters, API contracts) stable during migration.

## Track 1: Storage Migration (In-Memory -> PostgreSQL)

### Goal

Replace in-memory repositories with PostgreSQL-backed persistence while preserving repository contracts.

### Plan

1. Introduce PostgreSQL infrastructure project with repository implementations matching existing interfaces.
2. Add schema migrations for events, rules, subscriptions, and delivery attempts.
3. Add optimistic concurrency/versioning strategy for update paths.
4. Add connection resilience (retry policy, timeout standards, startup readiness check).
5. Add data retention and indexing strategy for alert/event query patterns.

### Validation Gates

1. Integration tests passing against PostgreSQL container.
2. No API contract changes required by clients.
3. Performance baseline defined for key list/filter endpoints.

## Track 2: Security Hardening (Admin Login + JWT/Bearer for API)

### Goal

Protect admin UI and admin API endpoints with authentication and authorization controls.

### Plan

1. Add identity/auth provider integration (or internal identity bootstrap for non-production stage).
2. Protect admin Razor Pages with authenticated/authorized policy.
3. Protect admin API route group with JWT Bearer authentication and role/claim policies.
4. Add token validation hardening (issuer, audience, signing key rotation strategy).
5. Add audit logging for authentication failures and privileged admin operations.

### Validation Gates

1. Unauthenticated access to admin routes returns 401/redirect as intended.
2. Unauthorized role access returns 403.
3. Positive-path admin workflows pass with valid JWT tokens.

## Track 3: UI and Workflow Completion (MVP -> Production UX)

### Goal

Evolve MVP screens into complete user/admin workflows with production-grade clarity, error handling, and consistency.

### Plan

1. Perform UX gap audit for all current flows (rules, subscriptions, ingest simulation, admin operations).
2. Add complete validation/error states and explicit success feedback for every action.
3. Normalize navigation patterns, table filtering/sorting/pagination, and edit-mode ergonomics.
4. Add role-aware UI affordances and operational safeguards for destructive actions.
5. Add accessibility and responsive behavior pass across desktop/mobile.

### Validation Gates

1. End-to-end acceptance scenarios documented and manually executed.
2. UI regression checklist maintained per release.
3. Critical flows validated in integration/UI smoke suite.

## Track 4: Event Processing Evolution (Sync Loops -> Async Message Broker)

### Goal

Move from synchronous in-process iteration to asynchronous, broker-backed event processing.

### Plan

1. Introduce event publisher abstraction in ingestion path.
2. Add broker adapter (RabbitMQ, Azure Service Bus, or Kafka) behind abstraction.
3. Implement consumer worker for matching + dispatch pipeline.
4. Add idempotency strategy and deduplication key model.
5. Add retry/dead-letter policies and operational telemetry.
6. Define delivery guarantees and ordering expectations per event type.

### Validation Gates

1. Ingestion acknowledged independently from downstream processing latency.
2. Failed processing recoverable through retry/dead-letter replay.
3. Correlation trace continuity preserved across publisher/consumer boundaries.

## Recommended Execution Order

1. Security hardening for admin surfaces.
2. PostgreSQL migration for durable state.
3. UI/workflow completion against durable/authenticated flows.
4. Broker migration for asynchronous scale-out.

## Risks and Mitigations

- Risk: Contract drift during migrations.
  - Mitigation: Keep interface-first design and contract tests as release gates.
- Risk: Security changes breaking current demos.
  - Mitigation: Maintain explicit local/dev profile and staging policy toggles.
- Risk: Operational complexity increase from broker adoption.
  - Mitigation: Incremental rollout with feature flags and replay tooling.
