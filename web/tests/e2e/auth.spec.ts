import { test, expect } from "@playwright/test";
import { loginAsManager, loginAsCashier } from "./helpers/auth";

test.describe("Authentication", () => {
  // AUTH-01: Manager login สำเร็จ -> ไป /sales, เมนูครบ 5 ลิงก์
  test("AUTH-01: manager login shows all 5 nav links", async ({ page }) => {
    await loginAsManager(page);
    await expect(page.getByRole("link", { name: "ขายสินค้า" })).toBeVisible();
    await expect(page.getByRole("link", { name: "ประวัติการขาย" })).toBeVisible();
    await expect(page.getByRole("link", { name: "จัดการสต็อก" })).toBeVisible();
    await expect(page.getByRole("link", { name: "โปรโมชั่น" })).toBeVisible();
    await expect(page.getByRole("link", { name: "รายงาน" })).toBeVisible();
  });

  // AUTH-02: Cashier login สำเร็จ -> ไป /sales, เมนูมีแค่ 2 ลิงก์ (ไม่มี manager-only)
  test("AUTH-02: cashier login shows only sales + history nav links", async ({ page }) => {
    await loginAsCashier(page);
    await expect(page.getByRole("link", { name: "ขายสินค้า" })).toBeVisible();
    await expect(page.getByRole("link", { name: "ประวัติการขาย" })).toBeVisible();
    await expect(page.getByRole("link", { name: "จัดการสต็อก" })).toHaveCount(0);
    await expect(page.getByRole("link", { name: "โปรโมชั่น" })).toHaveCount(0);
    await expect(page.getByRole("link", { name: "รายงาน" })).toHaveCount(0);
  });

  // AUTH-03: username/password ผิด -> ข้อความแจ้งเตือน ค้างที่ /login
  test("AUTH-03: invalid credentials show an inline error and stay on /login", async ({ page }) => {
    await page.goto("/login");
    await page.getByRole("textbox", { name: "ชื่อผู้ใช้" }).fill("manager");
    await page.getByRole("textbox", { name: "รหัสผ่าน" }).fill("wrong-password");
    await page.getByRole("button", { name: "เข้าสู่ระบบ" }).click();

    await expect(page.getByText("ชื่อผู้ใช้หรือรหัสผ่านไม่ถูกต้อง")).toBeVisible();
    await expect(page).toHaveURL(/\/login$/);
  });

  // AUTH-04: Logout -> กลับ /login, session ถูกล้าง (เข้าหน้า protected ซ้ำต้อง login ใหม่)
  test("AUTH-04: logout clears the session and redirects to /login", async ({ page }) => {
    await loginAsManager(page);
    await page.getByRole("button", { name: "ออกจากระบบ" }).click();
    await expect(page).toHaveURL(/\/login$/);

    await page.goto("/sales");
    await expect(page).toHaveURL(/\/login$/);
  });

  // AUTH-05: เข้าหน้า protected โดยไม่ login -> เด้งไป /login
  test("AUTH-05: visiting a protected route while logged out redirects to /login", async ({ page }) => {
    await page.goto("/stock");
    await expect(page).toHaveURL(/\/login$/);
  });

  // AUTH-06: ฟอร์ม login เว้นว่าง -> submit ไม่ผ่าน (native required)
  test("AUTH-06: empty login form is blocked by native required validation", async ({ page }) => {
    await page.goto("/login");
    const usernameInput = page.getByRole("textbox", { name: "ชื่อผู้ใช้" });
    const isValid = await usernameInput.evaluate((el: HTMLInputElement) => el.checkValidity());
    expect(isValid).toBe(false);
    await expect(page).toHaveURL(/\/login$/);
  });
});
