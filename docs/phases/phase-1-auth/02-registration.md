# Feature 1.2: Registration

## Goal

Allow new users to create an account.

## Deliverables

### API

- `POST /api/auth/register`
  - Request: `{ username, email, password }`
  - Validates: unique username/email, password strength (min 8 chars)
  - Creates user with hashed password
  - Returns 201 Created (no auto-login, user must log in separately)
  - Returns 409 Conflict if username/email taken

### UI — Create Account Page

- Route: `/register`
- Fields: Username, Email, Password, Confirm Password
- Client-side validation (required fields, password match, min length)
- Server-side error display (duplicate username/email)
- Success: redirect to login page with success message
- Link: "Already have an account? Log in"

### Shared DTOs

- `RegisterRequest` record
- `RegisterResponse` record
- Validation attributes on request DTO

## Acceptance Criteria

- [ ] User can register with unique username + email
- [ ] Duplicate username/email returns clear error message
- [ ] Password is stored as BCrypt hash, never plain text
- [ ] After registration, user is redirected to login
- [ ] Integration test: register + verify user in DB
