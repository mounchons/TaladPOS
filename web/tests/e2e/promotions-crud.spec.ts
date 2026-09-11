import { test, expect } from "@playwright/test";
import { loginAsManager } from "./helpers/auth";

// Manager-only CRUD บนหน้า /promotions (spec 001 FR-019..022, FR-029)
test.describe("Promotions CRUD - Manager", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsManager(page);
    await page.goto("/promotions");
  });

  // PROMO-01: สร้างโปรฯ scope=รายสินค้า ส่วนลดถูกต้อง (90%)
  test("PROMO-01: creates an item-scope promotion with a valid discount", async ({ page }) => {
    await page.getByRole("button", { name: "+ สร้างโปรโมชั่น" }).click();
    const dialog = page.getByRole("dialog");
    await expect(dialog).toBeVisible();

    const productSelect = dialog.getByRole("combobox").nth(1);
    const productOptions = await productSelect.locator("option").allTextContents();
    const productName = productOptions.find((name) => name.trim() !== "เลือกสินค้า");
    expect(productName).toBeTruthy();
    await productSelect.selectOption({ label: productName! });

    await dialog.getByRole("spinbutton").fill("90");
    await dialog.getByRole("textbox").nth(1).fill("2026-12-31");

    const matchingRows = page.getByRole("row").filter({ hasText: productName! }).filter({ hasText: "90%" });
    const countBefore = await matchingRows.count();

    await dialog.getByRole("button", { name: "บันทึก" }).click();
    await expect(dialog).toBeHidden();

    await expect(matchingRows).toHaveCount(countBefore + 1);
    await expect(matchingRows.last().getByText("ใช้อยู่")).toBeVisible();
  });

  // PROMO-02: สร้างโปรฯ scope=ทั้งบิล
  test("PROMO-02: creates a bill-scope promotion (no product picker shown)", async ({ page }) => {
    await page.getByRole("button", { name: "+ สร้างโปรโมชั่น" }).click();
    const dialog = page.getByRole("dialog");
    await dialog.getByRole("combobox").nth(0).selectOption({ label: "ทั้งบิล" });

    // สลับเป็น "ทั้งบิล" แล้ว dropdown เลือกสินค้าต้องหายไปจาก DOM
    await expect(dialog.getByRole("combobox")).toHaveCount(1);

    await dialog.getByRole("spinbutton").fill("12");
    await dialog.getByRole("textbox").nth(1).fill("2026-12-31");

    const rows = page.getByRole("row").filter({ hasText: "12%" }).filter({ hasText: "ทั้งบิล" });
    const countBefore = await rows.count();

    await dialog.getByRole("button", { name: "บันทึก" }).click();
    await expect(dialog).toBeHidden();
    await expect(rows).toHaveCount(countBefore + 1);
  });

  // PROMO-03: ส่วนลด >100% ถูกกันด้วย max=100
  test("PROMO-03: rejects a discount percentage above 100", async ({ page }) => {
    await page.getByRole("button", { name: "+ สร้างโปรโมชั่น" }).click();
    const dialog = page.getByRole("dialog");
    const discountInput = dialog.getByRole("spinbutton");
    await discountInput.fill("150");

    await expect(discountInput).toHaveAttribute("max", "100");
    const isValid = await discountInput.evaluate((el: HTMLInputElement) => el.checkValidity());
    expect(isValid).toBe(false);
  });

  // PROMO-04: ส่วนลด <= 0 ถูกกันด้วย min=0.01
  test("PROMO-04: rejects a discount percentage of zero", async ({ page }) => {
    await page.getByRole("button", { name: "+ สร้างโปรโมชั่น" }).click();
    const dialog = page.getByRole("dialog");
    const discountInput = dialog.getByRole("spinbutton");
    await discountInput.fill("0");

    await expect(discountInput).toHaveAttribute("min", "0.01");
    const isValid = await discountInput.evaluate((el: HTMLInputElement) => el.checkValidity());
    expect(isValid).toBe(false);
  });

  // PROMO-05: วันสิ้นสุด < วันเริ่ม ถูกกันด้วย min={startDate} บน input วันที่สิ้นสุด
  test("PROMO-05: end-date input cannot go before the start date", async ({ page }) => {
    await page.getByRole("button", { name: "+ สร้างโปรโมชั่น" }).click();
    const dialog = page.getByRole("dialog");

    const startDate = dialog.getByRole("textbox").nth(0);
    await startDate.fill("2026-09-15");

    const endDate = dialog.getByRole("textbox").nth(1);
    await expect(endDate).toHaveAttribute("min", "2026-09-15");
  });

  // PROMO-06: ติ๊ก "เฉพาะสมาชิกเท่านั้น" -> คอลัมน์เงื่อนไขแสดง "เฉพาะสมาชิก"
  test("PROMO-06: members-only checkbox reflects in the ' เงื่อนไข' column", async ({ page }) => {
    await page.getByRole("button", { name: "+ สร้างโปรโมชั่น" }).click();
    const dialog = page.getByRole("dialog");

    const productSelect = dialog.getByRole("combobox").nth(1);
    const productOptions = await productSelect.locator("option").allTextContents();
    const productName = productOptions.find((name) => name.trim() !== "เลือกสินค้า")!;
    await productSelect.selectOption({ label: productName });

    await dialog.getByRole("spinbutton").fill("33");
    await dialog.getByRole("textbox").nth(1).fill("2026-12-31");
    await dialog.getByRole("checkbox", { name: "ส่วนลดสำหรับสมาชิกเท่านั้น" }).check();

    const row = page.getByRole("row").filter({ hasText: productName }).filter({ hasText: "33%" });
    await dialog.getByRole("button", { name: "บันทึก" }).click();
    await expect(dialog).toBeHidden();

    await expect(row.last().getByText("เฉพาะสมาชิก")).toBeVisible();
  });

  // PROMO-07: แก้ไขโปรโมชั่นที่มีอยู่ -> ฟอร์ม prefill ถูกต้อง และอัปเดตสำเร็จ
  test("PROMO-07: edits an existing promotion", async ({ page }) => {
    // สร้างโปรฯ ตั้งต้นก่อน เพื่อให้มีแถวที่รู้ค่าแน่นอนให้แก้ไข
    await page.getByRole("button", { name: "+ สร้างโปรโมชั่น" }).click();
    const createDialog = page.getByRole("dialog");
    const productSelect = createDialog.getByRole("combobox").nth(1);
    const productOptions = await productSelect.locator("option").allTextContents();
    const productName = productOptions.find((name) => name.trim() !== "เลือกสินค้า")!;
    await productSelect.selectOption({ label: productName });
    await createDialog.getByRole("spinbutton").fill("7");
    await createDialog.getByRole("textbox").nth(1).fill("2026-12-31");
    await createDialog.getByRole("button", { name: "บันทึก" }).click();
    await expect(createDialog).toBeHidden();

    const row = page.getByRole("row").filter({ hasText: productName }).filter({ hasText: "7%" }).last();
    await expect(row).toBeVisible();
    await row.getByRole("button", { name: "แก้ไข" }).click();

    const editDialog = page.getByRole("dialog");
    await expect(editDialog).toBeVisible();
    // ฟอร์มต้อง prefill ค่าเดิมมา
    await expect(editDialog.getByRole("spinbutton")).toHaveValue("7");

    await editDialog.getByRole("spinbutton").fill("77");
    await editDialog.getByRole("button", { name: "บันทึก" }).click();
    await expect(editDialog).toBeHidden();

    const updatedRow = page.getByRole("row").filter({ hasText: productName }).filter({ hasText: "77%" });
    await expect(updatedRow.last()).toBeVisible();
  });

  // PROMO-08: ลบโปรโมชั่น -> หายจากตารางทันที (ไม่มี confirm dialog)
  test("PROMO-08: deletes a promotion immediately with no confirmation dialog", async ({ page }) => {
    await page.getByRole("button", { name: "+ สร้างโปรโมชั่น" }).click();
    const createDialog = page.getByRole("dialog");
    const productSelect = createDialog.getByRole("combobox").nth(1);
    const productOptions = await productSelect.locator("option").allTextContents();
    const productName = productOptions.find((name) => name.trim() !== "เลือกสินค้า")!;
    await productSelect.selectOption({ label: productName });
    await createDialog.getByRole("spinbutton").fill("41");
    await createDialog.getByRole("textbox").nth(1).fill("2026-12-31");
    await createDialog.getByRole("button", { name: "บันทึก" }).click();
    await expect(createDialog).toBeHidden();

    const row = page.getByRole("row").filter({ hasText: productName }).filter({ hasText: "41%" }).last();
    await expect(row).toBeVisible();

    await row.getByRole("button", { name: "ลบ" }).click();
    await expect(row).toBeHidden();
  });

  // PROMO-09: checkbox "เฉพาะที่ใช้ได้ตอนนี้" กรองเฉพาะโปรโมชั่น active
  test("PROMO-09: 'active only' filter hides inactive promotions", async ({ page }) => {
    // สร้างโปรฯ ที่หมดอายุไปแล้วแน่นอน (2020) ให้เป็นเคสอ้างอิง
    await page.getByRole("button", { name: "+ สร้างโปรโมชั่น" }).click();
    const dialog = page.getByRole("dialog");
    const productSelect = dialog.getByRole("combobox").nth(1);
    const productOptions = await productSelect.locator("option").allTextContents();
    const productName = productOptions.find((name) => name.trim() !== "เลือกสินค้า")!;
    await productSelect.selectOption({ label: productName });
    await dialog.getByRole("spinbutton").fill("9");
    await dialog.getByRole("textbox").nth(0).fill("2020-01-01");
    await dialog.getByRole("textbox").nth(1).fill("2020-01-31");
    await dialog.getByRole("button", { name: "บันทึก" }).click();
    await expect(dialog).toBeHidden();

    const expiredRow = page.getByRole("row").filter({ hasText: productName }).filter({ hasText: "9%" });
    await expect(expiredRow.last()).toBeVisible();

    await page.getByRole("checkbox", { name: "เฉพาะที่ใช้ได้ตอนนี้" }).check();
    await expect(expiredRow).toHaveCount(0);
  });
});
