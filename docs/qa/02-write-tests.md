[← บทที่ 1](01-read-codebase.md) · [สารบัญ](README.md) · บทถัดไป: [03 รันเทสต์](03-run-tests.md)

# บทที่ 2 — สั่งเขียนเทสต์จากแผน

## เป้าหมาย

แปลแผนจากบทที่ 1 เป็นโค้ดเทสต์ที่รันได้จริง **ทีละชั้น ทีละหัวข้อ** ไม่ใช่ทีเดียวทั้ง 265 เคส

---

## กฎข้อแรก: อย่าสั่งทีเดียวทั้งแผน

```
❌ เขียนเทสต์ทั้ง 265 เคสตาม test-plan.md
```

AI จะทำได้ แต่คุณจะได้ไฟล์ 25 ไฟล์ที่ตรวจไม่ไหว และถ้ามันเข้าใจผิดตั้งแต่เคสที่ 10
ความผิดนั้นจะถูกคัดลอกไปอีก 255 เคส

**สั่งทีละหัวข้อของแผน** (§4, §5.2, §6.4 …) แล้วรันดูผลก่อนไปหัวข้อถัดไป
หัวข้อหนึ่งประมาณ 10–25 เคส ซึ่งเป็นขนาดที่คนอ่านตรวจไหว

---

## ลำดับที่ควรทำ

ทำจากชั้นล่างขึ้นบน เพราะชั้นล่างเร็วและไม่ต้องพึ่งอะไรเลย

```
1. Application (§4)  → ไม่ต้องมี DB  → รันเสร็จใน 25 ms
2. API (§5)          → ต้องมี DB     → รันเสร็จใน ~3 วินาที
3. Web unit          → ต้องติดตั้ง Vitest ก่อน
4. UI E2E (§6)       → ต้องติดตั้ง Playwright ก่อน  ← ยากสุด ทำท้ายสุด
```

---

## 2.1 ชั้น Application — จุดเริ่มที่ง่ายที่สุด

### ❌ Prompt อ่อน

```
เขียนเทสต์ให้ CreatePromotionUseCase
```

**จะได้อะไร**: AI อ่านโค้ด use case แล้วเขียนเทสต์ตามที่โค้ดทำ — ถ้าโค้ดลืมตรวจว่า `productId` มีจริงไหม
เทสต์ก็จะไม่ตรวจเรื่องนั้นเหมือนกัน และมันอาจสร้างเทสต์ที่ต่อ database จริง ซึ่งผิด constitution ข้อ III

---

### ✅ Prompt ดี

```
เขียนเทสต์ตาม test-plan.md หัวข้อ 4 เฉพาะ 4 เคสนี้:
TC-APP-PROMO-001, TC-APP-PROMO-002, TC-APP-PROMO-003, TC-APP-PROMO-004

ข้อกำหนด:
- ค่าที่ assert ต้องมาจาก spec.md (FR-019) และ contracts/promotions.md
  ไม่ใช่จากพฤติกรรมของโค้ดปัจจุบัน
- ทำตามสไตล์ของเทสต์ที่มีอยู่แล้ว ดูตัวอย่างจาก
  api/tests/TaladPOS.Application.Tests/Products/ProductUseCaseTests.cs
- ห้ามต่อ database จริง ใช้ fake repository ตาม constitution ข้อ III
  (ชั้น Domain/Application ต้องรันได้โดยไม่มี PostgreSQL)
- ชื่อเทสต์ใส่รหัส TC ไว้ใน XML doc comment เพื่อให้ย้อนกลับมาที่แผนได้
- เขียนเสร็จแล้วรัน dotnet test tests/TaladPOS.Application.Tests ให้ดูผลด้วย

ถ้าเทสต์แดงเพราะโค้ดจริงไม่ตรงกับสเปก ให้หยุดแล้วรายงาน อย่าแก้เทสต์ให้เขียว
```

**บรรทัดสุดท้ายสำคัญที่สุด** ถ้าไม่ใส่ AI มักจะปรับ assertion ให้เข้ากับโค้ดจนเขียว
แล้วรายงานว่า "เสร็จแล้ว ผ่านหมด" ทั้งที่เพิ่งกลบบั๊กไป

---

### 🔮 ผลที่คาด

