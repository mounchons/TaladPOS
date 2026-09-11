import { test, expect } from "@playwright/test";
import { loginAsManager, loginAsCashier } from "./helpers/auth";

const MANAGER_ONLY_TEXT = "หน้านี้เปิดให้เฉพาะผู้จัดการ";

test.describe("Role-based access guard (Manager-only screens)", () => {
  // RBAC-01
  test("RBAC-01: cashier sees ManagerOnly fallback on /stock", async ({ page }) => {
    await loginAsCashier(page);
    await page.goto("/stock");
    await expect(page.getByText(MANAGER_ONLY_TEXT)).toBeVisible();
    await expect(page.getByRole("button", { name: "+ เพิ่มสินค้า" })).toHaveCount(0);
  });

  // RBAC-02
  test("RBAC-02: cashier sees ManagerOnly fallback on /promotions", async ({ page }) => {
    await loginAsCashier(page);
    await page.goto("/promotions");
    await expect(page.getByText(MANAGER_ONLY_TEXT)).toBeVisible();
    await expect(page.getByRole("button", { name: "+ สร้างโปรโมชั่น" })).toHaveCount(0);
  });

  // RBAC-03
  test("RBAC-03: cashier sees ManagerOnly fallback on /reports", async ({ page }) => {
    await loginAsCashier(page);
    await page.goto("/reports");
    await expect(page.getByText(MANAGER_ONLY_TEXT)).toBeVisible();
    await expect(page.getByRole("tablist")).toHaveCount(0);
  });

  // RBAC-04
  test("RBAC-04: manager can access all protected routes normally", async ({ page }) => {
    await loginAsManager(page);

    await page.goto("/stock");
    await expect(page.getByRole("heading", { name: "สต็อกสินค้า" })).toBeVisible();

    await page.goto("/promotions");
    await expect(page.getByRole("heading", { name: "โปรโมชั่น" })).toBeVisible();

    await page.goto("/reports");
    await expect(page.getByRole("tablist")).toBeVisible();

    await page.goto("/sales/history");
    await expect(page.getByRole("heading", { name: "ประวัติการขาย" })).toBeVisible();

    await page.goto("/sales");
    await expect(page.getByPlaceholder("สแกนบาร์โค้ด หรือพิมพ์ชื่อสินค้า")).toBeVisible();
  });
});
