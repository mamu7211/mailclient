---
name: test-ui
description: Verify UI features via browser — visual checks, interaction testing, and assertions using Playwright MCP and dev-harness
user_invocable: true
args: path_and_description
---

# Test UI

Verify that a UI feature works correctly through browser interaction. Combines the dev-harness (app lifecycle, auth, data) with Playwright MCP (navigation, forms, clicks, screenshots, assertions).

Use this skill to:
- Verify a feature works after implementation (called by `/implement-feature`, `/implement-ui`)
- Debug UI issues during development
- Explore multi-page flows end-to-end
- Discover Playwright test scenarios for `/implement-playwright-tests`

## Input

```
/test-ui <path> <what to test>
```

Examples:
- `/test-ui /compose verify the CC/BCC toggle shows "No copies" when off and "CC / BCC" when on`
- `/test-ui /admin/jobs toggle a job disabled and verify the API returns enabled=false`
- `/test-ui /settings/mailboxes/create fill the form and verify validation errors`

When called from `/implement-feature`: use acceptance criteria from the GitHub issue. If unclear, ask the user.

## Procedure

### 1. Ensure the app is running with current code

Check containers and API health:

```bash
# Check container status — shows names, uptime, and creation time
podman ps --format "{{.Names}}\t{{.Status}}\t{{.Created}}" 2>/dev/null || \
docker ps --format "{{.Names}}\t{{.Status}}\t{{.Created}}" 2>/dev/null

# Check API health
curl -sk https://localhost:7272/health
```

- **If healthy AND containers are recent** (started after latest code change): proceed to step 2
- **If healthy BUT containers are stale** (code changed since they started): restart:
  ```bash
  .claude/skills/dev-harness/stop.sh
  .claude/skills/dev-harness/start.sh
  ```
- **If not running:** start with seeding (default):
  ```bash
  .claude/skills/dev-harness/start.sh
  ```
  If the test requires a fresh environment (e.g. testing the setup wizard), start without seeding:
  ```bash
  .claude/skills/dev-harness/start.sh --no-seeding
  ```
  (Note: `start.sh` needs to be updated to support `--seeding` flag per design decision)
- **If running but wrong mode needed:** ask the user how to proceed

### 2. Authenticate

Get JWT tokens via the API and inject them into the browser's localStorage.

**Default user: `admin`** — use `alice` (regular user) when testing authorization or non-admin flows.

```bash
# Get tokens
curl -sk -X POST https://localhost:7272/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"password"}' \
  > /tmp/feirb-auth.json
```

Extract both tokens:
```bash
ACCESS=$(python3 -c "import sys,json; print(json.load(sys.stdin)['accessToken'])" < /tmp/feirb-auth.json)
REFRESH=$(python3 -c "import sys,json; print(json.load(sys.stdin)['refreshToken'])" < /tmp/feirb-auth.json)
```

Navigate to the app first, then inject tokens via Playwright:
```
mcp__playwright__browser_navigate → https://localhost:7272
mcp__playwright__browser_evaluate → () => { window.blazorAuth.setTokens('<ACCESS>', '<REFRESH>'); }
```

Then navigate to the target page:
```
mcp__playwright__browser_navigate → https://localhost:7272/<path>
```

### 3. Verify — Visual

Take a screenshot and assess it:
```
mcp__playwright__browser_take_screenshot
```

- Describe what you see — layout, components, text, styling
- Flag anything that looks wrong (broken layout, missing elements, wrong colors)
- If unsure or if there was a visual change, ask the user to review

When the user needs to review visually, ensure the browser is running in **non-headless mode** (default for Playwright MCP) so they can see the browser window.

### 4. Verify — Interactive

Interact with the page to test functionality:

```
mcp__playwright__browser_snapshot          # get accessibility tree for element refs
mcp__playwright__browser_click             # click elements by ref
mcp__playwright__browser_fill_form         # fill form fields (clears first!)
mcp__playwright__browser_press_key         # keyboard input
mcp__playwright__browser_select_option     # dropdowns
```

