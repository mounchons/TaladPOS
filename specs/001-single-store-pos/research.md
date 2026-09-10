# Research: ระบบ POS สำหรับร้านค้าเดี่ยว (Single-Store POS)

**Date**: 2026-09-10
**Input**: [spec.md](./spec.md), [plan.md](./plan.md), `.specify/memory/constitution.md`

สเปกและ Technical Context ไม่มี `NEEDS CLARIFICATION` เหลืออยู่ (ประเด็นทางธุรกิจถูกแก้ไปแล้วในขั้นตอน
`/speckit-clarify`) เอกสารนี้จึงเน้นตัดสินใจประเด็น **ทางเทคนิค/สถาปัตยกรรม** ที่จำเป็นก่อนออกแบบ data model
และ contracts ในขั้นตอนถัดไป

## 1. การยืนยันตัวตนพนักงาน (Authentication) สำหรับ FR-007/FR-009

**Decision**: ใช้ JWT bearer token — พนักงานล็อกอินผ่าน `POST /api/v1/auth/login` ด้วย username/password, API
ตรวจสอบรหัสผ่านด้วย `PasswordHasher<Staff>` (จาก `Microsoft.AspNetCore.Identity`) แล้วออก short-lived JWT
(รวม staff id + role claim) ให้ `web/` เก็บไว้ฝั่ง client และแนบเป็น `Authorization: Bearer` ทุก request
"ล็อกเอาต์" (FR-009) คือการทิ้ง token ฝั่ง client (ไม่ต้องมี server-side session store)

**Rationale**: `api/` และ `web/` เป็นคนละ origin ตามหลัก Separation of API and Frontend (constitution
Principle I) — JWT เป็นแนวทางมาตรฐานสำหรับ REST API ที่ frontend เรียกข้าม origin โดยไม่ต้องพึ่ง cookie/session
affinity ฝั่งเซิร์ฟเวอร์ ทำให้ API ยัง stateless และ scale ไปหลายจุดขาย (multi-register) ได้ตรงตามผลการ clarify
เรื่อง concurrency; การใช้ `PasswordHasher<Staff>` แบบ standalone (ไม่ดึงทั้ง ASP.NET Core Identity framework
พร้อม `IdentityDbContext`) ทำให้ `Staff` ยังคงเป็น domain entity ที่ควบคุมรูปร่างเองได้เต็มที่ตามหลัก DDD
(constitution Principle II) แทนที่จะถูกบังคับด้วย schema ของ Identity

**Alternatives considered**:
- ASP.NET Core Identity เต็มรูปแบบ + cookie auth — ถูกปฏิเสธ เพราะ Identity's `IdentityDbContext`/`IdentityUser`
  ทำให้ domain model ผูกกับ infrastructure concern (ขัดกับ DDD) และ cookie auth ข้าม origin ต้องจัดการ
  CORS/SameSite เพิ่มความซับซ้อนโดยไม่จำเป็นสำหรับ scope นี้
- Session ฝั่งเซิร์ฟเวอร์ (in-memory/Redis) — ถูกปฏิเสธ เพราะเพิ่ม stateful component ที่ไม่จำเป็นสำหรับร้านค้าเดี่ยว
  ขนาดเล็ก และซับซ้อนกว่า JWT โดยไม่ได้ประโยชน์เพิ่มในขอบเขตนี้

## 2. การตัดสต็อกแบบปลอดภัยจากการแข่งขัน (Concurrency-safe Stock Deduction) สำหรับหลายจุดขาย

**Decision**: ใช้ atomic conditional update ที่ระดับฐานข้อมูล — คำสั่งตัดสต็อกออกแบบเป็น
`UPDATE products SET stock_quantity = stock_quantity - @qty WHERE id = @id AND stock_quantity >= @qty`
ภายใน database transaction เดียวกับการบันทึก Sale/SaleLineItem ถ้าจำนวนแถวที่ได้รับผลกระทบ (affected rows) เป็น 0
แปลว่าสต็อกไม่พอ ให้ยกเลิกทั้ง transaction และแจ้งพนักงานว่าสินค้าหมด (ตาม Edge Case ในสเปก) วิธีนี้เขียนด้วย EF Core 8
`ExecuteUpdateAsync` พร้อม predicate ในชั้น `TaladPOS.Infrastructure`

**Rationale**: PostgreSQL รับประกันความเป็น atomic ของ single-statement UPDATE ระดับแถวอยู่แล้ว จึงไม่จำเป็นต้องพึ่ง
optimistic concurrency token + retry loop (ซับซ้อนกว่าและต้องจัดการ retry เอง) หรือ pessimistic lock
(`SELECT ... FOR UPDATE`, ถือ lock นานกว่าและเสี่ยง deadlock เมื่อมีหลายจุดขายพร้อมกัน) วิธีนี้ตรงตามข้อกำหนด FR-016
และ Edge Case ที่ผลการ clarify กำหนดไว้ (อนุญาตเฉพาะบิลที่ตัดสต็อกสำเร็จก่อน) โดยความซับซ้อนต่ำสุด

**Alternatives considered**:
- Optimistic concurrency (EF Core `RowVersion`/`xmin`) พร้อม retry — ถูกปฏิเสธเป็นตัวเลือกหลัก เพราะภายใต้จุดขาย
  หลายเครื่องแย่งสินค้าชิ้นสุดท้ายพร้อมกัน จะเกิด retry ซ้ำหลายรอบโดยไม่จำเป็น ในเมื่อ conditional UPDATE ให้ผลลัพธ์
  ที่ถูกต้องในครั้งเดียว
- Pessimistic row lock (`SELECT ... FOR UPDATE`) — ถูกปฏิเสธเป็นตัวเลือกหลัก เพราะถือ lock ตลอดที่ทำ business logic
  อื่นในบิลเดียวกัน (คำนวณส่วนลด, บันทึกสมาชิก) ทำให้ throughput ลดลงโดยไม่จำเป็นเมื่อ conditional UPDATE ทำได้เร็วกว่า
