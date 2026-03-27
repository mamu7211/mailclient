# Feirb — API Reference

Base URL: `https://localhost:7200`

All endpoints return JSON. Error responses use [RFC 7807 Problem Details](https://datatracker.ietf.org/doc/html/rfc7807).

## Authentication

JWT Bearer authentication. Obtain a token via `/api/auth/login`, include it as `Authorization: Bearer <token>` on subsequent requests.

## Implemented Endpoints

### Auth — `/api/auth`

| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/auth/register` | Create a new user account |
| POST | `/api/auth/login` | Authenticate, returns JWT + refresh token |
| POST | `/api/auth/refresh` | Refresh an expired JWT |
| POST | `/api/auth/request-reset` | Request a password reset email |
| GET | `/api/auth/validate-reset-token/{token}` | Validate a password reset token |
| POST | `/api/auth/reset-password` | Complete password reset with new password |

### Setup — `/api/setup`

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/setup/status` | Check if initial setup is complete |
| POST | `/api/setup/complete` | Complete initial setup (admin account + SMTP) |
| POST | `/api/setup/test-smtp` | Test SMTP connection with provided settings |

### Admin — `/api/admin` (requires admin role)

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/admin/users` | List all users |
| POST | `/api/admin/users` | Create a new user |
| PUT | `/api/admin/users/{id}` | Update user details |
| DELETE | `/api/admin/users/{id}` | Delete a user |
| POST | `/api/admin/users/{id}/reset-password` | Admin-initiated password reset |

## Planned Endpoints

> These endpoints are part of the project roadmap but **not yet implemented**.

### Mail — `/api/mail`

| Method | Path | Description | Phase |
|--------|------|-------------|-------|
| GET | `/api/mail` | List messages (query: folder, page, size, search) | 4 |
| GET | `/api/mail/{id}` | Get full message | 4 |
| POST | `/api/mail` | Send a new message | 5 |
| POST | `/api/mail/{id}/reply` | Reply to a message | 5 |
| POST | `/api/mail/{id}/forward` | Forward a message | 5 |
| DELETE | `/api/mail/{id}` | Delete message | 4 |
| PATCH | `/api/mail/{id}/read` | Mark as read/unread | 4 |
| PATCH | `/api/mail/{id}/move` | Move to folder | 4 |

### Folders — `/api/folders`

| Method | Path | Description | Phase |
|--------|------|-------------|-------|
| GET | `/api/folders` | List all folders with unread counts | 3 |
| POST | `/api/folders` | Create folder | Future |
| DELETE | `/api/folders/{name}` | Delete folder | Future |

### Settings — `/api/settings`

| Method | Path | Description | Phase |
|--------|------|-------------|-------|
| GET | `/api/settings/accounts` | List configured mail accounts | 2 |
| POST | `/api/settings/accounts` | Add mail account | 2 |
| PUT | `/api/settings/accounts/{id}` | Update mail account | 2 |
| DELETE | `/api/settings/accounts/{id}` | Remove mail account | 2 |
| POST | `/api/settings/accounts/{id}/test` | Test account connection | 2 |
| GET | `/api/settings/preferences` | Get user preferences | Future |
| PUT | `/api/settings/preferences` | Update user preferences | Future |

### Labels — `/api/labels` (requires auth)

| Method | Path | Description | Issue |
|--------|------|-------------|-------|
| GET | `/api/labels` | List all labels for current user | #123 |
| POST | `/api/labels` | Create a label | #123 |
| PUT | `/api/labels/{id}` | Update a label | #123 |
| DELETE | `/api/labels/{id}` | Delete a label | #123 |

### Rules — `/api/rules` (requires auth)

| Method | Path | Description | Issue |
|--------|------|-------------|-------|
| GET | `/api/rules` | List all classification rules for current user | #124 |
| POST | `/api/rules` | Create a classification rule | #124 |
| PUT | `/api/rules/{id}` | Update a classification rule | #124 |
| DELETE | `/api/rules/{id}` | Delete a classification rule | #124 |

### Admin Jobs — `/api/admin/jobs` (requires admin role)

| Method | Path | Description | Issue |
|--------|------|-------------|-------|
| GET | `/api/admin/jobs` | List all job settings | #125 |
| PUT | `/api/admin/jobs/{id}` | Update job schedule and enabled state | #125 |

### AI — `/api/ai`

| Method | Path | Description | Phase |
|--------|------|-------------|-------|
| POST | `/api/ai/summarize` | Summarize a mail message | 6 |
| POST | `/api/ai/draft-reply` | Generate a reply draft | 6 |

## Error Codes

| Status | Meaning |
|--------|---------|
| 400 | Bad Request — validation error |
| 401 | Unauthorized — missing or invalid JWT |
| 403 | Forbidden — insufficient permissions (e.g., non-admin) |
| 404 | Not Found — resource doesn't exist |
| 409 | Conflict — e.g., duplicate username or email |
| 429 | Too Many Requests — AI endpoint rate limit (planned) |
| 502 | Bad Gateway — mail server connection failed (planned) |
| 503 | Service Unavailable — Ollama not ready (planned) |
