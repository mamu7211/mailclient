---
name: implement-playwright-tests
description: Create and run Playwright E2E tests for Blazor pages
user_invocable: true
args: page_or_feature
---

# Implement Playwright Tests

Create and run Playwright end-to-end tests for Blazor pages and features.

## Steps

### Phase 1: Understand the Target

1. **If a page or feature is supplied**, locate the relevant `.razor` file(s) and read them to understand:
   - The route (`@page` directive)
   - Authorization requirements (`@attribute [Authorize]` vs `[AllowAnonymous]`)
   - Data loaded on init (API calls in `OnInitializedAsync`)
   - User interactions (buttons, modals, forms, navigation)
   - UI components used (Table, ContentSection, Button, etc.)

2. **If no target is supplied**, ask the user what page or feature to test.

3. **Determine the test type:**
   - **Component/showcase test** — target is a UI component that has a showcase page at `/components-showcase/{section}`. No auth needed. Prefer this for testing component rendering, sizes, variants, and interactive behavior.
   - **Page test** — target is an app page (e.g., `/settings/labels`). Requires auth flow.

### Phase 2: Explore the UI with `/test-ui`

Before writing tests, use `/test-ui <path> explore the page` to interactively explore the target page in the browser. This helps you:
- See how components actually render (DOM structure, classes, attributes)
- Discover the right selectors by inspecting the accessibility tree (`browser_snapshot`)
- Understand interaction flows (what happens when you click, toggle, fill)

### Phase 3: Understand the UI Components

Before writing selectors, check how the UI components actually render. The shared component library (`src/Feirb.Web/Components/UI/`) uses custom elements, not native HTML:

- **Card** renders as `<div class="feirb-card">`, NOT Bootstrap's `.card`
- **Table** renders as `<div role="table" class="feirb-table">` with CSS grid, NOT `<table>`
  - Headers: `<div role="columnheader">` inside `.feirb-table-header`
  - Body rows: `<div role="row" class="feirb-table-row">` inside `.feirb-table-body`
  - Cells: `<div role="cell">` inside rows
- **Button** renders as `<button class="btn btn-{variant}">` — standard Bootstrap
- **ContentSection** wraps content in a styled container
- **Modals** use Bootstrap `.modal` classes

**Blazor CSS isolation gotcha:** Blazor rewrites scoped CSS classes with unique suffixes (e.g., `.feirb-person-chip-default` becomes `.feirb-person-chip-default[b-abc123]`). These scoped classes are NOT directly queryable from Playwright. Instead:
- Use **unscoped classes** (classes defined in global CSS or on the component's root element)
- Use **positional selectors** like `.nth(0)`, `.nth(1)` when differentiating variants
- Use **`hasText`** filters to find the right container, then query children within it

If unsure about a component's rendered HTML, read the `.razor` file in `src/Feirb.Web/Components/UI/` before writing selectors.

**Components Showcase:** The app has a live showcase at `/components-showcase` (`[AllowAnonymous]`) with per-component routes (`/components-showcase/button`, `/components-showcase/table`, etc.). Source: `src/Feirb.Web/Pages/ComponentsShowcase.razor` with individual sections in `src/Feirb.Web/Pages/Showcase/`. Use the showcase to:
- Test component rendering, variants, and interactive behavior without auth
- Verify how a component actually renders in the browser when selectors aren't working
- Debug visual regressions in isolation

### Phase 3: Write the Test

1. **Create a new test file** at `tests/playwright/tests/{feature-name}.spec.ts`

2. **For component/showcase tests** (no auth needed):
   ```typescript
   test.describe("ComponentName component", () => {
     test.beforeEach(async ({ page }) => {
       await page.goto("/components-showcase/{section}");
       await page.waitForLoadState("networkidle");
     });

     test("renders correctly", async ({ page }) => {
       // assertions
     });
   });
   ```

3. **For page tests** (auth required), use `test.describe.serial()` and this login helper in `beforeEach`:
   ```typescript
   const ADMIN_USERNAME = "admin-playwright";
   const ADMIN_PASSWORD = "TestPassword123!";

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

5. **Selector preferences** (in order):
   - ARIA roles: `page.getByRole("button", { name: "..." })`
   - Component CSS classes: `.feirb-card`, `.feirb-table-body .feirb-table-row`
   - Text filters: `page.locator(".feirb-card", { hasText: "Section Title" })`
   - Positional: `.locator(".feirb-person-chip").nth(0)` for variant differentiation
   - IDs: `page.locator("#fieldId")`
   - Avoid native HTML tag selectors for custom components

### Phase 4: Run the Tests

1. **Ensure dependencies are installed:**
   ```bash
   cd tests/playwright && npm install
   ```

2. **For page tests**, run auth setup first (required on fresh database):
   ```bash
   npx playwright test auth.spec.ts
   ```

3. **Run the new test file:**
   ```bash
   npx playwright test {feature-name}.spec.ts
   ```

4. **If tests fail**, read the screenshot in `test-results/` to see what the page actually looked like. Fix selectors or timing and re-run.

## Notes

- The app must be running on `https://localhost:7272` (via `dotnet run --project src/Feirb.AppHost`)
- Playwright config: `tests/playwright/playwright.config.ts` (single Chromium worker, no parallelism)
- `auth.spec.ts` must run before any test that requires authentication
- Test credentials: `admin-playwright` / `TestPassword123!` (created by `auth.spec.ts` setup)
