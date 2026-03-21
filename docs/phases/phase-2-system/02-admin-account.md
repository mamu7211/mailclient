# Feature 2.2: Admin Account

## Goal

Distinguish admin users from regular users so that system management features are restricted to administrators.

## Deliverables

### Data Model

- Add `IsAdmin` (bool, default `false`) to `User` entity
- EF Core migration
- First admin created via System Setup (#18)

### Auth Service

- Include `IsAdmin` as claim in JWT token generation (`role` or custom claim)
- `AuthService.GenerateTokens()` reads `IsAdmin` from user entity

### Authorization Policy

- Register `RequireAdmin` policy in `Program.cs`
- Policy checks for admin claim in JWT
- Admin endpoint group: `/api/admin/*` protected by this policy

### Frontend

- Parse admin claim from JWT in `JwtAuthenticationStateProvider`
- Conditional navigation: admin users see additional menu entries (settings, user management)
- `[Authorize(Policy = "Admin")]` on admin Blazor pages
- Non-admin users who navigate to admin pages see 403 / redirect

### Rules

- No self-registration as admin
- Only existing admins can promote users (via User Management, #20)
- First admin is created during System Setup (#18)

## Acceptance Criteria

- [ ] `IsAdmin` field on User entity with migration
- [ ] JWT contains admin claim when `IsAdmin = true`
- [ ] `RequireAdmin` authorization policy registered and enforced
- [ ] Admin-only API endpoints return 403 for non-admin users
- [ ] Frontend shows admin navigation only for admin users
- [ ] Unit test: admin claim present/absent in JWT
- [ ] Integration test: admin vs non-admin endpoint access
