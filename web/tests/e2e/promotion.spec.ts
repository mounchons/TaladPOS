import { test, expect } from "@playwright/test";

const MANAGER_USERNAME = "manager";
const MANAGER_PASSWORD = "Manager123!";

test.describe("Promotions - Manager", () => {
  test("creates a 90% discount promotion on a product", async ({ page }) => {
    await page.goto("/login");
    await page.getByRole("textbox", { name: "ชื่อผู้ใช้" }).fill(MANAGER_USERNAME);
    await page.getByRole("textbox", { name: "รหัสผ่าน" }).fill(MANAGER_PASSWORD);
    await page.getByRole("button", { name: "เข้าสู่ระบบ" }).click();
    await expect(page).toHaveURL(/\/sales$/);

    await page.goto("/promotions");
    await page.getByRole("button", { name: "+ สร้างโปรโมชั่น" }).click();

    const dialog = page.getByRole("dialog");
    await expect(dialog).toBeVisible();

    // ขอบเขต = รายสินค้า (default), เลือกสินค้าตัวแรกในรายการ
    const productSelect = dialog.getByRole("combobox").nth(1);
    const productOptions = await productSelect.locator("option").allTextContents();
    const productName = productOptions.find((name) => name.trim() !== "เลือกสินค้า");
    expect(productName).toBeTruthy();
    await productSelect.selectOption({ label: productName! });

    await dialog.getByRole("spinbutton").fill("90");

    const endDate = dialog.getByRole("textbox").nth(1);
    await endDate.fill("2026-12-31");

    // นับแถวที่ตรงเงื่อนไขก่อนบันทึก เผื่อมีโปรโมชั่นซ้ำสภาพนี้อยู่แล้วจากรันครั้งก่อน
    const matchingRows = page.getByRole("row").filter({ hasText: productName! }).filter({ hasText: "90%" });
    const countBefore = await matchingRows.count();

    await dialog.getByRole("button", { name: "บันทึก" }).click();
    await expect(dialog).toBeHidden();

    await expect(matchingRows).toHaveCount(countBefore + 1);
    await expect(matchingRows.last().getByText("ใช้อยู่")).toBeVisible();
  });

  test("rejects a discount percentage above 100", async ({ page }) => {
    await page.goto("/login");
    await page.getByRole("textbox", { name: "ชื่อผู้ใช้" }).fill(MANAGER_USERNAME);
    await page.getByRole("textbox", { name: "รหัสผ่าน" }).fill(MANAGER_PASSWORD);
    await page.getByRole("button", { name: "เข้าสู่ระบบ" }).click();
    await expect(page).toHaveURL(/\/sales$/);

    await page.goto("/promotions");
    await page.getByRole("button", { name: "+ สร้างโปรโมชั่น" }).click();

    const dialog = page.getByRole("dialog");
    const discountInput = dialog.getByRole("spinbutton");
    await discountInput.fill("150");

    // HTML max="100" constraint on the input itself
    await expect(discountInput).toHaveAttribute("max", "100");
    const isValid = await discountInput.evaluate((el: HTMLInputElement) => el.checkValidity());
    expect(isValid).toBe(false);
  });
});
