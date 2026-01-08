FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ./src/Orchestrator.App/ ./Orchestrator.App/
RUN dotnet publish ./Orchestrator.App/Orchestrator.App.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/runtime:8.0
# hadolint ignore=DL3008
RUN apt-get update && apt-get install -y --no-install-recommends \
    ca-certificates \
    curl \
    git \
    && rm -rf /var/lib/apt/lists/* \
    && curl -fsSL https://get.docker.com -o get-docker.sh \
    && sh get-docker.sh \
    && rm get-docker.sh

WORKDIR /app
COPY --from=build /out ./

# Configure Git to trust any directory (safe for Docker environment)
RUN git config --global --add safe.directory '*'

ENTRYPOINT ["dotnet", "Orchestrator.App.dll"]