After each interaction:
- Take a screenshot or snapshot to verify the result
- Check for console errors: `mcp__playwright__browser_console_messages`
- Verify the page state changed as expected

### 5. Verify — Assertions

Use the accessibility tree (`browser_snapshot`) for content assertions:

```
mcp__playwright__browser_snapshot
```

The snapshot returns structured content like:
```
- checkbox "No copies" [unchecked]
- textbox "To" [placeholder: "recipient@example.com"]
- button "Send"
```

Assert against this tree — it's more reliable than parsing HTML.

### 6. Verify — Backend Data

After interactions that modify data (form submissions, toggles, etc.), verify the backend state using dev-harness:

```bash
# Check API response
.claude/skills/dev-harness/check.sh /api/jobs

# Query the database directly
.claude/skills/dev-harness/query.sh 'SELECT "Enabled" FROM "Jobs" WHERE "Type" = '\''Classification'\'''

# Check job execution logs
.claude/skills/dev-harness/logs.sh classification
```

When Aspire MCP is available, also check:
- Structured logs: `mcp__aspire__list_structured_logs`
- Traces: `mcp__aspire__list_traces`

### 7. Handle Failures

When something fails (page error, element not found, assertion mismatch):

1. **Report immediately** — describe what failed + take screenshot
2. **Gather diagnostics:**
   - Console errors: `mcp__playwright__browser_console_messages`
   - Network requests: `mcp__playwright__browser_network_requests`
   - Aspire logs: `mcp__aspire__list_structured_logs` (if available)
   - Database state: `.claude/skills/dev-harness/query.sh`
3. **Present combined diagnosis** — the failure + all diagnostic context

### 8. Report Results

Conversational output:
- **Pass:** brief summary of what was tested + final screenshot
- **Fail:** what failed + screenshot + diagnostic context
- **Needs review:** screenshot + description of what's unclear, browser stays open

### 9. Suggest Playwright Tests

After a successful test session, suggest Playwright test scenarios based on the interactions performed. These can feed into `/implement-playwright-tests`.

Example output:
```
Suggested Playwright tests based on this session:
1. Navigate to /compose, verify CC/BCC toggle shows "No copies" when unchecked
2. Toggle CC/BCC on, verify CC and BCC input fields appear
3. Switch to Markdown mode, verify "Send as plain text" toggle appears
```

## Multi-Page Flows

For end-to-end flows spanning multiple pages:
- Execute steps sequentially, verifying each page transition
- Maintain context across pages (e.g. "created mailbox X, now compose using it")
- Verify data flows correctly between pages using both UI assertions and backend checks

Example flow: "Create a mailbox, send an email, verify it appears in sent folder"
1. Navigate to /settings/mailboxes/create, fill form, submit
2. Navigate to /compose, select new mailbox, fill email, send
3. Navigate to /mail/sent, verify email appears
4. Query DB to confirm message record exists

## Available Playwright MCP Tools

| Tool | Purpose |
|------|---------|
| `browser_navigate` | Go to a URL |
| `browser_snapshot` | Accessibility tree (for assertions and element refs) |
| `browser_take_screenshot` | Visual capture |
| `browser_click` | Click by element ref or coordinates |
| `browser_fill_form` | Fill form fields (clears existing value first) |
| `browser_type` | Type text into focused element |
| `browser_press_key` | Keyboard input (e.g. `Enter`, `Tab`, `Control+a`) |
| `browser_select_option` | Select dropdown option |
| `browser_hover` | Hover over element |
| `browser_evaluate` | Execute JavaScript on the page |
| `browser_console_messages` | Read console output (errors, warnings) |
| `browser_network_requests` | Inspect network activity |
| `browser_wait_for` | Wait for element or condition |

## Seeded Test Data

When running with `--seeding`:
- **Users:** `admin` / `password` (admin), `alice` / `password` (user)
- **Mailboxes:** One per user, connected to GreenMail
- **Labels:** Newsletter, Work, Personal (admin)
- **Jobs:** All enabled with 1-minute cron intervals
- **Emails:** 10 per account preloaded in GreenMail
