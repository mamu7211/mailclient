---
name: dev-harness
description: Start Aspire, authenticate, send test emails, check classification, read logs — all autonomously
user_invocable: true
---

# Dev Harness

Autonomous development workflow via shell scripts and Aspire MCP tools. Shell scripts live in `.claude/skills/dev-harness/`.

## Arguments

Optional subcommand: `start`, `stop`, `cleanup`, `login`, `status`, `check`, `query`, `send-mail`, `trigger-job`, `logs`.
If no argument, run `status` to show what's running.

---

## Two Modes: Shell Scripts vs Aspire MCP

The harness works in two modes depending on how Aspire was started:

| Mode | How Aspire was started | Observability | Actions |
|------|----------------------|---------------|---------|
| **Shell-only** | `start.sh` (uses `dotnet run`) | Scripts + DB queries | Scripts |
| **Shell + MCP** | `aspire run` (user starts manually) | Aspire MCP tools + scripts + DB queries | Scripts |

### When to use Aspire MCP tools

When the Aspire MCP server is connected (check with `mcp__aspire__list_apphosts`), prefer MCP tools for **observability**:

| Task | Shell script | Aspire MCP tool |
|------|-------------|-----------------|
| Service health/status | `status.sh` | `mcp__aspire__list_resources` |
| API structured logs | _(not available)_ | `mcp__aspire__list_structured_logs` (resource: `api`) |
| Console output | _(not available)_ | `mcp__aspire__list_console_logs` |
| Distributed traces | _(not available)_ | `mcp__aspire__list_traces` |
| Job execution history | `logs.sh` | `mcp__aspire__list_structured_logs` (filter by `Feirb.Api.Services`) |
| Restart a service | _(stop/start scripts)_ | `mcp__aspire__execute_resource_command` (command: `restart`) |

### Always use shell scripts for actions

MCP tools are read-only (except resource commands). These always require scripts:

- `login.sh` — authenticate and store JWT
- `check.sh` — query API endpoints
- `query.sh` — run SQL against PostgreSQL
- `send-mail.sh` — send test emails via SMTP
- `trigger-job.sh` — trigger background jobs via API

---

## Scripts

### Lifecycle

| Script | Usage | What it does |
|--------|-------|-------------|
| `start.sh` | `./start.sh` | Start Aspire with `FEIRB_SEED_DATA=true`, wait for API health, store PID |
| `stop.sh` | `./stop.sh` | Stop Aspire via PID file |
| `cleanup.sh` | `./cleanup.sh` | Stop Aspire + remove Postgres container + prune volumes (full reset) |
| `login.sh` | `./login.sh` | Auth as admin, store JWT to `/tmp/feirb-token.txt` |
| `status.sh` | `./status.sh` | Check Aspire, API, token, GreenMail, Ollama |

### Inspect

| Script | Usage | What it does |
|--------|-------|-------------|
| `check.sh` | `./check.sh /api/jobs` | GET any API endpoint with stored token, pretty-print JSON |
| `query.sh` | `./query.sh 'SELECT ...'` | Run SQL against Postgres via local psql on port 15432 |
| `logs.sh` | `./logs.sh [type]` | Show recent job executions from DB (optionally filter by job type) |

### Act

| Script | Usage | What it does |
|--------|-------|-------------|
| `trigger-job.sh` | `./trigger-job.sh classification` | Trigger job(s) by type name via API |
| `send-mail.sh` | `./send-mail.sh [from] [to] [subject] [body]` | Send email via SMTP to GreenMail (no auth needed) |

---

## Aspire MCP Quick Reference

### Check if MCP is available

Call `mcp__aspire__list_apphosts`. If it returns an apphost, MCP is available.
If empty, fall back to shell scripts only.

### Resource names

| Display name | Resource name (use in MCP calls) |
|-------------|----------------------------------|
| API | Use the name from `list_resources` matching `resource_type: "Project"` |
| PostgreSQL | `feirb-postgres-*` |
| GreenMail | `feirb-greenmail-*` |
| Ollama | `feirb-ollama-*` |
| Ollama model | `feirb-ollama-qwen3` |

Resource names include a random suffix (e.g. `feirb-postgres-unsgsdjw`). Get the exact name from `mcp__aspire__list_resources`.

### Common MCP operations

```
# All resources with health status
mcp__aspire__list_resources

# API structured logs (job executions, errors, request traces)
mcp__aspire__list_structured_logs(resourceName: "api-XXXXX")

# Console output for debugging startup failures
mcp__aspire__list_console_logs(resourceName: "feirb-ollama-XXXXX")

# Distributed traces across services
mcp__aspire__list_traces(resourceName: "api-XXXXX")

# Restart a stuck service
mcp__aspire__execute_resource_command(resourceName: "feirb-ollama-XXXXX", commandName: "restart")

# Rebuild API after code changes (recompiles from source)
mcp__aspire__execute_resource_command(resourceName: "api-XXXXX", commandName: "rebuild")
```

---

## Typical Workflows

### Fresh start (clean DB) — shell mode

```bash
.claude/skills/dev-harness/cleanup.sh
.claude/skills/dev-harness/start.sh     # seeds users, labels, rules, enables jobs
.claude/skills/dev-harness/login.sh
```

### Fresh start — MCP mode

User starts Aspire manually with `aspire run` in a terminal. Then:

```bash
.claude/skills/dev-harness/login.sh
```

Use `mcp__aspire__list_resources` to verify all services are healthy.

### Restart (keep data)

