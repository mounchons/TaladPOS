import { test, expect } from "@playwright/test";
import { loginAsManager } from "./helpers/auth";

function randomPhone(): string {
  return "08" + Math.floor(10000000 + Math.random() * 89999999).toString();
}

// สมัคร/ค้นหาสมาชิก ฝังอยู่ใน /sales (spec 001 FR-010..013)
test.describe("Member registration & search (on /sales)", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsManager(page);
    await page.goto("/sales");
  });

  // MEMBER-01: สมัครสมาชิกใหม่ -> ผูกกับตะกร้าสำเร็จ
  test("MEMBER-01: registers a new member and attaches them to the cart", async ({ page }) => {
    const name = `สมาชิกใหม่-${Date.now()}`;
    const phone = randomPhone();

    await page.getByRole("button", { name: "สมัครสมาชิกใหม่" }).click();
    const dialog = page.getByRole("dialog");
    await expect(dialog).toBeVisible();

    await dialog.getByLabel("ชื่อ").fill(name);
    await dialog.getByLabel("เบอร์โทรศัพท์").fill(phone);
    await dialog.getByRole("button", { name: "สมัครสมาชิก" }).click();
    await expect(dialog).toBeHidden();

    await expect(page.getByText(name)).toBeVisible();
    await expect(page.getByText(phone)).toBeVisible();
  });

  // MEMBER-02: เบอร์ซ้ำ -> 409
  test("MEMBER-02: duplicate phone number is rejected", async ({ page }) => {
    const name1 = `สมาชิกเอ-${Date.now()}`;
    const name2 = `สมาชิกบี-${Date.now()}`;
    const phone = randomPhone();

    await page.getByRole("button", { name: "สมัครสมาชิกใหม่" }).click();
    let dialog = page.getByRole("dialog");
    await dialog.getByLabel("ชื่อ").fill(name1);
    await dialog.getByLabel("เบอร์โทรศัพท์").fill(phone);
    await dialog.getByRole("button", { name: "สมัครสมาชิก" }).click();
    await expect(dialog).toBeHidden();

    // เอาสมาชิกออกจากตะกร้าเพื่อเปิดสมัครสมาชิกใหม่อีกครั้ง
    await page.getByRole("button", { name: "เอาออก" }).click();
    await page.getByRole("button", { name: "สมัครสมาชิกใหม่" }).click();
    dialog = page.getByRole("dialog");
    await dialog.getByLabel("ชื่อ").fill(name2);
    await dialog.getByLabel("เบอร์โทรศัพท์").fill(phone);
    await dialog.getByRole("button", { name: "สมัครสมาชิก" }).click();

    await expect(dialog.getByText("เบอร์โทรศัพท์นี้ถูกใช้สมัครสมาชิกไปแล้ว")).toBeVisible();
    await expect(dialog).toBeVisible();
  });

  // MEMBER-03: เว้นชื่อ/เบอร์ว่าง -> submit ไม่ผ่าน
  test("MEMBER-03: blocks submit when name or phone is empty", async ({ page }) => {
    await page.getByRole("button", { name: "สมัครสมาชิกใหม่" }).click();
    const dialog = page.getByRole("dialog");
    const nameInput = dialog.getByLabel("ชื่อ");
    const isValid = await nameInput.evaluate((el: HTMLInputElement) => el.checkValidity());
    expect(isValid).toBe(false);
  });

  // MEMBER-04: ค้นหาสมาชิกด้วยชื่อ/เบอร์ (debounced) -> เลือกได้
  test("MEMBER-04: searching by name finds and selects the member", async ({ page }) => {
    const name = `ค้นหาสมาชิก-${Date.now()}`;
    const phone = randomPhone();

    await page.getByRole("button", { name: "สมัครสมาชิกใหม่" }).click();
    const dialog = page.getByRole("dialog");
    await dialog.getByLabel("ชื่อ").fill(name);
    await dialog.getByLabel("เบอร์โทรศัพท์").fill(phone);
    await dialog.getByRole("button", { name: "สมัครสมาชิก" }).click();
    await expect(dialog).toBeHidden();

    // เอาออกจากตะกร้าก่อน แล้วค้นหากลับมาใหม่ผ่านช่องค้นหา
    await page.getByRole("button", { name: "เอาออก" }).click();
    const searchBox = page.getByPlaceholder("ค้นหาสมาชิก ชื่อหรือเบอร์โทร");
    await searchBox.fill(name);
    const option = page.getByRole("option", { name: new RegExp(name) });
    await expect(option).toBeVisible();
    await option.click();

    await expect(page.getByText(name)).toBeVisible();
    await expect(page.getByText(phone)).toBeVisible();
  });

  // MEMBER-05: ค้นหาเบอร์ที่ไม่มีในระบบ -> ไม่พบ, ยังสมัครใหม่ต่อได้
  test("MEMBER-05: searching an unknown phone finds nothing but registration still works", async ({ page }) => {
    const searchBox = page.getByPlaceholder("ค้นหาสมาชิก ชื่อหรือเบอร์โทร");
    await searchBox.fill("0899999999999");
    await page.waitForTimeout(500); // ให้ debounce (250ms) + round trip ทำงานจบก่อนเช็ค
    await expect(page.getByRole("listbox")).toHaveCount(0);

    await expect(page.getByRole("button", { name: "สมัครสมาชิกใหม่" })).toBeVisible();
  });
});
