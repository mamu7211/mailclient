# Feature 3.3: Mail API

## Goal

API endpoints for reading the unified inbox.

**Depends on:** Feature 3.1 (entities to query)

## Deliverables

### Endpoints

```
GET  /api/mail       → Paginated message list
GET  /api/mail/{id}  → Single message detail
```

Both endpoints require JWT authentication. Users can only access messages from their own mailboxes.

### `GET /api/mail`

- Returns messages from all user's mailboxes, merged
- Ordered by `Date` descending
- Paginated (page number + page size, default 25)
- No filtering, search, or custom sort (deferred to #65)

**Response DTO (`MailListItemResponse`):**

| Field | Type |
|-------|------|
| `Id` | Guid |
| `MailboxName` | string |
| `BadgeColor` | string |
| `From` | string |
| `Subject` | string |
| `Date` | DateTimeOffset |

**Response wrapper (`MailListResponse`):**

| Field | Type |
|-------|------|
| `Items` | `MailListItemResponse[]` |
| `Page` | int |
| `PageSize` | int |
| `TotalCount` | int |

### `GET /api/mail/{id}`

- Returns full message detail including body and attachment metadata
- 404 if message doesn't exist or belongs to another user

**Response DTO (`MailDetailResponse`):**

| Field | Type |
|-------|------|
| `Id` | Guid |
| `MailboxName` | string |
| `BadgeColor` | string |
| `From` | string |
| `ReplyTo` | string? |
| `To` | string |
| `Cc` | string? |
| `Subject` | string |
| `Date` | DateTimeOffset |
| `BodyHtml` | string? |
| `BodyPlainText` | string? |
| `Attachments` | `AttachmentResponse[]` |

**`AttachmentResponse`:**

| Field | Type |
|-------|------|
| `Id` | Guid |
| `Filename` | string |
| `Size` | long |
| `MimeType` | string |

### DTOs

All request/response records in `Feirb.Shared`.

## Testing

- Integration tests against seeded database:
  - `GET /api/mail` returns paginated results ordered by date
  - `GET /api/mail` only returns messages from the authenticated user's mailboxes
  - `GET /api/mail/{id}` returns full detail
  - `GET /api/mail/{id}` returns 404 for another user's message

## Acceptance Criteria

- [ ] Paginated inbox list ordered by date descending
- [ ] Messages from all user mailboxes merged in response
- [ ] Message detail includes body and attachment metadata
- [ ] User isolation — no access to other users' messages
- [ ] All DTOs in `Feirb.Shared`
