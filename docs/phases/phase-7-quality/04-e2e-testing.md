# Feature 7.4: End-to-End Testing

## Goal

Establish end-to-end testing with Playwright (browser tests) and Bruno (API tests) to validate user journeys and API contracts against a running Aspire stack.

## Infrastructure

- Tests run against a **locally running Aspire AppHost** (started separately, not auto-started by tests)
- Base URL: `https://localhost:7272`
- Not part of `Feirb.sln` or `dotnet test` — run manually via CLI
- No CI/CD integration initially

## Project Structure

```
tests/
  playwright/          # TypeScript Playwright project
  bruno/               # Bruno API collection
```

## Playwright

- **Language:** TypeScript
- **Runner:** `npx playwright test`
- **Mode:** Headless by default
- **Scope:** Full user-journey tests across all pages

## Bruno

- **Runner:** `bru run`
- **Auth:** Single request fetches JWT token, stored in collection variable, reused by all requests
- **Data:** Expects seeded database (via Aspire/DatabaseSeeder)
- **Idempotent:** Collections can run repeatedly against the same instance
- **Coverage:** Happy paths and error cases for all API endpoints

## Deliverables

### Story 1: Playwright Setup + Auth Tests

- Initialize Playwright TypeScript project in `tests/playwright/`
- Configure base URL, headless mode
- Implement setup flow test (first-time admin creation)
- Implement login/logout flow test

### Story 2: Bruno API Collection

- Initialize Bruno collection in `tests/bruno/`
- Auth request with token variable
- Request + assertions for all API endpoint groups:
  - `/api/auth` (register, login, refresh)
  - `/api/setup` (initial setup)
  - `/api/admin` (users, system settings)
  - `/api/mail` (messages, folders)
  - `/api/mailboxes` (CRUD)
  - `/api/profile` (personal info, password)
  - `/api/settings` (SMTP config, LLM settings)
- Happy paths and error cases per endpoint

### Story 3: Playwright User Journey Tests

- Mailbox management (add, edit, delete)
- Mail reading (inbox, message detail)
- Settings pages (profile, language switching)
- Admin panel (user management, LLM settings, system settings)

## Acceptance Criteria

- [ ] `npx playwright test` passes against running Aspire stack
- [ ] `bru run` passes against running Aspire stack
- [ ] Tests are idempotent (repeatable without DB reset)
- [ ] Playwright runs headless by default
