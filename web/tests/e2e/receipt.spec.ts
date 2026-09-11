import { test, expect } from "@playwright/test";
import { loginAsManager, uniqueSuffix } from "./helpers/auth";

test.describe("Receipt page (/sales/receipt/[id])", () => {
  // RCPT-01: id ถูกต้อง -> แสดงรายการ/ราคา/ส่วนลด/รวม
  test("RCPT-01: a valid sale id shows the full receipt", async ({ page }) => {
    await loginAsManager(page);

    const productName = `ใบเสร็จเดี่ยว-${uniqueSuffix()}`;
    await page.goto("/stock");
    await page.getByRole("button", { name: "+ เพิ่มสินค้า" }).click();
    const productDialog = page.getByRole("dialog");
    await productDialog.getByLabel("ชื่อสินค้า").fill(productName);
    await productDialog.getByLabel("URL รูปภาพ").fill("https://example.com/rcpt.jpg");
    await productDialog.getByLabel("ราคา (บาท)").fill("42");
    await productDialog.getByLabel("จำนวนคงเหลือ").fill("5");
    await productDialog.getByRole("button", { name: "บันทึก" }).click();
    await expect(productDialog).toBeHidden();

    await page.goto("/sales");
    await page.getByPlaceholder("สแกนบาร์โค้ด หรือพิมพ์ชื่อสินค้า").fill(productName);
    await page.getByRole("button", { name: productName }).click();
    await page.getByRole("button", { name: "ชำระเงิน" }).click();

    const receiptDialog = page.getByRole("dialog").filter({ hasText: "ขายสำเร็จ" });
    await expect(receiptDialog).toBeVisible();
    await receiptDialog.getByRole("button", { name: "ขายรายการต่อไป" }).click();

    // ไปหน้าประวัติแล้วกดใบเสร็จของบิลล่าสุด เพื่อเข้าหน้า /sales/receipt/[id] จริง
    await page.goto("/sales/history");
    const row = page.getByRole("row").nth(1);
    await row.getByRole("button", { name: "ใบเสร็จ" }).click();

    await expect(page).toHaveURL(/\/sales\/receipt\/.+/);
    await expect(page.getByText("ใบเสร็จรับเงิน")).toBeVisible();
    await expect(page.getByText("ยอดก่อนลด")).toBeVisible();
    await expect(page.getByText("ส่วนลด", { exact: true })).toBeVisible();
    await expect(page.getByText("ยอดสุทธิ")).toBeVisible();
  });

  // RCPT-02: id ไม่มีอยู่จริง -> "ไม่พบบิลขายนี้"
  test("RCPT-02: an unknown sale id shows the not-found message", async ({ page }) => {
    await loginAsManager(page);
    await page.goto("/sales/receipt/00000000-0000-0000-0000-000000000000");
    await expect(page.getByText("ไม่พบบิลขายนี้")).toBeVisible();
  });

  // RCPT-03: ปุ่มพิมพ์ทำงาน (แสดงอยู่และเรียก window.print ได้โดยไม่ error)
  test("RCPT-03: the print button triggers window.print()", async ({ page }) => {
    await loginAsManager(page);
    await page.goto("/sales/history");
    const row = page.getByRole("row").nth(1);
    await row.getByRole("button", { name: "ใบเสร็จ" }).click();
    await expect(page).toHaveURL(/\/sales\/receipt\/.+/);

    let printCalled = false;
    await page.exposeFunction("__onPrint", () => {
      printCalled = true;
    });
    await page.evaluate(() => {
      window.print = () => (window as unknown as { __onPrint: () => void }).__onPrint();
    });

    await page.getByRole("button", { name: "พิมพ์ใบเสร็จ" }).click();
    expect(printCalled).toBe(true);
  });
});
