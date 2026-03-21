# Feature 7.3: User Documentation

## Goal

Provide comprehensive end-user documentation for installation, configuration, and daily usage.

## Deliverables

### USER-DOCUMENTATION.md

A single, self-contained documentation file covering:

#### Getting Started

- System requirements (Docker, .NET 10, hardware recommendations)
- Installation steps (clone, build, run via Aspire)
- First-run setup wizard walkthrough
- Default URLs and ports

#### Account Management

- Creating an account (registration)
- Logging in and out
- Password reset flow
- Language preferences

#### Mail Account Setup

- Adding IMAP/SMTP mail accounts
- Connection testing
- Supported mail providers and known configurations
- Troubleshooting connection issues

#### Using the Mail Client

- Reading and searching mail
- Composing, replying, forwarding
- Managing folders
- Attachments

#### AI Features

- Enabling/disabling AI features
- Mail summarization
- Smart reply drafts
- Automatic categorization
- Ollama model requirements

#### Administration

- Data backup and restore (PostgreSQL volume)
- Updating Feirb
- Environment variables and configuration
- Security best practices

## Acceptance Criteria

- [ ] USER-DOCUMENTATION.md exists in project root
- [ ] All current features are documented with step-by-step instructions
- [ ] Screenshots or diagrams included for key workflows
- [ ] Documentation is written in en_US (translations tracked by i18n feature)
- [ ] Troubleshooting section covers common issues
