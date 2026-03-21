# Feature 1.3: Login

## Goal

Authenticate users and establish a session via JWT.

## Deliverables

### API

- `POST /api/auth/login`
  - Request: `{ username, password }`
  - Validates credentials against stored hash
  - Returns JWT access token + refresh token
  - Returns 401 Unauthorized on invalid credentials
- `POST /api/auth/refresh`
  - Request: `{ refreshToken }`
  - Returns new access token + refresh token
  - Returns 401 if refresh token is invalid/expired

### UI — Login Page

- Route: `/login`
- Layout: centered card, no sidebar/navigation (standalone auth layout)
- **Feirb logo** displayed above the form (SVG, centered)
- Fields: Username, Password
- "Log in" button (primary, full-width)
- Links below the form:
  - "Forgot password?" → `/reset-password`
  - "Create account" → `/register`
- Error display for invalid credentials
- Loading state on submit

### Blazor Auth Integration

- `JwtAuthenticationStateProvider` implementing `AuthenticationStateProvider`
- Token storage in `localStorage`
- `HttpClient` interceptor adding `Authorization: Bearer {token}` header
- Auto-redirect to `/login` when token expires
- Auto-redirect to `/` (dashboard) after successful login

### Auth Layout

- Separate `AuthLayout.razor` — minimal layout without sidebar/nav
- Used by Login, Register, and Password Reset pages
- Centered content, responsive

## Acceptance Criteria

- [ ] User can log in with valid credentials
- [ ] Invalid credentials show error message, no redirect
- [ ] JWT token stored in localStorage after login
- [ ] Authenticated API calls include Bearer token
- [ ] Token refresh works transparently
- [ ] Unauthenticated users are redirected to login
- [ ] Logo displays correctly on login page
- [ ] Links to register and password reset work
- [ ] Integration test: login + access protected endpoint
