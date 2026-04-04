---
name: run-tests
description: Run unit and integration tests
user_invocable: true
---

# Run Tests

Execute tests for the Feirb solution. There are three test layers:

## Test Layers

| Layer | Framework | Location | Count | What it covers |
|-------|-----------|----------|-------|----------------|
| Unit / Integration | xUnit + FluentAssertions | `tests/Feirb.Api.Tests/`, `tests/Feirb.Web.Tests/` | ~300 | Endpoints, services, components, localization |
| API (contract) | Bruno | `tests/bruno/` | 82 | HTTP request/response contracts across 13 collections |
| E2E (browser) | Playwright | `tests/playwright/tests/` | 3 specs | Auth flows, admin UI, component rendering |

## Running Tests

### 1. Unit + Integration tests (fast, no infrastructure needed)

```bash
# All .NET tests
dotnet test Feirb.sln

# Single project
dotnet test tests/Feirb.Api.Tests

# Filter by class or method name
dotnet test --filter "ClassificationServiceTests"
dotnet test --filter "Execute_AlreadyRunning_SkipsExecutionAsync"
```

### 2. Full stack tests (Bruno + Playwright in Docker)

Requires Docker or Podman. Builds API image, starts Postgres + GreenMail, runs Bruno API tests and Playwright E2E tests sequentially, then tears everything down.

```bash
tests/run-tests.sh
```

This spins up via `tests/docker-compose.test.yml`:
- **postgres** (seeded with `FEIRB_SEED_DATA`)
- **greenmail** (SMTP/IMAP mock with preloaded emails)
- **api** (published .NET app on port 7273)
- **bruno** (runs `bru run --env docker`)
- **playwright** (runs `npx playwright test` in Chromium)

### 3. Bruno tests only (against running local API)

If the API is already running via Aspire / dev-harness:

```bash
cd tests/bruno
npx @usebruno/cli run --env local
```

The `local` environment points to `https://localhost:7272`.

### 4. Playwright tests only (against running local API)

```bash
cd tests/playwright
npx playwright test
```

Set `BASE_URL` environment variable if the API is not on the default port.

## Reporting Results

- Report number of tests passed, failed, skipped per layer
- If tests fail: show failing test names, error messages, and identify relevant source code
- For Bruno/Playwright failures in Docker: check `tests/run-tests.sh` exit codes and container logs

## Bruno Test Collections

| Collection | Tests | Covers |
|------------|-------|--------|
| 01-auth | 10 | Login, register, refresh, password reset |
| 02-setup | 3 | System initialization, SMTP/IMAP connectivity |
| 03-admin-users | 8 | User CRUD, pagination, filters |
| 04-admin-settings | 2 | System configuration |
| 05-mail | 5 | Message listing, retrieval, authorization |
| 06-settings-mailboxes | 7 | Mailbox CRUD |
| 07-settings-profile | 8 | Profile management |
| 08-admin-jobs | 5 | Job administration |
| 08-settings-labels | 5 | Label CRUD |
| 09-settings-preferences | 4 | User preferences |
| 10-dashboard | 7 | Dashboard data |
| 11-mail-stats | 2 | Email statistics |
| 12-avatars | 9 | Avatar handling |
| 13-settings-classification-rules | 6 | Classification rule CRUD |

## Playwright Specs

| Spec | Suites | Covers |
|------|--------|--------|
| auth.spec.ts | 3 | Setup, login/logout, error handling |
| admin-users.spec.ts | 7 | User table, badges, modals, permissions |
| person-chip.spec.ts | 7 | Component rendering, sizes, interactivity |
