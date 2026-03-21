# Phase 2: System Setup & Administration

## Features (in implementation order)

| # | Feature | Issue | Description |
|---|---------|-------|-------------|
| 1 | [System Setup](01-system-setup.md) | #18 | Initial setup wizard: admin account creation, SMTP configuration |
| 2 | [Admin Account](02-admin-account.md) | #19 | IsAdmin flag, admin authorization policy, JWT admin claim |
| 3 | [User Management](03-user-management.md) | #20 | Admin page for managing user accounts |
| 4 | [Password Reset Mail](04-password-reset-mail.md) | #21 | Send reset link via SMTP instead of console log |
| 5 | [Main Page](05-main-page.md) | #22 | Authenticated landing page with right-side navbar |

## Dependencies

- Phase 1 (Auth) must be complete — login, registration, password reset working
- Feature 1 is the foundation for all other features in this phase
- Feature 2 depends on Feature 1 (first admin created during setup)
- Feature 3 depends on Feature 2 (admin role required)
- Feature 4 depends on Feature 1 (SMTP configuration from setup)
- Feature 5 depends on Feature 2 (admin navigation entries)
