# Deployment Guide: Ubuntu LTS 24.04

This guide describes how to deploy the AI-SDLC Orchestrator on an Ubuntu Server.

## Prerequisites

1.  **Docker & Docker Compose:**
    ```bash
    # Add Docker's official GPG key:
    sudo apt-get update
    sudo apt-get install ca-certificates curl
    sudo install -m 0755 -d /etc/apt/keyrings
    sudo curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
    sudo chmod a+r /etc/apt/keyrings/docker.asc

    # Add the repository to Apt sources:
    echo \
      "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/ubuntu \
      $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | \
      sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
    sudo apt-get update

    # Install Docker:
    sudo apt-get install docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
    ```

2.  **User Permissions:**
    Add your user to the docker group (so you don't need sudo for docker commands):
    ```bash
    sudo usermod -aG docker $USER
    # Log out and back in for this to take effect
    ```

## Installation

1.  **Prepare Directories:**
    ```bash
    sudo mkdir -p /opt/repos/orchestrator
    sudo chown -R $USER:$USER /opt/repos/orchestrator
    ```

2.  **Clone Repository:**
    ```bash
    git clone https://github.com/sterob-2/orchestrator.git /opt/repos/orchestrator
    cd /opt/repos/orchestrator
    ```

3.  **Create Workspace Directory:**
    The Orchestrator needs a workspace directory to check out the target project.
    ```bash
    mkdir -p workspace
    ```

4.  **Configure Environment:**
    Copy the example config and edit it.
    ```bash
    cp .env.example .env
    nano .env
    ```

    **Critical Settings for Server:**
    ```bash
    # ... standard keys (OPENAI, GITHUB, etc) ...

    # The absolute path on the HOST machine where the workspace resides.
    # This is required for sibling containers (MCP) to mount the volume correctly.
    WORKSPACE_HOST_PATH=/opt/repos/orchestrator/workspace
    
    # SonarQube (if applicable)
    SONAR_HOST_URL=https://sonarcloud.io
    SONAR_TOKEN=your_token
    ```

5.  **Start Service:**
    ```bash
    docker compose up -d --build
    ```

6.  **Verify:**
    ```bash
    docker compose logs -f
    ```

## Maintenance

*   **Update:** `git pull && docker compose up -d --build`
*   **Stop:** `docker compose down`
*   **Clean:** `docker system prune`
