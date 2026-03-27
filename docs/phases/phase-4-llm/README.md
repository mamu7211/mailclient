# Phase 4: LLM Integration

## Overview

Local AI features powered by Ollama/Qwen3. Automatic mail categorization, summarization, and smart reply drafts. All inference runs locally on the NAS — no data leaves the device.

**Depends on:** Phase 3 (synced messages in the database)

## Features (in implementation order)

| # | Feature | Issue | Description |
|---|---------|-------|-------------|
| 1 | [Prompt Injection Mitigation](01-prompt-injection-mitigation.md) | #83 | Input sanitization and output validation for LLM pipelines |
| 2 | [Mail Categorization](02-mail-categorization.md) | #120 | Automatic category assignment via Ollama |
| 3 | Mail Summarization | #121 | One-line summaries for inbox list |
| 4 | Smart Reply Drafts | #122 | AI-generated reply suggestions |

## Dependencies

- Feature 1 is the foundation — all LLM features depend on it
- Features 2, 3, 4 each depend on Feature 1
- Features 2, 3, 4 are independent of each other

## Security Considerations

Email is an attacker-controlled input channel. Every message body and subject is untrusted text that will be passed to the LLM. Prompt injection is the primary threat — a malicious email could attempt to manipulate the LLM into producing incorrect classifications, leaking context from other emails, or generating harmful reply drafts. Feature 1 must be implemented before any LLM feature goes live.
