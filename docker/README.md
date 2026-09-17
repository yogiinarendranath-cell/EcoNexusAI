# EcoNexus AI — Docker

This folder contains Dockerfiles for the API and Worker services.

## Services (see docker-compose.yml at repo root)

| Service | Image | Port(s) | Purpose |
|---------|-------|---------|---------|
| sqlserver | mcr.microsoft.com/mssql/server:2022-latest | 1433 | SQL Server 2022 Developer edition |
| azurite | mcr.microsoft.com/azure-storage/azurite:latest | 10000-10002 | Azure Storage emulator |
| api | built from Dockerfile.api | 5067 -> 8080 | ASP.NET Core API |
| worker | built from Dockerfile.worker | - | Background service host |

## Prerequisites

- Docker Desktop (Windows/macOS) or Docker Engine + Compose plugin (Linux)
- On Windows: WSL2 enabled, hardware virtualization enabled in BIOS

## Usage

    # Build and start everything in the background
    docker compose up -d --build

    # Tail logs
    docker compose logs -f api
    docker compose logs -f worker

    # Stop
    docker compose down

    # Stop and wipe database + storage volumes
    docker compose down -v

## Connection details (dev only)

| What | Value |
|------|-------|
| SQL Server host (from host machine) | localhost,1433 |
| SQL Server host (from other containers) | sqlserver,1433 |
| SA password | EcoNexus!Dev2026 |
| Azurite blob endpoint | http://localhost:10000/devstoreaccount1 |

> Security note: the SA password and Azurite account key in docker-compose.yml
> are development-only secrets. They never reach production. Production secrets
> live in Azure Key Vault (see Step 16).

## Status

These files have not yet been executed. Docker is not installed on the
primary dev machine. They will be verified end-to-end before Azure deployment
(Step 16). Until then, treat them as documented intent, not proven behavior.
