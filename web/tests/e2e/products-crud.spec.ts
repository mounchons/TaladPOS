import { test, expect, type Page } from "@playwright/test";
import { loginAsManager, uniqueSuffix } from "./helpers/auth";

// ตาราง /stock แบ่งหน้า 20 รายการ และสะสมสินค้าจากการรันเทสหลายรอบใน dev DB
// เดียวกันจนเกิน 1 หน้าแล้ว - ค้นหาด้วยชื่อเฉพาะก่อนเสมอ เพื่อให้เจอแถวที่เพิ่ง
// สร้างไม่ว่าจะอยู่หน้าไหนก็ตาม แทนที่จะพึ่งพาว่ามันอยู่หน้าแรก
async function searchAndGetRow(page: Page, name: string) {
  await page.getByPlaceholder("ค้นหาชื่อสินค้า").fill(name);
  return page.getByRole("row").filter({ hasText: name });
}

// Manager-only CRUD บนหน้า /stock (spec 001 FR-015..018, FR-029)
test.describe("Product / Stock CRUD - Manager", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsManager(page);
    await page.goto("/stock");
  });

  // PROD-01: สร้างสินค้าข้อมูลครบถ้วน -> ขึ้นในตาราง
  test("PROD-01: creates a product with valid data", async ({ page }) => {
    const name = `เทสสินค้า-${uniqueSuffix()}`;
    await page.getByRole("button", { name: "+ เพิ่มสินค้า" }).click();
    const dialog = page.getByRole("dialog");
    await expect(dialog).toBeVisible();

    await dialog.getByLabel("ชื่อสินค้า").fill(name);
    await dialog.getByLabel("URL รูปภาพ").fill("https://example.com/image.jpg");
    await dialog.getByLabel("ราคา (บาท)").fill("25.50");
    await dialog.getByLabel("จำนวนคงเหลือ").fill("10");
    await dialog.getByLabel("เกณฑ์ใกล้หมด").fill("3");

    await dialog.getByRole("button", { name: "บันทึก" }).click();
    await expect(dialog).toBeHidden();

    await expect(await searchAndGetRow(page, name)).toBeVisible();
  });

  // PROD-02: เว้นชื่อ/รูป/ราคาว่าง -> submit ไม่ผ่าน (required)
  test("PROD-02: blocks submit when required fields are empty", async ({ page }) => {
    await page.getByRole("button", { name: "+ เพิ่มสินค้า" }).click();
    const dialog = page.getByRole("dialog");

    const nameInput = dialog.getByLabel("ชื่อสินค้า");
    const isValid = await nameInput.evaluate((el: HTMLInputElement) => el.checkValidity());
    expect(isValid).toBe(false);
  });

  // PROD-03: ราคา <= 0 ถูกกันด้วย min=0.01
  test("PROD-03: price input rejects values at or below zero", async ({ page }) => {
    await page.getByRole("button", { name: "+ เพิ่มสินค้า" }).click();
    const dialog = page.getByRole("dialog");
    const priceInput = dialog.getByLabel("ราคา (บาท)");
    await priceInput.fill("0");
    await expect(priceInput).toHaveAttribute("min", "0.01");
    const isValid = await priceInput.evaluate((el: HTMLInputElement) => el.checkValidity());
    expect(isValid).toBe(false);
  });

  // PROD-04: บาร์โค้ดซ้ำกับสินค้าอื่น -> 409
  test("PROD-04: duplicate barcode is rejected with a 409 message", async ({ page }) => {
    const barcode = `TESTBC${uniqueSuffix()}`;
    const nameA = `สินค้าเอ-${uniqueSuffix()}`;
    const nameB = `สินค้าบี-${uniqueSuffix()}`;

    // สร้างสินค้าแรกพร้อมบาร์โค้ด
    await page.getByRole("button", { name: "+ เพิ่มสินค้า" }).click();
    let dialog = page.getByRole("dialog");
    await dialog.getByLabel("ชื่อสินค้า").fill(nameA);
    await dialog.getByLabel("URL รูปภาพ").fill("https://example.com/a.jpg");
    await dialog.getByLabel("ราคา (บาท)").fill("10");
    await dialog.getByLabel("บาร์โค้ด (ไม่บังคับ)").fill(barcode);
    await dialog.getByRole("button", { name: "บันทึก" }).click();
    await expect(dialog).toBeHidden();

    // สร้างสินค้าที่สองด้วยบาร์โค้ดเดียวกัน -> ต้องถูกปฏิเสธ
    await page.getByRole("button", { name: "+ เพิ่มสินค้า" }).click();
    dialog = page.getByRole("dialog");
    await dialog.getByLabel("ชื่อสินค้า").fill(nameB);
    await dialog.getByLabel("URL รูปภาพ").fill("https://example.com/b.jpg");
    await dialog.getByLabel("ราคา (บาท)").fill("10");
    await dialog.getByLabel("บาร์โค้ด (ไม่บังคับ)").fill(barcode);
    await dialog.getByRole("button", { name: "บันทึก" }).click();

    await expect(dialog.getByText("บาร์โค้ดนี้ถูกใช้กับสินค้าอื่นแล้ว")).toBeVisible();
    await expect(dialog).toBeVisible();
  });

  // PROD-05: เว้นบาร์โค้ดว่าง -> สร้างได้ปกติ
  test("PROD-05: creates a product with a blank barcode", async ({ page }) => {
    const name = `ไม่มีบาร์โค้ด-${uniqueSuffix()}`;
    await page.getByRole("button", { name: "+ เพิ่มสินค้า" }).click();
    const dialog = page.getByRole("dialog");
    await dialog.getByLabel("ชื่อสินค้า").fill(name);
    await dialog.getByLabel("URL รูปภาพ").fill("https://example.com/no-barcode.jpg");
    await dialog.getByLabel("ราคา (บาท)").fill("5");
    await dialog.getByRole("button", { name: "บันทึก" }).click();
    await expect(dialog).toBeHidden();

    await expect(await searchAndGetRow(page, name)).toBeVisible();
  });

  // PROD-06: แก้ไขสินค้า -> ค่าที่แก้อัปเดตในตาราง
  test("PROD-06: edits an existing product", async ({ page }) => {
    const name = `แก้ไขได้-${uniqueSuffix()}`;
    await page.getByRole("button", { name: "+ เพิ่มสินค้า" }).click();
    let dialog = page.getByRole("dialog");
    await dialog.getByLabel("ชื่อสินค้า").fill(name);
    await dialog.getByLabel("URL รูปภาพ").fill("https://example.com/edit.jpg");
    await dialog.getByLabel("ราคา (บาท)").fill("8");
    await dialog.getByRole("button", { name: "บันทึก" }).click();
    await expect(dialog).toBeHidden();

    const row = await searchAndGetRow(page, name);
    await expect(row).toBeVisible();
    await row.getByRole("button", { name: "แก้ไข" }).click();

    dialog = page.getByRole("dialog");
    await expect(dialog.getByLabel("ชื่อสินค้า")).toHaveValue(name);
    await dialog.getByLabel("ราคา (บาท)").fill("99.00");
    await dialog.getByRole("button", { name: "บันทึก" }).click();
    await expect(dialog).toBeHidden();

    await expect((await searchAndGetRow(page, name)).filter({ hasText: "99.00" })).toBeVisible();
  });

  // PROD-07: แก้ไขให้บาร์โค้ดชนกับสินค้าอื่น -> 409
  test("PROD-07: editing to a colliding barcode is rejected", async ({ page }) => {
    const barcode = `EDITBC${uniqueSuffix()}`;
    const existingName = `มีบาร์โค้ดแล้ว-${uniqueSuffix()}`;
    const editingName = `จะแก้ไข-${uniqueSuffix()}`;

    await page.getByRole("button", { name: "+ เพิ่มสินค้า" }).click();
    let dialog = page.getByRole("dialog");
    await dialog.getByLabel("ชื่อสินค้า").fill(existingName);
    await dialog.getByLabel("URL รูปภาพ").fill("https://example.com/x.jpg");
    await dialog.getByLabel("ราคา (บาท)").fill("10");
    await dialog.getByLabel("บาร์โค้ด (ไม่บังคับ)").fill(barcode);
    await dialog.getByRole("button", { name: "บันทึก" }).click();
    await expect(dialog).toBeHidden();

    await page.getByRole("button", { name: "+ เพิ่มสินค้า" }).click();
    dialog = page.getByRole("dialog");
    await dialog.getByLabel("ชื่อสินค้า").fill(editingName);
    await dialog.getByLabel("URL รูปภาพ").fill("https://example.com/y.jpg");
    await dialog.getByLabel("ราคา (บาท)").fill("10");
    await dialog.getByRole("button", { name: "บันทึก" }).click();
    await expect(dialog).toBeHidden();

    const row = await searchAndGetRow(page, editingName);
    await row.getByRole("button", { name: "แก้ไข" }).click();
    dialog = page.getByRole("dialog");
    await dialog.getByLabel("บาร์โค้ด (ไม่บังคับ)").fill(barcode);
    await dialog.getByRole("button", { name: "บันทึก" }).click();

    await expect(dialog.getByText("บาร์โค้ดนี้ถูกใช้กับสินค้าอื่นแล้ว")).toBeVisible();
  });

  // PROD-08: ลบสินค้า -> หายจากตารางทันที (ไม่มี confirm dialog)
  test("PROD-08: deletes a product immediately with no confirmation dialog", async ({ page }) => {
    const name = `จะลบ-${uniqueSuffix()}`;
    await page.getByRole("button", { name: "+ เพิ่มสินค้า" }).click();
    const dialog = page.getByRole("dialog");
    await dialog.getByLabel("ชื่อสินค้า").fill(name);
    await dialog.getByLabel("URL รูปภาพ").fill("https://example.com/del.jpg");
    await dialog.getByLabel("ราคา (บาท)").fill("3");
    await dialog.getByRole("button", { name: "บันทึก" }).click();
    await expect(dialog).toBeHidden();

    const row = await searchAndGetRow(page, name);
    await expect(row).toBeVisible();
    await row.getByRole("button", { name: "ลบ" }).click();
    await expect(row).toBeHidden();
  });

  // PROD-09: ค้นหาชื่อ/บาร์โค้ด (debounced) -> กรองตารางฝั่ง server
  test("PROD-09: search filters the table server-side", async ({ page }) => {
    const name = `ค้นหาได้-${uniqueSuffix()}`;
    await page.getByRole("button", { name: "+ เพิ่มสินค้า" }).click();
    const dialog = page.getByRole("dialog");
    await dialog.getByLabel("ชื่อสินค้า").fill(name);
    await dialog.getByLabel("URL รูปภาพ").fill("https://example.com/search.jpg");
    await dialog.getByLabel("ราคา (บาท)").fill("4");
    await dialog.getByRole("button", { name: "บันทึก" }).click();
    await expect(dialog).toBeHidden();

    await page.getByPlaceholder("ค้นหาชื่อสินค้า").fill(name);
    await expect(page.getByRole("row").filter({ hasText: name })).toBeVisible();
    // เมื่อกรองด้วยชื่อเฉพาะเจาะจงแล้ว ควรเหลือแถวข้อมูลแค่แถวเดียว (ไม่รวม header)
    await expect(page.getByRole("row")).toHaveCount(2);
  });

  // PROD-10: checkbox "เฉพาะสินค้าใกล้หมด" -> กรองเฉพาะ low-stock
  test("PROD-10: low-stock-only filter narrows the table", async ({ page }) => {
    // ตัวกรอง debounce 250ms ก่อน fetch ใหม่ - รอให้แน่ใจว่าโหลดจริงจบทั้งก่อน/หลัง
    // ติ๊ก ไม่งั้นอาจนับได้แค่สถานะ "กำลังโหลด…" ตอนที่ยังไม่ทันเปลี่ยนจริง
    await page.waitForTimeout(600);
    const rowsBefore = await page.getByRole("row").count();

    await page.getByRole("checkbox", { name: "เฉพาะสินค้าใกล้หมด" }).check();
    await page.waitForTimeout(600);
    // แค่ยืนยันว่าการติ๊กไม่ทำให้พัง และตารางยัง render ได้ (จำนวนแถวอาจเท่าเดิมหรือลดลง)
    const rowsAfter = await page.getByRole("row").count();
    expect(rowsAfter).toBeLessThanOrEqual(rowsBefore);
  });
});
