# Feature 2.4: Password Reset Mail

## Goal

Send the password reset link via email instead of only logging it to the console.

## Deliverables

### Email Service

- `IEmailService` / `EmailService` using MailKit for SMTP
- Reads SMTP configuration from `SystemSettings` in database
- Generic `SendAsync(to, subject, htmlBody)` method for reuse in future features

### Password Reset Integration

- Modify `POST /api/auth/request-reset` to call email service
- Email contains link: `{baseUrl}/reset-password/{token}`
- Fallback: if no SMTP is configured (pre-setup or dev without Mailpit), log token to console as before

### Email Template

- Simple HTML email with inline CSS
- Feirb branding (app name, primary color)
- Content: greeting, reset link (clickable button), expiry notice (1 hour), "if you didn't request this" disclaimer
- Localized in the user's preferred locale (or system default)

### i18n

- Email subject and body strings in resource files (en, de, fr, it)
- Subject example: "Reset your Feirb password" / "Setze dein Feirb-Passwort zurück"

### Notes

- In dev, Mailpit (localhost:8025) catches all emails — visible in Mailpit web UI
- Base URL for reset link: read from configuration or request origin
- MailKit is already in the project dependencies (for future IMAP/SMTP mail client features)

## Acceptance Criteria

- [ ] Password reset email sent via configured SMTP server
- [ ] Email contains correct, clickable reset link
- [ ] Email body and subject are localized
- [ ] Falls back to console logging if SMTP is not configured
- [ ] Works with Mailpit in dev (email visible in Mailpit UI)
- [ ] HTML email renders correctly in common mail clients
- [ ] Integration test with Mailpit