- Serializable isolation level ทั้ง transaction — ถูกปฏิเสธ เพราะ overhead สูงเกินความจำเป็นสำหรับ scope ร้านค้าเดี่ยว

## 3. การเลือกส่วนลดที่ดีที่สุด (Best-of Discount Resolution) สำหรับ FR-022

**Decision**: สร้าง Domain Service ชื่อ `DiscountResolver` ใน `TaladPOS.Domain` รับ input เป็นรายการ candidate
discounts ที่ใช้ได้กับ line item/บิลนั้น ๆ (จากโปรโมชั่นทั่วไปที่ยัง active ตามช่วงวันที่ + ส่วนลดสมาชิกถ้ามีสมาชิกผูกบิล)
แล้วคืนค่าเฉพาะส่วนลดที่ให้มูลค่าสูงสุดหนึ่งรายการ ไม่รวมหลายส่วนลดเข้าด้วยกัน

**Rationale**: เป็น business rule ที่ต้องทดสอบแยกจาก infrastructure ได้ตรง ๆ ตาม constitution Principle III
(Test-First for Business Logic) การแยกเป็น domain service ทำให้ unit test ครอบคลุม edge case ได้ครบ (ไม่มีโปรโมชั่น,
มีโปรโมชั่นอย่างเดียว, มีส่วนลดสมาชิกอย่างเดียว, มีทั้งคู่แต่ค่าเท่ากัน ฯลฯ) โดยไม่ต้องพึ่งฐานข้อมูลหรือ ASP.NET Core

**Alternatives considered**:
- คำนวณส่วนลดใน Application layer use case โดยตรง — ถูกปฏิเสธ เพราะ business rule "เลือกส่วนลดสูงสุด" เป็นกฎทาง
  ธุรกิจที่ควรอยู่ใน Domain layer ตาม DDD ไม่ใช่ orchestration logic ของ use case
- ให้ฐานข้อมูลคำนวณผ่าน SQL/stored procedure — ถูกปฏิเสธ เพราะขัดกับ Principle II ที่กำหนดให้ business logic
  อยู่ใน domain layer ของ .NET ไม่ใช่ฝังอยู่ในชั้นข้อมูล และทดสอบเป็น unit test ได้ยากกว่า

## 4. การแจ้งเตือนสินค้าใกล้หมด (Low-stock Notification) สำหรับ FR-017/SC-003

**Decision**: ไม่ต้องมี push/real-time channel (SignalR/WebSocket) — คำนวณสถานะ "ใกล้หมด" เป็น computed flag
(`stock_quantity <= low_stock_threshold`) ตอนตอบกลับ query สินค้า (`GET /api/v1/products`) และแสดงผลใน `web/` ตอนโหลด/
รีเฟรชหน้าจอสต็อก

**Rationale**: SC-003 กำหนดไว้ชัดว่า "เห็นภายในการใช้งานหน้าจอสต็อกครั้งถัดไป" ไม่ได้ต้องการ real-time push
การเพิ่ม SignalR/WebSocket จะเป็นความซับซ้อนเกินความจำเป็น (YAGNI) สำหรับร้านค้าเดี่ยวขนาดเล็ก

**Alternatives considered**:
- SignalR push แจ้งเตือนทันทีที่สต็อกลดต่ำกว่าเกณฑ์ — ถูกปฏิเสธ เพราะเกินความต้องการที่ระบุใน success criteria
  และเพิ่ม infrastructure component (persistent connection) โดยไม่มี requirement รองรับ

## 5. รูปแบบใบเสร็จ (Receipt) สำหรับ FR-030

**Decision**: `GET /api/v1/sales/{id}/receipt` คืนข้อมูลใบเสร็จเป็น JSON (รายการสินค้า/ราคา/ส่วนลด/ยอดรวม) ให้
`web/` render เป็นหน้าใบเสร็จ HTML แล้วใช้ browser print (`window.print()`) เพื่อพิมพ์ ไม่ผูกกับเครื่องพิมพ์ใบเสร็จ
เฉพาะทาง (thermal printer) ในเวอร์ชันนี้

**Rationale**: สอดคล้องกับ Assumption ในสเปกที่ระบุว่าใบเสร็จเป็นแบบง่าย ไม่ใช่ใบกำกับภาษี และไม่มีการระบุอุปกรณ์
พิมพ์เฉพาะทางในข้อกำหนด การ render ผ่านเบราว์เซอร์ทำให้ไม่ต้องเพิ่ม dependency ด้าน hardware driver

**Alternatives considered**:
- สร้าง PDF ฝั่งเซิร์ฟเวอร์ (เช่น QuestPDF) — ถูกปฏิเสธสำหรับเวอร์ชันนี้ เพราะเพิ่ม dependency โดยไม่มีข้อกำหนดว่าต้อง
  ได้ไฟล์ PDF ที่ดาวน์โหลดได้ การพิมพ์จากหน้าเว็บเพียงพอต่อ FR-030
- เชื่อมต่อเครื่องพิมพ์ใบเสร็จความร้อน (ESC/POS) โดยตรง — ถูกปฏิเสธ เพราะ Assumption ในสเปกระบุว่าไม่รวมอุปกรณ์ฮาร์ดแวร์
  เฉพาะทางไว้ในขอบเขตเวอร์ชันนี้

### 5.1 การแยกใบเสร็จออกจากหน้าจอตอนสั่งพิมพ์ (เพิ่มระหว่าง implement — TD02)

