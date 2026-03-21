# Feirb — Project Plan

## Overview

Feirb is built in vertical feature slices. Each phase delivers a working increment: data model, API, UI, and tests together. Phases build on each other — the order matters.

## Phase 0: Foundation ✅

*Completed — merged on `main`.*

- Aspire skeleton with PostgreSQL, Ollama (qwen3:4b), Mailpit
- Minimal API + Blazor WASM + Shared projects
- CI pipeline, documentation, Claude Code skills

## Phase 1: Auth & User Management

Multi-user authentication with JWT-based API security.

| Layer | Deliverable |
|-------|-------------|
| **Data Model** | `User` entity (Id, Username, Email, PasswordHash, CreatedAt) |
| **API** | `POST /api/auth/register` — create user account |
| | `POST /api/auth/login` — authenticate, return JWT |
| | `POST /api/auth/refresh` — refresh token |
| | JWT bearer authentication middleware on all `/api/*` endpoints |
| **Security** | Password hashing (BCrypt or ASP.NET Identity) |
| | ASP.NET Data Protection API setup (key persistence in volume) |
| **UI** | Login page, Registration page |
| | Token storage (localStorage), `AuthenticationStateProvider` |
| | Redirect to login when unauthenticated |
| **API Docs** | OpenAPI spec via `Microsoft.AspNetCore.OpenApi`, Scalar UI (`/scalar`) |
| **Tests** | Auth endpoint tests, JWT validation tests |
| **EF Core** | Initial migration with User table |

## Phase 2: Mail Account Setup

Users configure their IMAP/SMTP mail accounts.

| Layer | Deliverable |
|-------|-------------|
| **Data Model** | `MailAccount` entity (per user, IMAP/SMTP host/port/credentials) |
| **Security** | IMAP/SMTP passwords encrypted via Data Protection API |
| **API** | `GET/POST/PUT/DELETE /api/settings/accounts` — CRUD |
| | `POST /api/settings/accounts/{id}/test` — connection test |
| **UI** | Account setup wizard (IMAP + SMTP config) |
| | Connection test with success/error feedback |
| | Account list in settings page |
| **Tests** | Account CRUD tests, encryption round-trip tests |
| **EF Core** | Migration adding MailAccount table |

## Phase 3: Dashboard & Navigation Shell

The application layout and navigation structure.

| Layer | Deliverable |
|-------|-------------|
| **UI** | Main layout: sidebar (folder tree), top bar, content area |
| | Dashboard page: account overview, unread counts per folder |
| | Responsive Bootstrap 5 layout (mobile-friendly) |
| | Navigation between dashboard, inbox, settings |
| **API** | `GET /api/folders` — folder list with unread counts |
| **Tests** | Layout rendering tests |

## Phase 4: Mail Fetching & Inbox

Background synchronization and mail reading.

| Layer | Deliverable |
|-------|-------------|
| **Data Model** | `CachedMessage` entity (per account, cached mail metadata + preview) |
| **Background** | IMAP sync service (Hosted Service or Aspire Worker) |
| | Periodic sync with configurable interval |
| | Initial full sync + incremental updates |
| **API** | `GET /api/mail` — list messages (folder, pagination, search) |
| | `GET /api/mail/{id}` — full message with body and attachments |
| | `PATCH /api/mail/{id}/read` — mark read/unread |
| | `PATCH /api/mail/{id}/move` — move to folder |
| | `DELETE /api/mail/{id}` — delete message |
| **Search** | PostgreSQL full-text search on subject, from, body preview |
| **UI** | Inbox list with pagination and search |
| | Mail viewer (HTML body sanitized, plain text fallback) |
| | Attachment list with download |
| **Tests** | Sync service tests, mail endpoint tests |
| **EF Core** | Migration adding CachedMessage table + FTS index |

## Phase 5: Mail Compose & Send

Writing and sending emails.

| Layer | Deliverable |
|-------|-------------|
| **API** | `POST /api/mail` — send new message |
| | `POST /api/mail/{id}/reply` — reply |
| | `POST /api/mail/{id}/forward` — forward |
| **SMTP** | MailKit SMTP sending (real servers + Mailpit in dev) |
| **UI** | Compose dialog (to, cc, bcc, subject, body) |
| | Reply / Forward actions from mail viewer |
| | Attachment upload |
| **Tests** | Send endpoint tests (against Mailpit) |

## Phase 6: AI Features

LLM-powered mail intelligence.

| Layer | Deliverable |
|-------|-------------|
| **API** | `POST /api/ai/summarize` — summarize a message |
| | `POST /api/ai/draft-reply` — generate reply draft |
| | `POST /api/ai/categorize` — categorize messages |
| | Rate limiting on AI endpoints |
| **Integration** | OllamaSharp client via DI, connected to qwen3:4b |
| **UI** | AI sidebar panel (summary, draft reply, categories) |
| | Toggle AI features on/off in user preferences |
| **Tests** | AI endpoint tests (mock Ollama responses) |

## Phase 7: Quality & Compliance

Cross-cutting quality features applied retroactively to all existing UI.

| Layer | Deliverable |
|-------|-------------|
| **i18n** | Multi-language support: `en_US` (default), `de_DE`, `fr_FR`, `it_IT` |
| | .NET resource files (`.resx`) + `IStringLocalizer<T>` in Blazor |
| | Language switcher component, browser `Accept-Language` detection |
| | Localized API error messages |
| **Accessibility** | WCAG 2.2 Level AA conformance across all UI |
| | Color contrast, keyboard navigation, focus indicators, ARIA |
| | Skip-to-content link, semantic HTML, screen reader compatibility |
| | Automated audit (axe-core) + manual testing |
| **Documentation** | `USER-DOCUMENTATION.md` — end-user guide |
| | Installation, account setup, mail usage, AI features, administration |
| | Screenshots and troubleshooting |

## Future (not scheduled)

- Google Stitch / MCP integration
- Stitch2Blazor skill for component generation
- Folder management UI (create, rename, delete)
- Mail rules and filters
- Dark mode
- User roles and permissions

## Cross-Cutting Concerns

These are not separate phases — they grow with each phase:

- **Tests:** Every phase delivers unit + integration tests
- **EF Core Migrations:** Schema evolves with each phase
- **CI:** Already running, extended as needed
- **Error Handling:** Consistent ProblemDetails (RFC 7807) across all endpoints
- **API Documentation:** OpenAPI spec auto-generated from endpoints, Scalar UI for interactive exploration in dev mode. External tools (Bruno, Postman) can import the OpenAPI spec.
