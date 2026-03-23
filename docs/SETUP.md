# Feirb — Developer Setup Guide

## Prerequisites

| Tool | Version | Purpose |
|------|---------|---------|
| [.NET SDK](https://dotnet.microsoft.com/download) | 10.0.100+ | Build and run the solution |
| [Docker](https://www.docker.com/) | Latest | Aspire containers (PostgreSQL, Ollama, GreenMail) |
| [Git](https://git-scm.com/) | Latest | Version control |

**Recommended IDE (pick one):**
- [Visual Studio 2022](https://visualstudio.microsoft.com/) 17.12+ with ASP.NET workload
- [JetBrains Rider](https://www.jetbrains.com/rider/) 2024.3+
- [VS Code](https://code.visualstudio.com/) with [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit)

## Clone & First Run

```bash
# Clone the repository
git clone git@github.com:mamu7211/feirb-mailclient.git
cd feirb-mailclient

# Install Aspire workload (first time only)
dotnet workload install aspire

# Restore dependencies
dotnet restore Feirb.sln

# Start with database seeding (recommended for development)
./run.sh
```

The `run.sh` script starts Aspire with database seeding enabled by default, creating a preconfigured admin user and SMTP settings so you can skip the setup wizard. Use `./run.sh --no-seeding` to start without seeding.

On first run, Aspire will:
1. Start a PostgreSQL container for the database
2. Start the API and Web projects
3. Pull and start the Ollama Docker container
4. Download the `qwen3:4b` model (~2.6GB — this takes a while on first run)
5. Start GreenMail for development email testing (SMTP + IMAP)

## Development Services

Once running, these services are available:

| Service | URL | Description |
|---------|-----|-------------|
| Aspire Dashboard | https://localhost:18888 | Logs, traces, metrics, health |
| Blazor Frontend | https://localhost:7100 | The mail client UI |
| API Backend | https://localhost:7200 | REST API |
| PostgreSQL | localhost:5432 | Database |
| GreenMail API / OpenAPI UI | http://localhost:8080 | View and manage test emails |
| GreenMail SMTP | localhost:3025 | Send test emails |
| GreenMail IMAP | localhost:3143 | Fetch test emails |

## Helper Scripts

| Script | Purpose |
|--------|---------|
| `./run.sh` | Start Feirb with database seeding (admin + alice users, mailboxes, GreenMail SMTP) |
| `./run.sh --no-seeding` | Start Feirb without seeding |
| `./stop-aspire-containers.sh` | Stop and remove Aspire containers and orphaned volumes |

### Database Seeding

When started via `./run.sh` (or with `FEIRB_SEED_DATA=true`), the following data is seeded:

| Data | Value |
|------|-------|
| Admin email | `admin@feirb.local` |
| Admin password | `admin@feirb.local` |
| Alice email | `alice@feirb.local` |
| Alice password | `alice@feirb.local` |
| System SMTP | `localhost:3025` (GreenMail) |
| SMTP from address | `noreply@feirb.local` |
| TLS / Auth | disabled |
| IMAP host | `localhost:3143` (GreenMail) |
| Mailbox credentials | email address as both username and password |

The seeding is idempotent — it checks whether the data already exists and skips if so.

## Development Workflow

### Branching

| Prefix | Use |
|--------|-----|
| `feature/` | New features |
| `fix/` | Bug fixes |
| `docs/` | Documentation changes |
| `chore/` | Tooling, CI, config |

```bash
git checkout -b feature/mail-list-view
# ... make changes ...
git commit -m "feat(web): add mail list component with pagination"
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific project tests
dotnet test tests/Feirb.Api.Tests

# Run with verbose output
dotnet test --verbosity normal

# Filter by test name
dotnet test --filter "MailService_GetMessages"
```

### Code Formatting

```bash
# Check formatting (CI runs this)
dotnet format --verify-no-changes

# Auto-fix formatting
dotnet format
```

## Ollama Setup

Aspire manages the Ollama container automatically. For faster development, you can pre-pull the model:

```bash
# Optional: pre-pull qwen3:4b model
docker run -d --name ollama -v ollama_data:/root/.ollama -p 11434:11434 ollama/ollama
docker exec ollama ollama pull qwen3:4b
docker stop ollama && docker rm ollama
```

The Aspire configuration uses a persistent volume, so the model is only downloaded once.

## GreenMail (Dev Mail Server)

GreenMail provides SMTP, IMAP, and a REST API in a single container for development. No real emails are sent.

- **SMTP** at localhost:3025 — system and per-mailbox outgoing mail
- **IMAP** at localhost:3143 — mail fetching for inbox sync
- **REST API / OpenAPI UI** at http://localhost:8080 — inspect and manage test emails
- **Preloaded mail** — 30 .eml files (10 per account) are mounted from `seeding/mails/` and loaded on startup
- **Test accounts:** admin@feirb.local, alice@feirb.local, bob@feirb.local (bob has mail but no Feirb account)

## Configuration

### Application Settings

Settings are managed via `appsettings.json` and `appsettings.Development.json` in each project. Aspire injects service URLs and connection strings automatically.

### Sensitive Values

Use .NET User Secrets for local sensitive configuration:

```bash
cd src/Feirb.Api
dotnet user-secrets init
dotnet user-secrets set "Mail:TestPassword" "your-test-password"
```

Never commit passwords or secrets to the repository.

## Troubleshooting

### Docker not running

Aspire requires Docker for Ollama and GreenMail containers. Ensure Docker Desktop is running:

```bash
docker info
```

### Port conflicts

If default ports are in use, check which process occupies them:

```bash
# Linux
ss -tlnp | grep -E '(5432|7100|7200|8025|11434|18888)'
```

Stop conflicting processes or modify `launchSettings.json` in the respective projects.

### Ollama model not found

If the qwen3:4b model fails to download automatically:

```bash
# Check Ollama container logs via Aspire dashboard
# Or manually pull:
docker exec -it <ollama-container-name> ollama pull qwen3:4b
```

### Reset PostgreSQL Database

The PostgreSQL container is managed by Aspire. To reset the database:

```bash
# Stop the application, then remove the Postgres volume
docker volume rm mailclient-postgres-data

# Restart — EF Core migrations will recreate the schema
dotnet run --project src/Feirb.AppHost
```

### Clean Build

```bash
dotnet clean Feirb.sln
dotnet build Feirb.sln
```