**Decision**: ทำเครื่องหมายตัวสลิปด้วย `data-receipt-print` แล้วใน `@media print` ซ่อนทุกอย่างด้วย
`body * { visibility: hidden }` เปิดเฉพาะ `[data-receipt-print]` กับลูก ๆ แล้วยกสลิปออกจาก flow
(`position: absolute; top: 0`) พร้อมจำกัด `max-width: 22rem` และจัดกึ่งกลาง

**Rationale**: ข้อ 5 ตัดสินใจว่าใช้ `window.print()` ของเบราว์เซอร์ แต่ไม่ได้ครอบคลุมว่า *อะไรบ้าง* ที่จะถูกพิมพ์
ออกมา ของเดิมมี print rule แค่ `body { background: #fff }` จึงพิมพ์ app bar/ตารางสินค้า/แผงเงินติดมาด้วย
และเมื่อใบเสร็จย้ายมาอยู่ใน modal (TD02) ก็ยังมี dialog chrome เพิ่มอีกชั้น

เลือก `visibility` แทน `display: none` เพราะ `display: none` ที่ ancestor จะพา element ลูกหายไปด้วย ทำให้ซ่อน
ทุกอย่างแล้วเปิดเฉพาะสลิปไม่ได้ ส่วน `max-width` จำเป็นเพราะถ้าปล่อยให้กว้างเต็มแผ่น ป้ายกับจำนวนเงินของแต่ละแถว
จะถูกดันไปคนละขอบกระดาษ (บนจอไม่เห็นปัญหานี้เพราะ host จำกัดความกว้างไว้ที่ 22rem อยู่แล้ว)

กฎชุดเดียวใช้ได้ทั้งใบเสร็จใน modal และหน้า `/sales/receipt/{id}` เพราะผูกกับ attribute บนตัวสลิป ไม่ผูกกับ
โครงหน้าใดหน้าหนึ่ง

**Alternatives considered**:
- `print:hidden` (Tailwind `display: none`) รายจุดบนทุก element ที่ไม่ต้องการ — ถูกปฏิเสธ เพราะต้องไล่แปะทั่วทั้ง
  แอปและจะพังเงียบ ๆ ทุกครั้งที่เพิ่ม UI ใหม่ที่ลืมแปะ
- เปิดใบเสร็จเป็นหน้าต่าง/แท็บใหม่แล้วสั่งพิมพ์จากที่นั่น — ถูกปฏิเสธ เพราะ popup ถูกบล็อกได้และทำให้พนักงานหลุด
  ออกจากหน้าขายกลางคัน ซึ่งขัดกับเหตุผลที่ย้ายใบเสร็จมาเป็น modal ตั้งแต่แรก

**การตรวจสอบ**: สั่งพิมพ์เป็น PDF จริงจากทั้งสองทาง ได้ไฟล์หน้าเดียวทั้งคู่ที่มีเฉพาะสลิป — การตรวจด้วย computed
style อย่างเดียวไม่พอ เพราะมองไม่เห็นทั้งการแบ่งหน้าและปัญหาความกว้างที่กล่าวข้างต้น

## 6. รายงาน (Reports) สำหรับ FR-025–FR-028

**Decision**: implement เป็น read-only application queries (`GetDailySalesReport`, `GetBestSellingProductsReport`,
`GetSalesByStaffReport`, `GetStockReport`) ที่ query ตรงจาก EF Core `DbContext` ด้วย LINQ `GroupBy`/aggregate
ไม่มี read-model/data warehouse แยกต่างหาก

**Rationale**: ปริมาณข้อมูลระดับร้านค้าเดี่ยว (Scale/Scope ใน Technical Context) เล็กพอที่ query ตรงจาก
transactional table จะตอบสนองได้ตาม SC-004 (ไม่มีความล่าช้าของข้อมูล) โดยไม่ต้องเพิ่มความซับซ้อนของ CQRS/ETL

**Alternatives considered**:
- แยก reporting database/read replica — ถูกปฏิเสธ เพราะเกินความจำเป็นสำหรับ scale ของร้านค้าเดี่ยว และขัดกับหลัก
  Simplicity/YAGNI ไม่มี requirement ด้าน scale ที่ต้องการ read replica

## 7. PrimeReact เป็น UI component library ร่วมกับ Tailwind CSS ใน `web/`

> **แทนที่แล้วโดยข้อ 8 (รอบที่ 2)** — ข้อนี้เก็บไว้เป็นบันทึกว่าเคยตัดสินใจอะไรและด้วยเหตุผลใด ไม่ใช่สภาพปัจจุบัน
> PrimeReact ถูกถอดออกทั้งหมดแล้ว เหตุผลที่เปลี่ยนอยู่ในข้อ 8

**Decision**: ใช้ PrimeReact ในโหมด **unstyled** (`unstyled` prop / `PrimeReactProvider` แบบไม่โหลดธีม CSS สำเร็จรูป)
แล้วกำหนดหน้าตาด้วย Tailwind utility classes ผ่าน `pt` (passthrough) prop ของแต่ละ component (`DataTable`,
`Dialog`, `Button` ฯลฯ) หรือ preset ที่กำหนดรวมไว้ที่เดียวใน `web/src/styles/` แทนการโหลด PrimeReact theme
(เช่น Lara/Material) เต็มรูปแบบ

**Rationale**: constitution Principle IV กำหนดให้ Tailwind CSS เป็น styling stack ของ `web/` — โหมด unstyled ทำให้
PrimeReact ทำหน้าที่เฉพาะ behavior/accessibility/structure ของ component (เช่น keyboard navigation ของ DataTable,
focus trap ของ Dialog) โดยไม่นำ CSS theme ของตัวเองมาซ้อนทับหรือขัดแย้งกับ Tailwind utility classes ทำให้ยังมี
"แหล่งความจริงเดียว" (single source of truth) ด้าน styling ตามที่ constitution กำหนด และลดความเสี่ยงเรื่อง CSS
specificity conflict ระหว่างสอง design system