```
api/tests/TaladPOS.Application.Tests/Promotions/PromotionUseCaseTests.cs   (ไฟล์ใหม่)

Passed! - Failed: 0, Passed: 43, Skipped: 0, Total: 43 - TaladPOS.Application.Tests.dll
                              ↑ เดิม 39 + ใหม่ 4
```

พร้อมสรุปจาก AI ว่าเคสไหนเขียว เคสไหนแดง และถ้าแดงเพราะอะไร

---

## 2.2 ชั้น API Integration — ต้องใช้ฐานข้อมูลจริง

ชั้นนี้มีโครงสร้างพร้อมอยู่แล้ว **อย่าให้ AI สร้างใหม่** ต้องบอกให้มันใช้ของเดิม

`api/tests/TaladPOS.Api.IntegrationTests/Infrastructure/ApiTestBase.cs` มีให้แล้ว:

| สิ่งที่มีให้ | ใช้ทำอะไร |
|---|---|
| `ManagerUsername` / `ManagerPassword` | บัญชี seed สำหรับทดสอบสิทธิ์ Manager |
| `CashierUsername` / `CashierPassword` | บัญชี seed สำหรับทดสอบว่า Cashier ถูกปฏิเสธ |
| `CreateAuthenticatedClientAsync(...)` | ได้ `HttpClient` ที่แนบ JWT ให้เรียบร้อย |
| `InitializeAsync()` | ล้างและ seed ข้อมูลใหม่**ก่อนทุกเทสต์** |
| `PagedResponse<T>` | ตัวรับ envelope `{items, page, pageSize, totalCount, totalPages}` |

### ✅ Prompt ดี

```
เขียนเทสต์ตาม test-plan.md หัวข้อ 5.2 (Products) เคส TC-API-PROD-012 ถึง TC-API-PROD-016

ข้อกำหนด:
- สืบทอดจาก ApiTestBase ที่มีอยู่แล้ว ห้ามสร้าง WebApplicationFactory ใหม่
- ใช้ CreateAuthenticatedClientAsync กับบัญชี manager และ cashier ที่ base class มีให้
- status code และรูปแบบ error body ต้องตรงกับ contracts/products.md
  ไม่ใช่ตรงกับสิ่งที่โค้ดส่งออกมาตอนนี้
- TC-API-PROD-013 (Cashier โดน 403) สำคัญที่สุด เพราะ FR-029 เป็นเรื่องความปลอดภัย

หมายเหตุ: R-08 ใน test-plan.md ยังไม่ได้ข้อสรุปว่ารูปแบบ error body ฝั่งไหนถูก
ถ้าเคสไหนติดปัญหานี้ ให้ mark Skip พร้อมเขียนเหตุผลอ้าง R-08 อย่าเดาเอง
```

**ย่อหน้าสุดท้ายคือทักษะที่ต้องสอน** — เมื่อสเปกยังไม่ชัด ทางที่ถูกคือ *หยุดแล้วบอก* ไม่ใช่เดา
`Skip` ที่มีเหตุผลกำกับคือหนี้ที่มองเห็น ส่วนเทสต์ที่เดาแล้วเขียวคือหนี้ที่มองไม่เห็น

---

### 🔮 ผลที่คาด

```
api/tests/TaladPOS.Api.IntegrationTests/ProductsCrudTests.cs   (ไฟล์ใหม่)

Passed! - Failed: 0, Passed: 13, Skipped: 1, Total: 14
                                   ↑ TC-API-PROD-015 รอข้อสรุป R-08
```

---

## 2.3 ตั้งค่า Playwright — สั่งให้ AI ทำให้

> ⚠️ **`web/` ยังไม่มีเครื่องมือทดสอบใด ๆ เลย** ไม่มี Playwright ไม่มี Vitest ไม่มี Jest
> หัวข้อนี้จึงเป็นบทเรียนเรื่อง "สั่ง AI ตั้งค่าเครื่องมือ" ซึ่งเป็นทักษะที่ต้องฝึกจริง

### ❌ Prompt อ่อน

```
ติดตั้ง Playwright ให้หน่อย
```

