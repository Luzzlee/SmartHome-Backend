# SmartHome-Backend

.NET 9 Web API for the [SmartHome](https://github.com/Luzzlee/SmartHome-Backend) home-automation project. Serves device data over REST, pushes live updates over SignalR, and talks to physical devices via MQTT. Cookie-based session auth protects the API for its single configured user.

Part of a multi-repo project — see the other repos: [SmartHome-Svelte-Frontend](https://github.com/Luzzlee/SmartHome-Svelte-Frontend) (dashboard UI), [SmartHome-Infrastructure](https://github.com/Luzzlee/SmartHome-Infrastructure) (MQTT broker), [SmartHome-Sketchbook](https://github.com/Luzzlee/SmartHome-Sketchbook) (device firmware).

## Tech stack

.NET 9, ASP.NET Core Web API (vertical-slice architecture), SQLite via Dapper, MQTTnet, SignalR, NUnit 4 + Moq, Swashbuckle (OpenAPI/Swagger).

## Getting started

```bash
dotnet restore
cd Api/SmartHome.Api
dotnet user-secrets set "Mqtt:Password" "<real-value>"
dotnet user-secrets set "Auth:PasswordHash" "<hash-of-real-password>"
cd ../..
dotnet run --project Api/SmartHome.Api
```

The API listens on `http://localhost:5241` / `https://localhost:7201`; Swagger UI is available at `/swagger` in Development. It expects an MQTT broker to be reachable (see `SmartHome-Infrastructure`) and starts even if one isn't, retrying the connection in the background.

```bash
dotnet test -c Release
```

For full details — architecture, authentication design, MQTT reconnect behavior, Docker, and known gaps — see [`CLAUDE.md`](./CLAUDE.md).
