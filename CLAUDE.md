# Feirb

A smart mail client for your NAS, powered by AI.

## Project Language

English is the project language. All code, comments, commit messages, documentation, issues, and PR descriptions must be written in English.

## Project Overview

Feirb is a self-hosted mail client designed for NAS systems. It provides a modern web-based interface for managing email with integrated AI features powered by Ollama/Qwen3 for mail summarization, smart reply drafts, and automatic categorization.

## Tech Stack

- **Runtime:** .NET 10 / C# 14
- **Orchestration:** .NET Aspire
- **Frontend:** Blazor WebAssembly
- **Backend:** ASP.NET Core Minimal APIs
- **Mail:** MailKit (IMAP/SMTP) + MimeKit (MIME handling)
- **AI:** Ollama with qwen3:4b model via OllamaSharp
- **Database:** PostgreSQL via Entity Framework Core (Aspire-managed container)
- **UI Framework:** Bootstrap 5
- **Testing:** xUnit + FluentAssertions

## Solution Structure

```
src/
  Feirb.AppHost/          # Aspire orchestration (entry point)
  Feirb.ServiceDefaults/  # Shared service config (OpenTelemetry, health, resilience)
  Feirb.Api/              # ASP.NET Minimal API backend
  Feirb.Web/              # Blazor WASM frontend
  Feirb.Shared/           # DTOs, interfaces, enums
tests/
  Feirb.Api.Tests/        # API unit & integration tests
  Feirb.Web.Tests/        # Frontend tests
```

## Prerequisites

- .NET 10 SDK (`dotnet --version`)
- Podman or Docker (container runtime for Aspire-managed services)
- `psql` client (for direct DB queries via dev-harness)
- Python 3 (for JSON parsing and SMTP in dev-harness scripts)
- `gh` CLI (for GitHub issue/PR management)

## First-Time Setup

```bash
# 1. Build
dotnet build Feirb.sln

# 2. Start the app with dev-harness (seeds data, waits for health)
.claude/skills/dev-harness/start.sh --seeding

# 3. Authenticate
.claude/skills/dev-harness/login.sh
```

Tool permissions are configured in `.claude/settings.json` (checked in) so dev-harness scripts work without manual approval.

On first run, the Ollama model (`qwen3:0.6b` for dev) downloads automatically (~400MB). Models persist in `.ollama-data/` across restarts.

## Build & Run

```bash
# Build everything
dotnet build Feirb.sln

# Run via Aspire (starts all services)
dotnet run --project src/Feirb.AppHost

# Run with seed data (creates users, labels, rules, enables jobs)
FEIRB_SEED_DATA=true dotnet run --project src/Feirb.AppHost

# Run tests
dotnet test

# Check formatting
dotnet format --verify-no-changes

# Apply formatting
dotnet format
```

## Dev Harness (`/dev-harness`)

Shell scripts in `.claude/skills/dev-harness/` for autonomous app interaction:

| Script | Purpose |
|--------|---------|
| `start.sh [--seeding]` | Start Aspire (bare by default), `--seeding` to seed test data |
| `stop.sh` / `cleanup.sh` | Stop Aspire / full reset (removes containers + volumes) |
| `login.sh [user] [pass]` | Authenticate (default: admin), store access + refresh tokens |
| `status.sh` | Check all services (API, GreenMail, Ollama, token) |
| `check.sh /api/...` | GET any API endpoint with stored token |
| `query.sh 'SELECT ...'` | Run SQL against PostgreSQL via local psql |
| `trigger-job.sh <type>` | Trigger a background job (e.g., `classification`, `imap-sync`) |
| `send-mail.sh [from] [to] [subject] [body]` | Send test email via SMTP |
| `logs.sh [type]` | Show recent job execution history from DB |

## Development Services (via Aspire)

| Service | URL / Port |
|---------|-----------|
| API (HTTPS) | https://localhost:7272 |
| Aspire Dashboard | https://localhost:18888 |
| PostgreSQL | localhost:15432 |
| GreenMail SMTP | localhost:3025 |
| GreenMail IMAP | localhost:3143 |
| GreenMail REST/OpenAPI | http://localhost:8080 |
| Ollama | http://localhost:11434 |

## Seeded Dev Data (`FEIRB_SEED_DATA=true`)

- **Users:** `admin` / `admin@feirb.local` / password: `admin@feirb.local` (admin), `alice` / `alice@feirb.local` / password: `alice@feirb.local` (user)
- **Mailboxes:** One per user, connected to GreenMail
- **Labels:** Newsletter, Work, Personal (admin)
- **Classification rule:** Basic newsletter/work/personal rule (admin)
- **Jobs:** All enabled with 1-minute cron intervals
- **Emails:** 10 preloaded per account in GreenMail (synced on first IMAP sync run)

## Conventions

### Code Style

