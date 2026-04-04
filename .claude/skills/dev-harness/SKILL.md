---
name: dev-harness
description: Start Aspire, authenticate, send test emails, check classification, read logs — all autonomously
user_invocable: true
---

# Dev Harness

Autonomous development workflow: start the app, authenticate, send test emails, inspect results, and read logs — without requiring user interaction for each step.

## Arguments

Optional subcommand as first argument: `start`, `login`, `send-mail`, `check`, `logs`, `stop`, `status`.
If no argument, run `status` to show what's running and available.

---

## Subcommands

### `start` — Start Aspire and wait for readiness

1. Check if already running:
   ```bash
   curl -sk https://localhost:7272/health 2>/dev/null
   ```

2. If not running, start Aspire in background with log capture:
   ```bash
   cd /home/martin/Workspace/mailclient && dotnet run --project src/Feirb.AppHost > /tmp/feirb-aspire.log 2>&1 &
   echo $! > /tmp/feirb-aspire.pid
   ```

3. Poll until the API is healthy (max 120 seconds, Ollama model download may take longer on first run):
   ```bash
   for i in $(seq 1 40); do
     curl -sk https://localhost:7272/health 2>/dev/null && break
     sleep 3
   done
   ```

4. Once healthy, automatically run `login` subcommand.

### `login` — Authenticate and store JWT token

1. Login with seeded admin credentials:
   ```bash
   curl -sk -X POST https://localhost:7272/api/auth/login \
     -H "Content-Type: application/json" \
     -d '{"username":"admin","password":"admin@feirb.local"}' \
     > /tmp/feirb-auth.json
   ```

2. Extract and store the access token:
   ```bash
   TOKEN=$(cat /tmp/feirb-auth.json | python3 -c "import sys,json; print(json.load(sys.stdin)['accessToken'])")
   echo "$TOKEN" > /tmp/feirb-token.txt
   ```

3. Verify the token works:
   ```bash
   curl -sk -H "Authorization: Bearer $(cat /tmp/feirb-token.txt)" https://localhost:7272/api/mail/messages?page=1\&pageSize=1
   ```

### `send-mail` — Send a test email via SMTP to GreenMail

Use Python's smtplib (no auth required for GreenMail):

```bash
python3 -c "
import smtplib
from email.mime.text import MIMEText

msg = MIMEText('This is a test email for classification testing.')
msg['Subject'] = 'Test: Newsletter subscription confirmation'
msg['From'] = 'newsletter@example.com'
msg['To'] = 'admin@feirb.local'

with smtplib.SMTP('localhost', 3025) as s:
    s.send_message(msg)
print('Email sent successfully')
"
```

Customize subject/from/body based on what's being tested. For classification testing, vary the sender and subject to match different classification rules.

After sending, mention that the IMAP sync job needs to run to pick up the email. The user can trigger a sync manually or wait for the next scheduled run.

### `check` — Inspect classification state via API

Use the stored token to query API endpoints:

```bash
TOKEN=$(cat /tmp/feirb-token.txt)
```

1. **List messages** (check if synced):
   ```bash
   curl -sk -H "Authorization: Bearer $TOKEN" "https://localhost:7272/api/mail/messages?page=1&pageSize=10"
   ```

