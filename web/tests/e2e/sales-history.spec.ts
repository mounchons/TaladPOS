import { test, expect, type Page } from "@playwright/test";
import { loginAsManager, loginAsCashier, uniqueSuffix } from "./helpers/auth";

async function createProductAndCheckout(page: Page, opts: { productName: string; price: string }) {
  await page.goto("/stock");
  await page.getByRole("button", { name: "+ เพิ่มสินค้า" }).click();
  const productDialog = page.getByRole("dialog");
  await productDialog.getByLabel("ชื่อสินค้า").fill(opts.productName);
  await productDialog.getByLabel("URL รูปภาพ").fill("https://example.com/history-test.jpg");
  await productDialog.getByLabel("ราคา (บาท)").fill(opts.price);
  await productDialog.getByLabel("จำนวนคงเหลือ").fill("5");
  await productDialog.getByRole("button", { name: "บันทึก" }).click();
  await expect(productDialog).toBeHidden();

  await page.goto("/sales");
  await page.getByPlaceholder("สแกนบาร์โค้ด หรือพิมพ์ชื่อสินค้า").fill(opts.productName);
  await page.getByRole("button", { name: opts.productName }).click();
  await page.getByRole("button", { name: "ชำระเงิน" }).click();
  const receiptDialog = page.getByRole("dialog").filter({ hasText: "ขายสำเร็จ" });
  await expect(receiptDialog).toBeVisible();
  await receiptDialog.getByRole("button", { name: "ขายรายการต่อไป" }).click();
  await expect(receiptDialog).toBeHidden();
}

test.describe("Sales History (/sales/history)", () => {
  // HIST-01: โหลดรายการ paged เริ่มต้นถูกต้อง
  test("HIST-01: loads the sales history table with a pager", async ({ page }) => {
    await loginAsManager(page);
    const productName = `ประวัติโหลด-${uniqueSuffix()}`;
    await createProductAndCheckout(page, { productName, price: "10" });

    await page.goto("/sales/history");
    await expect(page.getByRole("columnheader", { name: "วันที่" })).toBeVisible();
    await expect(page.getByText(/หน้า \d+ จาก/)).toBeVisible();
  });

  // HIST-02: filter ช่วงวันที่ที่ไม่มีข้อมูล -> empty state
  test("HIST-02: a date range with no matches shows the empty state", async ({ page }) => {
    await loginAsManager(page);
    await page.goto("/sales/history");

    await page.getByLabel("จากวันที่").fill("2027-01-01");
    await page.getByLabel("ถึงวันที่").fill("2027-01-02");
    await expect(page.getByText("ไม่พบบิลขายในเงื่อนไขนี้ ลองขยายช่วงวันที่")).toBeVisible();
  });

  // HIST-02b: "ถึงวันที่" ไม่สามารถเลือกก่อน "จากวันที่" ได้ (min attribute)
  test("HIST-02b: end-date input cannot go before the start date", async ({ page }) => {
    await loginAsManager(page);
    await page.goto("/sales/history");
    await page.getByLabel("จากวันที่").fill("2026-05-10");
    await expect(page.getByLabel("ถึงวันที่")).toHaveAttribute("min", "2026-05-10");
  });

  // HIST-03: "เฉพาะบิลของฉัน" ต้องกรองให้เหลือเฉพาะบิลของพนักงานคนปัจจุบันเท่านั้น
  // (ตารางไม่มีคอลัมน์ชื่อสินค้า จึงเช็คผ่านคอลัมน์ "พนักงาน" ของทุกแถวที่เหลือแทน
  // การจับคู่แถวที่เพิ่งสร้าง ซึ่งเสี่ยงชนกับเทสไฟล์อื่นที่ checkout พร้อมกัน)
  test("HIST-03: 'only mine' filter narrows every visible row to the current staff", async ({ page }) => {
    await loginAsManager(page);
    const productName = `ประวัติของฉัน-${uniqueSuffix()}`;
    await createProductAndCheckout(page, { productName, price: "10" });

    await page.goto("/sales/history");
    await page.getByRole("checkbox", { name: "เฉพาะบิลของฉัน" }).check();

    const rows = page.getByRole("row");
    const rowCount = await rows.count();
    expect(rowCount).toBeGreaterThan(1); // header + อย่างน้อย 1 แถวข้อมูล (บิลที่เพิ่ง checkout)
    for (let i = 1; i < rowCount; i++) {
      await expect(rows.nth(i)).toContainText("ผู้จัดการร้าน");
    }
  });

  // HIST-06: คลิก "ใบเสร็จ" -> ไปหน้า receipt ที่มียอดรวมตรงกับที่แสดงในตาราง
  // (เทียบผ่านคอลัมน์ "ยอดรวม" เพราะเป็นค่าเดียวที่ตารางประวัติกับหน้าใบเสร็จแสดงร่วมกัน)
  test("HIST-06: clicking the receipt button navigates to the matching receipt", async ({ page }) => {
    await loginAsManager(page);
    const productName = `ประวัติใบเสร็จ-${uniqueSuffix()}`;
    await createProductAndCheckout(page, { productName, price: "10" });

    await page.goto("/sales/history");
    const firstRow = page.getByRole("row").nth(1);
    const totalText = (await firstRow.locator("td").nth(5).innerText()).trim();

    await firstRow.getByRole("button", { name: "ใบเสร็จ" }).click();

    await expect(page).toHaveURL(/\/sales\/receipt\/.+/);
    await expect(page.getByText("ยอดสุทธิ")).toBeVisible();
    await expect(page.getByText(totalText, { exact: false }).first()).toBeVisible();
  });

  // HIST-07: ปุ่ม Export -> ดาวน์โหลด .xlsx
  test("HIST-07: export button downloads an .xlsx file", async ({ page }) => {
    await loginAsManager(page);
    await page.goto("/sales/history");

    const [download] = await Promise.all([
      page.waitForEvent("download"),
      page.getByRole("button", { name: "Export" }).click(),
    ]);

    expect(download.suggestedFilename()).toContain("taladpos-sales-history");
    expect(download.suggestedFilename()).toMatch(/\.xlsx$/);
  });

  // HIST-08: Export ใช้ได้ทั้ง Manager และ Cashier
  test("HIST-08: export is also available to a cashier", async ({ page }) => {
    await loginAsCashier(page);
    await page.goto("/sales/history");

    const [download] = await Promise.all([
      page.waitForEvent("download"),
      page.getByRole("button", { name: "Export" }).click(),
    ]);
    expect(download.suggestedFilename()).toMatch(/\.xlsx$/);
  });
});
