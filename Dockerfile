FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ./src/Orchestrator.App/ ./Orchestrator.App/
RUN dotnet publish ./Orchestrator.App/Orchestrator.App.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/runtime:8.0
SHELL ["/bin/bash", "-o", "pipefail", "-c"]
# hadolint ignore=DL3008
RUN apt-get update && apt-get install -y --no-install-recommends \
    ca-certificates \
    curl \
    git \
    gnupg \
    && install -m 0755 -d /etc/apt/keyrings \
    && curl -fsSL --proto '=https' --tlsv1.2 https://download.docker.com/linux/debian/gpg -o /etc/apt/keyrings/docker.asc \
    && chmod a+r /etc/apt/keyrings/docker.asc \
    && echo \
      "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/debian \
      bookworm stable" | tee /etc/apt/sources.list.d/docker.list > /dev/null \
    && apt-get update \
    && apt-get install -y --no-install-recommends docker-ce-cli \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /out ./

# Configure Git to trust any directory (safe for Docker environment)
RUN git config --global --add safe.directory '*'

ENTRYPOINT ["dotnet", "Orchestrator.App.dll"]
