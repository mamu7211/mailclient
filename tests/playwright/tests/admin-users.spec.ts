import { test, expect } from "@playwright/test";

const ADMIN_USERNAME = "admin-playwright";
const ADMIN_PASSWORD = "TestPassword123!";

test.describe.serial("Admin Users table", () => {
  test.beforeEach(async ({ page }) => {
    // Navigate to the app and check if already logged in
    await page.goto("/");
    await page.waitForLoadState("networkidle");

    // If redirected to login, authenticate as admin
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

  test("displays users table with correct columns", async ({ page }) => {
    await page.goto("/admin/user-management");

    // Wait for the table to render (loading spinner should disappear)
    await expect(page.locator(".spinner-border")).toBeHidden({ timeout: 10000 });

    // Table component renders as div[role="table"] with CSS grid
    const table = page.getByRole("table");
    await expect(table).toBeVisible();

    // Header columns are div[role="columnheader"] inside the table
    const headers = table.getByRole("columnheader");
    await expect(headers).toHaveCount(5);

    // Verify at least one data row is visible (rows inside rowgroup, excluding header row)
    const bodyRowgroup = table.locator(".feirb-table-body");
    const rows = bodyRowgroup.getByRole("row");
    await expect(rows.first()).toBeVisible();
  });

  test("shows admin badge for admin user", async ({ page }) => {
    await page.goto("/admin/user-management");
    await expect(page.locator(".spinner-border")).toBeHidden({ timeout: 10000 });

    // Find the row containing the admin username
    const adminRow = page
      .locator(".feirb-table-body .feirb-table-row", {
        hasText: ADMIN_USERNAME,
      });
    await expect(adminRow).toBeVisible();
    await expect(adminRow.locator(".badge", { hasText: "Admin" })).toBeVisible();
  });

  test("action buttons are present for each user", async ({ page }) => {
    await page.goto("/admin/user-management");
    await expect(page.locator(".spinner-border")).toBeHidden({ timeout: 10000 });

    const firstRow = page.locator(".feirb-table-body .feirb-table-row").first();
    await expect(firstRow).toBeVisible();

    // Verify the three icon action buttons exist (edit, reset password, delete)
    const actionButtons = firstRow.locator("button");
    await expect(actionButtons).toHaveCount(3);
  });

  test("delete button is disabled for current user", async ({ page }) => {
    await page.goto("/admin/user-management");
    await expect(page.locator(".spinner-border")).toBeHidden({ timeout: 10000 });

    // Find the row for the logged-in admin user
    const adminRow = page
      .locator(".feirb-table-body .feirb-table-row", {
        hasText: ADMIN_USERNAME,
      });
    await expect(adminRow).toBeVisible();

    // The delete button (third action button) should be disabled
    const deleteButton = adminRow.locator("button").nth(2);
    await expect(deleteButton).toBeDisabled();
  });

  test("create user modal opens from toolbar", async ({ page }) => {
    await page.goto("/admin/user-management");
    await expect(page.locator(".spinner-border")).toBeHidden({ timeout: 10000 });

    // Click the "Create User" toolbar button
    await page.getByRole("button", { name: /Create User/i }).click();

    // Verify the create modal is visible
    await expect(page.locator(".modal")).toBeVisible();
    await expect(page.locator("#createUsername")).toBeVisible();
    await expect(page.locator("#createEmail")).toBeVisible();
    await expect(page.locator("#createPassword")).toBeVisible();
    await expect(page.locator("#createConfirmPassword")).toBeVisible();
  });

  test("edit modal opens when clicking edit button", async ({ page }) => {
    await page.goto("/admin/user-management");
    await expect(page.locator(".spinner-border")).toBeHidden({ timeout: 10000 });

    const firstRow = page.locator(".feirb-table-body .feirb-table-row").first();
    // Click the edit button (first action button)
    await firstRow.locator("button").first().click();

    // Verify the edit modal is visible
    await expect(page.locator(".modal")).toBeVisible();
    await expect(page.locator("#editEmail")).toBeVisible();
  });
});
