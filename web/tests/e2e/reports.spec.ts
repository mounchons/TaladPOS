import { test, expect } from "@playwright/test";
import { loginAsManager } from "./helpers/auth";

// รายงาน 4 แท็บ, Manager-only (spec 001 FR-025..028, FR-029)
test.describe("Reports (/reports) - Manager", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsManager(page);
    await page.goto("/reports");
  });

  // RPT-01: แท็บ "ยอดขายวันนี้" แสดง stat 3 ตัว
  test("RPT-01: 'today' tab shows the three stat tiles", async ({ page }) => {
    await expect(page.getByText("ยอดขายรวม")).toBeVisible();
    await expect(page.getByText("ส่วนลดรวม")).toBeVisible();
    await expect(page.getByText("จำนวนบิล")).toBeVisible();
  });

  // RPT-02: แท็บ "สินค้าขายดี" ผูกกับ date range filter
  test("RPT-02: best-selling tab responds to the date range filter", async ({ page }) => {
    await page.getByRole("tab", { name: "สินค้าขายดี" }).click();
    await expect(page.getByRole("columnheader", { name: "สินค้า" })).toBeVisible();
    await expect(page.getByRole("columnheader", { name: "ขายได้" })).toBeVisible();

    // ช่วงวันที่ในอดีตไกล ๆ ที่ไม่มียอดขาย -> ต้องเป็น empty state
    await page.getByLabel("ตั้งแต่วันที่").fill("2020-01-01");
    await page.getByLabel("ถึงวันที่").fill("2020-01-02");
    await expect(page.getByText("ยังไม่มียอดขายในช่วงนี้")).toBeVisible();
  });

  // RPT-03: แท็บ "ยอดขายตามพนักงาน"
  test("RPT-03: sales-by-staff tab shows the staff breakdown table", async ({ page }) => {
    await page.getByRole("tab", { name: "ยอดขายตามพนักงาน" }).click();
    await expect(page.getByRole("columnheader", { name: "พนักงาน" })).toBeVisible();
    await expect(page.getByRole("columnheader", { name: "จำนวนบิล" })).toBeVisible();
  });

  // RPT-04: แท็บ "สต็อกคงเหลือ" เป็น snapshot ไม่มี date filter
  test("RPT-04: stock tab shows a snapshot table with no date filter", async ({ page }) => {
    await page.getByRole("tab", { name: "สต็อกคงเหลือ" }).click();
    await expect(page.getByRole("columnheader", { name: "สินค้า" })).toBeVisible();
    await expect(page.getByRole("columnheader", { name: "คงเหลือ" })).toBeVisible();
    await expect(page.getByLabel("ตั้งแต่วันที่")).toHaveCount(0);
  });

  // RPT-05: ปุ่ม Export บนแท็บสต็อก -> ดาวน์โหลด .xlsx
  test("RPT-05: stock tab export downloads an .xlsx file", async ({ page }) => {
    await page.getByRole("tab", { name: "สต็อกคงเหลือ" }).click();

    const [download] = await Promise.all([
      page.waitForEvent("download"),
      page.getByRole("button", { name: "Export" }).click(),
    ]);

    expect(download.suggestedFilename()).toContain("taladpos-stock-report");
    expect(download.suggestedFilename()).toMatch(/\.xlsx$/);
  });
});
