# Feature 2.3: User Management

## Goal

Allow admin users to view, create, edit, and delete user accounts.

## Deliverables

### API (admin-only)

- `GET /api/admin/users` — list all users (id, username, email, isAdmin, createdAt)
- `POST /api/admin/users` — create a new user
- `PUT /api/admin/users/{id}` — update user (email, isAdmin)
- `DELETE /api/admin/users/{id}` — delete user
- `POST /api/admin/users/{id}/reset-password` — admin-triggered password reset (generates token, logs or emails it)

### UI — User Management Page (`/admin/users`)

- Table with user list: username, email, admin badge, created date
- Actions per row: edit, delete, reset password
- Create user button → dialog or inline form
- Edit user → dialog or inline form
- Delete → confirmation dialog

### Business Rules

- Admin cannot delete their own account
- Admin cannot remove their own admin flag
- Last remaining admin cannot be demoted (at least one admin must exist)
- Username and email uniqueness enforced (existing validation)

### DTOs (Feirb.Shared)

- `UserListResponse` — list of user summaries
- `CreateUserRequest` — username, email, password, isAdmin
- `UpdateUserRequest` — email, isAdmin

## Acceptance Criteria

- [ ] User list page shows all users
- [ ] Admin can create new users
- [ ] Admin can edit user email and admin status
- [ ] Admin can delete users (with confirmation)
- [ ] Admin can trigger password reset for other users
- [ ] Self-deletion prevented
- [ ] Last-admin demotion prevented
- [ ] Only accessible to admin users (API + UI)
- [ ] i18n for all UI strings
- [ ] Integration tests for all endpoints including edge cases