**Alternatives considered**:
- ใช้ PrimeReact styled mode พร้อมธีมสำเร็จรูป (เช่น Lara, Material) — ถูกปฏิเสธ เพราะจะมี CSS 2 ระบบทำงานพร้อมกัน
  (PrimeReact theme CSS + Tailwind utility CSS) เสี่ยงต่อการปะทะกันของ style และขัดกับเจตนาให้ Tailwind เป็นแหล่ง
  styling หลักเพียงแหล่งเดียวตาม constitution
- เขียน component เอง (ProductCard, DataTable, Dialog) ทั้งหมดโดยไม่ใช้ library — ถูกปฏิเสธ เพราะผู้ใช้ระบุชัดเจนว่า
  ต้องการใช้ PrimeReact สำหรับ DataTable/Dialog/Button แทนการเขียนเองทั้งหมด เพื่อลดเวลาพัฒนาและได้ accessibility/
  behavior ที่ผ่านการทดสอบมาแล้ว

## 8. เปลี่ยน UI library จาก PrimeReact เป็น daisyUI (รอบที่ 2 — แทนที่ข้อ 7)

**Decision**: ใช้ **daisyUI 5.7.32 บน Tailwind CSS 4.3.3** — อัปเกรด Tailwind 3.4.1 → 4 เป็นงานลำดับแรกของรอบนี้
แล้วลงทะเบียน daisyUI แบบ CSS-first ใน `web/src/app/globals.css` ตามเอกสารที่ผู้ใช้ส่งมา

**ต้องมีสองบล็อก ไม่ใช่บล็อกเดียว** — จุดที่พลาดง่ายที่สุดของการย้ายนี้คือใส่แต่ธีม daisyUI แล้วสีเดิมหายทั้งแอป
เพราะตัวแปรใน `@plugin "daisyui/theme"` สร้างแค่ token ของ daisyUI (`btn-primary`, `bg-base-200`) **ไม่ได้สร้าง**
utility class เดิมอย่าง `bg-steel-50`, `text-ink-300`, `border-chili/30`, `font-display`, `rounded-control`
ที่กระจายอยู่ทั้ง 15 ไฟล์ — ตัวเหล่านั้นมาจาก `@theme` เท่านั้น

```css
@import "tailwindcss";
@plugin "daisyui";

/* บล็อกที่ 1 — ธีม Tailwind เดิมทั้งชุด ย้ายมาจาก tailwind.config.ts ตรง ๆ
   ตัวนี้คือตัวที่ทำให้ utility class เดิมทุกตัวยังใช้ได้ */
@theme {
  --color-ink: #17242F;
  --color-ink-700: #24384A;
  --color-ink-500: #4A6376;
  --color-ink-300: #8598A6;
  --color-steel-50: #F0F3F5;
  --color-steel-100: #E2E8EC;
  --color-steel-200: #CFD8DE;
  --color-mango: #F5A524;
  --color-mango-600: #D98A0B;
  --color-mango-100: #FDF0D6;
  --color-leaf: #2F7D4F;
  --color-chili: #C8362B;
  --font-display: var(--font-kanit), sans-serif;
  --font-sans: var(--font-plex-thai), sans-serif;
  --radius-control: 5px;
}

/* บล็อกที่ 2 — ธีม daisyUI map สีเดิมเข้ากับ semantic token ของ daisyUI
   ตัวนี้คือตัวที่ทำให้ btn / input / table มีสีของร้านเรา ไม่ใช่สี default */
@plugin "daisyui/theme" {
  name: "talad";
  default: true;
  color-scheme: light;
  --color-primary: #F5A524;        /* mango — เงินและ "คุณอยู่ตรงนี้" */
  --color-primary-content: #17242F;
  --color-base-100: #FFFFFF;
  --color-base-200: #F0F3F5;       /* steel-50 */
  --color-base-300: #E2E8EC;       /* steel-100 */
  --color-base-content: #17242F;   /* ink */
  --color-success: #2F7D4F;        /* leaf */
  --color-error: #C8362B;          /* chili */
  --radius-field: 5px;             /* borderRadius.control เดิม */
  --radius-box: 5px;
}
```

**Rationale**: daisyUI คือ Tailwind plugin ที่แจก component class (`btn`, `input`, `select`, `table`, `modal`,
`join`) ไม่ใช่ CSS framework แยก จึงยังอยู่ใน Principle IV ("Tailwind CSS สำหรับ styling") เต็มตัว และเป็นการ
**ลดจำนวน styling system จากสองระบบเหลือหนึ่ง** — PrimeReact unstyled + passthrough preset ตามข้อ 7 นั้นต้อง
ประกาศ Tailwind class เป็น string ใน `web/src/styles/primereact-passthrough.ts` ซึ่งต้องมี glob พิเศษใน
`content` เพื่อกัน purge daisyUI ไม่ต้องใช้กลไกนี้เลยเพราะ class อยู่ใน markup ตรง ๆ

**เหตุผลที่เลือกสาย 5 (Tailwind 4)**: daisyUI 5.7.32 คือเวอร์ชันที่หน้า `daisyui.com/docs/install` ที่ผู้ใช้ส่งมา
อธิบาย และใช้ไวยากรณ์ `@import "tailwindcss"; @plugin "daisyui";` ซึ่งเป็น CSS-first config ของ Tailwind 4
เท่านั้น (daisyui@4.12.24 เป็นเวอร์ชันสุดท้ายของสายที่รองรับ Tailwind 3) **ผู้ใช้เลือกอัปเกรดเป็น Tailwind 4
อย่างชัดเจน** จึงได้เวอร์ชันปัจจุบันของทั้งสองตัวและ feature ที่ยังพัฒนาต่อ แลกกับการต้องแตะระบบ build

