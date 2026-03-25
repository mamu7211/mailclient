import { test, expect } from "@playwright/test";

const TEST_USERNAME = `e2euser_${Date.now()}`;
const TEST_EMAIL = `${TEST_USERNAME}@feirb.local`;
const TEST_PASSWORD = "TestPassword123!";

test.describe("Login and logout flow", () => {
  test.beforeAll(async ({ request }) => {
    // Ensure setup is complete
    const statusResponse = await request.get("/api/setup/status");
    const status = await statusResponse.json();
    test.skip(!status.isComplete, "Setup not completed — run setup.spec.ts first");

    // Register a fresh test user via API
    const registerResponse = await request.post("/api/auth/register", {
      data: {
        username: TEST_USERNAME,
        email: TEST_EMAIL,
        password: TEST_PASSWORD,
      },
    });
    expect(registerResponse.ok()).toBeTruthy();
  });

  test("logs in, reaches dashboard, and logs out", async ({ page }) => {
    await page.goto("/login");
    await expect(page.locator("#username")).toBeVisible();

    // Fill login form
    await page.locator("#username").fill(TEST_USERNAME);
    await page.locator("#password").fill(TEST_PASSWORD);

    // Submit login
    await page.getByRole("button", { name: "Log In" }).click();

    // Verify redirect to dashboard (Blazor WASM client-side navigation)
    await expect(page.getByLabel("breadcrumb").getByText("Dashboard")).toBeVisible({ timeout: 15000 });

    // Click logout button in the nav menu
    await page.getByRole("button", { name: "Logout" }).click();

    // Verify redirect to login page
    await page.waitForURL("**/login");
    await expect(page.locator("#username")).toBeVisible();
  });

  test("shows error for invalid credentials", async ({ page }) => {
    await page.goto("/login");

    await page.locator("#username").fill("wronguser");
    await page.locator("#password").fill("wrongpassword");
    await page.getByRole("button", { name: "Log In" }).click();

    // Verify error message appears
    await expect(page.locator(".alert-danger")).toBeVisible();
  });
});