2. **List labels** (check user's label set):
   ```bash
   curl -sk -H "Authorization: Bearer $TOKEN" "https://localhost:7272/api/settings/labels"
   ```

3. **List classification rules**:
   ```bash
   curl -sk -H "Authorization: Bearer $TOKEN" "https://localhost:7272/api/settings/rules"
   ```

4. **Check job status** (background jobs):
   ```bash
   curl -sk -H "Authorization: Bearer $TOKEN" "https://localhost:7272/api/jobs"
   ```

5. **Check admin job stats** (detailed execution history):
   ```bash
   curl -sk -H "Authorization: Bearer $TOKEN" "https://localhost:7272/api/admin/jobs/stats"
   ```

Parse JSON responses with `python3 -c "import sys,json; ..."` or `jq` for readability.

### `logs` — Read application logs

1. **Recent logs** (last 100 lines):
   ```bash
   tail -100 /tmp/feirb-aspire.log
   ```

2. **Filter for errors**:
   ```bash
   grep -i "error\|exception\|fail\|warn" /tmp/feirb-aspire.log | tail -50
   ```

3. **Filter for classification/Ollama**:
   ```bash
   grep -i "classif\|ollama\|chat\|qwen\|label" /tmp/feirb-aspire.log | tail -50
   ```

4. **Follow logs in real-time** (use with caution — will block):
   ```bash
   tail -f /tmp/feirb-aspire.log
   ```

### `stop` — Stop the Aspire process

```bash
if [ -f /tmp/feirb-aspire.pid ]; then
  kill $(cat /tmp/feirb-aspire.pid) 2>/dev/null
  rm /tmp/feirb-aspire.pid
  echo "Aspire stopped"
else
  echo "No PID file found, trying pkill"
  pkill -f "Feirb.AppHost" 2>/dev/null
fi
```

### `query` — Query PostgreSQL directly via psql

`psql` is installed locally. Discover the Aspire-managed Postgres password and query directly:

```bash
CRUNTIME=$(command -v podman &>/dev/null && echo podman || echo docker)
PG_CONTAINER=$($CRUNTIME ps --format '{{.Names}}' | grep postgres)
PG_PASS=$($CRUNTIME exec "$PG_CONTAINER" bash -c 'echo $POSTGRES_PASSWORD')
PGPASSWORD="$PG_PASS" psql -h localhost -U postgres mailclientdb -c '<SQL>'
```

Useful queries:

```sql
-- Classification queue status
SELECT "Status", COUNT(*) FROM "ClassificationQueueItems" GROUP BY "Status";

-- Recent classification results
SELECT cr."ClassifiedAt", cr."Result", cm."Subject"
FROM "ClassificationResults" cr
JOIN "CachedMessages" cm ON cr."CachedMessageId" = cm."Id"
ORDER BY cr."ClassifiedAt" DESC LIMIT 10;

-- Job execution history
SELECT "JobName", "Status", "StartedAt", "Error"
FROM "JobExecutions" ORDER BY "StartedAt" DESC LIMIT 10;

-- Messages with their labels
SELECT cm."Subject", l."Name" as "Label"
FROM "CachedMessages" cm
LEFT JOIN "CachedMessageLabel" cml ON cm."Id" = cml."CachedMessagesId"
LEFT JOIN "Labels" l ON cml."LabelsId" = l."Id"
ORDER BY cm."Date" DESC LIMIT 20;
```

Note: Table and column names are PascalCase and must be double-quoted in SQL.

### `status` — Show current state

Check and report:
- Is Aspire running? (check PID file + health endpoint)
- Is the token valid? (try an authenticated request)
- Is GreenMail reachable? (`curl http://localhost:8080`)
- Is Ollama reachable? (`curl http://localhost:11434/api/tags`)
- How many messages are synced?
- Any classification queue items pending/failed?

---

## File Locations

| File | Purpose |
|------|---------|
| `/tmp/feirb-aspire.log` | Aspire stdout/stderr |
| `/tmp/feirb-aspire.pid` | Aspire process ID |
| `/tmp/feirb-auth.json` | Full auth response (tokens) |
| `/tmp/feirb-token.txt` | JWT access token only |

## Seeded Credentials

| User | Username | Password | Role |
|------|----------|----------|------|
| Admin | `admin` | `admin@feirb.local` | Admin |
| Alice | `alice` | `alice@feirb.local` | User |

## Service URLs

| Service | URL |
|---------|-----|
| API (HTTPS) | https://localhost:7272 |
| API (HTTP) | http://localhost:5263 |
| GreenMail REST | http://localhost:8080 |
| GreenMail SMTP | localhost:3025 |
| GreenMail IMAP | localhost:3143 |
| Ollama | http://localhost:11434 |
| Aspire Dashboard | https://localhost:18888 |

## Notes

- Always use `curl -sk` for HTTPS (self-signed cert in dev)
- Token expires — if you get 401, re-run `login`
- GreenMail SMTP requires no authentication
- After sending mail, IMAP sync job must run to pull it into the app
- Ollama model download (~2.6GB) happens on first start only
