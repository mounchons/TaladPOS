# Research: ระบบ POS สำหรับร้านค้าเดี่ยว (Single-Store POS)

**Date**: 2026-09-10
**Input**: [spec.md](./spec.md), [plan.md](./plan.md), `.specify/memory/constitution.md`

สเปกและ Technical Context ไม่มี `NEEDS CLARIFICATION` เหลืออยู่ (ประเด็นทางธุรกิจถูกแก้ไปแล้วในขั้นตอน
`/speckit-clarify`) เอกสารนี้จึงเน้นตัดสินใจประเด็น **ทางเทคนิค/สถาปัตยกรรม** ที่จำเป็นก่อนออกแบบ data model
และ contracts ในขั้นตอนถัดไป

## 1. การยืนยันตัวตนพนักงาน (Authentication) สำหรับ FR-007/FR-009

**Decision**: ใช้ JWT bearer token — พนักงานล็อกอินผ่าน `POST /api/auth/login` ด้วย username/password, API
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
(`stock_quantity <= low_stock_threshold`) ตอนตอบกลับ query สินค้า (`GET /api/products`) และแสดงผลใน `web/` ตอนโหลด/
รีเฟรชหน้าจอสต็อก

**Rationale**: SC-003 กำหนดไว้ชัดว่า "เห็นภายในการใช้งานหน้าจอสต็อกครั้งถัดไป" ไม่ได้ต้องการ real-time push
การเพิ่ม SignalR/WebSocket จะเป็นความซับซ้อนเกินความจำเป็น (YAGNI) สำหรับร้านค้าเดี่ยวขนาดเล็ก

**Alternatives considered**:
- SignalR push แจ้งเตือนทันทีที่สต็อกลดต่ำกว่าเกณฑ์ — ถูกปฏิเสธ เพราะเกินความต้องการที่ระบุใน success criteria
  และเพิ่ม infrastructure component (persistent connection) โดยไม่มี requirement รองรับ

## 5. รูปแบบใบเสร็จ (Receipt) สำหรับ FR-030

**Decision**: `GET /api/sales/{id}/receipt` คืนข้อมูลใบเสร็จเป็น JSON (รายการสินค้า/ราคา/ส่วนลด/ยอดรวม) ให้
`web/` render เป็นหน้าใบเสร็จ HTML แล้วใช้ browser print (`window.print()`) เพื่อพิมพ์ ไม่ผูกกับเครื่องพิมพ์ใบเสร็จ
เฉพาะทาง (thermal printer) ในเวอร์ชันนี้

**Rationale**: สอดคล้องกับ Assumption ในสเปกที่ระบุว่าใบเสร็จเป็นแบบง่าย ไม่ใช่ใบกำกับภาษี และไม่มีการระบุอุปกรณ์
พิมพ์เฉพาะทางในข้อกำหนด การ render ผ่านเบราว์เซอร์ทำให้ไม่ต้องเพิ่ม dependency ด้าน hardware driver

**Alternatives considered**:
- สร้าง PDF ฝั่งเซิร์ฟเวอร์ (เช่น QuestPDF) — ถูกปฏิเสธสำหรับเวอร์ชันนี้ เพราะเพิ่ม dependency โดยไม่มีข้อกำหนดว่าต้อง
  ได้ไฟล์ PDF ที่ดาวน์โหลดได้ การพิมพ์จากหน้าเว็บเพียงพอต่อ FR-030
- เชื่อมต่อเครื่องพิมพ์ใบเสร็จความร้อน (ESC/POS) โดยตรง — ถูกปฏิเสธ เพราะ Assumption ในสเปกระบุว่าไม่รวมอุปกรณ์ฮาร์ดแวร์
  เฉพาะทางไว้ในขอบเขตเวอร์ชันนี้

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

## สรุปผล

ทุกประเด็นด้านบนได้ข้อสรุปแล้ว ไม่มี `NEEDS CLARIFICATION` เหลืออยู่ในขอบเขตทางเทคนิค พร้อมเข้าสู่ Phase 1
(data-model.md, contracts/, quickstart.md)
