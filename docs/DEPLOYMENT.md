# Feirb — Deployment

> **Status: Early Development**
>
> Feirb does not yet have a production deployment story. There is no Docker Compose file, no container images, and no published releases. The only way to run Feirb today is via the development setup described below.

## Current: Development Mode (Aspire)

The only supported way to run Feirb is through .NET Aspire, which orchestrates all services locally:

```bash
dotnet workload install aspire
dotnet restore Feirb.sln
dotnet run --project src/Feirb.AppHost
```

This starts:
- **Blazor WASM frontend** — https://localhost:7100
- **API backend** — https://localhost:7200
- **PostgreSQL** — localhost:5432 (Aspire-managed container)
- **Ollama** — with qwen3:4b model (Aspire-managed container)
- **GreenMail** — SMTP localhost:3025, IMAP localhost:3143, API http://localhost:8080 (dev mail server)
- **Aspire Dashboard** — https://localhost:18888

### Requirements

- .NET 10 SDK (10.0.100+)
- Docker (for PostgreSQL, Ollama, and GreenMail containers)

See [Developer Setup](SETUP.md) for detailed instructions.

## Planned: Production Deployment

The following deployment options are planned but not yet implemented:

### Docker Compose

A `docker-compose.yml` will be provided for self-hosted deployment on NAS systems. Target architecture:

```yaml
# Planned — not yet available
services:
  feirb-api:      # Backend API
  feirb-web:      # Blazor WASM (served via nginx or similar)
  postgres:       # Database
  ollama:         # AI model server
```

### What's Needed Before Production

- Core mail functionality (IMAP fetch, compose, send) must be implemented
- Dockerfiles for API and Web projects
- Docker Compose configuration with persistent volumes
- Environment-based configuration (connection strings, JWT secrets, etc.)
- Database migration strategy for upgrades
- Health checks and restart policies
- Documentation for NAS-specific setups (Synology, QNAP, Unraid, etc.)

## Data Persistence

Currently, Aspire manages all containers and their storage. In development:

- **PostgreSQL data** is stored in Docker volumes managed by Aspire
- **Ollama models** are stored in a persistent Docker volume (~2.6GB for qwen3:4b)
- **No backup strategy** exists yet — this is a development environment

For future production deployment, persistent volume mounts and backup procedures will be documented.