**จะได้อะไร**: `npm init playwright@latest` แล้วได้ config เริ่มต้นที่ชี้ไปที่ `localhost:3000`
โดยไม่รู้ว่าต้องสตาร์ท API ด้วย ไม่รู้ว่าต้องล้าง database ก่อนรัน และตั้ง `workers` แบบขนาน
ซึ่งจะทำให้เทสต์แย่งสต็อกสินค้ากันจนแดงสลับเขียว

---

### ✅ Prompt ดี

```
ติดตั้ง Playwright ใน web/ สำหรับ E2E test ตาม test-plan.md หัวข้อ 6

ต้องตั้งค่าให้ครบ 4 อย่างนี้ เพราะโปรเจกต์นี้มีข้อจำกัดเฉพาะ:

1. webServer — สตาร์ทให้เองทั้ง API และ web
   API อยู่ที่ http://localhost:5054 (ดู api/src/TaladPOS.Api/Properties/launchSettings.json)
   web อยู่ที่ http://localhost:3000 และอ่าน NEXT_PUBLIC_API_BASE_URL จาก .env.local

2. globalSetup — ล้างและ seed ข้อมูลใหม่ก่อนเริ่มรันทุกครั้ง
   เหตุผล: สต็อกสินค้าลดลงทุกครั้งที่เทสต์ checkout ถ้าไม่ล้าง รันรอบสองจะแดง
   เรื่องนี้เคยเกิดขึ้นจริงแล้ว บันทึกไว้ใน quickstart-results.md
   ดูวิธีล้างได้จาก ResetTransactionalDataAsync() ใน TaladPOSApiFactory.cs

3. workers: 1 — เพราะเทสต์ที่เขียนข้อมูลใช้ database เดียวกัน รันขนานแล้วแย่งสต็อกกัน

4. storageState — login ครั้งเดียวเก็บ session ของ manager กับ cashier ไว้ใช้ซ้ำ
   หมายเหตุ: โปรเจกต์นี้เก็บ JWT ใน localStorage (ดู web/src/lib/api/tokenStore.ts)
   ไม่ใช่ cookie ต้องตั้ง storageState ให้ครอบ localStorage ด้วย

เสร็จแล้วเขียนเทสต์ตัวอย่าง 1 ตัว (TC-UI-AUTH-001 ล็อกอิน manager) เพื่อพิสูจน์ว่า
ทุกอย่างต่อกันติดจริง แล้วรันให้ดู

ยังไม่ต้องเขียนเคสอื่น
```

**ทำไมต้องบอกละเอียดขนาดนี้** — ทั้ง 4 ข้อคือสิ่งที่ AI ไม่มีทางเดาได้จากโค้ด แต่ถ้าไม่มี
ชุดเทสต์จะแดงสลับเขียวจนไม่มีใครเชื่อถือภายในสองสัปดาห์

---

### 🔮 ผลที่คาด

```
web/
  playwright.config.ts        ← webServer + globalSetup + workers: 1
  e2e/
    global-setup.ts           ← reset + seed database
    auth.setup.ts             ← login เก็บ storageState
    auth.spec.ts              ← TC-UI-AUTH-001
  .auth/
    manager.json              ← session ที่เก็บไว้ (ต้องใส่ .gitignore)
    cashier.json
  package.json                ← เพิ่ม script "test:e2e"

Running 1 test using 1 worker
  ✓ auth.spec.ts:8:1 › TC-UI-AUTH-001 ล็อกอิน manager สำเร็จ (2.1s)
1 passed (4.5s)
```

---

## 2.4 เขียนเทสต์ UI ตามแผน

### ✅ Prompt ดี

```
เขียนเทสต์ E2E ตาม test-plan.md หัวข้อ 6.4 (/stock) เคส TC-UI-STOCK-001 ถึง 007
เท่านั้น อย่าเพิ่งทำเคสอื่น

ข้อกำหนด:
- ชื่อ test ขึ้นต้นด้วยรหัส TC เช่น test('TC-UI-STOCK-003 เพิ่มสินค้าใหม่', ...)
  เพื่อให้เทสต์แดงย้อนกลับมาที่แผนได้ทันที
- ใช้ storageState ของ manager ไม่ต้อง login ใหม่ทุกเทสต์
- เลือก element ด้วย getByRole + ข้อความไทยที่อยู่ในโค้ดจริง
  ถ้าจุดไหนไม่มี selector ที่เสถียรพอ ให้บอกมาว่าจุดไหน อย่าเดา
  (D-2 ใน test-plan.md เรื่องจะเพิ่ม data-testid หรือไม่ ยังไม่ได้ข้อสรุป)
- เทสต์ที่สร้างข้อมูล ต้องลบข้อมูลของตัวเองทิ้งท้ายเทสต์
- ห้ามใช้ waitForTimeout รอเวลาแบบตายตัว ให้ใช้ expect ที่รอเงื่อนไขแทน
```

