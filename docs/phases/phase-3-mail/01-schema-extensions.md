# Feature 3.1: Schema Extensions

## Goal

Extend the data model for mail storage and sync configuration.

**Depends on:** Phase 2 (Mailbox entity exists)

## Deliverables

### `CachedMessage` Entity

| Field | Type | Notes |
|-------|------|-------|
| `Id` | Guid | PK |
| `MailboxId` | Guid | FK → Mailbox, CASCADE delete |
| `MessageId` | string | RFC 2822 Message-ID header, unique business identifier |
| `ImapUid` | uint? | IMAP UID for sync mechanics, nullable (POP3 future-proofing) |
| `Subject` | string | |
| `From` | string | |
| `ReplyTo` | string? | Nullable, only set if different from From |
| `To` | string | |
| `Cc` | string? | Nullable |
| `Date` | DateTimeOffset | Message date from headers |
| `BodyPlainText` | string? | Nullable, populated in second iteration |
| `BodyHtml` | string? | Nullable, populated in second iteration |
| `SyncedAt` | DateTimeOffset | When this message was fetched |

- Index on `MailboxId`
- Index on `MessageId` (unique)
- Index on `Date` (for pagination ordering)

### `CachedAttachment` Entity (metadata only)

| Field | Type | Notes |
|-------|------|-------|
| `Id` | Guid | PK |
| `CachedMessageId` | Guid | FK → CachedMessage, CASCADE delete |
| `Filename` | string | |
| `Size` | long | Bytes |
| `MimeType` | string | |

### `Mailbox` Additions

| Field | Type | Notes |
|-------|------|-------|
| `BadgeColor` | string | Hex color (e.g., `#3B82F6`) |
| `InitialSyncDays` | int | 0 = sync all, N = last N days |
| `PollIntervalMinutes` | int | Default 60 |

### `User` Addition

| Field | Type | Notes |
|-------|------|-------|
| `TimeZone` | string | IANA timezone (e.g., `Europe/Vienna`) |

### EF Core Migration

Single migration adding `CachedMessage` + `CachedAttachment` tables and new columns on `Mailbox` + `User`.

## Acceptance Criteria

- [ ] `CachedMessage` table with all fields and indexes
- [ ] `CachedAttachment` table with FK to `CachedMessage`
- [ ] `Mailbox` has `BadgeColor`, `InitialSyncDays`, `PollIntervalMinutes`
- [ ] `User` has `TimeZone`
- [ ] CASCADE delete: Mailbox → CachedMessages → CachedAttachments
- [ ] Migration applies cleanly on existing database
