# Feature 2.1: System Setup

## Goal

Provide a first-launch setup wizard that creates the initial admin account and configures the mail server connection.

## Deliverables

### Guard Logic

- On every request, check if an admin user exists in the database
- If no admin exists: redirect to `/setup`
- If admin exists: `/setup` redirects to `/login`
- `GET /api/setup/status` — returns `{ isComplete: true/false }` (anonymous)

### Data Model

- `SystemSettings` entity for runtime configuration (SMTP host, port, username, password, TLS)
- Stored in database so settings can be changed without redeployment
- EF Core migration

### API

- `GET /api/setup/status` — check if setup is complete (anonymous)
- `POST /api/setup/complete` — create admin user + save SMTP config (anonymous, only works if no admin exists)

### UI — Setup Page (`/setup`)

- Centered layout (same style as auth pages)
- Step 1: Admin account (username, email, password, confirm password)
- Step 2: SMTP configuration (host, port, username, password, TLS toggle, test connection button)
- Step 3: Success message, redirect to login
- Language switcher available

### Notes

- In dev with Aspire, Mailpit runs on `localhost:1025` (SMTP) — setup could pre-fill these values
- SMTP test connection should attempt to connect and report success/failure
- Setup endpoint must be idempotent-safe: reject if an admin already exists

## Acceptance Criteria

- [ ] Setup page appears on first launch (no admin in DB)
- [ ] Admin user created with `IsAdmin = true`
- [ ] SMTP settings stored in database
- [ ] Setup endpoint rejects if admin already exists (409 Conflict)
- [ ] `/setup` redirects to `/login` after setup is complete
- [ ] SMTP test connection works
- [ ] i18n for all UI strings (en, de, fr, it)
- [ ] Integration test: complete setup flow
