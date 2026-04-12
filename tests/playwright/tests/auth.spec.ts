import { test, expect } from "@playwright/test";

const ADMIN_USERNAME = process.env.ADMIN_USERNAME ?? "admin-playwright";
const ADMIN_EMAIL = process.env.ADMIN_EMAIL ?? "admin@playwright.local";
const ADMIN_PASSWORD = process.env.ADMIN_PASSWORD ?? "TestPassword123!";

const TEST_USERNAME = `e2euser_${Date.now()}`;
const TEST_EMAIL = `${TEST_USERNAME}@feirb.local`;
const TEST_PASSWORD = "TestPassword123!";

test.describe.serial("Auth flows", () => {
  test("setup: creates admin account if needed", async ({ page }) => {
    const statusResponse = await page.request.get("/api/setup/status");
    const status = await statusResponse.json();

    if (status.isComplete) {
      test.skip();
      return;
    }

    // Navigate to the app — SetupGuard should redirect to /setup
    await page.goto("/");
    await page.waitForURL("**/setup");

    // Step 1: Fill admin account details
    await page.locator("#username").fill(ADMIN_USERNAME);
    await page.locator("#email").fill(ADMIN_EMAIL);
    await page.locator("#password").fill(ADMIN_PASSWORD);
    await page.locator("#confirmPassword").fill(ADMIN_PASSWORD);
    await page.getByRole("button", { name: "Next" }).click();

    // Step 2: Fill SMTP configuration (GreenMail dev defaults)
    await page.locator("#smtpHost").fill("localhost");
    await page.locator("#smtpPort").fill("3025");
    await page.locator("#smtpUsername").fill("test@feirb.local");
    await page.locator("#smtpPassword").fill("changeit");

    await page.locator("#smtpTlsMode").selectOption("None");

    const requiresAuthCheckbox = page.locator("#smtpRequiresAuth");
    if (await requiresAuthCheckbox.isChecked()) {
      await requiresAuthCheckbox.uncheck();
    }

    await page.getByRole("button", { name: "Complete Setup" }).click();

    // Step 3: Verify completion page
    await expect(page.getByText("Setup Complete!")).toBeVisible();
    await page.getByRole("link", { name: "Go to Login" }).click();
    await page.waitForURL("**/login?setup=true");
    await expect(page.locator("#username")).toBeVisible();
  });

  test("login: authenticates, reaches dashboard, and logs out", async ({
    page,
    request,
  }) => {
    // Register a fresh test user via API
    const registerResponse = await request.post("/api/auth/register", {
      data: {
        username: TEST_USERNAME,
        email: TEST_EMAIL,
        password: TEST_PASSWORD,
      },
    });
    expect(registerResponse.ok()).toBeTruthy();

    // Log in via UI
    await page.goto("/login");
    await expect(page.locator("#username")).toBeVisible();
    await page.locator("#username").fill(TEST_USERNAME);
    await page.locator("#password").fill(TEST_PASSWORD);
    await page.getByRole("button", { name: "Log In" }).click();

    // Verify redirect to dashboard
    await expect(
      page.getByLabel("breadcrumb").getByText("Dashboard"),
    ).toBeVisible({ timeout: 15000 });

    // Logout
    await page.getByRole("button", { name: "Logout" }).click();
    await page.waitForURL("**/login");
    await expect(page.locator("#username")).toBeVisible();
  });

  test("login: stores only access token in localStorage, not refresh token", async ({
    page,
    request,
  }) => {
    const username = `e2etoken_${Date.now()}`;
    await request.post("/api/auth/register", {
      data: {
        username,
        email: `${username}@feirb.local`,
        password: TEST_PASSWORD,
      },
    });

    await page.goto("/login");
    await page.locator("#username").fill(username);
    await page.locator("#password").fill(TEST_PASSWORD);
    await page.getByRole("button", { name: "Log In" }).click();

    await expect(
      page.getByLabel("breadcrumb").getByText("Dashboard"),
    ).toBeVisible({ timeout: 15000 });

    // Verify AccessToken is stored
    const accessToken = await page.evaluate(() =>
      localStorage.getItem("AccessToken"),
    );
    expect(accessToken).toBeTruthy();

    // Verify RefreshToken is NOT stored (XSS protection)
    const refreshToken = await page.evaluate(() =>
      localStorage.getItem("RefreshToken"),
    );
    expect(refreshToken).toBeNull();

    // Verify no other token-like keys exist
    const allKeys = await page.evaluate(() => Object.keys(localStorage));
    expect(allKeys).not.toContain("RefreshToken");
    expect(allKeys).not.toContain("refreshToken");
  });

  test("logout: clears access token from localStorage", async ({
    page,
    request,
  }) => {
    const username = `e2elogout_${Date.now()}`;
    await request.post("/api/auth/register", {
      data: {
        username,
        email: `${username}@feirb.local`,
        password: TEST_PASSWORD,
      },
    });

    await page.goto("/login");
    await page.locator("#username").fill(username);
    await page.locator("#password").fill(TEST_PASSWORD);
    await page.getByRole("button", { name: "Log In" }).click();

    await expect(
      page.getByLabel("breadcrumb").getByText("Dashboard"),
    ).toBeVisible({ timeout: 15000 });

    // Verify token is present before logout
    const tokenBefore = await page.evaluate(() =>
      localStorage.getItem("AccessToken"),
    );
    expect(tokenBefore).toBeTruthy();

    // Logout
    await page.getByRole("button", { name: "Logout" }).click();
    await page.waitForURL("**/login");

    // Verify token is cleared after logout
    const tokenAfter = await page.evaluate(() =>
      localStorage.getItem("AccessToken"),
    );
    expect(tokenAfter).toBeNull();
  });

  test("login: refresh token is not in API response body", async ({
    request,
  }) => {
    const username = `e2eapi_${Date.now()}`;
    await request.post("/api/auth/register", {
      data: {
        username,
        email: `${username}@feirb.local`,
        password: TEST_PASSWORD,
      },
    });

    const loginResponse = await request.post("/api/auth/login", {
      data: { username, password: TEST_PASSWORD },
    });
    expect(loginResponse.ok()).toBeTruthy();

    const body = await loginResponse.json();
    expect(body.accessToken).toBeTruthy();
    expect(body.expiresAt).toBeTruthy();
    expect(body).not.toHaveProperty("refreshToken");
  });

  test("login: shows error for invalid credentials", async ({ page }) => {
    await page.goto("/login");
    await page.locator("#username").fill("wronguser");
    await page.locator("#password").fill("wrongpassword");
    await page.getByRole("button", { name: "Log In" }).click();

    await expect(page.locator(".alert-danger")).toBeVisible();
  });
});
