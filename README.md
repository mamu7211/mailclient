# Feirb Mailclient

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![Blazor WASM](https://img.shields.io/badge/Blazor-WebAssembly-blue.svg)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![Aspire](https://img.shields.io/badge/.NET%20Aspire-Orchestrated-green.svg)](https://learn.microsoft.com/dotnet/aspire/)

**A smart mail client for your NAS, powered by AI.**

> **⚠️ Early Development — Not Functional Yet**
>
> Feirb is in active early development and is **not yet functional**. There is no release candidate, no stable build, and features are incomplete or missing entirely. Everything is subject to change. Use at your own risk — or better yet, check back later.

Feirb is a self-hosted, modern web-based mail client designed for NAS systems. It combines full IMAP/SMTP support with AI-powered features like mail summarization, smart reply drafts, and automatic categorization — all running locally on your hardware.

### Why "Feirb"?

Pronounced like "fire-bee", the name is simply "Brief" (German for *letter*) spelled backwards. After hours of searching for a proper name, this was the one that stuck.

## Features

- **Full Mail Support** — Read, compose, reply, and manage emails via IMAP/SMTP using MailKit
- **AI-Powered** — Mail summarization, smart reply drafts, and categorization via Ollama/Qwen3
- **Modern UI** — Responsive Blazor WebAssembly frontend with Bootstrap 5
- **Self-Hosted** — Runs entirely on your NAS, no cloud dependency
- **Observable** — Built-in dashboards, health checks, and distributed tracing via .NET Aspire

## Tech Stack

| Component | Technology |
|-----------|-----------|
| Frontend | Blazor WebAssembly |
| Backend | ASP.NET Core 10 Minimal APIs |
| Orchestration | .NET Aspire |
| Mail | MailKit + MimeKit |
| AI | Ollama (qwen3:4b) via OllamaSharp |
| Database | PostgreSQL via EF Core |
| UI | Bootstrap 5 |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (10.0.100+)
- [Docker](https://www.docker.com/) (for Aspire, Ollama, and GreenMail containers)
- Git

## Quick Start

```bash
git clone git@github.com:mamu7211/feirb-mailclient.git
cd feirb-mailclient
dotnet workload install aspire
dotnet restore Feirb.sln
dotnet run --project src/Feirb.AppHost
```

This starts all services via Aspire:
- **Aspire Dashboard:** https://localhost:18888
- **Blazor Frontend:** https://localhost:7100
- **API Backend:** https://localhost:7200
- **GreenMail API (dev):** http://localhost:8080

> **Note:** On first run, the Ollama qwen3:4b model (~2.6GB) will be downloaded automatically.

### Development Quickstart with Seeded Data

To skip the initial setup wizard and start with a preconfigured admin user and SMTP settings, set the `FEIRB_SEED_DATA` environment variable:

```bash
FEIRB_SEED_DATA=true dotnet run --project src/Feirb.AppHost
```

This seeds the database with:
- **Admin user:** `admin@feirb.local` / `admin@feirb.local` (password)
- **Alice user:** `alice@feirb.local` / `alice@feirb.local` (password)
- **SMTP settings:** GreenMail on `localhost:3025` (no TLS, no auth), from address `noreply@feirb.local`
- **Mailboxes:** One per user, IMAP on `localhost:3143`, SMTP on `localhost:3025`

The seeding is idempotent — it checks whether the data already exists and skips if so.

## Documentation

- **[Architecture](docs/ARCHITECTURE.md)** — System design, project structure, and technical decisions
- **[Developer Setup](docs/SETUP.md)** — Environment setup and development workflow
- **[API Reference](docs/API.md)** — Endpoint documentation (implemented and planned)
- **[Deployment](docs/DEPLOYMENT.md)** — Current deployment options and roadmap
- **[UI/UX Design](docs/DESIGN.md)** — Color tokens, typography, and component styles
- **[Project Plan](docs/PROJECT-PLAN.md)** — Phase roadmap and progress

## Contributing

Feirb is currently in early development and **not accepting external contributions** (pull requests) at this time. The architecture and core features are still taking shape, and managing external PRs would slow things down right now.

**What you _can_ do:**
- **Report bugs** — found something broken? Open an [issue](https://github.com/mamu7211/feirb-mailclient/issues)
- **Request features** — ideas are welcome as issues, even if implementation is a while off

External contributions will be opened up once the project reaches a more stable state. Thanks for your patience and interest!

## License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.
