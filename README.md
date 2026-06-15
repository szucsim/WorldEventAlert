# Word Event Alerts

Prototype notification platform for world-event alerting with a .NET 10 API and Razor Pages Web UI.

## Run With Docker Compose (Reviewer Path)

From the repository root, run:

```powershell
docker compose up --build
```

After startup:

- Web UI: http://localhost:5111
- API: http://localhost:5239
- API health endpoint: http://localhost:5239/api/v1/health

Stop containers with:

```powershell
docker compose down
```

## Local Run Without Docker

Start API:

```powershell
dotnet run --project .\src\WorldEventAlerts.Api\WorldEventAlerts.Api.csproj
```

Start Web in a separate terminal:

```powershell
dotnet run --project .\src\WorldEventAlerts.Web\WorldEventAlerts.Web.csproj
```

Default local ports:

- API: http://localhost:5239
- Web: http://localhost:5111

## Documentation

- Project docs hub: [docs/README.md](docs/README.md)
- Reviewer chat transcript: [docs/copilot-chat/chat.md](docs/copilot-chat/chat.md)
- Architecture and scoping: [docs/architecture/phase1-scoping.md](docs/architecture/phase1-scoping.md)
- Future production roadmap: [docs/architecture/future-production-roadmap.md](docs/architecture/future-production-roadmap.md)
- Architecture decisions: [docs/decisions/architecture-and-decision-log.md](docs/decisions/architecture-and-decision-log.md)
- Implementation log: [docs/implementation/README.md](docs/implementation/README.md)
