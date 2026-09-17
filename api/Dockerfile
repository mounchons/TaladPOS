# TaladPOS API (.NET 8) - built by docker-compose.yml at the repo root.
# Build context is api/, see api/.dockerignore.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore from the project files alone so the NuGet layer stays cached until a
# package reference changes. Restoring TaladPOS.sln instead would also pull in
# tests/ and tools/, which the image never runs.
COPY src/TaladPOS.Domain/TaladPOS.Domain.csproj src/TaladPOS.Domain/
COPY src/TaladPOS.Application/TaladPOS.Application.csproj src/TaladPOS.Application/
COPY src/TaladPOS.Infrastructure/TaladPOS.Infrastructure.csproj src/TaladPOS.Infrastructure/
COPY src/TaladPOS.Api/TaladPOS.Api.csproj src/TaladPOS.Api/
RUN dotnet restore src/TaladPOS.Api/TaladPOS.Api.csproj

COPY src/ src/
RUN dotnet publish src/TaladPOS.Api/TaladPOS.Api.csproj -c Release -o /app/publish --no-restore

# Debian-based runtime rather than Alpine: it ships ICU, so culture-aware
# string handling behaves the same as on a developer machine.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Non-root user that the .NET 8 images ship with but do not switch to.
USER $APP_UID

# The aspnet:8.0 image listens on 8080 by default (ASPNETCORE_HTTP_PORTS).
EXPOSE 8080
ENTRYPOINT ["dotnet", "TaladPOS.Api.dll"]
