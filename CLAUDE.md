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

The API is protected by cookie-based session auth for a single, statically configured user (see "Authentication" below) — set its password hash locally the same way:

```bash
cd Api/SmartHome.Api
dotnet user-secrets set "Auth:PasswordHash" "<hash-of-real-password>"
```

(or via env var `Auth__PasswordHash`). The username lives in `appsettings.json` under the `Auth` section; only the password hash is excluded from source control. Generate a hash for a chosen password with ASP.NET Core Identity's `PasswordHasher<TUser>` (the same type `AuthService` uses to verify it), e.g. via `dotnet-script`/a scratch console app:

```csharp
new Microsoft.AspNetCore.Identity.PasswordHasher<string>().HashPassword("admin", "<real-password>")
```

## Architecture

Vertical slices under `Slices/{Devices,Auth}/{Repository,Services,UnitTests}` (Auth has no `Repository` — there's no user table, just a single user validated against config), each its own project with an `Extensions.cs` exposing `Add{Feature}{Layer}(this IServiceCollection)` for DI registration. Dependency direction: Services → Repository → Shared. Shared code (DB init, MQTT helper, SignalR hub, `Device` model) lives in `Shared/SmartHome.Shared/`. `Api/SmartHome.Api` hosts everything and wires the controllers straight to Services.

Device ID format: `yyyyMMdd-XXX` (e.g. `20250829-001`), validated in `DevicesService`.

## Authentication

Cookie-based session auth (`Microsoft.AspNetCore.Authentication.Cookies`) for a single, statically configured user — no registration, no multi-user support, no roles. Credentials live in the `Auth` config section (username in `appsettings.json`, password hash via user-secrets/env-var, see above), verified with `PasswordHasher<TUser>` (`AuthService`, `Slices/Auth/Services`). `AuthController` (`/auth/login`, `/auth/logout`, `/auth/me`) issues/clears the cookie; all `DevicesController` endpoints require it via `[Authorize]`. The `DeviceHub` SignalR hub (`/devicehub`) requires the same cookie via `[Authorize]` on the hub class and `.RequireAuthorization()` on its `MapHub` call in `Program.cs`, so live device-state broadcasts aren't reachable without a valid session either. `GET /health` is the one deliberate exception — it's mapped with `.AllowAnonymous()` so external monitoring/load balancers can probe it without a session. CORS uses the `AllowFrontend` policy (explicit origin + `AllowCredentials()`) rather than a wildcard origin, since browsers require that combination for cookies to be sent cross-origin. The cookie is `Secure` outside of Development; HTTPS enforcement for actual remote access is still a separate, later concern.

## MQTT connectivity and health checks

`MqttHelper` (`Shared/SmartHome.Shared/Mqtt`) connects to the broker once at startup; if the broker is unreachable, the app logs the failure and starts anyway instead of crashing (`Program.cs` wraps the initial `ConnectAsync()` call in a try/catch). A background retry loop with exponential backoff (5s up to a 1-minute cap) keeps attempting to (re-)connect, both after a failed initial connect and after any later disconnect, so the transport connection recovers automatically once the broker comes back — no app restart needed. The client connects with `.WithCleanSession()`, which means the broker forgets subscriptions on every connect; `MqttHelper` tracks every topic subscribed via `SubscribeAsync` (used by `DevicesService.SubscribeToDevice`) and automatically replays all of them after any successful (re)connect — initial, manual, or from the background retry loop — so previously subscribed devices keep receiving live SignalR state pushes after a broker blip, not just the transport-level connection. `GET /health` (anonymous, see "Authentication" above) reports `database` and `mqtt` connectivity as separate entries in its JSON response via ASP.NET Core's health checks middleware (`Api/SmartHome.Api/HealthChecks`), so DB and MQTT problems can be told apart from the outside.

## Docker

A dev-convenience `Dockerfile` at the repo root builds and runs the API (single-stage, using the full SDK image — not an optimized multi-stage production build, that's a separate later concern):

```bash
docker build -t smarthome-backend:dev .
docker run -p 8080:8080 -e Mqtt__Password=<real-value> -e Auth__PasswordHash=<hash-of-real-password> smarthome-backend:dev
```

`ASPNETCORE_ENVIRONMENT` defaults to `Development` and the API listens on `8080` inside the container (`ASPNETCORE_URLS=http://+:8080`); nothing sensitive is baked into the image — `ConnectionStrings:Default`, the `Mqtt` section (incl. `Mqtt:Password`) and `Auth` section (incl. `Auth:PasswordHash`) all stay overridable via env vars (`ConnectionStrings__Default`, `Mqtt__Password`, `Auth__PasswordHash`, etc.), same as the user-secrets pattern above. Wiring this into the `SmartHome-Infrastructure` docker-compose stack alongside Mosquitto and the frontend is a separate, not-yet-implemented issue there.

## Known issues / not yet done

- CI (`.github/workflows/ci-dev.yml`, `ci-feature.yml`) runs on push to `dev` and `f#**` branches, and additionally on `pull_request` events targeting `dev` (job `build-and-test`). No coverage of `main`. `dev` has no branch protection / required status checks configured yet.
- `smarthome.db` is local dev state (gitignored, recreated on startup) — don't expect it to carry data between machines.
