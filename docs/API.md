# Feirb — API Documentation

Base URL: `https://localhost:7200`

All endpoints return JSON. Error responses use [RFC 7807 Problem Details](https://datatracker.ietf.org/doc/html/rfc7807).

## Authentication

Currently none — Feirb is designed for NAS-local access. Future versions may add authentication.

## Endpoints

### Mail — `/api/mail`

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/mail` | List messages (query: folder, page, size, search) |
| GET | `/api/mail/{id}` | Get full message |
| POST | `/api/mail` | Send a new message |
| POST | `/api/mail/{id}/reply` | Reply to a message |
| POST | `/api/mail/{id}/forward` | Forward a message |
| DELETE | `/api/mail/{id}` | Delete message |
| PATCH | `/api/mail/{id}/read` | Mark as read/unread |
| PATCH | `/api/mail/{id}/move` | Move to folder |

### Folders — `/api/folders`

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/folders` | List all folders with unread counts |
| POST | `/api/folders` | Create folder |
| DELETE | `/api/folders/{name}` | Delete folder |

### Settings — `/api/settings`

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/settings/accounts` | List configured mail accounts |
| POST | `/api/settings/accounts` | Add mail account |
| PUT | `/api/settings/accounts/{id}` | Update mail account |
| DELETE | `/api/settings/accounts/{id}` | Remove mail account |
| POST | `/api/settings/accounts/{id}/test` | Test account connection |
| GET | `/api/settings/preferences` | Get user preferences |
| PUT | `/api/settings/preferences` | Update user preferences |

### AI — `/api/ai`

| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/ai/summarize` | Summarize a mail message |
| POST | `/api/ai/draft-reply` | Generate a reply draft |
| POST | `/api/ai/categorize` | Categorize messages |

## Error Codes

| Status | Meaning |
|--------|---------|
| 400 | Bad Request — validation error |
| 404 | Not Found — resource doesn't exist |
| 409 | Conflict — e.g., duplicate folder name |
| 429 | Too Many Requests — AI endpoint rate limit |
| 502 | Bad Gateway — mail server connection failed |
| 503 | Service Unavailable — Ollama not ready |
