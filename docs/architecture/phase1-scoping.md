# Word Event Alerts - Phase 1 Scoping and Architecture Design

Date: 2026-06-15  
Status: Draft Approved for Implementation Baseline

## 1. Purpose

This document defines the Phase 1 architecture baseline before production feature implementation. It resolves ambiguity in the product brief and establishes domain boundaries, extensibility strategy, observability standards, testing boundaries, and milestone sequencing.

## 2. Scope and Constraints

### In Scope (Phase 1)

- .NET 10 Web API backend.
- Native .NET OpenAPI with Scalar integration.
- Minimal Razor Pages UI for user and admin workflows.
- In-memory repository implementations behind abstractions.
- Pluggable notification channels with Email and Slack adapters.
- Structured logging with correlation IDs from ingestion to dispatch.
- Unit, integration, and API contract test foundations.

### Out of Scope (Phase 1)

- Real external event-feed integrations.
- Persistent storage implementation (SQLite/Postgres adapters).
- Production-grade auth and RBAC hardening.
- Distributed queue/bus infrastructure.
- Advanced rule expression language beyond baseline filters.

## 3. Ambiguity Resolution and Assumptions

### 3.1 "Something important happening in the world"

Assumption: All incoming events are normalized to a canonical telemetry payload.

`WorldEvent` canonical fields (initial):

- `EventId` (internal ID, UUID)
- `SourceEventId` (external/source-system ID)
- `SourceSystem`
- `Category` (`BreakingNews`, `MarketMovement`, `NaturalDisaster`, `Other`)
- `SeverityScore` (0-100)
- `Headline`
- `Summary`
- `Regions` (ISO tags or agreed region codes)
- `Keywords` (normalized terms)
- `OccurredAtUtc`
- `IngestedAtUtc`
- `SchemaVersion` (server-managed, derived from API route version such as `/api/v1/...`)
- `CorrelationId`

### 3.2 Rule Triggering Semantics

Assumption: Rules are evaluated synchronously at ingest time in Phase 1.

Matching semantics:

- Condition groups combine with AND.
- Values inside a group combine with OR.
- A rule must be enabled to match.

MVP-supported predicates:

- `Category IN [..]`
- `SeverityScore >= threshold`
- `Regions intersects selected regions`
- `Keywords contains any selected keyword`

### 3.3 User Channel Preferences

Assumption: A user may subscribe multiple channels per rule.

`ChannelSubscription` fields (initial):

- `ChannelType` (`Email`, `Slack`)
- `Destination` (email address, webhook or channel mapping)
- `Enabled`
- `Priority` (fallback order)
- `RetryPolicyOverride` (optional)

### 3.4 Delivery Guarantees

Assumption: Best-effort delivery with bounded retries and dead-letter visibility.

- Not guaranteeing exactly-once delivery in Phase 1.
- Apply idempotency guards at the dispatcher boundary.
- Persist attempt outcomes for operational inspection.

### 3.5 Admin View Scope

Assumption: Admin must support full CRUD for users, rules, and channels plus operational visibility.

- CRUD operations for primary entities.
- Delivery attempt history view.
- Dead-letter inspection and replay action.

## 4. Architectural Strategy

### 4.1 Architectural Style

Clean/Hexagonal layering to preserve domain isolation.

Layers:

1. Core domain and application policies.
2. Infrastructure adapters (storage, channels, observability).
3. Delivery layers (API and Razor UI).

Dependency direction:

- Outer layers depend on inner layers.
- Core has no dependency on ASP.NET, Razor, SMTP/Slack SDKs, or storage technology.

### 4.2 Pluggable Notification Channel Pattern

Pattern: Strategy + Registry + Dispatcher.

Core abstractions:

- `INotificationChannel`:
  - `ChannelType`
  - `ValidateConfiguration(...)`
  - `SendAsync(...)`
- `IChannelRegistry`:
  - Resolve channel strategy by `ChannelType`.
- `NotificationDispatcher`:
  - Resolve strategy.
  - Execute send attempts with retry policy.
  - Capture structured outcomes.

Extension rule:

- Add new channel by implementing `INotificationChannel`, registering via DI, and adding tests.
- No domain matching code changes required.

### 4.3 Repository Strategy

Pattern: Repository interfaces in Core, adapters in Infrastructure.

Initial interfaces:

- `IWorldEventRepository`
- `IAlertRuleRepository`
- `IUserPreferenceRepository`
- `IDeliveryAttemptRepository`

Phase 1 adapters:

- In-memory implementations only.

Future adapters:

- SQLite/Postgres implementations preserving same contracts.

## 5. System Components and Folder Boundaries

Proposed structure:

- `src/WorldEventAlerts.Api`
  - HTTP endpoints, OpenAPI/Scalar wiring, middleware, composition root.
- `src/WorldEventAlerts.Web`
  - Razor Pages for user/admin workflows; thin presentation layer.
- `src/WorldEventAlerts.Core`
  - Domain models, contracts, domain services, policies.
- `src/WorldEventAlerts.Infrastructure.InMemory`
  - In-memory repositories and ephemeral state stores.
- `src/WorldEventAlerts.Infrastructure.Notifications.Email`
  - Email channel adapter implementation.
- `src/WorldEventAlerts.Infrastructure.Notifications.Slack`
  - Slack channel adapter implementation.
- `src/WorldEventAlerts.Infrastructure.Observability`
  - Correlation middleware and logging helpers.
- `tests/WorldEventAlerts.Tests.Unit`
  - Domain and policy unit tests.
- `tests/WorldEventAlerts.Tests.Integration`
  - Pipeline integration tests.
- `tests/WorldEventAlerts.Tests.Api`
  - API contract/integration tests.

## 6. Logging and Correlation Strategy

### 6.1 Correlation Protocol

- Inbound API accepts `X-Correlation-Id`.
- If absent, service generates one.
- Correlation ID flows through:
  - event ingestion
  - rule matching
  - notification dispatch
  - retry/dead-letter handling

### 6.2 Structured Logging Contract

Required log properties:

- `CorrelationId`
- `EventId`
- `RuleId` (when applicable)
- `UserId` (when applicable)
- `ChannelType` (when applicable)
- `DeliveryAttemptId` (when applicable)
- `Outcome`
- `FailureClassification` (`Transient` or `Permanent`)

Standard event names:

- `EventIngested`
- `RuleMatched`
- `RuleNotMatched`
- `DeliveryAttempted`
- `DeliverySucceeded`
- `DeliveryFailed`
- `DeliveryDeadLettered`

## 7. Testing Strategy and Verification Boundaries

### 7.1 Unit Tests

Focus:

- Domain invariants and validation.
- Matching engine semantics and edge cases.
- Channel registry behavior.
- Retry classification and idempotency policy units.

### 7.2 Integration Tests

Focus:

- End-to-end ingest -> match -> dispatch flow.
- In-memory repository behavior under realistic scenarios.
- Retry and dead-letter transition behavior.

### 7.3 API Contract Tests

Focus:

- Request/response schema correctness.
- Status codes and problem detail contracts.
- OpenAPI operation discoverability.

### 7.4 Quality Gates

- Build must pass.
- Unit and integration tests must pass for milestone merges.
- Correlation traceability validated in tests.
- No forbidden architecture dependency inversions.

## 8. Milestone and Commit Plan

1. Milestone 1: Solution bootstrap + OpenAPI/Scalar foundation.
2. Milestone 2: Core domain models/contracts + unit test harness.
3. Milestone 3: In-memory repositories + alert matching engine.
4. Milestone 4: Channel plugin framework + Email/Slack adapters.
5. Milestone 5: User/admin API endpoints + API contract tests.
6. Milestone 6: Minimal Razor user/admin UI.
7. Milestone 7: Observability hardening + end-to-end integration coverage.
8. Milestone 8: ADR freeze, architecture review, and readiness sign-off.

## 9. Risk Register and Mitigations

- Risk: Rule model under-specification.
  - Mitigation: lock MVP predicate set and document expansion points.
- Risk: Channel-specific SDK coupling leaks into core.
  - Mitigation: enforce adapter-only SDK usage and interface contracts.
- Risk: Duplicate notifications during retries.
  - Mitigation: idempotency key strategy and attempt tracking.
- Risk: In-memory assumptions hide persistence concerns.
  - Mitigation: repository interfaces include query semantics compatible with persistent backends.

## 10. Acceptance for Phase 1 Architecture Completion

- Ambiguities resolved or explicitly deferred.
- Baseline structure agreed.
- Logging/correlation protocol defined and testable.
- Testing boundaries agreed.
- Milestone plan approved for implementation.