**พื้นที่ที่การอัปเกรด Tailwind 4 กระทบ (ตรวจจากไฟล์จริงแล้ว ไม่ใช่รายการทั่วไป)**:

| ไฟล์ | สภาพปัจจุบัน | ต้องเป็น |
|---|---|---|
| `web/postcss.config.mjs` | `plugins: { tailwindcss: {} }` | `plugins: { "@tailwindcss/postcss": {} }` (แพ็กเกจใหม่ 4.3.3) |
| `web/src/app/globals.css` บรรทัด 1–3 | `@tailwind base; components; utilities;` | `@import "tailwindcss";` |
| `web/src/app/globals.css` บรรทัด ~25 | `outline: 2px solid theme("colors.mango.DEFAULT")` | `var(--color-mango)` — ฟังก์ชัน `theme()` เปลี่ยนสัญญาใน v4 |
| `web/src/app/globals.css` บรรทัด ~30 | `@layer components { .money, .receipt-settle }` | v4 ใช้ `@utility` สำหรับ custom utility — ต้องแปลงสองคลาสนี้ ไม่ใช่ปล่อยไว้ |
| `web/tailwind.config.ts` | ธีมทั้งชุดเป็น JS object | ย้ายเป็น `@theme` ใน CSS แล้วลบไฟล์ทิ้ง |
| `web/package.json` | `tailwindcss: ^3.4.1` | `tailwindcss: ^4.3.3` + `@tailwindcss/postcss` |

**ความเสี่ยงที่ระบุไว้ล่วงหน้า**: Next.js ที่ตรึงไว้คือ 14.2.35 ซึ่งออกก่อน Tailwind 4 — การอัปเกรดต้องพิสูจน์
ด้วยการรัน `npm run build` และเปิดหน้าจริงทุกหน้า ไม่ใช่แค่ดูว่า compile ผ่าน และต้องทำ **ก่อน** งาน daisyUI
ทั้งหมด เพื่อให้แยกออกได้ว่าถ้าพังเป็นเพราะ Tailwind 4 หรือเพราะ daisyUI

**Alternatives considered**:
- daisyUI 4 + คง Tailwind 3 — **ผู้ใช้เลือกทางนี้ก่อนในตอนแรก แล้วเปลี่ยนใจเป็น Tailwind 4 ในข้อความถัดมา**
  ทางนี้ไม่แตะระบบ build เลย (แก้ไฟล์เดียวคือ `tailwind.config.ts`) จึงเป็นทางถอยที่ยังใช้ได้ถ้าการอัปเกรด
  Tailwind 4 ติดปัญหากับ Next.js 14.2.35 แต่ติดอยู่บนสายที่ไม่พัฒนาต่อและไม่ตรงกับเอกสารที่ส่งมา
- คง PrimeReact ไว้แล้วแก้ responsive อย่างเดียว — ถูกปฏิเสธ เพราะผู้ใช้ระบุชัดว่าต้องการเปลี่ยนเครื่องมือ

**ผลที่ตามมาที่ต้องจัดการ**: `web/src/styles/primereact-passthrough.ts`, glob `"./src/styles/**/*.{js,ts}"` ที่มี
อยู่เพื่อกัน purge preset นั้นโดยเฉพาะ, และ `web/tailwind.config.ts` ทั้งไฟล์ **จะถูกลบทิ้งทั้งหมด** พร้อม
dependency `primereact` และ `primeicons`

### 8.1 สิ่งที่เกิดขึ้นจริงตอน implement (บันทึกหลังทำเสร็จ — T126)

**Next.js 14.2.35 ทำงานกับ Tailwind 4 ได้โดยไม่ต้องอัปเกรด** — ความเสี่ยงที่ระบุไว้ข้างบนไม่เกิดขึ้นจริง
`npm run build` ผ่านตั้งแต่ครั้งแรกและทุกหน้าเปิดได้ ไม่ต้องใช้ทางถอย (daisyUI 4 + Tailwind 3) เลย

**สิ่งที่แผนประเมินไว้ไม่ครบ**:

1. **มี `theme()` สามจุดไม่ใช่จุดเดียว** — นอกจากบรรทัด ~25 (`colors.mango.DEFAULT`) ยังมีอีกสองจุดใน
   บล็อก CSS ของตาราง PrimeReact (`colors.steel.200`, `colors.ink.300`) ที่แผนไม่ได้นับ
   ทั้งหมดเปลี่ยนเป็น `var(--color-*)` แล้ว

2. **`font-display` เสียแบบเงียบ ๆ — ข้อบกพร่องที่ร้ายแรงที่สุดของรอบนี้** ย้ายธีมเข้า `@theme` แล้ว
   `--font-display: var(--font-kanit), sans-serif` ถูกวางไว้ที่ `:root` แต่ `next/font` ประกาศ
   `--font-kanit` ไว้ที่ `<body>` — custom property ที่มี `var()` ข้างในถูก resolve **ณ element ที่
   ประกาศมัน** ไม่ใช่ ณ ที่ใช้ ดังนั้น `--font-display` จึงกลายเป็น guaranteed-invalid ที่ `:root`
   และทุก `font-display` ตกกลับไปใช้ฟอนต์ของ body **โดยไม่มี error, ไม่มี warning, build ผ่านปกติ**
   Tailwind 3 ไม่เจอปัญหานี้เพราะ utility เขียน `font-family` ลงบน element ตรง ๆ ไม่ผ่านตัวแปรกลาง
   → แก้โดยย้าย `kanit.variable`/`plexThai.variable` จาก `<body>` ไป `<html>` ใน `layout.tsx`
   → **บทเรียน**: ตรวจ Tailwind 4 ด้วยการอ่าน `getComputedStyle` ของ class จริง ไม่ใช่แค่ดูว่า build ผ่าน