- C# 14 / .NET 10, nullable reference types enabled everywhere
- File-scoped namespaces
- Primary constructors preferred
- Minimal APIs — no controllers
- Record types for all DTOs in `Feirb.Shared`
- All async methods suffixed with `Async`
- Expression-bodied members for single-line implementations
- **Icons:** Bootstrap Icons (`<i class="bi bi-icon-name"></i>`). Do NOT use Google Material Symbols
- See `.editorconfig` for full formatting rules

### Testing

- **Browser testing:** Playwright MCP for interactive UI verification (`/test-ui` skill)
- xUnit as test framework, FluentAssertions for assertions
- Test naming: `MethodName_Scenario_ExpectedResult`
- Unit tests mirror `src/` project structure in `tests/`
- Integration tests use Aspire's `DistributedApplicationTestingBuilder`

### Git

- Conventional Commits: `feat:`, `fix:`, `docs:`, `chore:`, `test:`, `refactor:`
- Scopes: `(api)`, `(web)`, `(apphost)`, `(shared)`, `(design)`
- Branch naming: `feature/<issue#>-slug`, `fix/<issue#>-slug`, `docs/<issue#>-slug`, `chore/<issue#>-slug`
- **Before starting work on an issue:** check the current branch (`git branch --show-current`). If it doesn't match the issue, create or switch to the correct branch from `main` (e.g., `feature/200-recipient-autocomplete` for issue #200). Never commit work for one issue onto another issue's branch.
- Target branch: `main`

### Issues

- See [`docs/ISSUES.md`](docs/ISSUES.md) for issue templates, labels, and guidelines
- Always apply at least one type label (`bug`, `feature`, `enhancement`, etc.)
- Feature specs live inline in the issue body — no separate spec docs

### API Design

- Minimal API endpoints grouped by feature in separate static classes
- Endpoint groups: `/api/mail`, `/api/folders`, `/api/settings`, `/api/ai`
- Return `Results<T>` types for explicit HTTP response modeling
- All request/response DTOs in `Feirb.Shared`
- **Data belongs on the server, not in the client.** If the frontend needs specific data, create a dedicated API endpoint that returns exactly that data. Never fetch a broad list and filter client-side — it breaks at pagination boundaries, wastes bandwidth, and couples the UI to assumptions about data volume.
- **Every endpoint must enforce authorization.** No endpoint may return data the requesting user is not authorized to see. Prefer dedicated endpoints for admin and user contexts when the requirements differ. When a shared endpoint avoids duplication, it must filter server-side based on the authenticated user — admin sees all, regular users see only their own data. Authorization is never optional or deferred.

## LLM Integration

- **Abstraction:** `Microsoft.Extensions.AI` (`IChatClient` interface)
- **Default Provider:** OllamaSharp (implements `IChatClient`), registered via DI
- **Model:** `qwen3:4b` (configurable via Aspire/appsettings)
- **Aspire:** `CommunityToolkit.Aspire.Hosting.Ollama` for container management
- **Features:**
  - Automatic mail categorization (labels + classification rules + LLM pipeline)
  - Mail summarization
  - Smart reply draft generation

## Internationalization (i18n)

- **Supported locales:** `en-US` (default/fallback), `de-DE`, `fr-FR`, `it-IT`
- **UI strings:** `src/Feirb.Web/Resources/SharedResources.resx` + locale variants
- **API strings:** `src/Feirb.Api/Resources/ApiMessages.resx` + locale variants
- **Usage:** `@inject IStringLocalizer<SharedResources> L` in Blazor components, `IStringLocalizer<ApiMessages>` in API endpoints
- **Culture detection:** localStorage (`BlazorCulture`) → browser culture → `en-US` fallback
- **Adding a new locale:** Create `.{locale}.resx` files in both `Resources/` directories, add culture code to `supportedCultures` in `Feirb.Api/Program.cs`, add option to `LanguageSwitcher.razor`
- **Adding a new string:** Add key to all `.resx` files (default + all locales), use `L["Key"]` in components or `localizer["Key"]` in API endpoints

## UI/UX Work (`/implement-ui`)

**You MUST invoke the `/implement-ui` skill before touching any Razor file for UI/UX work** — new pages, new components, layout changes, or non-trivial styling. The skill enforces the shared component library, documents hard rules (Button/Icon/Card/CircularButton usage), and captures project-wide patterns like the toolbar actions convention on edit/detail pages.

Exceptions (inline edits OK, no skill needed):
- Fixing a typo, broken `@onclick`, or a localization key
- Wiring an existing page to a new API endpoint with no visual changes
- Bug fixes that don't change the DOM structure

When in doubt, invoke the skill.

## Browser Testing (Playwright MCP)

Use `/test-ui <path> <what to test>` to verify UI features via browser. The skill:
- Checks app health and container freshness before testing
- Authenticates via JWT injection into browser localStorage
- Uses Playwright MCP tools for navigation, form filling, clicking, screenshots, and assertions
- Verifies backend state via dev-harness scripts (API, DB, logs)

MCP server configured in `.mcp.json` with `--ignore-https-errors` for self-signed dev certs.

## Future Plans

- Google Stitch integration via MCP (Model Context Protocol)
- Stitch2Blazor skill for generating Blazor components from designs
