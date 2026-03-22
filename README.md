<p align="center">
  <img src="feirb-logo.svg" alt="Feirb Logo" width="200">
</p>

# Feirb

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![Blazor WASM](https://img.shields.io/badge/Blazor-WebAssembly-blue.svg)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![Aspire](https://img.shields.io/badge/.NET%20Aspire-Orchestrated-green.svg)](https://learn.microsoft.com/dotnet/aspire/)

**A smart mail client for your NAS, powered by AI.**

> **⚠️ Early Development — Not Functional Yet**
>
> Feirb is in active early development and is **not yet functional**. There is no release candidate, no stable build, and features are incomplete or missing entirely. Everything is subject to change. Use at your own risk — or better yet, check back later.

Feirb is a self-hosted, modern web-based mail client designed for NAS systems. It combines full IMAP/SMTP support with AI-powered features like mail summarization, smart reply drafts, and automatic categorization — all running locally on your hardware.

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
- [Docker](https://www.docker.com/) (for Aspire, Ollama, and Mailpit containers)
- Git

## Quick Start

```bash
git clone git@github.com:mamu7211/fireb-mailclient.git
cd mailclient
dotnet workload install aspire
dotnet restore Feirb.sln
dotnet run --project src/Feirb.AppHost
```

This starts all services via Aspire:
- **Aspire Dashboard:** https://localhost:18888
- **Blazor Frontend:** https://localhost:7100
- **API Backend:** https://localhost:7200
- **Mailpit (dev):** http://localhost:8025

> **Note:** On first run, the Ollama qwen3:4b model (~2.6GB) will be downloaded automatically.

## Project Structure

```
mailclient/
├── src/
│   ├── Feirb.AppHost/          # Aspire orchestration (start here)
│   ├── Feirb.ServiceDefaults/  # Shared service configuration
│   ├── Feirb.Api/              # Backend API
│   ├── Feirb.Web/              # Blazor WASM frontend
│   └── Feirb.Shared/           # Shared DTOs and interfaces
├── tests/
│   ├── Feirb.Api.Tests/
│   └── Feirb.Web.Tests/
├── docs/
│   ├── ARCHITECTURE.md                    # Architecture & design decisions
│   ├── SETUP.md                     # Developer setup guide
│   └── API.md                       # API documentation
├── CLAUDE.md                        # Claude Code project instructions
└── Feirb.sln
```

## Documentation

- **[Design Document](docs/ARCHITECTURE.md)** — Architecture, data model, and technical decisions
- **[Setup Guide](docs/SETUP.md)** — Developer onboarding and environment setup
- **[API Documentation](docs/API.md)** — Endpoint reference and examples

## Contributing

Feirb is currently in early development and **not accepting external contributions** (pull requests) at this time. The architecture and core features are still taking shape, and managing external PRs would slow things down right now.

**What you _can_ do:**
- **Report bugs** — found something broken? Open an [issue](https://github.com/mamu7211/fireb-mailclient/issues)
- **Request features** — ideas are welcome as issues, even if implementation is a while off

External contributions will be opened up once the project reaches a more stable state. Thanks for your patience and interest!

## License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.