3. **Tailwind 4 ไม่ต้องมี `content` glob** — ไฟล์ `src/styles/primereact-passthrough.ts` ที่ v3 ต้อง
   ประกาศ glob พิเศษให้ ถูก auto-detect ของ v4 สแกนเจอเอง ยืนยันแล้วว่า class ที่มีเฉพาะในไฟล์นั้น
   (`hover:bg-mango-100/40`, `border-steel-100`) ยังถูกสร้างครบก่อนที่ไฟล์จะถูกลบทิ้ง

4. **`@layer components` ใช้ต่อได้ แต่เปลี่ยนเป็น `@utility` แล้ว** ตามรูปแบบ v4 (`.money`, `.receipt-settle`)

5. **daisyUI ตั้งขนาด control สำหรับเมาส์** — `btn`/`input` สูง 40px และ `btn-sm` 32px ซึ่งต่ำกว่า 44px
   ที่นิ้วต้องการ แก้ด้วยกฎเดียวใน `@media (max-width: 767.98px)` แทนการไล่ใส่ class ทีละปุ่ม 47 จุด

**ผลข้างเคียงที่ดีเกินคาด**: bundle เล็กลงมาก — `/promotions` 247→101 kB (−59%),
`/sales/history` 237→92 kB (−61%), `/stock` 225→101 kB (−55%) ดูตารางเต็มใน quickstart-results.md

