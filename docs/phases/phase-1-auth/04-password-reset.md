# Feature 1.4: Password Reset

## Goal

Allow users to reset their password.

## Deliverables

### API

- `POST /api/auth/request-reset`
  - Request: `{ email }`
  - Generates a time-limited reset token (stored in DB or signed JWT)
  - In production: would send email with reset link
  - In dev: logs token to console / Aspire dashboard (no real email yet)
  - Always returns 200 OK (don't reveal if email exists)
- `POST /api/auth/reset-password`
  - Request: `{ token, newPassword }`
  - Validates token and updates password hash
  - Returns 200 OK on success, 400 on invalid/expired token

### Data Model

- `PasswordResetToken` entity or signed JWT approach (TBD during implementation)
- Tokens expire after 1 hour

### UI — Password Reset Pages

**Request Reset Page** (`/reset-password`)
- Field: Email address
- Submit button
- Success message: "If an account exists, a reset link has been sent"
- Link: "Back to login"

**Reset Password Page** (`/reset-password/{token}`)
- Fields: New Password, Confirm Password
- Submit button
- Success: redirect to login with success message
- Error: invalid/expired token message

### Notes

- No actual email sending in Phase 1 — token is logged for dev use
- Email sending will be added when SMTP is set up in Phase 5
- Consider: for NAS-local single/small-team use, an admin reset via CLI might be sufficient initially

## Acceptance Criteria

- [ ] Reset token is generated and logged in dev
- [ ] Valid token allows password change
- [ ] Expired token is rejected
- [ ] UI flow works end-to-end (request → reset → login)
- [ ] Security: don't reveal whether email exists
- [ ] Unit test: token generation, validation, expiry
