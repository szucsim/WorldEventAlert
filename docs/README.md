# Documentation Hub

This file is the base index for project documentation and reviewer navigation.

## Code Validation & Quality Assurance Methodology

To ensure strict engineering control over the AI agent and prevent code regression, I established a rigorous gatekeeping process for every single agent step (documented across the implementation steps in `docs/implementation`):

### 1. Static Code Review

Before accepting or committing any AI-generated changes, I perform a thorough line-by-line manual code review. I explicitly check for hidden hallucinations, architectural shortcuts, and adherence to .NET 10 best practices.

### 2. Runtime Verification & UI Testing

Following the code review, I compile the application and perform comprehensive manual runtime testing. I interactively navigate the Razor Pages UI, trigger event simulations, and monitor backend behaviors.

### 3. Adaptive Feedback Loop

If any bug, edge-case vulnerability, or UI inconsistency is discovered during runtime testing, I immediately halt the automated generation pipeline. I then either adapt my engineering prompt to force a corrective architecture pivot or recalibrate the agent to safely steer it back onto the original technical roadmap.

## Architecture

- [phase1-scoping.md](architecture/phase1-scoping.md)
- [future-production-roadmap.md](architecture/future-production-roadmap.md)

## Decisions

- [architecture-and-decision-log.md](decisions/architecture-and-decision-log.md)

## Implementation

- [README.md](implementation/README.md)
- [step-template.md](implementation/step-template.md)
- [step-01-repository-bootstrap.md](implementation/step-01-repository-bootstrap.md)
- [step-02-openapi-scalar.md](implementation/step-02-openapi-scalar.md)
- [step-03-core-domain-contracts.md](implementation/step-03-core-domain-contracts.md)
- [step-04-inmemory-repositories.md](implementation/step-04-inmemory-repositories.md)
- [step-05-channel-dispatcher.md](implementation/step-05-channel-dispatcher.md)
- [step-06-observability-foundation.md](implementation/step-06-observability-foundation.md)
- [step-07-function-docs-backfill.md](implementation/step-07-function-docs-backfill.md)
- [step-08-alert-matching-api.md](implementation/step-08-alert-matching-api.md)
- [step-09-route-versioning-contract-hardening.md](implementation/step-09-route-versioning-contract-hardening.md)
- [step-10-admin-api-delivery-ops.md](implementation/step-10-admin-api-delivery-ops.md)
- [step-11-minimal-razor-user-admin-ui.md](implementation/step-11-minimal-razor-user-admin-ui.md)
- [step-12-observability-hardening-e2e-trace-coverage.md](implementation/step-12-observability-hardening-e2e-trace-coverage.md)
- [step-13-manual-testing-hardening-rule-naming.md](implementation/step-13-manual-testing-hardening-rule-naming.md)
- [step-14-user-admin-edit-delete-mock-simulation.md](implementation/step-14-user-admin-edit-delete-mock-simulation.md)
- [step-15-admin-outcome-ux-and-user-subscription-grouping.md](implementation/step-15-admin-outcome-ux-and-user-subscription-grouping.md)
- [step-16-header-copyright-and-create-mode-reset.md](implementation/step-16-header-copyright-and-create-mode-reset.md)
- [step-17-form-reset-and-mock-ingestion-visibility-fix.md](implementation/step-17-form-reset-and-mock-ingestion-visibility-fix.md)
- [step-18-user-subscription-destination-prevalidation.md](implementation/step-18-user-subscription-destination-prevalidation.md)
- [step-19-documentation-standard-alignment.md](implementation/step-19-documentation-standard-alignment.md)
- [step-20-chat-json-to-markdown-export.md](implementation/step-20-chat-json-to-markdown-export.md)
- [step-21-documentation-hub-and-validation-methodology.md](implementation/step-21-documentation-hub-and-validation-methodology.md)
- [step-22-docker-compose-reviewer-runtime.md](implementation/step-22-docker-compose-reviewer-runtime.md)
- [step-23-future-production-roadmap-planning.md](implementation/step-23-future-production-roadmap-planning.md)
- [step-24-project-rename-world-event-alerts.md](implementation/step-24-project-rename-world-event-alerts.md)
- [step-25-chat-export-script-fine-tuning.md](implementation/step-25-chat-export-script-fine-tuning.md)

## Reviewer Quick Path

1. Start with architecture: `docs/architecture/phase1-scoping.md`.
2. Review decision rationale: `docs/decisions/architecture-and-decision-log.md`.
3. Walk execution history in order: `docs/implementation/README.md` and linked step files.

## Setup and Run

### Option A: Docker Compose (Single Command)

From repository root:

```powershell
docker compose up --build
```

Access points:

- Web UI: http://localhost:5111
- API: http://localhost:5239
- API DOCS (SCALAR): http://localhost:5239/scalar/
- API health: http://localhost:5239/api/v1/health

Stop:

```powershell
docker compose down
```

### Option B: Local dotnet Run

Run API:

```powershell
dotnet run --project .\src\WorldEventAlerts.Api\WorldEventAlerts.Api.csproj
```

Run Web in another terminal:

```powershell
dotnet run --project .\src\WorldEventAlerts.Web\WorldEventAlerts.Web.csproj
```