**ข้อบกพร่องฝั่ง API ที่เจอ**: `PagedResult<ProductDto>` กับ `PagedResult<SaleDto>` ได้ schemaId เดียวกัน
คือ `` PagedResult`1 `` (เพราะ `CustomSchemaIds` เดิมใช้ `type.Name` เมื่อ `DeclaringType is null` ซึ่ง
generic ระดับบนสุดเข้าเงื่อนไขนี้) ทำให้ Swagger พัง 500 แบบเดียวกับที่ `StaffSummaryDto` เคยทำ
→ ขยาย `CustomSchemaIds` ให้แยกชื่อ closed generic และเพิ่ม assertion ใน `OpenApiDocumentTests` ให้จับไว้

## 9. component ที่ daisyUI ไม่มีของแทน — ขอบเขตการถอด PrimeReact

**Decision**: ถอด PrimeReact ออก **ทั้งหมด** ไม่เหลือ dependency ค้าง โดยจับคู่ของแทนดังนี้

| PrimeReact ที่ใช้อยู่ | จำนวนไฟล์ | ของแทน |
|---|---|---|
| `Button` | 11 | class `btn` ของ daisyUI |
| `InputText`, `Password` | 6 | class `input` + `<input type="password">` |
| `Dropdown` | 1 | class `select` บน `<select>` |
| `Checkbox` | 2 | class `checkbox` บน `<input type="checkbox">` |
| `Dialog` | 4 | `<dialog>` + class `modal` ของ daisyUI |
| `DataTable` + `Column` | 4 | **component ใหม่ในแอป** — ดูข้อ 10 |
| `InputNumber` | 3 | `<input type="number" class="input">` + logic ปัดทศนิยมของเราเอง |
| `Calendar` | 2 | `<input type="date" class="input">` (native) |
| `AutoComplete` | 1 | เขียนเอง: `input` + `dropdown` ของ daisyUI + รายการผลลัพธ์ |
| `TabView`/`TabPanel` | 1 | class `tabs tabs-border` ของ daisyUI |

**Rationale**: สามตัวที่ daisyUI ไม่มีของแทนตรง ๆ คือ `InputNumber`, `Calendar`, `AutoComplete` แต่ทั้งสามตัว
มีทางออกที่ **ดีกว่าสำหรับ POS บนมือถือ** — `<input type="number">` และ `<input type="date">` เรียกแป้นพิมพ์
ตัวเลขและ date picker ของระบบปฏิบัติการเอง ซึ่งบนแท็บเล็ต/มือถือใช้งานง่ายกว่า widget ที่วาดเองและได้
accessibility ของ native ฟรี ส่วน `AutoComplete` ใน `MemberSearch.tsx` ทำงานแค่ "พิมพ์ → debounce → เรียก
`GET /api/v1/members?search=` → เลือกหนึ่งรายการ" ซึ่งสั้นพอที่จะเขียนเองโดยไม่ต้องแบก dependency ทั้งก้อนไว้
เพื่อมันตัวเดียว

**Alternatives considered**:
- คง PrimeReact ไว้เฉพาะ `AutoComplete` + `Calendar` — ถูกปฏิเสธ เพราะจะเหลือ styling สองระบบและ bundle ของ
  PrimeReact ทั้งก้อนอยู่ในแอปเพื่อ component สองตัว ซึ่งย้อนแย้งกับเหตุผลทั้งหมดของการเปลี่ยนในข้อ 8
  (และ PrimeReact 10 ยังไม่รับประกันว่าทำงานกับ Tailwind 4)
- ใช้ library อื่นแทน `Calendar`/`AutoComplete` (เช่น react-datepicker) — ถูกปฏิเสธ เพราะเพิ่ม dependency ใหม่
  เพื่อแก้ปัญหาที่ native input แก้ได้อยู่แล้ว

## 10. `DataTable` ที่รองรับ server paging + filter

**Decision**: เขียน component เดียวชื่อ `DataTable` ไว้ที่ `web/src/components/DataTable.tsx` แบบ generic
(`<DataTable<T>>`) โดยใช้ class `table` / `table-zebra` / `join` ของ daisyUI เป็นหน้าตา **ไม่เพิ่ม library ใด ๆ**
component นี้รับ `columns`, `rows`, `page`, `pageSize`, `totalCount`, `onPageChange`, `isLoading`, `emptyText`
และ render ปุ่มเลขหน้าเป็น `join` ของ daisyUI ส่วนแถบ filter เป็น `children` ที่หน้าเรียกส่งเข้ามา เพราะ
เงื่อนไขกรองของแต่ละหน้าไม่เหมือนกัน

**Rationale**: daisyUI **ไม่มี datagrid** — มีแค่ class `table` ที่จัดหน้าตาของ `<table>` ธรรมดา ไม่มี paging
ไม่มี filter ไม่มี state ดังนั้น "datagrid ที่รองรับ server paging และ filter ได้" ต้องเป็น component ระดับแอป
ไม่ว่าจะเลือกทางไหน ในเมื่อมีตารางแค่ 4 ตัว คอลัมน์ตายตัวรู้ล่วงหน้าทุกตัว และ paging เป็น server-side (แปลว่า
filter/paginate เกิดที่ API ไม่ใช่ที่ client) งานที่เหลือฝั่ง client จึงมีแค่ "วาดหัวตาราง วาดแถว วาดปุ่มหน้า"
ซึ่งไม่คุ้มกับการเพิ่ม dependency

**Alternatives considered**:
- TanStack Table (headless) — ถูกปฏิเสธสำหรับรอบนี้ ความสามารถหลักที่ TanStack ให้ (client-side sorting,
  grouping, virtualization, column resizing) ไม่มีข้อไหนที่รอบนี้ต้องการ เพราะ paging/filter ย้ายไปฝั่ง server
  หมดแล้ว เหลือแค่ค่าใช้จ่ายด้าน bundle และ API ที่ต้องเรียนรู้
- ใช้ `table` ของ daisyUI ตรง ๆ ในทุกหน้าโดยไม่มี component กลาง — ถูกปฏิเสธ เพราะ logic ของ paging + filter +
  empty state + loading state จะถูกคัดลอกซ้ำ 4 ที่ และเป็นจุดที่พฤติกรรมจะเริ่มไม่ตรงกันในภายหลัง

## 11. รูปแบบ response สำหรับ server paging

**Decision**: เปลี่ยน response ของ endpoint ที่แบ่งหน้าจาก array เปล่า เป็น **envelope** รูปแบบเดียวกันทุกตัว

```json
{
  "items": [ ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 137,
  "totalPages": 7
}
```

query parameter ที่เพิ่ม: `page` (เริ่มที่ 1, default 1) และ `pageSize` (default 20, **เพดาน 100**)
ค่านอกช่วงตอบ `400` พร้อม `{"error":"invalid_pagination"}` ไม่ใช่ปัดเงียบ ๆ ให้เข้าช่วง เพราะการปัดเงียบทำให้
client ที่ส่งค่าผิดเข้าใจว่าได้ข้อมูลครบแล้วทั้งที่ไม่ครบ

**ขอบเขต — แบ่งหน้าเฉพาะสองตัวนี้**:

| Endpoint | แบ่งหน้า? | เหตุผล |
|---|---|---|
| `GET /api/v1/products` | **ใช่** | หน้าจัดการสต็อกเป็น datagrid และจำนวนสินค้าโตไม่จำกัด |
| `GET /api/v1/sales` | **ใช่** | ประวัติการขายโตทุกวันตลอดอายุร้าน เป็นชุดข้อมูลที่โตเร็วที่สุดในระบบ |
| `GET /api/v1/promotions` | ไม่ | โปรโมชั่นที่ร้านเดี่ยวตั้งไว้มีหลักสิบ ไม่ใช่ชุดข้อมูลที่โตไม่จำกัด |
| `GET /api/v1/members` | ไม่ | เป็น autocomplete search ไม่ใช่ datagrid — จำกัดด้วย `search` อยู่แล้ว |
| `GET /api/v1/reports/*` | ไม่ | คืน aggregate ที่มีขอบเขตในตัว (top-N, หนึ่งแถวต่อพนักงาน) แบ่งหน้าแล้วผู้ใช้ไม่ได้อะไรเพิ่ม |

**Rationale**: แบ่งหน้าเฉพาะที่ข้อมูล**โตไม่จำกัด**จริง ๆ การไล่ทำทุก endpoint จะทำให้สัญญา API พังเป็นวงกว้าง
โดยไม่มีใครได้ประโยชน์ และเพิ่มพื้นที่ให้เกิด bug โดยเปล่าประโยชน์

**หมายเหตุสำหรับหน้าขายสินค้า**: `GET /api/v1/products` ถูกใช้โดยหน้า `/sales` ด้วย ซึ่งไม่ควรมีปุ่มเลขหน้า —
หน้านั้นจะส่ง `pageSize` ที่ใหญ่พอ (เช่น 100) และอ่านเฉพาะ `items` โดยไม่แสดงตัวแบ่งหน้า พฤติกรรมการค้นหา/สแกน
บาร์โค้ดจึงไม่เปลี่ยน

**ผลกระทบที่ต้องยอมรับ — เป็น breaking change ของ `v1`**: response ของสอง endpoint นี้เปลี่ยนรูปทรง client
เดิมที่คาดว่าได้ array จะพัง ในระบบนี้มี client เดียวคือ `web/` ซึ่งแก้พร้อมกันในรอบเดียวกัน และยังไม่มีการ
release สู่ผู้ใช้จริง จึงเลือก**เปลี่ยนรูปทรงของ `v1` ตรง ๆ** แทนการสร้าง `v2` คู่ขนาน (ซึ่งจะทำให้ต้องดูแล
โค้ดสองชุดตลอดไปเพื่อผู้บริโภคที่ไม่มีอยู่จริง) สิ่งที่ต้องแก้ตามพร้อมกัน: `contracts/products.md`,
`contracts/sales.md`, `OpenApiDocumentTests.cs`, integration test ที่ deserialize เป็น `List<T>`,
`web/src/lib/api/products.ts` และ `sales.ts`

**และจุดที่มองข้ามง่ายที่สุด — scenario ที่มีอยู่เดิมใน `quickstart.md` หัวข้อ 4–6 กับสคริปต์ตรวจของ T080**
ทุกที่ที่เรียก list endpoint โดยไม่ส่ง `pageSize` จะเปลี่ยนความหมายเงียบ ๆ จาก "ทุกแถว" เป็น "20 แถวแรก"
ทำให้การตรวจอ่อนลงโดยที่ผลยังขึ้น PASS (เช่น NFR-3 ที่ไล่ตรวจบิลทั้งวัน จะเหลือตรวจ 20 บิล)
ต้องไล่แก้ให้ส่ง `pageSize` ชัดเจนหรืออ่านทีละหน้าจนครบ **ก่อน**ที่จะเชื่อผลการรันซ้ำ — ดู quickstart.md ข้อ 7.6

**Alternatives considered**:
- ส่ง metadata ทาง response header (`X-Total-Count`) แล้วคง body เป็น array — ถูกปฏิเสธ เพราะไม่ใช่ breaking
  change ก็จริง แต่ header ไม่ปรากฏใน OpenAPI schema ตามธรรมชาติ ทำให้ `OpenApiDocumentTests` ตรวจไม่ได้ และ
  ต้องเขียนโค้ดอ่าน header แยกในทุก client
- แบ่งหน้าแบบ cursor (`?after=<id>`) — ถูกปฏิเสธ เพราะ UI ที่ผู้ใช้ต้องการคือปุ่มเลข "หน้า 1 2 3" ซึ่งต้องรู้
  `totalPages` ล่วงหน้า cursor ให้ไม่ได้ และข้อมูลระดับร้านเดี่ยวไม่มีปัญหา deep-offset ที่ cursor แก้

## 12. Responsive — สิ่งที่วัดได้จริงและเกณฑ์ที่จะใช้

**สิ่งที่วัดได้จริงก่อนวางแผน** (Playwright บน dev server, 2026-09-10): **ไม่พบ horizontal overflow ในหน้าใดเลย**
— `documentElement.scrollWidth - clientWidth = 0` ทั้งที่ 375px, 900px และ 1280px และหน้า `/stock` ที่ 375px
แสดงตารางเป็นการ์ดซ้อนแนวตั้งได้ถูกต้อง **จึงยังทำซ้ำอาการในภาพที่ผู้ใช้ส่งมาไม่ได้** (ภาพนั้นเนื้อหาถูกตัดที่
ขอบขวา) — ดูหมายเหตุใน plan.md ประเด็นนี้ไม่บล็อกงาน เพราะการรื้อไป daisyUI เขียน layout ใหม่อยู่แล้ว

**ข้อบกพร่องที่ทำซ้ำได้จริงและจะแก้**: ที่ความกว้าง 900px แผงแคชเชียร์ (`Cart`) กว้างคงที่ 384px (24rem) ไม่ยอม
ยืดหด เหลือพื้นที่ชั้นวางสินค้า 516px → grid เหลือ 3 คอลัมน์ กว้างคอลัมน์ละ 147px แผงกินพื้นที่จอ 43%
ในช่วง 768–1100px ซึ่งเป็นช่วงความกว้างของแท็บเล็ตแนวนอนและหน้าต่างเบราว์เซอร์ที่ย่อลงมา

**Decision**: กำหนดเกณฑ์ที่ตรวจอัตโนมัติได้สามข้อ แทนการบอกว่า "ปรับให้ responsive" ลอย ๆ
1. ทุกหน้าที่ทุก breakpoint ที่ระบุ (360 / 768 / 1024 / 1440) ต้องมี `scrollWidth - clientWidth <= 1`
2. แผงแคชเชียร์ต้องกว้างตามสัดส่วน (`clamp`) ไม่ใช่ค่าคงที่ — ตลอดช่วง 768–1100px แผงต้องกิน
   **ไม่เกิน 32%** ของความกว้างจอ และที่ 900px ชั้นวางต้องได้ **อย่างน้อย 68%**
3. ทุก datagrid ที่ ≤ 768px ต้องไม่บีบคอลัมน์ ให้เลื่อนแนวนอนในกล่องของตัวเอง (`overflow-x-auto`) หรือสลับเป็น
   การ์ดซ้อนแนวตั้ง — และกล่องนั้นต้องไม่ทำให้ทั้งหน้าเลื่อนแนวนอน

**Rationale**: ข้อ 1 และ 3 เป็นตัวเลขที่สคริปต์วัดได้ ทำให้ regression ในอนาคตถูกจับได้ ไม่ใช่ความเห็น

**ที่มาของตัวเลข 32% / 68% ในข้อ 2**: สภาพปัจจุบันที่ 900px คือแผง 384/900 = **42.7%** ชั้นวาง 516/900 = **57.3%**
เกณฑ์จึงต้องอยู่ห่างจากค่านี้พอที่การแก้จริงเท่านั้นจะผ่าน — ถ้าตั้งไว้ที่ "ชั้นวาง ≥ 60%" การหดแผงจาก 384px
เหลือ 360px ก็ผ่านแล้ว ทั้งที่ไม่ได้แก้ต้นเหตุ (แผงยังกว้างคงที่อยู่) 32% ที่ 900px = 288px ซึ่งเป็นความกว้าง
ที่แผงแคชเชียร์ยังแสดงชื่อสินค้า จำนวน และราคาได้ครบในบรรทัดเดียว จึงเป็นเพดานที่แก้ได้จริงไม่ใช่เพดานที่บีบเกิน

## สรุปผล

ทุกประเด็นด้านบนได้ข้อสรุปแล้ว ไม่มี `NEEDS CLARIFICATION` เหลืออยู่ในขอบเขตทางเทคนิค พร้อมเข้าสู่ Phase 1
(data-model.md, contracts/, quickstart.md)
