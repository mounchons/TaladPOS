import { test, expect } from "@playwright/test";
import { loginAsManager, uniqueSuffix } from "./helpers/auth";

interface ProductSeed {
  price: string;
  stock: string;
  threshold: string;
  barcode: boolean;
}

// เพิ่มสินค้าหลายรายการผ่านหน้าเว็บจริง (ไม่ใช่ seed ตรง DB) เพื่อให้มีข้อมูล
// หลากหลายสำหรับทดสอบ: ราคา/สต็อกต่างกัน, มี/ไม่มีบาร์โค้ด, ครอบคลุมทั้งสถานะ
// ปกติ/ใกล้หมด/หมดสต็อก (stock 0 กับ 12 ตัวสุดท้ายเพื่อชน low-stock threshold)
const SEEDS: ProductSeed[] = [
  { price: "15.00", stock: "20", threshold: "5", barcode: true },
  { price: "25.50", stock: "8", threshold: "5", barcode: false },
  { price: "9.90", stock: "0", threshold: "3", barcode: true },
  { price: "120.00", stock: "2", threshold: "5", barcode: false },
  { price: "45.00", stock: "15", threshold: "10", barcode: true },
  { price: "60.75", stock: "1", threshold: "2", barcode: false },
  { price: "8.00", stock: "50", threshold: "10", barcode: true },
  { price: "199.99", stock: "3", threshold: "3", barcode: false },
  { price: "33.33", stock: "0", threshold: "5", barcode: true },
  { price: "12.25", stock: "7", threshold: "5", barcode: false },
  { price: "88.00", stock: "30", threshold: "10", barcode: true },
  { price: "5.50", stock: "4", threshold: "5", barcode: false },
];

test("PROD-SEED: seeds multiple products (>10) for manual/exploratory testing", async ({ page }) => {
  await loginAsManager(page);
  await page.goto("/stock");

  for (const [index, seed] of SEEDS.entries()) {
    const name = `สินค้าseed-${index + 1}-${uniqueSuffix()}`;

    await page.getByRole("button", { name: "+ เพิ่มสินค้า" }).click();
    const dialog = page.getByRole("dialog");
    await expect(dialog).toBeVisible();

    await dialog.getByLabel("ชื่อสินค้า").fill(name);
    await dialog.getByLabel("URL รูปภาพ").fill(`https://example.com/seed-${index + 1}.jpg`);
    await dialog.getByLabel("ราคา (บาท)").fill(seed.price);
    await dialog.getByLabel("จำนวนคงเหลือ").fill(seed.stock);
    await dialog.getByLabel("เกณฑ์ใกล้หมด").fill(seed.threshold);
    if (seed.barcode) {
      await dialog.getByLabel("บาร์โค้ด (ไม่บังคับ)").fill(`SEED${uniqueSuffix()}`);
    }

    await dialog.getByRole("button", { name: "บันทึก" }).click();
    await expect(dialog).toBeHidden();

    // ค้นหาด้วยชื่อเฉพาะเพื่อยืนยันว่าสร้างสำเร็จ ไม่ว่าตารางจะโตแค่ไหนก็ตาม
    await page.getByPlaceholder("ค้นหาชื่อสินค้า").fill(name);
    await expect(page.getByRole("row").filter({ hasText: name })).toBeVisible();
    await page.getByPlaceholder("ค้นหาชื่อสินค้า").fill("");
  }
});
