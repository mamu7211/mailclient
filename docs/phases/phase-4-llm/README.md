# Phase 4: LLM Integration

## Overview

Local AI features powered by Ollama/Qwen3. Automatic mail categorization, summarization, and smart reply drafts. All inference runs locally on the NAS — no data leaves the device.

**Depends on:** Phase 3 (synced messages in the database)

## Features (in implementation order)

| # | Feature | Issue | Description |
|---|---------|-------|-------------|
| 1 | [Prompt Injection Mitigation](01-prompt-injection-mitigation.md) | #83 | Input sanitization and output validation for LLM pipelines |
| 2a | Label Management | #123 | Label entity, CRUD API, Settings UI |
| 2b | Classification Rules | #124 | ClassificationRule entity, CRUD API, Settings UI |
| 2c | Job Settings Infrastructure | #125 | Generic job scheduling, entity, Admin UI |
| 2d | Classification Job Shell | #126 | ClassificationQueue entity, noop background job, sync integration |
| 2e | [Classification Service](02-mail-categorization.md) | #127 | MEAI + OllamaSharp LLM integration, MailLabelAssignmentService |
| 3 | Mail Summarization | #121 | One-line summaries for inbox list |
| 4 | Smart Reply Drafts | #122 | AI-generated reply suggestions |

Parent tracker for 2a-2e: #120

## Dependencies

- Feature 1 is the foundation — all LLM features depend on it
- Features 2a, 2b, 2c are independent and can be worked in parallel
- Feature 2d depends on 2c (job settings infrastructure)
- Feature 2e depends on 2a, 2b, and 2d
- Features 3, 4 each depend on Feature 1 and can reuse the IChatClient setup from 2e

## Security Considerations

Email is an attacker-controlled input channel. Every message body and subject is untrusted text that will be passed to the LLM. Prompt injection is the primary threat — a malicious email could attempt to manipulate the LLM into producing incorrect classifications, leaking context from other emails, or generating harmful reply drafts. Feature 1 must be implemented before any LLM feature goes live.
