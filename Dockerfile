FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ./src/Orchestrator.App/ ./Orchestrator.App/
RUN dotnet publish ./Orchestrator.App/Orchestrator.App.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/runtime:8.0
# hadolint ignore=DL3008
RUN apt-get update && apt-get install -y --no-install-recommends git ca-certificates && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /out ./
ENTRYPOINT ["dotnet", "Orchestrator.App.dll"]
