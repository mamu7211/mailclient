# Phase 1: Auth & User Management

## Features (in implementation order)

| # | Feature | Description |
|---|---------|-------------|
| 1 | [Auth Infrastructure](01-auth-infrastructure.md) | User entity, EF Core migration, JWT service, auth middleware |
| 2 | [Registration](02-registration.md) | Create account page and API endpoint |
| 3 | [Login](03-login.md) | Login page with logo, credentials, and navigation links |
| 4 | [Password Reset](04-password-reset.md) | Password reset flow |

## Dependencies

- Phase 0 (Foundation) must be complete — Aspire skeleton running with PostgreSQL
- Feature 1 is the foundation for all other features in this phase
- Feature 2 must come before Feature 3 (need users to log in)
- Feature 4 requires Feature 1 + 2
