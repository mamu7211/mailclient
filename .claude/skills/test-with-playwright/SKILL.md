---
name: test-with-playwright
description: Create and run Playwright E2E tests for Blazor pages
user_invocable: true
args: page_or_feature
---

# Test with Playwright

Create and run Playwright end-to-end tests for Blazor pages and features.

## Steps

### Phase 1: Understand the Target

1. **If a page or feature is supplied**, locate the relevant `.razor` file(s) and read them to understand:
   - The route (`@page` directive)
   - Authorization requirements (`@attribute [Authorize]`)
   - Data loaded on init (API calls in `OnInitializedAsync`)
   - User interactions (buttons, modals, forms, navigation)
   - UI components used (Table, ContentSection, Button, etc.)

2. **If no target is supplied**, ask the user what page or feature to test.

### Phase 2: Understand the UI Components

Before writing selectors, check how the UI components actually render. The shared component library (`src/Feirb.Web/Components/UI/`) uses custom elements, not native HTML:

- **Table** renders as `<div role="table" class="feirb-table">` with CSS grid, NOT `<table>`
  - Headers: `<div role="columnheader">` inside `.feirb-table-header`
  - Body rows: `<div role="row" class="feirb-table-row">` inside `.feirb-table-body`
  - Cells: `<div role="cell">` inside rows
- **Button** renders as `<button class="btn btn-{variant}">` — standard Bootstrap
- **ContentSection** wraps content in a styled container
- **Modals** use Bootstrap `.modal` classes

If unsure about a component's rendered HTML, read the `.razor` file in `src/Feirb.Web/Components/UI/` before writing selectors.

### Phase 3: Write the Test

1. **Create a new test file** at `tests/playwright/tests/{feature-name}.spec.ts`

2. **Follow existing patterns** from `tests/playwright/tests/auth.spec.ts`:
   - Use `test.describe.serial()` for tests that share state
   - Use `beforeEach` for login flow
   - Use role-based and class-based selectors, not native HTML tag selectors

3. **Login helper pattern** — use this in `beforeEach` to handle both fresh and existing sessions:
   ```typescript
   test.beforeEach(async ({ page }) => {
     await page.goto("/");
     await page.waitForLoadState("networkidle");

     if (page.url().includes("/login") || page.url().includes("/setup")) {
       await page.goto("/login");
       await expect(page.locator("#username")).toBeVisible({ timeout: 10000 });
       await page.locator("#username").fill(ADMIN_USERNAME);
       await page.locator("#password").fill(ADMIN_PASSWORD);
       await page.getByRole("button", { name: "Log In" }).click();
       await expect(
         page.getByLabel("breadcrumb").getByText("Dashboard"),
       ).toBeVisible({ timeout: 15000 });
     }
   });
   ```

4. **Wait for loading spinners** before asserting content:
   ```typescript
   await expect(page.locator(".spinner-border")).toBeHidden({ timeout: 10000 });
   ```

5. **Test credentials** — the `auth.spec.ts` setup wizard creates:
   - Admin: `admin-playwright` / `TestPassword123!`
   - These only exist if `auth.spec.ts` setup has run on a fresh (non-seeded) database

6. **Selector preferences** (in order):
   - ARIA roles: `page.getByRole("table")`, `page.getByRole("button", { name: "..." })`
   - Component CSS classes: `.feirb-table-body .feirb-table-row`
   - IDs: `page.locator("#createUsername")`
   - Text content: `page.locator(".badge", { hasText: "Admin" })`
   - Avoid native HTML tag selectors for custom components

### Phase 4: Run the Tests

1. **Ensure dependencies are installed:**
   ```bash
   cd tests/playwright && npm install
   ```

2. **Run auth setup first** (required on fresh database):
   ```bash
   npx playwright test auth.spec.ts
   ```

3. **Run the new test file:**
   ```bash
   npx playwright test {feature-name}.spec.ts
   ```

4. **If tests fail**, check the screenshot in `test-results/` to diagnose the issue. Read the screenshot file to see what the page actually looked like.

### Phase 5: Report Results

1. Report pass/fail counts
2. For failures: read the screenshot, diagnose the root cause, fix selectors or timing, and re-run
3. Iterate until all tests pass

## Notes

- The app must be running on `https://localhost:7272` (via `dotnet run --project src/Feirb.AppHost`)
- Do NOT start the app with seeding or `--auto-login` — Playwright tests expect a fresh setup flow
- Playwright config: `tests/playwright/playwright.config.ts` (single Chromium worker, no parallelism)
- `auth.spec.ts` must run before any test that requires authentication
- Tests for admin pages require the `admin-playwright` user which is an admin
