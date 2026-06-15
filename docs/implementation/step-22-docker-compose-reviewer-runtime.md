# Step 22 - Docker Compose Reviewer Runtime

## Commit

- Hash: `<pending>`
- Message: `<pending>`

## Objective

- Add a single-command reviewer runtime path using Docker Compose.
- Start both API and Web with deterministic port mappings and internal service wiring.
- Document setup/run in README surfaces and record rationale in decisions.

## Delivered

- Added root `Dockerfile` with two runnable targets:
  - `api`
  - `web`
- Added root `docker-compose.yml` with both services.
- Configured service contract:
  - Host `5239` -> API container `8080`
  - Host `5111` -> Web container `8080`
  - Web -> API base URL via `AlertsApi__BaseUrl=http://api:8080`
- Added root project README with Docker and local run instructions.
- Updated docs hub with setup and run instructions.
- Updated decision log with containerization rationale and port contract.

## Function Documentation Added

- Not applicable for this step (container/runtime and documentation updates only).

## Logging Added

- No new runtime logging events were introduced in this step.

## Validation

- Compose config validated successfully with `docker compose config`.
- Services started successfully with `docker compose up --build -d`.
- Running service mappings confirmed with `docker compose ps`:
  - `5239:8080` for API
  - `5111:8080` for Web
- Runtime checks passed:
  - `http://localhost:5239/api/v1/health` returned HTTP 200
  - `http://localhost:5111` returned HTTP 200

## Notes

- Reviewer path is now: `docker compose up --build` from repository root.