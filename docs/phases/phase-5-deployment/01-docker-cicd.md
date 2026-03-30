# Docker + CI/CD Pipeline (#158)

## Overview

Multi-stage Dockerfile, three docker-compose tiers for different self-hosted deployment scenarios, and a GitHub Actions workflow to build and push container images on release.

## Dockerfile

Multi-stage build at repository root.

**Build stage** (`dotnet/sdk:10.0`):
- Copies solution file and all project files first (layer caching for restore)
- Runs `dotnet restore`
- Copies full source and runs `dotnet publish -c Release` on `Feirb.Api`
- The publish of `Feirb.Api` automatically includes Blazor WASM output (project reference + `Microsoft.AspNetCore.Components.WebAssembly.Server`)

**Runtime stage** (`dotnet/aspnet:10.0`):
- Copies published output from build stage
- Runs as non-root user (`app` user, built into the aspnet base image)
- Exposes port 8080
- Entry point: `dotnet Feirb.Api.dll`

**`.dockerignore`** excludes: `bin/`, `obj/`, `.git/`, `tests/`, `*.md`, Aspire projects, IDE files.

## Docker Compose — Three Tiers

### Tier 1: API Only (`docker-compose.yml`)

User provides Postgres and Ollama externally.

```yaml
services:
  feirb:
    image: ghcr.io/mamu7211/feirb-mailclient:latest
    ports: ["8080:8080"]
    environment:
      - ConnectionStrings__feribdb=<user-provided>
      - Ollama__Endpoint=<user-provided>
```

### Tier 2: With Postgres (`docker-compose.postgres.yml`)

User provides Ollama externally.

```yaml
services:
  feirb:
    image: ghcr.io/mamu7211/feirb-mailclient:latest
    ports: ["8080:8080"]
    depends_on:
      postgres: { condition: service_healthy }
    environment:
      - ConnectionStrings__feribdb=Host=postgres;Database=feirb;Username=feirb;Password=feirb
      - Ollama__Endpoint=<user-provided>

  postgres:
    image: postgres:17
    volumes: ["pgdata:/var/lib/postgresql/data"]
    environment:
      - POSTGRES_DB=feirb
      - POSTGRES_USER=feirb
      - POSTGRES_PASSWORD=feirb
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U feirb"]

volumes:
  pgdata:
```

### Tier 3: Full Stack (`docker-compose.full.yml`)

Zero external dependencies.

```yaml
services:
  feirb:
    image: ghcr.io/mamu7211/feirb-mailclient:latest
    ports: ["8080:8080"]
    depends_on:
      postgres: { condition: service_healthy }
      ollama: { condition: service_healthy }
    environment:
      - ConnectionStrings__feribdb=Host=postgres;Database=feirb;Username=feirb;Password=feirb
      - Ollama__Endpoint=http://ollama:11434

  postgres:
    image: postgres:17
    volumes: ["pgdata:/var/lib/postgresql/data"]
    environment:
      - POSTGRES_DB=feirb
      - POSTGRES_USER=feirb
      - POSTGRES_PASSWORD=feirb
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U feirb"]

  ollama:
    image: ollama/ollama:latest
    volumes: ["ollama:/root/.ollama"]
    environment:
      - OLLAMA_NUM_GPU=0
    healthcheck:
      test: ["CMD-SHELL", "ollama list"]

  ollama-init:
    image: ollama/ollama:latest
    depends_on:
      ollama: { condition: service_healthy }
    entrypoint: ["ollama", "pull", "qwen3:4b"]
    environment:
      - OLLAMA_HOST=http://ollama:11434
    restart: "no"

volumes:
  pgdata:
  ollama:
```

The `ollama-init` service runs once to pull the model, then exits.

## GitHub Actions Workflow

File: `.github/workflows/release.yml`

**Trigger:** Push of tags matching `v*.*.*`

**Steps:**
1. Checkout code
2. Setup .NET 10
3. Install Aspire workload
4. `dotnet build -c Release`
5. `dotnet format --verify-no-changes`
6. `dotnet test -c Release --no-build`
7. Setup Docker Buildx
8. Login to ghcr.io via `GITHUB_TOKEN`
9. Build and push image with `docker/build-push-action`:
   - Platform: `linux/amd64`
   - Tags: `latest` + semver extracted from git tag

The existing `ci.yml` workflow remains unchanged.

## Configuration

All configuration via environment variables:

| Variable | Description | Required |
|---|---|---|
| `ConnectionStrings__feribdb` | PostgreSQL connection string | Yes |
| `Ollama__Endpoint` | Ollama API URL | No (AI features degrade, see #169) |

## Documentation

- `docs/DEPLOYMENT-DOCKER.md` — deployment guide for all three tiers
- `README.md` updated to link to deployment docs

## Dependencies

- **#169**: API must start gracefully without Ollama. Without this, Tier 1 and Tier 2 deployments will fail if Ollama is unreachable at startup.

## Out of Scope

- Multi-arch builds (arm64)
- `.env` file pattern
- Internal config UI for connection settings
- Secrets management
