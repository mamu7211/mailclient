# Feature 2.5: Main Page

## Goal

Build the authenticated landing page with navigation that serves as the application shell for all future mail features.

## Deliverables

### Layout (`MainLayout.razor`)

- **Navbar on the right side** with:
  - App name / logo at the top
  - Navigation entries for mail features (placeholder for now: Inbox, Sent, Drafts, etc.)
  - Admin section (only visible to admin users): System Settings, User Management
  - User profile / logout at the bottom
  - Language switcher
- **Main content area** on the left
- Responsive design (navbar collapses on mobile)

### Route: `/`

- Requires authentication
- Unauthenticated users → redirect to `/login`
- Placeholder content in main area (will be replaced by inbox in later phases)

### Redirect Logic

- Unauthenticated → `/login`
- Authenticated + no setup complete → `/setup`
- Authenticated → `/` (main page)

### Navigation Entries

**All users:**
- Inbox (placeholder)
- Sent (placeholder)
- Drafts (placeholder)
- Trash (placeholder)

**Admin users (conditional):**
- System Settings → `/admin/settings`
- User Management → `/admin/users`

**Bottom:**
- Username display
- Logout button
- Language switcher

### Design

- Final visual design TBD (potentially via Stitch)
- This feature covers the structural layout and navigation, not the final styling
- Navbar on the right side (confirmed)

## Acceptance Criteria

- [ ] Authenticated users see main page with right-side navbar
- [ ] Unauthenticated users redirected to `/login`
- [ ] Admin users see admin navigation entries
- [ ] Non-admin users see only mail navigation
- [ ] Logout works (clears tokens, redirects to login)
- [ ] Language switcher in navbar
- [ ] Responsive layout (mobile-friendly)
- [ ] i18n for all navigation strings
- [ ] Placeholder content in main area
