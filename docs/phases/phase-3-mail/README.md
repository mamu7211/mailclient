# Phase 3: Mail Fetching & Inbox

## Overview

Background IMAP synchronization and a unified inbox view. All mailboxes merge into a single chronological inbox — no folder tree, no per-account silos. AI categorization (Phase 6) replaces traditional folder navigation.

**Depends on:** Phase 2 (Mailbox entity with IMAP credentials, Data Protection API)

## Features (in implementation order)

| # | Feature | Issue | Description |
|---|---------|-------|-------------|
| 1 | [Schema Extensions](01-schema-extensions.md) | #68 | New entities, Mailbox/User field additions, EF migration |
| 2 | [IMAP Sync Service](02-imap-sync-service.md) | #69 | Quartz.NET background sync embedded in Feirb.Api |
| 3 | [Mail API](03-mail-api.md) | #70 | Paginated inbox list and message detail endpoints |
| 4 | [Inbox UI](04-inbox-ui.md) | #71 | Unified inbox list page and message detail page |

## Dependencies

- Feature 1 is the foundation — all others depend on it
- Feature 2 depends on Feature 1 (entities to write into)
- Feature 3 depends on Feature 1 (entities to query)
- Feature 4 depends on Feature 3 (API to call)

## Deferred Feature Requests

| Issue | Title |
|-------|-------|
| #64 | IMAP UIDVALIDITY change detection and re-sync |
| #65 | Message filtering, search, and sort options |
| #66 | Bidirectional read/unread state sync with IMAP |
| #67 | Sync error handling and UI status display |
