---
name: query-db
description: Query the PostgreSQL database running in the Aspire-managed container
user_invocable: true
---

# Query Database

Run SQL queries against the Feirb PostgreSQL database in the Aspire-managed container.

## Prerequisites

- The Aspire AppHost must be running (`/aspire-run`)

## Steps

`psql` is installed locally — connect directly to the Aspire-managed Postgres container (no exec needed).

1. **Detect container runtime and get password:**
   ```bash
   CRUNTIME=$(command -v podman &>/dev/null && echo podman || echo docker)
   PG_CONTAINER=$($CRUNTIME ps --format '{{.Names}}' | grep postgres)
   PG_PASS=$($CRUNTIME exec "$PG_CONTAINER" printenv POSTGRES_PASSWORD)
   ```

2. **Run the query via local psql:**
   ```bash
   PGPASSWORD="$PG_PASS" psql -h localhost -p 15432 -U postgres mailclientdb -c '<SQL>'
   ```

   For multi-line or complex queries, use a heredoc:
   ```bash
   PGPASSWORD="$PG_PASS" psql -h localhost -p 15432 -U postgres mailclientdb <<EOF
   SELECT * FROM "Users";
   EOF
   ```

## Connection Details

- **User:** `postgres`
- **Database:** `mailclientdb`
- **Container runtime:** Podman or Docker (auto-detected, prefer podman)
- **Password:** Dynamic per container — always read from `$POSTGRES_PASSWORD` env var

## Table Names

Table names use PascalCase and must be double-quoted in SQL:

| Table | Description |
|-------|-------------|
| `"Users"` | User accounts |
| `"Mailboxes"` | IMAP/SMTP mailbox configurations |
| `"CachedMessages"` | Synced email messages |
| `"CachedAttachments"` | Email attachments |
| `"CachedMessageLabel"` | Join table: messages <-> labels |
| `"Labels"` | User-defined classification labels |
| `"ClassificationRules"` | User-defined classification instructions |
| `"ClassificationQueueItems"` | Messages queued for classification |
| `"ClassificationResults"` | LLM classification results |
| `"JobSettings"` | Background job configuration |
| `"JobExecutions"` | Background job run history |
| `"DashboardLayouts"` | Per-user dashboard layout JSON |
| `"WidgetConfigs"` | Per-user widget configuration |
| `"Avatars"` | Contact avatar images |
| `"SmtpSettings"` | System SMTP configuration |
| `"PasswordResetTokens"` | Password reset tokens |
| `"DataProtectionKeys"` | ASP.NET Data Protection keys |
| `"__EFMigrationsHistory"` | EF Core migration tracking |

## Arguments

If the user provides a SQL query as an argument, run it directly. Otherwise, ask what they want to query.

## Notes

- Always double-quote PascalCase table and column names in SQL
- For read-only queries, prefer `SELECT`. Never run destructive queries (DROP, TRUNCATE, DELETE) without explicit user confirmation
- The container name changes each time Aspire restarts — always discover it dynamically
