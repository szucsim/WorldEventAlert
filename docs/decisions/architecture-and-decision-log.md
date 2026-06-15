# Architecture & Decision Log

## Technology Stack Selection (Context)

As the position targets a .NET engineer role, the core stack selection was straightforward: .NET 10 Web API with native OpenAPI + Scalar integration for modern, greenfield API documentation (moving away from the legacy Swagger defaults).

### UI Layer

Implemented using thin Razor Pages (.cshtml) for minimal overhead and simple server-rendering during this prototype phase. The design strictly decouples backend endpoints from the UI, ensuring seamless future replacement with any modern SPA framework (for example: Angular, React, Vue).

### Database/Storage

Utilizing an In-Memory database to eliminate infrastructure setup constraints and maintain rapid prototyping velocity, with interfaces designed to easily swap in a production-ready PostgreSQL instance later.

## Step 0

The development was initiated by structuring a comprehensive, highly constrained architectural prompt to align the AI assistant before generating any code.

## Technical Decisions & Course Corrections

## Decision 1: Payload Contract Refactoring (API Versioning)

### Context

During the implementation of Milestone 2, the AI-generated contract included an explicit version field inside the client payload.

### Course-Correction (Milestone 2)

I identified a critical architecture flaw in the generated IngestWorldEventRequest contract. The AI automatically included a SchemaVersion string field within the client request payload. I rejected this design because schema versioning belongs at the API routing/header layer, not as an editable payload field which introduces unnecessary validation vectors and breaks strict contract safety. I removed the field entirely and locked versioning to the standard API route prefix (/api/v1/...).

## Decision 2: Architectural Decision - Mock Notification Gateways via Structured Logging

### Context

The product brief specifies active support for Email and Slack delivery channels. However, configuring functional SMTP relays, managing Slack Webhook secrets securely, and handling external network runtime dependencies falls outside the 24-hour evaluation scope for a core architectural prototype.

### Decision

I chose to implement EmailNotificationChannel and SlackNotificationChannel using asynchronous simulated latency and structured ILogger outputs.

### Justification

This approach fully exercises the dependency injection layer, the Open-Closed Principle (via the Strategy Pattern), and the NotificationDispatcher pipeline without introducing external operational complexity or the risk of leaking sensitive infrastructure secrets in a public repository submission. The entire end-to-end notification delivery can be cleanly verified in the application console logs via cross-cutting Correlation IDs.

## Decision 3: Reviewer Runtime Standardization via Docker Compose

### Context

Although the initial plan focused on local dotnet execution, reviewer environments can vary in SDK/runtime setup and local tooling readiness. That variability creates friction during evaluation and can hide architecture quality behind machine-specific setup issues.

### Decision

I added containerized startup support for both API and Web using a shared Dockerfile with two targets and a root docker-compose.yml. This enables single-command startup with docker compose up.

### Justification

This keeps evaluation focused on architecture and behavior rather than environment bootstrapping. The compose setup enforces deterministic service wiring, including explicit API and Web port mappings and container-to-container communication for the Web to call the API.

### Port and Service Contract

- Host port 5239 -> API container port 8080
- Host port 5111 -> Web container port 8080
- Web container API base URL -> http://api:8080

## Decision 4: Future Production Roadmap Definition (Planning Only)

### Context

The current solution is intentionally MVP-oriented (in-memory storage, minimal auth posture, and synchronous processing paths). Before any production implementation work, a formal roadmap is required to prevent ad hoc architectural drift.

### Decision

I defined a future production roadmap document that sequences four major upgrade tracks without implementing them in this phase:

1. In-memory storage migration to PostgreSQL.
2. Security hardening for admin UI and admin endpoints with JWT/Bearer.
3. UI/workflow completion from MVP ergonomics to production-ready flows.
4. Event pipeline evolution from synchronous loops to asynchronous broker processing.

### Justification

This creates a controlled transition path from prototype to production while preserving current architecture boundaries and reducing migration risk through explicit validation gates per track.

### Reference

- Roadmap document: `docs/architecture/future-production-roadmap.md`
