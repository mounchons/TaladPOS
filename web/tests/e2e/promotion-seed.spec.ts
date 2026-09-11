import { test, expect, type Page } from "@playwright/test";

const MANAGER_USERNAME = "manager";
const MANAGER_PASSWORD = "Manager123!";

interface PromotionSeed {
  scope: "Item" | "Bill";
  discount: number;
  membersOnly: boolean;
  start: string;
  end: string;
}

// เพิ่มโปรโมชั่นหลายรายการผ่านหน้าเว็บจริง (ไม่ใช่ seed ตรง DB) เพื่อให้มีข้อมูล
// หลากหลายสำหรับทดสอบ: ทั้งขอบเขตรายสินค้า/ทั้งบิล, เฉพาะสมาชิก/ลูกค้าทุกคน,
// ช่วงวันที่ต่างกัน (อดีต/ปัจจุบัน/อนาคต)
const SEEDS: PromotionSeed[] = [
  { scope: "Item", discount: 15, membersOnly: false, start: "2026-09-11", end: "2026-09-30" },
  { scope: "Item", discount: 25, membersOnly: true, start: "2026-09-11", end: "2026-10-15" },
  { scope: "Bill", discount: 35, membersOnly: false, start: "2026-09-11", end: "2026-11-30" },
  { scope: "Item", discount: 45, membersOnly: false, start: "2026-01-01", end: "2026-01-31" },
  { scope: "Bill", discount: 55, membersOnly: true, start: "2026-09-11", end: "2026-12-31" },
  { scope: "Item", discount: 65, membersOnly: false, start: "2026-09-11", end: "2026-09-11" },
  { scope: "Item", discount: 5, membersOnly: false, start: "2025-01-01", end: "2025-12-31" },
];

async function login(page: Page) {
  await page.goto("/login");
  await page.getByRole("textbox", { name: "ชื่อผู้ใช้" }).fill(MANAGER_USERNAME);
  await page.getByRole("textbox", { name: "รหัสผ่าน" }).fill(MANAGER_PASSWORD);
  await page.getByRole("button", { name: "เข้าสู่ระบบ" }).click();
  await expect(page).toHaveURL(/\/sales$/);
}

test("seeds multiple promotions (>5) for manual/exploratory testing", async ({ page }) => {
  await login(page);
  await page.goto("/promotions");

  // อ่านรายชื่อสินค้าที่มีอยู่ครั้งเดียว แล้วปิด dialog ทิ้ง
  await page.getByRole("button", { name: "+ สร้างโปรโมชั่น" }).click();
  const probeDialog = page.getByRole("dialog");
  await expect(probeDialog).toBeVisible();
  const productOptions = (await probeDialog.getByRole("combobox").nth(1).locator("option").allTextContents())
    .map((s) => s.trim())
    .filter((s) => s !== "เลือกสินค้า");
  expect(productOptions.length).toBeGreaterThan(0);
  await probeDialog.getByRole("button", { name: "ยกเลิก" }).click();
  await expect(probeDialog).toBeHidden();

  for (const [index, seed] of SEEDS.entries()) {
    await page.getByRole("button", { name: "+ สร้างโปรโมชั่น" }).click();
    const dialog = page.getByRole("dialog");
    await expect(dialog).toBeVisible();

    await dialog
      .getByRole("combobox")
      .nth(0)
      .selectOption({ label: seed.scope === "Item" ? "รายสินค้า" : "ทั้งบิล" });

    if (seed.scope === "Item") {
      const productName = productOptions[index % productOptions.length];
      await dialog.getByRole("combobox").nth(1).selectOption({ label: productName });
    }

    await dialog.getByRole("spinbutton").fill(String(seed.discount));

    await dialog.getByRole("textbox").nth(0).fill(seed.start);
    await dialog.getByRole("textbox").nth(1).fill(seed.end);

    if (seed.membersOnly) {
      await dialog.getByRole("checkbox", { name: "ส่วนลดสำหรับสมาชิกเท่านั้น" }).check();
    }

    await dialog.getByRole("button", { name: "บันทึก" }).click();
    await expect(dialog).toBeHidden();

    const row = page.getByRole("row").filter({ hasText: `${seed.discount}%` }).first();
    await expect(row).toBeVisible();
  }
});
