<p align="center">
  <img src="fireb-logo.svg" alt="Feirb Logo" width="200">
</p>

# Feirb

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![Blazor WASM](https://img.shields.io/badge/Blazor-WebAssembly-blue.svg)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![Aspire](https://img.shields.io/badge/.NET%20Aspire-Orchestrated-green.svg)](https://learn.microsoft.com/dotnet/aspire/)

**A smart mail client for your NAS, powered by AI.**

MailClient is a self-hosted, modern web-based mail client designed for NAS systems. It combines full IMAP/SMTP support with AI-powered features like mail summarization, smart reply drafts, and automatic categorization — all running locally on your hardware.

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
git clone git@github.com:mamu7211/mailclient.git
cd mailclient
dotnet workload install aspire
dotnet restore MailClient.sln
dotnet run --project src/MailClient.AppHost
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
│   ├── MailClient.AppHost/          # Aspire orchestration (start here)
│   ├── MailClient.ServiceDefaults/  # Shared service configuration
│   ├── MailClient.Api/              # Backend API
│   ├── MailClient.Web/              # Blazor WASM frontend
│   └── MailClient.Shared/           # Shared DTOs and interfaces
├── tests/
│   ├── MailClient.Api.Tests/
│   └── MailClient.Web.Tests/
├── docs/
│   ├── DESIGN.md                    # Architecture & design decisions
│   ├── SETUP.md                     # Developer setup guide
│   └── API.md                       # API documentation
├── CLAUDE.md                        # Claude Code project instructions
└── MailClient.sln
```

## Documentation

- **[Design Document](docs/DESIGN.md)** — Architecture, data model, and technical decisions
- **[Setup Guide](docs/SETUP.md)** — Developer onboarding and environment setup
- **[API Documentation](docs/API.md)** — Endpoint reference and examples

## Contributing

1. Create a feature branch: `git checkout -b feature/my-feature`
2. Follow [Conventional Commits](https://www.conventionalcommits.org/): `feat:`, `fix:`, `docs:`, etc.
3. Ensure tests pass: `dotnet test`
4. Ensure formatting: `dotnet format --verify-no-changes`
5. Open a Pull Request against `main`

## License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.
