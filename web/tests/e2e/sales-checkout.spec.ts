import { test, expect, type Page } from "@playwright/test";
import { loginAsManager, uniqueSuffix } from "./helpers/auth";

// สร้างสินค้าทดสอบผ่านหน้า /stock ด้วย stock ที่รู้ค่าแน่นอน เพื่อไม่ให้เทส
// เช็คเอาท์ (ซึ่งตัดสต็อกจริงถาวรใน dev DB) ไปพึ่งพาข้อมูล seed เดิมที่อาจถูก
// ใช้จนหมดจากการรันเทสซ้ำหลายครั้ง
async function createTestProduct(
  page: Page,
  opts: { name: string; price: string; stock: string; barcode?: string },
) {
  await page.goto("/stock");
  await page.getByRole("button", { name: "+ เพิ่มสินค้า" }).click();
  const dialog = page.getByRole("dialog");
  await dialog.getByLabel("ชื่อสินค้า").fill(opts.name);
  await dialog.getByLabel("URL รูปภาพ").fill("https://example.com/sales-test.jpg");
  await dialog.getByLabel("ราคา (บาท)").fill(opts.price);
  await dialog.getByLabel("จำนวนคงเหลือ").fill(opts.stock);
  if (opts.barcode) await dialog.getByLabel("บาร์โค้ด (ไม่บังคับ)").fill(opts.barcode);
  await dialog.getByRole("button", { name: "บันทึก" }).click();
  await expect(dialog).toBeHidden();
}

