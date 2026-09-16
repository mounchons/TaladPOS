import { expect, type Page } from "@playwright/test";

export const MANAGER = { username: "manager", password: "Manager123!" };
export const CASHIER = { username: "cashier", password: "Cashier123!" };

export async function login(page: Page, credentials: { username: string; password: string }) {
  await page.goto("/login");
  await page.getByRole("textbox", { name: "ชื่อผู้ใช้" }).fill(credentials.username);
  await page.getByRole("textbox", { name: "รหัสผ่าน" }).fill(credentials.password);
  await page.getByRole("button", { name: "เข้าสู่ระบบ" }).click();
  await expect(page).toHaveURL(/\/sales$/);
}

export function loginAsManager(page: Page) {
  return login(page, MANAGER);
}

export function loginAsCashier(page: Page) {
  return login(page, CASHIER);
}

/** สุ่มค่าให้ไม่ชนกันเมื่อรันเทสซ้ำ ๆ กับข้อมูลจริงบน dev DB */
export function uniqueSuffix(): string {
  return Date.now().toString(36) + Math.random().toString(36).slice(2, 6);
}