```bash
.claude/skills/dev-harness/stop.sh
.claude/skills/dev-harness/start.sh
.claude/skills/dev-harness/login.sh     # token expired after restart
```

### Test classification end-to-end

```bash
.claude/skills/dev-harness/trigger-job.sh imap-sync      # sync emails from GreenMail
sleep 10
.claude/skills/dev-harness/trigger-job.sh classification  # classify synced emails
sleep 60                                                   # qwen3:0.6b needs ~5s per message
.claude/skills/dev-harness/logs.sh classification          # check job execution status
.claude/skills/dev-harness/query.sh 'SELECT cm."Subject", cr."Result" FROM "ClassificationResults" cr JOIN "CachedMessages" cm ON cr."CachedMessageId" = cm."Id";'
```

With MCP available, use `mcp__aspire__list_structured_logs` to watch classification progress in real-time instead of polling with `logs.sh`.

### Send test email and classify

```bash
.claude/skills/dev-harness/send-mail.sh "newsletter@example.com" "admin@feirb.local" "Weekly digest" "Here is your weekly newsletter summary."
.claude/skills/dev-harness/trigger-job.sh imap-sync
sleep 10
.claude/skills/dev-harness/trigger-job.sh classification
```

### Debug classification failures

```bash
.claude/skills/dev-harness/logs.sh classification
.claude/skills/dev-harness/query.sh 'SELECT "Status", COUNT(*) FROM "ClassificationQueueItems" GROUP BY "Status";'
.claude/skills/dev-harness/query.sh 'SELECT LEFT("Error", 300) FROM "ClassificationQueueItems" WHERE "Error" IS NOT NULL LIMIT 5;'
```

With MCP: `mcp__aspire__list_structured_logs` filtered by source `Feirb.Api.Services.ClassificationJob` shows errors with full stack traces.

### Debug service startup failures

With MCP:
```
mcp__aspire__list_resources              # check which service is not Running/Healthy
mcp__aspire__list_console_logs(...)      # check stdout for crash output
mcp__aspire__execute_resource_command(resourceName: "...", commandName: "restart")
```

---

## Useful Queries

```sql
-- Classification queue status
SELECT "Status", COUNT(*) FROM "ClassificationQueueItems" GROUP BY "Status";

-- Recent classification results with subjects
SELECT cm."Subject", cr."Result"
FROM "ClassificationResults" cr
JOIN "CachedMessages" cm ON cr."CachedMessageId" = cm."Id"
ORDER BY cr."ClassifiedAt" DESC LIMIT 10;

-- Messages with their assigned labels
SELECT cm."Subject", l."Name" as "Label"
FROM "CachedMessages" cm
JOIN "CachedMessageLabel" cml ON cm."Id" = cml."CachedMessageId"
JOIN "Labels" l ON cml."LabelsId" = l."Id"
ORDER BY cm."Date" DESC LIMIT 20;

-- Classification errors
SELECT LEFT("Error", 300) FROM "ClassificationQueueItems" WHERE "Error" IS NOT NULL LIMIT 5;

-- Job execution history
SELECT js."JobName", je."Status", je."StartedAt", je."FinishedAt", LEFT(je."Error", 200)
FROM "JobExecutions" je
JOIN "JobSettings" js ON je."JobSettingsId" = js."Id"
ORDER BY je."StartedAt" DESC LIMIT 10;

-- Reset stuck Processing items to Pending
UPDATE "ClassificationQueueItems" SET "Status" = 'Pending', "Error" = NULL WHERE "Status" = 'Processing';
```

Note: Table and column names are PascalCase and must be double-quoted in SQL.

---

## File Locations

| File | Purpose |
|------|---------|
| `/tmp/feirb-aspire.log` | Aspire orchestrator stdout (shell mode only) |
| `/tmp/feirb-aspire.pid` | Aspire process ID (shell mode only) |
| `/tmp/feirb-auth.json` | Full auth response |
| `/tmp/feirb-token.txt` | JWT access token |

## Seeded Data (via `FEIRB_SEED_DATA=true`)

| Data | Details |
|------|---------|
| Users | `admin` (admin), `alice` (user) — password = email |
| Mailboxes | One per user, pointing to GreenMail |
| Labels | Newsletter (#4CAF50), Work (#2196F3), Personal (#FF9800) — admin only |
| Classification rule | "Classify newsletter/work/personal emails" — admin only |
| Jobs | All enabled, 1-minute cron interval |
| Emails | 10 per account preloaded in GreenMail (synced on first IMAP sync run) |

## Service URLs (fixed ports)

| Service | URL |
|---------|-----|
| API (HTTPS) | https://localhost:7272 |
| GreenMail REST | http://localhost:8080 |
| GreenMail SMTP | localhost:3025 |
| GreenMail IMAP | localhost:3143 |
| Ollama | http://localhost:11434 |
| PostgreSQL | localhost:15432 |

## Notes

- Always use `curl -sk` for HTTPS (self-signed dev cert)
- Token expires after restart — re-run `login.sh`
- GreenMail SMTP requires no authentication
- After sending mail, run `trigger-job.sh imap-sync` to pull it into the app
- Ollama model (`qwen3:0.6b`) persists in `.ollama-data/` across cleanups
- API logs go through Aspire OTLP — use `logs.sh` or MCP structured logs to query
- Auto-disable threshold is 10 consecutive failures
- MCP mode requires `aspire run` (not `dotnet run` or `start.sh`) and Aspire CLI 13.2+
- MCP resource names have random suffixes — always get exact names from `list_resources`
