# MailClient — API Documentation

Base URL: `https://localhost:7200`

All endpoints return JSON. Error responses use [RFC 7807 Problem Details](https://datatracker.ietf.org/doc/html/rfc7807).

## Authentication

Currently none — MailClient is designed for NAS-local access. Future versions may add Basic Auth or API key authentication.

---

## Mail Endpoints

### List Messages

```
GET /api/mail?accountId={id}&folder={name}&page={n}&size={n}&search={query}
```

**Query Parameters:**

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| accountId | int | required | Mail account ID |
| folder | string | INBOX | Folder name |
| page | int | 1 | Page number (1-based) |
| size | int | 25 | Page size (max 100) |
| search | string? | null | Search query (subject, from, body) |

**Response:** `200 OK`

```json
{
  "items": [
    {
      "id": "msg-abc123",
      "subject": "Meeting Tomorrow",
      "from": { "name": "Alice", "address": "alice@example.com" },
      "to": [{ "name": "Martin", "address": "martin@example.com" }],
      "date": "2026-03-20T10:30:00Z",
      "bodyPreview": "Hi Martin, just confirming our meeting...",
      "isRead": false,
      "hasAttachments": true
    }
  ],
  "totalCount": 142,
  "page": 1,
  "pageSize": 25
}
```

### Get Message

```
GET /api/mail/{id}?accountId={id}
```

**Response:** `200 OK`

```json
{
  "id": "msg-abc123",
  "messageId": "<abc123@example.com>",
  "subject": "Meeting Tomorrow",
  "from": { "name": "Alice", "address": "alice@example.com" },
  "to": [{ "name": "Martin", "address": "martin@example.com" }],
  "cc": [],
  "date": "2026-03-20T10:30:00Z",
  "bodyHtml": "<p>Hi Martin, just confirming our meeting...</p>",
  "bodyText": "Hi Martin, just confirming our meeting...",
  "isRead": true,
  "attachments": [
    {
      "filename": "agenda.pdf",
      "contentType": "application/pdf",
      "size": 45200
    }
  ]
}
```

### Send Message

```
POST /api/mail
```

**Request Body:**

```json
{
  "accountId": 1,
  "to": [{ "name": "Alice", "address": "alice@example.com" }],
  "cc": [],
  "bcc": [],
  "subject": "Re: Meeting Tomorrow",
  "bodyHtml": "<p>Confirmed!</p>",
  "inReplyTo": "<abc123@example.com>",
  "attachments": []
}
```

**Response:** `201 Created`

### Delete Message

```
DELETE /api/mail/{id}?accountId={id}
```

**Response:** `204 No Content`

### Mark Read/Unread

```
PATCH /api/mail/{id}/read?accountId={id}
```

**Request Body:**

```json
{ "isRead": true }
```

**Response:** `204 No Content`

### Move Message

```
PATCH /api/mail/{id}/move?accountId={id}
```

**Request Body:**

```json
{ "targetFolder": "Archive" }
```

**Response:** `204 No Content`

---

## Folder Endpoints

### List Folders

```
GET /api/folders?accountId={id}
```

**Response:** `200 OK`

```json
[
  {
    "name": "INBOX",
    "displayName": "Inbox",
    "unreadCount": 5,
    "totalCount": 142,
    "children": []
  },
  {
    "name": "Sent",
    "displayName": "Sent",
    "unreadCount": 0,
    "totalCount": 89,
    "children": []
  }
]
```

### Create Folder

```
POST /api/folders?accountId={id}
```

**Request Body:**

```json
{ "name": "Projects", "parentFolder": null }
```

**Response:** `201 Created`

### Delete Folder

```
DELETE /api/folders/{name}?accountId={id}
```

**Response:** `204 No Content`

---

## Settings Endpoints

### List Accounts

```
GET /api/settings/accounts
```

**Response:** `200 OK`

```json
[
  {
    "id": 1,
    "displayName": "Personal",
    "emailAddress": "martin@example.com",
    "imapHost": "imap.example.com",
    "imapPort": 993,
    "smtpHost": "smtp.example.com",
    "smtpPort": 587,
    "useSsl": true,
    "isDefault": true
  }
]
```

> **Note:** Passwords are never included in API responses.

### Add Account

```
POST /api/settings/accounts
```

**Request Body:**

```json
{
  "displayName": "Work",
  "emailAddress": "martin@work.com",
  "imapHost": "imap.work.com",
  "imapPort": 993,
  "smtpHost": "smtp.work.com",
  "smtpPort": 587,
  "username": "martin@work.com",
  "password": "secret",
  "useSsl": true,
  "isDefault": false
}
```

**Response:** `201 Created`

### Update Account

```
PUT /api/settings/accounts/{id}
```

Same body as Add Account. **Response:** `204 No Content`

### Delete Account

```
DELETE /api/settings/accounts/{id}
```

**Response:** `204 No Content`

### Test Account Connection

```
POST /api/settings/accounts/{id}/test
```

**Response:** `200 OK`

```json
{
  "imapSuccess": true,
  "smtpSuccess": true,
  "imapError": null,
  "smtpError": null
}
```

### Get Preferences

```
GET /api/settings/preferences
```

**Response:** `200 OK`

```json
{
  "theme": "auto",
  "defaultAccountId": 1,
  "aiEnabled": true,
  "previewLineCount": 2,
  "pageSize": 25
}
```

### Update Preferences

```
PUT /api/settings/preferences
```

Same body as Get response. **Response:** `204 No Content`

---

## AI Endpoints

### Summarize Message

```
POST /api/ai/summarize
```

**Request Body:**

```json
{
  "messageId": "msg-abc123",
  "bodyText": "Hi Martin, just confirming our meeting tomorrow at 3pm in the main conference room. Please bring the Q1 reports. Also, Sarah mentioned she might join remotely.",
  "maxLength": 100
}
```

**Response:** `200 OK`

```json
{
  "summary": "Meeting confirmation for tomorrow at 3pm in main conference room. Bring Q1 reports. Sarah may join remotely.",
  "model": "mistral",
  "tokensUsed": 42
}
```

### Draft Reply

```
POST /api/ai/draft-reply
```

**Request Body:**

```json
{
  "messageId": "msg-abc123",
  "originalBody": "Hi Martin, can you review the proposal by Friday?",
  "tone": "professional",
  "intent": "accept"
}
```

**Response:** `200 OK`

```json
{
  "draft": "Hi Alice,\n\nThank you for sending the proposal. I'll review it and have my feedback ready by Friday.\n\nBest regards,\nMartin",
  "model": "mistral",
  "tokensUsed": 58
}
```

### Categorize Messages

```
POST /api/ai/categorize
```

**Request Body:**

```json
{
  "messages": [
    { "id": "msg-abc123", "subject": "Invoice #1234", "bodyPreview": "Please find attached..." },
    { "id": "msg-def456", "subject": "Team Lunch Friday", "bodyPreview": "Hey everyone, let's..." }
  ]
}
```

**Response:** `200 OK`

```json
{
  "categories": [
    { "messageId": "msg-abc123", "category": "finance", "confidence": 0.92 },
    { "messageId": "msg-def456", "category": "social", "confidence": 0.87 }
  ],
  "model": "mistral"
}
```

---

## Error Responses

All errors follow RFC 7807 Problem Details:

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Account not found",
  "status": 404,
  "detail": "Mail account with ID 99 does not exist.",
  "instance": "/api/settings/accounts/99"
}
```

| Status | Meaning |
|--------|---------|
| 400 | Bad Request — validation error |
| 404 | Not Found — resource doesn't exist |
| 409 | Conflict — e.g., duplicate folder name |
| 422 | Unprocessable Entity — valid syntax but semantic error |
| 429 | Too Many Requests — AI endpoint rate limit |
| 502 | Bad Gateway — mail server connection failed |
| 503 | Service Unavailable — Ollama not ready |