**สามบรรทัดสุดท้ายคือสิ่งที่แยกชุดเทสต์ที่ใช้ได้จริงออกจากชุดที่ต้องทิ้ง:**

| ข้อกำหนด | ถ้าไม่ใส่จะเกิดอะไร |
|---|---|
| บอกมาถ้าไม่มี selector ที่ดี | AI จะเดา selector เปราะ ๆ เช่น `.btn:nth-child(3)` ซึ่งพังทันทีที่แก้ CSS |
| ลบข้อมูลของตัวเองทิ้ง | ข้อมูลขยะสะสมจนเทสต์อื่นแดง |
| ห้าม `waitForTimeout` | เทสต์แดงสลับเขียวตามความเร็วเครื่อง — ปัญหาที่แก้ยากที่สุด |

---

## วิธีตรวจว่าเทสต์ที่ได้ดีจริง

**เช็กด้วยตา:**
- [ ] จำนวนเทสต์ที่รันได้ ตรงกับจำนวนเคสที่สั่งไป (สั่ง 5 ต้องได้ 5 ไม่ใช่ 3)
- [ ] ทุกเทสต์มีรหัส TC อ้างกลับไปที่แผน
- [ ] เทสต์ชั้น Domain/Application ไม่มีคำว่า `DbContext`, `Npgsql`, `ConnectionString` เลย
- [ ] ไม่มี `assert` ที่ไม่ตรวจอะไร เช่น `result.Should().NotBeNull()` อย่างเดียวจบ
- [ ] ค่าที่ assert ตรงกับที่ contract เขียนไว้ (เปิดเทียบจริง อย่าเชื่อคำสรุป)

**เช็กด้วยการทดลอง — สำคัญกว่าเช็กด้วยตา:**

```
พิสูจน์ว่าเทสต์ที่เพิ่งเขียนจับบั๊กได้จริง โดยแก้โค้ดใน
api/src/TaladPOS.Application/Promotions/CreatePromotionUseCase.cs
ให้ผิดโดยตั้งใจ 1 จุด แล้วรันเทสต์ให้ดูว่าแดง แล้วแก้กลับ
```

ถ้าทำให้โค้ดพังแล้ว **เทสต์ยังเขียวอยู่ แปลว่าเทสต์นั้นไม่ได้ตรวจอะไรเลย** ต้องเขียนใหม่

---

## แบบฝึกหัด

**1.** ใช้ prompt ในหัวข้อ 2.1 เขียน `TC-APP-PROMO-001` ถึง `004` จริง แล้วรัน `dotnet test`
บันทึกว่าจำนวนเทสต์เพิ่มจาก 39 เป็นเท่าไร

**2.** ทำการทดลอง "แก้โค้ดให้พัง" กับเทสต์ที่เพิ่งเขียน — ถ้ายังเขียว เขียน prompt สั่งแก้ให้จับได้

**3.** ลองใช้ prompt แบบอ่อน (`ติดตั้ง Playwright ให้หน่อย`) ในโปรเจกต์นี้ แล้วเปรียบเทียบ
`playwright.config.ts` ที่ได้ กับข้อกำหนด 4 ข้อในหัวข้อ 2.3 — ขาดข้อไหนไปบ้าง

**4.** (ยาก) เขียน prompt ให้ AI เขียน `TC-API-PROD-006` (pageSize นอกช่วงต้องได้ 400)
โดยที่ prompt ต้องอธิบายด้วยว่าทำไม **การปัดค่าให้เข้าช่วงแล้วตอบ 200 ถึงผิด** — คำใบ้อยู่ใน FR-035

---

[← บทที่ 1](01-read-codebase.md) · [สารบัญ](README.md) · บทถัดไป: [03 รันเทสต์](03-run-tests.md)
