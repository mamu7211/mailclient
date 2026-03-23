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

## Build & Run

```bash
# Build everything
dotnet build Feirb.sln

# Run via Aspire (starts all services)
dotnet run --project src/Feirb.AppHost

# Run tests
dotnet test

# Check formatting
dotnet format --verify-no-changes

# Apply formatting
dotnet format
```

## Development Services (via Aspire)

- **Aspire Dashboard:** https://localhost:18888
- **PostgreSQL:** Managed by Aspire, localhost:5432
- **GreenMail (dev mail server):** SMTP localhost:3025, IMAP localhost:3143, REST API / OpenAPI UI http://localhost:8080
- **Ollama:** Managed by Aspire, model `qwen3:4b` pulled automatically (~2.6GB on first run)

## Conventions

### Code Style

- C# 14 / .NET 10, nullable reference types enabled everywhere
- File-scoped namespaces
- Primary constructors preferred
- Minimal APIs — no controllers
- Record types for all DTOs in `Feirb.Shared`
- All async methods suffixed with `Async`
- Expression-bodied members for single-line implementations
- See `.editorconfig` for full formatting rules

### Testing

- xUnit as test framework, FluentAssertions for assertions
- Test naming: `MethodName_Scenario_ExpectedResult`
- Unit tests mirror `src/` project structure in `tests/`
- Integration tests use Aspire's `DistributedApplicationTestingBuilder`

### Git

- Conventional Commits: `feat:`, `fix:`, `docs:`, `chore:`, `test:`, `refactor:`
- Scopes: `(api)`, `(web)`, `(apphost)`, `(shared)`, `(design)`
- Branch naming: `feature/`, `fix/`, `docs/`, `chore/`
- Target branch: `main`

### API Design

- Minimal API endpoints grouped by feature in separate static classes
- Endpoint groups: `/api/mail`, `/api/folders`, `/api/settings`, `/api/ai`
- Return `Results<T>` types for explicit HTTP response modeling
- All request/response DTOs in `Feirb.Shared`

## LLM Integration

- **Client:** OllamaSharp, registered via DI
- **Model:** `qwen3:4b` (configurable via Aspire/appsettings)
- **Aspire:** `CommunityToolkit.Aspire.Hosting.Ollama` for container management
- **Features:**
  - Mail summarization
  - Smart reply draft generation
  - Automatic mail categorization

## Internationalization (i18n)

- **Supported locales:** `en-US` (default/fallback), `de-DE`, `fr-FR`, `it-IT`
- **UI strings:** `src/Feirb.Web/Resources/SharedResources.resx` + locale variants
- **API strings:** `src/Feirb.Api/Resources/ApiMessages.resx` + locale variants
- **Usage:** `@inject IStringLocalizer<SharedResources> L` in Blazor components, `IStringLocalizer<ApiMessages>` in API endpoints
- **Culture detection:** localStorage (`BlazorCulture`) → browser culture → `en-US` fallback
- **Adding a new locale:** Create `.{locale}.resx` files in both `Resources/` directories, add culture code to `supportedCultures` in `Feirb.Api/Program.cs`, add option to `LanguageSwitcher.razor`
- **Adding a new string:** Add key to all `.resx` files (default + all locales), use `L["Key"]` in components or `localizer["Key"]` in API endpoints

## Future Plans

- Google Stitch integration via MCP (Model Context Protocol)
- Stitch2Blazor skill for generating Blazor components from designs
