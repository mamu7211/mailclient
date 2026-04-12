import { test, expect } from "@playwright/test";

const ADMIN_USERNAME = process.env.ADMIN_USERNAME ?? "admin-playwright";
const ADMIN_PASSWORD = process.env.ADMIN_PASSWORD ?? "TestPassword123!";

test.describe.serial("Message Detail View", () => {
  test.beforeEach(async ({ page }) => {
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
  });

  test("navigates from inbox to message detail", async ({ page }) => {
    await page.goto("/mail/inbox");
    await expect(page.locator(".spinner-border")).toBeHidden({
      timeout: 10000,
    });

    // Click first mail card to navigate to detail
    const firstCard = page.locator("a.feirb-mail-card").first();
    await expect(firstCard).toBeVisible({ timeout: 10000 });
    await firstCard.click();

    // Should navigate to message detail route
    await expect(page).toHaveURL(/\/mail\/inbox\/[0-9a-f-]+/);

    // Loading spinner should disappear
    await expect(page.locator(".spinner-border")).toBeHidden({
      timeout: 10000,
    });
  });

  test("displays mailbox chip and relative time", async ({ page }) => {
    await page.goto("/mail/inbox");
    await expect(page.locator(".spinner-border")).toBeHidden({
      timeout: 10000,
    });

    const firstCard = page.locator("a.feirb-mail-card").first();
    await expect(firstCard).toBeVisible({ timeout: 10000 });
    await firstCard.click();
    await expect(page.locator(".spinner-border")).toBeHidden({
      timeout: 10000,
    });

    // Meta bar with mailbox chip and date
    const metaBar = page.locator('[data-testid="message-meta"]');
    await expect(metaBar).toBeVisible();

    // MailboxChip renders with .feirb-mailbox-chip class
    await expect(metaBar.locator(".feirb-mailbox-chip")).toBeVisible();

    // Relative time display with clock icon
    await expect(metaBar.locator(".feirb-detail-date")).toBeVisible();
  });

  test("displays sender as PersonChip with name and email", async ({
    page,
  }) => {
    await page.goto("/mail/inbox");
    await expect(page.locator(".spinner-border")).toBeHidden({
      timeout: 10000,
    });

    const firstCard = page.locator("a.feirb-mail-card").first();
    await expect(firstCard).toBeVisible({ timeout: 10000 });
    await firstCard.click();
    await expect(page.locator(".spinner-border")).toBeHidden({
      timeout: 10000,
    });

    // Headers section
    const headers = page.locator('[data-testid="message-headers"]');
    await expect(headers).toBeVisible();

    // From PersonChip (first/largest one in the header section)
    const fromChip = headers.locator(".feirb-person-chip").first();
    await expect(fromChip).toBeVisible();

    // PersonChip should have a name element
    await expect(
      fromChip.locator(".feirb-person-chip-name"),
    ).not.toBeEmpty();

    // PersonChip should have an email element
    await expect(
      fromChip.locator(".feirb-person-chip-email"),
    ).not.toBeEmpty();
  });

  test("displays To recipients as PersonChips", async ({ page }) => {
    await page.goto("/mail/inbox");
    await expect(page.locator(".spinner-border")).toBeHidden({
      timeout: 10000,
    });

    const firstCard = page.locator("a.feirb-mail-card").first();
    await expect(firstCard).toBeVisible({ timeout: 10000 });
    await firstCard.click();
    await expect(page.locator(".spinner-border")).toBeHidden({
      timeout: 10000,
    });

    const headers = page.locator('[data-testid="message-headers"]');

    // There should be at least 2 PersonChips total (From + at least one To)
    const chips = headers.locator(".feirb-person-chip");
    const count = await chips.count();
    expect(count).toBeGreaterThanOrEqual(2);
  });

  test("displays message body in a content section", async ({ page }) => {
    await page.goto("/mail/inbox");
    await expect(page.locator(".spinner-border")).toBeHidden({
      timeout: 10000,
    });

    const firstCard = page.locator("a.feirb-mail-card").first();
    await expect(firstCard).toBeVisible({ timeout: 10000 });
    await firstCard.click();
    await expect(page.locator(".spinner-border")).toBeHidden({
      timeout: 10000,
    });

    // Body section should be visible
    const bodySection = page.locator('[data-testid="message-body"]');
    await expect(bodySection).toBeVisible();

    // Should have either HTML or plain text body content (or "no content" message)
    const htmlBody = bodySection.locator(".feirb-message-body-html");
    const plainBody = bodySection.locator(".feirb-message-body-plain");
    const noContent = bodySection.locator(".text-muted");

    const hasHtml = (await htmlBody.count()) > 0;
    const hasPlain = (await plainBody.count()) > 0;
    const hasNoContent = (await noContent.count()) > 0;
    expect(hasHtml || hasPlain || hasNoContent).toBeTruthy();
  });

  test("HTML/plain text toggle appears when both formats exist", async ({
    page,
  }) => {
    await page.goto("/mail/inbox");
    await expect(page.locator(".spinner-border")).toBeHidden({
      timeout: 10000,
    });

    const firstCard = page.locator("a.feirb-mail-card").first();
    await expect(firstCard).toBeVisible({ timeout: 10000 });
    await firstCard.click();
    await expect(page.locator(".spinner-border")).toBeHidden({
      timeout: 10000,
    });

    const bodySection = page.locator('[data-testid="message-body"]');
    const toggleGroup = bodySection.locator(".feirb-toggle-group");

    // Toggle may or may not be visible depending on whether message has both formats
    if ((await toggleGroup.count()) > 0) {
      await expect(toggleGroup).toBeVisible();

      // Should have exactly 2 options: HTML and Plain Text
      const buttons = toggleGroup.getByRole("radio");
      await expect(buttons).toHaveCount(2);

      // HTML should be selected by default
      const htmlButton = toggleGroup.getByRole("radio", { name: "HTML" });
      await expect(htmlButton).toHaveAttribute("aria-checked", "true");

      // Click Plain Text
      const plainButton = toggleGroup.getByRole("radio", {
        name: "Plain Text",
      });
      await plainButton.click();
      await expect(plainButton).toHaveAttribute("aria-checked", "true");

      // Plain text body should now be visible
      await expect(
        bodySection.locator(".feirb-message-body-plain"),
      ).toBeVisible();

      // Switch back to HTML
      await htmlButton.click();
      await expect(htmlButton).toHaveAttribute("aria-checked", "true");
      await expect(
        bodySection.locator(".feirb-message-body-html"),
      ).toBeVisible();
    }
  });

  test("attachments section displays with file type icons", async ({
    page,
  }) => {
    // Navigate to inbox and find any message
    await page.goto("/mail/inbox");
    await expect(page.locator(".spinner-border")).toBeHidden({
      timeout: 10000,
    });

    const firstCard = page.locator("a.feirb-mail-card").first();
    await expect(firstCard).toBeVisible({ timeout: 10000 });
    await firstCard.click();
    await expect(page.locator(".spinner-border")).toBeHidden({
      timeout: 10000,
    });

    // Attachments section is conditional — only shown when message has attachments
    const attachments = page.locator('[data-testid="message-attachments"]');

    if ((await attachments.count()) > 0) {
      await expect(attachments).toBeVisible();

      // Each attachment card should have an icon and file info
      const cards = attachments.locator(".feirb-attachment-card");
      const cardCount = await cards.count();
      expect(cardCount).toBeGreaterThan(0);

      // First attachment card should have name and size
      const firstAttachment = cards.first();
      await expect(
        firstAttachment.locator(".feirb-attachment-name"),
      ).not.toBeEmpty();
      await expect(
        firstAttachment.locator(".feirb-attachment-meta"),
      ).not.toBeEmpty();
      await expect(
        firstAttachment.locator(".feirb-attachment-icon"),
      ).toBeVisible();
    }
  });

  test("relative time has absolute tooltip", async ({ page }) => {
    await page.goto("/mail/inbox");
    await expect(page.locator(".spinner-border")).toBeHidden({
      timeout: 10000,
    });

    const firstCard = page.locator("a.feirb-mail-card").first();
    await expect(firstCard).toBeVisible({ timeout: 10000 });
    await firstCard.click();
    await expect(page.locator(".spinner-border")).toBeHidden({
      timeout: 10000,
    });

    // The date element should have a title attribute with the absolute date
    const dateElement = page.locator(".feirb-detail-date");
    await expect(dateElement).toBeVisible();
    const title = await dateElement.getAttribute("title");
    expect(title).toBeTruthy();
    // Title should contain a date-like string (e.g., "April 10, 2026")
    expect(title!.length).toBeGreaterThan(5);
  });

  test("breadcrumb shows sender and subject", async ({ page }) => {
    await page.goto("/mail/inbox");
    await expect(page.locator(".spinner-border")).toBeHidden({
      timeout: 10000,
    });

    const firstCard = page.locator("a.feirb-mail-card").first();
    await expect(firstCard).toBeVisible({ timeout: 10000 });
    await firstCard.click();
    await expect(page.locator(".spinner-border")).toBeHidden({
      timeout: 10000,
    });

    // Breadcrumb last segment should have the sender name and subject (set via BreadcrumbOverrideService)
    const breadcrumb = page.getByLabel("breadcrumb");
    await expect(breadcrumb).toBeVisible();

    // Should contain the em dash separator between sender and subject
    const breadcrumbText = await breadcrumb.textContent();
    expect(breadcrumbText).toContain("\u2014");
  });

  test("content sections use proper layout structure", async ({ page }) => {
    await page.goto("/mail/inbox");
    await expect(page.locator(".spinner-border")).toBeHidden({
      timeout: 10000,
    });

    const firstCard = page.locator("a.feirb-mail-card").first();
    await expect(firstCard).toBeVisible({ timeout: 10000 });
    await firstCard.click();
    await expect(page.locator(".spinner-border")).toBeHidden({
      timeout: 10000,
    });

    // Page should have at least 2 ContentSections (headers + body)
    const sections = page.locator(".content-section");
    const count = await sections.count();
    expect(count).toBeGreaterThanOrEqual(2);

    // Each section should have a header with icon and heading
    const firstSection = sections.first();
    await expect(
      firstSection.locator(".content-section-header"),
    ).toBeVisible();
    await expect(firstSection.locator(".content-section-body")).toBeVisible();
  });
});
