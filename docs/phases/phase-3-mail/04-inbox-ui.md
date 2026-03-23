# Feature 3.4: Inbox UI

## Goal

Unified inbox list and message detail pages in the Blazor WASM frontend.

**Depends on:** Feature 3.3 (API endpoints)

## Deliverables

### Inbox List Page (`/inbox`)

Replaces the existing placeholder inbox page.

**Columns per row:**

| Column | Content |
|--------|---------|
| Mailbox | Color badge + `Mailbox.Name` |
| From | Sender name or email |
| Subject | Message subject |
| Date | Absolute date |

- Messages from all mailboxes merged, ordered by date descending
- Paginated (matching API page size)
- Clicking a row navigates to the message detail page

### Message Detail Page (`/inbox/{id}`)

**Breadcrumb:** `Dashboard / Inbox / {from} — {subject} — {date}`

**Content:**

- Mailbox badge (color + name)
- Headers: From, To, Cc, ReplyTo (shown only if different from From), Date, Subject
- Body: sanitized HTML with plain text fallback
  - **No external resource loading** — strip/block all remote `src` attributes (images, scripts, iframes, tracking pixels)
  - Inline styles preserved where safe
- Attachment list (display only): filename, size, MIME type
  - No download action (deferred)

### HTML Sanitization

- Strip `<script>`, `<iframe>`, `<object>`, `<embed>` tags
- Remove `src` attributes pointing to external URLs (http/https)
- Remove `on*` event handler attributes
- Allow inline images with `data:` URIs
- Preserve basic formatting tags and inline styles

## i18n

All UI strings localized in `en-US`, `de-DE`, `fr-FR`, `it-IT`.

## Acceptance Criteria

- [ ] Inbox list shows messages from all mailboxes with color badges
- [ ] Pagination works
- [ ] Clicking a message navigates to detail page
- [ ] Breadcrumb shows sender, subject, and date
- [ ] HTML body rendered with sanitization
- [ ] No external resources loaded (tracking protection)
- [ ] Plain text fallback when no HTML body
- [ ] Attachment metadata displayed (no download)
- [ ] All strings localized in 4 locales
