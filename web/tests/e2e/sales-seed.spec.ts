import { test, expect } from "@playwright/test";
import { loginAsManager, uniqueSuffix } from "./helpers/auth";

interface SaleSeed {
  quantity: number;
  withMember: boolean;
}

// ทำรายการขายจริงผ่านหน้า POS (/sales) หลายบิล (ไม่ใช่ seed ตรง DB) เพื่อให้มี
// ประวัติการขายหลากหลายสำหรับทดสอบ: จำนวนชิ้นต่างกัน, มี/ไม่มีสมาชิกผูกบิล
const SEEDS: SaleSeed[] = [
  { quantity: 1, withMember: false },
  { quantity: 2, withMember: true },
  { quantity: 1, withMember: false },
  { quantity: 3, withMember: false },
  { quantity: 1, withMember: true },
  { quantity: 2, withMember: false },
  { quantity: 1, withMember: false },
  { quantity: 4, withMember: true },
  { quantity: 1, withMember: false },
  { quantity: 2, withMember: false },
  { quantity: 1, withMember: true },
  { quantity: 3, withMember: false },
];

test("SALES-SEED: checks out multiple sales (>10) for manual/exploratory testing", async ({ page }) => {
  await loginAsManager(page);

  // สินค้าทดสอบสต็อกเยอะพอสำหรับทุกบิลในลูป (รวมจำนวนที่ขายทั้งหมด <= stock)
  const productName = `สินค้าขายseed-${uniqueSuffix()}`;
  const totalNeeded = SEEDS.reduce((sum, s) => sum + s.quantity, 0);
  await page.goto("/stock");
  await page.getByRole("button", { name: "+ เพิ่มสินค้า" }).click();
  const productDialog = page.getByRole("dialog");
  await productDialog.getByLabel("ชื่อสินค้า").fill(productName);
  await productDialog.getByLabel("URL รูปภาพ").fill("https://example.com/sales-seed.jpg");
  await productDialog.getByLabel("ราคา (บาท)").fill("15.00");
  await productDialog.getByLabel("จำนวนคงเหลือ").fill(String(totalNeeded + 10));
  await productDialog.getByRole("button", { name: "บันทึก" }).click();
  await expect(productDialog).toBeHidden();

  // สมาชิกทดสอบสำหรับบิลที่ withMember: true
  const memberName = `สมาชิกขายseed-${uniqueSuffix()}`;
  const memberPhone = "08" + Math.floor(10000000 + Math.random() * 89999999).toString();

  for (const [index, seed] of SEEDS.entries()) {
    await page.goto("/sales");
    await page.getByPlaceholder("สแกนบาร์โค้ด หรือพิมพ์ชื่อสินค้า").fill(productName);
    // ต้องระบุขอบเขตเป็นชั้นวางสินค้าโดยเฉพาะ เพราะหลังเพิ่มเข้าตะกร้าแล้ว
    // ปุ่ม +/-/เอาออกในตะกร้าก็มี aria-label ที่มีชื่อสินค้าเป็นส่วนหนึ่งด้วย
    const card = page.locator("div.mt-4.grid").getByRole("button", { name: productName });
    for (let i = 0; i < seed.quantity; i++) {
      await card.click();
    }

    if (seed.withMember) {
      if (index === SEEDS.findIndex((s) => s.withMember)) {
        // บิลแรกที่ต้องใช้สมาชิก -> สมัครใหม่ครั้งเดียว
        await page.getByRole("button", { name: "สมัครสมาชิกใหม่" }).click();
        const memberDialog = page.getByRole("dialog");
        await memberDialog.getByLabel("ชื่อ").fill(memberName);
        await memberDialog.getByLabel("เบอร์โทรศัพท์").fill(memberPhone);
        await memberDialog.getByRole("button", { name: "สมัครสมาชิก" }).click();
        await expect(memberDialog).toBeHidden();
      } else {
        // บิลถัดไปที่ต้องใช้สมาชิก -> ค้นหาสมาชิกที่สมัครไว้แล้ว
        const searchBox = page.getByPlaceholder("ค้นหาสมาชิก ชื่อหรือเบอร์โทร");
        await searchBox.fill(memberName);
        const option = page.getByRole("option", { name: new RegExp(memberName) });
        await expect(option).toBeVisible();
        await option.click();
      }
    }

    await page.getByRole("button", { name: "ชำระเงิน" }).click();
    const receiptDialog = page.getByRole("dialog").filter({ hasText: "ขายสำเร็จ" });
    await expect(receiptDialog).toBeVisible();
    await receiptDialog.getByRole("button", { name: "ขายรายการต่อไป" }).click();
    await expect(receiptDialog).toBeHidden();
  }
});
