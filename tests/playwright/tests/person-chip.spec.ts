import { test, expect } from "@playwright/test";

test.describe("PersonChip component", () => {
  test.beforeEach(async ({ page }) => {
    await page.goto("/components-showcase/person-chip");
    await page.waitForLoadState("networkidle");
  });

  test("renders default chip with name and email", async ({ page }) => {
    const chip = page.locator(".showcase-preview-area .feirb-person-chip").first();
    await expect(chip).toBeVisible();
    await expect(chip.locator(".feirb-person-chip-name")).toHaveText("Alice Johnson");
    await expect(chip.locator(".feirb-person-chip-email")).toHaveText("alice.johnson@example.com");
  });

  test("renders avatar placeholder icon", async ({ page }) => {
    const chip = page.locator(".showcase-preview-area .feirb-person-chip").first();
    const avatar = chip.locator(".feirb-person-chip-avatar");
    await expect(avatar).toBeVisible();
  });

  test("all three sizes render in the All Sizes section", async ({ page }) => {
    const allSizesCard = page.locator(".feirb-card", { hasText: "All Sizes" });
    const chips = allSizesCard.locator(".feirb-person-chip");
    await expect(chips).toHaveCount(3);
  });

  test("mini size hides email", async ({ page }) => {
    const allSizesCard = page.locator(".feirb-card", { hasText: "All Sizes" });
    // Mini is the third chip (Default, Small, Mini)
    const miniChip = allSizesCard.locator(".feirb-person-chip").nth(2);
    await expect(miniChip.locator(".feirb-person-chip-name")).toBeVisible();
    await expect(miniChip.locator(".feirb-person-chip-email")).toHaveCount(0);
  });

  test("default and small sizes show email", async ({ page }) => {
    const allSizesCard = page.locator(".feirb-card", { hasText: "All Sizes" });
    const defaultChip = allSizesCard.locator(".feirb-person-chip").nth(0);
    const smallChip = allSizesCard.locator(".feirb-person-chip").nth(1);
    await expect(defaultChip.locator(".feirb-person-chip-email")).toBeVisible();
    await expect(smallChip.locator(".feirb-person-chip-email")).toBeVisible();
  });

  test("preview updates when name input changes", async ({ page }) => {
    const nameInput = page.locator("input.form-control-sm").first();
    await nameInput.fill("Bob Smith");

    const chip = page.locator(".showcase-preview-area .feirb-person-chip").first();
    await expect(chip.locator(".feirb-person-chip-name")).toHaveText("Bob Smith");
  });
});
