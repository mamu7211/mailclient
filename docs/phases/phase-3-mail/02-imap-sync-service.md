# Feature 3.2: IMAP Sync Service

## Goal

Background service that periodically syncs mail from each user's mailbox via IMAP.

**Depends on:** Feature 3.1 (entities to write into)

## Deliverables

### Architecture

- Quartz.NET embedded in `Feirb.Api` as a hosted service
- One scheduled job per mailbox, interval = `Mailbox.PollIntervalMinutes`
- Jobs registered/updated when mailboxes are created, updated, or deleted

### Sync Behavior

#### Initial Sync (first run for a mailbox)

- Connect to IMAP Inbox folder
- Fetch messages within `InitialSyncDays` window (0 = all)
- Cutoff calculated using `User.TimeZone` at 00:00 local time
- Parse headers + attachment metadata, store as `CachedMessage` + `CachedAttachment`
- Body text (`BodyPlainText`, `BodyHtml`) fetched in second iteration — nullable for now
- Record highest `ImapUid` seen

#### Incremental Sync (subsequent runs)

- Connect to IMAP Inbox folder
- Fetch messages with `UID > lastSeenUid`
- Parse and store new messages
- Update highest `ImapUid`

#### Trigger on Mailbox Creation

- When a user saves a new mailbox, immediately trigger the initial sync (don't wait for the next scheduled poll)

### Credentials

- Decrypt IMAP password via Data Protection API per sync operation
- Same pattern as `EmailService`
- Never hold plaintext password in memory beyond the sync operation

### Error Handling

- On failure: log the error, retry on next scheduled poll
- No error state surfaced to the user (deferred to #67)

### Deduplication

- Same message in two mailboxes → stored as two separate `CachedMessage` rows (no cross-mailbox dedup)
- Same message synced twice within one mailbox → skip if `MessageId` already exists for that `MailboxId`

## Testing

- Unit tests with mocked `ImapClient`:
  - Header parsing produces correct `CachedMessage` fields
  - Incremental sync only fetches UIDs above last seen
  - Duplicate `MessageId` within same mailbox is skipped
- Test naming: `MethodName_Scenario_ExpectedResult`

## Acceptance Criteria

- [ ] Quartz.NET job runs per mailbox at configured interval
- [ ] Initial sync respects `InitialSyncDays` and user timezone
- [ ] Incremental sync fetches only new messages
- [ ] Immediate sync on mailbox creation
- [ ] Credentials decrypted per operation, not held in memory
- [ ] Duplicate messages (same MessageId + MailboxId) not created
- [ ] Sync errors logged, retried silently
