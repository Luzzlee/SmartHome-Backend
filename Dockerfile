# Dev-convenience Dockerfile for the SmartHome backend.
#
# Builds and runs the API for local development / the docker-compose dev stack (see
# SmartHome-Infrastructure - compose wiring for this service is a separate, not-yet-implemented
# issue there). This is intentionally a single-stage build using the full SDK image rather than an
# optimized, size-minimized multi-stage production build - see issue #7 "Abgrenzung". A production
# deploy path is a separate, later concern.
FROM mcr.microsoft.com/dotnet/sdk:9.0

WORKDIR /src

# Copy the whole solution - this is a multi-project solution (Api/, Shared/, Slices/*) and project
# references need every referenced project present to restore/build.
COPY . .

RUN dotnet restore Api/SmartHome.Api/SmartHome.Api.csproj
RUN dotnet build Api/SmartHome.Api/SmartHome.Api.csproj -c Release --no-restore

WORKDIR /src/Api/SmartHome.Api

# Dev-container defaults. All of these stay overridable at `docker run`/compose time via env vars -
# nothing sensitive is baked into the image (see repo CLAUDE.md "Commands" for the equivalent
# user-secrets/env-var pattern used outside a container):
#   ASPNETCORE_ENVIRONMENT, ConnectionStrings__Default, Mqtt__* (incl. Mqtt__Password), Auth__*.
ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "run", "-c", "Release", "--no-build", "--no-restore", "--no-launch-profile"]
