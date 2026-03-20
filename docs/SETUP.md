# MailClient — Developer Setup Guide

## Prerequisites

| Tool | Version | Purpose |
|------|---------|---------|
| [.NET SDK](https://dotnet.microsoft.com/download) | 10.0.100+ | Build and run the solution |
| [Docker](https://www.docker.com/) | Latest | Aspire containers (Ollama, Mailpit) |
| [Git](https://git-scm.com/) | Latest | Version control |

**Recommended IDE (pick one):**
- [Visual Studio 2022](https://visualstudio.microsoft.com/) 17.12+ with ASP.NET workload
- [JetBrains Rider](https://www.jetbrains.com/rider/) 2024.3+
- [VS Code](https://code.visualstudio.com/) with [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit)

## Clone & First Run

```bash
# Clone the repository
git clone git@github.com:mamu7211/mailclient.git
cd mailclient

# Restore dependencies
dotnet restore MailClient.sln

# Run via Aspire (starts all services)
dotnet run --project src/MailClient.AppHost
```

On first run, Aspire will:
1. Start the API and Web projects
2. Pull and start the Ollama Docker container
3. Download the `mistral` model (~4GB — this takes a while on first run)
4. Start Mailpit for development email testing

## Development Services

Once running, these services are available:

| Service | URL | Description |
|---------|-----|-------------|
| Aspire Dashboard | https://localhost:18888 | Logs, traces, metrics, health |
| Blazor Frontend | https://localhost:7100 | The mail client UI |
| API Backend | https://localhost:7200 | REST API |
| Mailpit Web UI | http://localhost:8025 | View test emails |
| Mailpit SMTP | localhost:1025 | Send test emails |

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
dotnet test tests/MailClient.Api.Tests

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
# Optional: pre-pull mistral model
docker run -d --name ollama -v ollama_data:/root/.ollama -p 11434:11434 ollama/ollama
docker exec ollama ollama pull mistral
docker stop ollama && docker rm ollama
```

The Aspire configuration uses a persistent volume, so the model is only downloaded once.

## Mailpit (Dev Mail Server)

Mailpit acts as a catch-all SMTP server for development. No real emails are sent.

- **Send test emails** to any address — they all arrive in Mailpit
- **Web UI** at http://localhost:8025 shows all captured emails
- **SMTP** at localhost:1025 — configure test accounts to use this

## Configuration

### Application Settings

Settings are managed via `appsettings.json` and `appsettings.Development.json` in each project. Aspire injects service URLs and connection strings automatically.

### Sensitive Values

Use .NET User Secrets for local sensitive configuration:

```bash
cd src/MailClient.Api
dotnet user-secrets init
dotnet user-secrets set "Mail:TestPassword" "your-test-password"
```

Never commit passwords or secrets to the repository.

## Troubleshooting

### Docker not running

Aspire requires Docker for Ollama and Mailpit containers. Ensure Docker Desktop is running:

```bash
docker info
```

### Port conflicts

If default ports are in use, check which process occupies them:

```bash
# Linux
ss -tlnp | grep -E '(7100|7200|8025|11434|18888)'
```

Stop conflicting processes or modify `launchSettings.json` in the respective projects.

### Ollama model not found

If the mistral model fails to download automatically:

```bash
# Check Ollama container logs via Aspire dashboard
# Or manually pull:
docker exec -it <ollama-container-name> ollama pull mistral
```

### Reset SQLite Database

Delete the database file and restart — EF Core migrations will recreate it:

```bash
rm src/MailClient.Api/mailclient.db
dotnet run --project src/MailClient.AppHost
```

### Clean Build

```bash
dotnet clean MailClient.sln
dotnet build MailClient.sln
```