test.describe("POS Checkout (/sales)", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsManager(page);
  });

  // SALES-01: ค้นหาสินค้าด้วยชื่อ -> การ์ดที่ตรงแสดงผล
  test("SALES-01: searching by name shows the matching product card", async ({ page }) => {
    const name = `ขายชื่อ-${uniqueSuffix()}`;
    await createTestProduct(page, { name, price: "10", stock: "5" });

    await page.goto("/sales");
    await page.getByPlaceholder("สแกนบาร์โค้ด หรือพิมพ์ชื่อสินค้า").fill(name);
    await expect(page.getByRole("button", { name })).toBeVisible();
  });

  // SALES-02: ค้นหาสินค้าด้วยบาร์โค้ด -> เจอตรงตัว
  test("SALES-02: searching by barcode finds the exact product", async ({ page }) => {
    const name = `ขายบาร์โค้ด-${uniqueSuffix()}`;
    const barcode = `SC${uniqueSuffix()}`;
    await createTestProduct(page, { name, price: "10", stock: "5", barcode });

    await page.goto("/sales");
    await page.getByPlaceholder("สแกนบาร์โค้ด หรือพิมพ์ชื่อสินค้า").fill(barcode);
    await expect(page.getByRole("button", { name })).toBeVisible();
  });

  // SALES-03: คลิกการ์ด -> เพิ่มเข้าตะกร้า จำนวน 1
  test("SALES-03: clicking a product card adds one unit to the cart", async ({ page }) => {
    const name = `ขายคลิก-${uniqueSuffix()}`;
    await createTestProduct(page, { name, price: "10", stock: "5" });

    await page.goto("/sales");
    await page.getByPlaceholder("สแกนบาร์โค้ด หรือพิมพ์ชื่อสินค้า").fill(name);
    await page.getByRole("button", { name }).click();

    await expect(page.getByLabel(`จำนวน ${name}`, { exact: true })).toHaveValue("1");
  });

  // SALES-04: ปรับจำนวนในตะกร้า clamp ไม่เกิน stock
  test("SALES-04: quantity stepper clamps to the available stock", async ({ page }) => {
    const name = `ขายจำกัด-${uniqueSuffix()}`;
    await createTestProduct(page, { name, price: "10", stock: "3" });

    await page.goto("/sales");
    await page.getByPlaceholder("สแกนบาร์โค้ด หรือพิมพ์ชื่อสินค้า").fill(name);
    await page.getByRole("button", { name }).click();

    const plusButton = page.getByLabel(`เพิ่มจำนวน ${name}`);
    await plusButton.click();
    await plusButton.click(); // now at 3 (stock cap)
    await expect(page.getByLabel(`จำนวน ${name}`, { exact: true })).toHaveValue("3");
    await expect(plusButton).toBeDisabled();

    // พิมพ์เกิน stock ตรง ๆ ก็ต้องถูก clamp เหมือนกัน
    const qtyInput = page.getByLabel(`จำนวน ${name}`, { exact: true });
    await qtyInput.fill("999");
    await expect(qtyInput).toHaveValue("3");
  });

  // SALES-05: ลบรายการออกจากตะกร้า
  test("SALES-05: removes a line item from the cart", async ({ page }) => {
    const name = `ขายลบ-${uniqueSuffix()}`;
    await createTestProduct(page, { name, price: "10", stock: "5" });

    await page.goto("/sales");
    await page.getByPlaceholder("สแกนบาร์โค้ด หรือพิมพ์ชื่อสินค้า").fill(name);
    await page.getByRole("button", { name }).click();
    await expect(page.getByLabel(`จำนวน ${name}`, { exact: true })).toBeVisible();

    await page.getByLabel(`เอา ${name} ออกจากตะกร้า`).click();
    await expect(page.getByLabel(`จำนวน ${name}`, { exact: true })).toHaveCount(0);
    await expect(page.getByText("แตะสินค้าเพื่อเริ่มขาย")).toBeVisible();
  });

  // SALES-06: สินค้าหมดสต็อก -> การ์ด disabled
  test("SALES-06: an out-of-stock product card is disabled", async ({ page }) => {
    const name = `ขายหมด-${uniqueSuffix()}`;
    await createTestProduct(page, { name, price: "10", stock: "0" });

    await page.goto("/sales");
    await page.getByPlaceholder("สแกนบาร์โค้ด หรือพิมพ์ชื่อสินค้า").fill(name);
    const card = page.getByRole("button", { name });
    await expect(card).toBeDisabled();
    await expect(card.getByText("สินค้าหมด")).toBeVisible();
  });

  // SALES-07: เช็คเอาท์สำเร็จ -> ReceiptDialog เปิด, ตะกร้าเคลียร์
  test("SALES-07: successful checkout opens the receipt and clears the cart", async ({ page }) => {
    const name = `ขายสำเร็จ-${uniqueSuffix()}`;
    await createTestProduct(page, { name, price: "12.50", stock: "5" });

    await page.goto("/sales");
    await page.getByPlaceholder("สแกนบาร์โค้ด หรือพิมพ์ชื่อสินค้า").fill(name);
    await page.getByRole("button", { name }).click();
    await page.getByRole("button", { name: "ชำระเงิน" }).click();

    const receiptDialog = page.getByRole("dialog").filter({ hasText: "ขายสำเร็จ" });
    await expect(receiptDialog).toBeVisible();
    await expect(receiptDialog.getByText(name)).toBeVisible();
    await expect(receiptDialog.getByText("12.50")).toBeVisible();

    await receiptDialog.getByRole("button", { name: "ขายรายการต่อไป" }).click();
    await expect(receiptDialog).toBeHidden();
    await expect(page.getByText("แตะสินค้าเพื่อเริ่มขาย")).toBeVisible();
  });

  // SALES-08: เช็คเอาท์ตะกร้าว่าง -> ปุ่มชำระเงิน disabled
  test("SALES-08: checkout is blocked when the cart is empty", async ({ page }) => {
    await page.goto("/sales");
    await expect(page.getByRole("button", { name: "ชำระเงิน" })).toBeDisabled();
  });

  // SALES-09: เช็คเอาท์พร้อมผูกสมาชิก -> ใบเสร็จแสดงชื่อสมาชิก
  test("SALES-09: checkout with a member attached shows the member on the receipt", async ({ page }) => {
    const productName = `ขายพร้อมสมาชิก-${uniqueSuffix()}`;
    await createTestProduct(page, { name: productName, price: "20", stock: "5" });

    await page.goto("/sales");
    const memberName = `สมาชิกเช็คเอาท์-${uniqueSuffix()}`;
    const phone = "08" + Math.floor(10000000 + Math.random() * 89999999).toString();
    await page.getByRole("button", { name: "สมัครสมาชิกใหม่" }).click();
    const memberDialog = page.getByRole("dialog");
    await memberDialog.getByLabel("ชื่อ").fill(memberName);
    await memberDialog.getByLabel("เบอร์โทรศัพท์").fill(phone);
    await memberDialog.getByRole("button", { name: "สมัครสมาชิก" }).click();
    await expect(memberDialog).toBeHidden();

    await page.getByPlaceholder("สแกนบาร์โค้ด หรือพิมพ์ชื่อสินค้า").fill(productName);
    await page.getByRole("button", { name: productName }).click();
    await page.getByRole("button", { name: "ชำระเงิน" }).click();

    const receiptDialog = page.getByRole("dialog").filter({ hasText: "ขายสำเร็จ" });
    await expect(receiptDialog).toBeVisible();
    await expect(receiptDialog.getByText(memberName)).toBeVisible();
  });

  // SALES-10: มีโปรโมชั่น active ตรงเงื่อนไข -> ยอดหักส่วนลดถูกต้องตอนเช็คเอาท์
  test("SALES-10: an active item-scope promotion discounts the checkout total", async ({ page }) => {
    const productName = `ขายมีโปร-${uniqueSuffix()}`;
    await createTestProduct(page, { name: productName, price: "100", stock: "5" });

    await page.goto("/promotions");
    await page.getByRole("button", { name: "+ สร้างโปรโมชั่น" }).click();
    const promoDialog = page.getByRole("dialog");
    await promoDialog.getByRole("combobox").nth(1).selectOption({ label: productName });
    await promoDialog.getByRole("spinbutton").fill("50");
    await promoDialog.getByRole("textbox").nth(1).fill("2026-12-31");
    await promoDialog.getByRole("button", { name: "บันทึก" }).click();
    await expect(promoDialog).toBeHidden();

    await page.goto("/sales");
    await page.getByPlaceholder("สแกนบาร์โค้ด หรือพิมพ์ชื่อสินค้า").fill(productName);
    await page.getByRole("button", { name: productName }).click();
    await page.getByRole("button", { name: "ชำระเงิน" }).click();

    const receiptDialog = page.getByRole("dialog").filter({ hasText: "ขายสำเร็จ" });
    await expect(receiptDialog).toBeVisible();

    // item-scope และ bill-scope รวมกันได้ (docs/logic/README.md) - ถ้ามีโปรโมชั่น
    // ทั้งบิลอื่นค้างอยู่ใน dev DB จากเทสก่อนหน้า ยอดสุทธิรวมอาจน้อยกว่า 50% พอดี
    // เช็คแค่ว่ายอดสุทธิ <= 50.00 (โปรโมชั่น 50% ของเราทำงานแน่นอน ส่วนลดรวมจะยิ่ง
    // มากกว่านี้ก็ได้ แต่ต้องไม่น้อยกว่า)
    const totalText = await receiptDialog.locator("span.text-2xl").innerText();
    const total = Number.parseFloat(totalText.replace(/[^0-9.]/g, ""));
    expect(total).toBeLessThanOrEqual(50.01);
  });

  // SALES-13: "ขายรายการต่อไป" ปิด dialog กลับตะกร้าว่าง (ครอบคลุมซ้ำใน SALES-07 แล้ว
  // แต่แยกเป็นเคสเดี่ยวเพื่อยืนยันปุ่มทำงานแม้ไม่มี interaction อื่นก่อนหน้า)
  test("SALES-13: dismissing the receipt returns to an empty cart ready for the next sale", async ({ page }) => {
    const name = `ขายต่อไป-${uniqueSuffix()}`;
    await createTestProduct(page, { name, price: "9", stock: "2" });

    await page.goto("/sales");
    await page.getByPlaceholder("สแกนบาร์โค้ด หรือพิมพ์ชื่อสินค้า").fill(name);
    await page.getByRole("button", { name }).click();
    await page.getByRole("button", { name: "ชำระเงิน" }).click();

    const receiptDialog = page.getByRole("dialog").filter({ hasText: "ขายสำเร็จ" });
    await expect(receiptDialog).toBeVisible();
    await receiptDialog.getByRole("button", { name: "ขายรายการต่อไป" }).click();

    await expect(receiptDialog).toBeHidden();
    await expect(page.getByRole("button", { name: "ชำระเงิน" })).toBeDisabled();
  });
});
