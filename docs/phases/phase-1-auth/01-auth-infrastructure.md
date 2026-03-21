# Feature 1.1: Auth Infrastructure

## Goal

Set up the backend authentication foundation that all other auth features build on.

## Deliverables

### Data Model

- `User` entity: Id (Guid), Username, Email, PasswordHash, CreatedAt, UpdatedAt
- EF Core `FeirbDbContext` with User DbSet
- Initial migration creating the Users table
- Unique constraints on Username and Email

### Auth Service

- `IAuthService` / `AuthService`
- Password hashing (BCrypt via `BCrypt.Net-Next`)
- JWT token generation (access token + refresh token)
- Token validation and refresh logic

### API

- JWT bearer authentication middleware registered in `Program.cs`
- All `/api/*` endpoints protected by default (except `/api/auth/*`)
- `[AllowAnonymous]` on auth endpoints

### Configuration

- JWT settings in `appsettings.json` (Issuer, Audience, Key, Expiry)
- Secrets via ASP.NET User Secrets / Aspire environment variables
- ASP.NET Data Protection API initialized (key persistence for credential encryption in Phase 2)

## Acceptance Criteria

- [ ] `FeirbDbContext` registered and migrations run on startup
- [ ] JWT tokens can be generated and validated
- [ ] Unauthenticated requests to protected endpoints return 401
- [ ] Unit tests for password hashing and JWT generation
