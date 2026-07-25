# SmartHome-Backend

.NET 9 Web API for the SmartHome project, built with a vertical-slice architecture. Serves device data over REST, pushes live updates over SignalR, and talks to devices via MQTT. SQLite (via Dapper) is the persistence layer.

See also: [top-level `D:\Git\CLAUDE.md`](../CLAUDE.md) for how this fits with the other SmartHome repos.

## Tech stack

.NET 9, ASP.NET Core Web API, SQLite via Dapper, MQTTnet (+ MQTTnet.AspNetCore), SignalR, NUnit 4 + Moq for tests, Swashbuckle for OpenAPI/Swagger.

## Commands

```bash
dotnet restore
dotnet build -c Release
dotnet test -c Release
dotnet run --project Api/SmartHome.Api   # http://localhost:5241, https://localhost:7201
```

MQTT broker credentials are **not** hardcoded — set the password locally before the app can connect to the broker:

```bash
cd Api/SmartHome.Api
dotnet user-secrets set "Mqtt:Password" "<real-value>"
```

(or via env var `Mqtt__Password`). Host/port/username/client-id live in `appsettings.json` under the `Mqtt` section; only the password is excluded from source control.

## Architecture

Vertical slices under `Slices/Devices/{Repository,Services,UnitTests}`, each its own project with an `Extensions.cs` exposing `Add{Feature}{Layer}(this IServiceCollection)` for DI registration. Dependency direction: Services → Repository → Shared. Shared code (DB init, MQTT helper, SignalR hub, `Device` model) lives in `Shared/SmartHome.Shared/`. `Api/SmartHome.Api` hosts everything and wires the controllers straight to Services.

Device ID format: `yyyyMMdd-XXX` (e.g. `20250829-001`), validated in `DevicesService`.

## Known issues / not yet done

- **No authentication** on the API yet. The Svelte frontend's login is intentionally simulated pending this.
- `DatabaseInitializer.Initialize` reads the SQL init script via a hardcoded relative path (`../../Shared/SmartHome.Shared/DatabaseScripts/create.sql`) relative to the process working directory — works when run via `dotnet run` from `Api/SmartHome.Api`, but is fragile for other run/deploy contexts.
- CI (`.github/workflows/ci-dev.yml`, `ci-feature.yml`) only runs on push to `dev` and `f#**` branches — no PR-triggered CI, no coverage of `main`.
- `smarthome.db` is local dev state (gitignored, recreated on startup) — don't expect it to carry data between machines.
