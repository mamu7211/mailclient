import { test, expect } from "@playwright/test";

const ADMIN_USERNAME = process.env.ADMIN_USERNAME ?? "admin-playwright";
const ADMIN_PASSWORD = process.env.ADMIN_PASSWORD ?? "TestPassword123!";

async function login(page: import("@playwright/test").Page) {
  await page.goto("/");
  await page.waitForLoadState("networkidle");

  if (
    page.url().includes("/login") ||
    page.url().includes("/setup") ||
    page.url().includes("/error")
  ) {
    await page.goto("/login");
    await expect(page.locator("#username")).toBeVisible({ timeout: 10000 });
    await page.locator("#username").fill(ADMIN_USERNAME);
    await page.locator("#password").fill(ADMIN_PASSWORD);
    await page.getByRole("button", { name: "Log In" }).click();
    await expect(
      page.getByLabel("breadcrumb").getByText("Dashboard"),
    ).toBeVisible({ timeout: 15000 });
  }
}

async function openFirstMessage(page: import("@playwright/test").Page) {
  await page.goto("/mail/inbox");
  await expect(page.locator(".spinner-border")).toBeHidden({ timeout: 10000 });

  const firstCard = page.locator('[data-testid="mail-card"]').first();
  await expect(firstCard).toBeVisible({ timeout: 10000 });
  await firstCard.click();

  await expect(page).toHaveURL(/\/mail\/inbox\/[0-9a-f-]+/);
  await expect(page.locator(".spinner-border")).toBeHidden({ timeout: 10000 });
}

test.describe.serial("Classify single mail", () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
  });

  test("toolbar exposes a Classify dropdown trigger", async ({ page }) => {
    await openFirstMessage(page);

    const trigger = page.locator('[data-testid="classify-dropdown"]');
    await expect(trigger).toBeVisible();
    await expect(trigger).toHaveAttribute("aria-haspopup", "menu");
    await expect(trigger).toHaveAttribute("aria-expanded", "false");
  });

  test("clicking the Classify trigger opens a menu with Preview and Apply", async ({
    page,
  }) => {
    await openFirstMessage(page);

    await page.locator('[data-testid="classify-dropdown"]').click();

    const menu = page.locator('[data-testid="classify-menu"]');
    await expect(menu).toBeVisible();
    await expect(
      menu.locator('[data-testid="classify-menu-preview"]'),
    ).toBeVisible();
    await expect(
      menu.locator('[data-testid="classify-menu-apply"]'),
    ).toBeVisible();
    await expect(
      page.locator('[data-testid="classify-dropdown"]'),
    ).toHaveAttribute("aria-expanded", "true");
  });

  test("clicking outside the menu closes it", async ({ page }) => {
    await openFirstMessage(page);

    await page.locator('[data-testid="classify-dropdown"]').click();
    await expect(page.locator('[data-testid="classify-menu"]')).toBeVisible();

    // Click somewhere clearly outside the dropdown.
    await page
      .locator('[data-testid="message-headers"]')
      .click({ position: { x: 5, y: 5 } });

    await expect(page.locator('[data-testid="classify-menu"]')).toBeHidden();
  });

  test("Preview opens a modal that surfaces a result or a help/error state", async ({
    page,
  }) => {
    await openFirstMessage(page);

    await page.locator('[data-testid="classify-dropdown"]').click();
    await page.locator('[data-testid="classify-menu-preview"]').click();

    // Either the preview modal opens, or (when no rules are configured) the
    // help-dialog opens. One of the two must be reachable from the menu.
    const previewModal = page.locator('[data-testid="classify-preview-modal"]');
    const noRulesModal = page.locator('[data-testid="classify-no-rules-modal"]');

    await expect(previewModal.or(noRulesModal)).toBeVisible({ timeout: 30000 });

    if (await previewModal.isVisible()) {
      // Wait for the API call to finish — spinner disappears.
      await expect(previewModal.locator(".spinner-border")).toBeHidden({
        timeout: 30000,
      });

      // The modal must show either labels (or "no labels" message), or a
      // visible error alert.
      const labels = previewModal.locator(
        '[data-testid="classify-preview-labels"], [data-testid="classify-preview-no-labels"], [data-testid="classify-preview-error"]',
      );
      await expect(labels.first()).toBeVisible();

      await previewModal.getByRole("button", { name: /close/i }).click();
      await expect(previewModal).toBeHidden();
    } else {
      // No-rules dialog must offer the help text and a deep link to settings.
      await expect(
        noRulesModal.locator('[data-testid="classify-no-rules-settings"]'),
      ).toBeVisible();
      await noRulesModal.getByRole("button", { name: /close/i }).click();
      await expect(noRulesModal).toBeHidden();
    }
  });

  test("Apply Now opens a confirmation dialog with Cancel/Confirm", async ({
    page,
  }) => {
    await openFirstMessage(page);

    await page.locator('[data-testid="classify-dropdown"]').click();
    await page.locator('[data-testid="classify-menu-apply"]').click();

    const applyModal = page.locator('[data-testid="classify-apply-modal"]');
    const noRulesModal = page.locator('[data-testid="classify-no-rules-modal"]');

    await expect(applyModal.or(noRulesModal)).toBeVisible({ timeout: 30000 });

    if (await applyModal.isVisible()) {
      await expect(applyModal.locator(".spinner-border")).toBeHidden({
        timeout: 30000,
      });

      // Cancel must dismiss the dialog without firing an apply.
      await applyModal
        .getByRole("button", { name: /cancel|abbrechen|annuler|annulla/i })
        .click();
      await expect(applyModal).toBeHidden();
    } else {
      await noRulesModal.getByRole("button", { name: /close/i }).click();
      await expect(noRulesModal).toBeHidden();
    }
  });
});
