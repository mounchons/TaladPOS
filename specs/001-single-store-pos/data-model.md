# Data Model: ระบบ POS สำหรับร้านค้าเดี่ยว (Single-Store POS)

**Date**: 2026-09-10
**Input**: [spec.md](./spec.md) Key Entities, [research.md](./research.md)

หน่วย entity ทั้งหมดด้านล่างอยู่ใน `TaladPOS.Domain` (ตาม constitution Principle II — DDD, business rules
ไม่ผูกกับ EF Core/PostgreSQL) ส่วน mapping ไปยังตาราง PostgreSQL อยู่ใน `TaladPOS.Infrastructure`

## ภาพรวมความสัมพันธ์

```text
Staff (1) ──< (N) Sale (N) >── (0..1) Member
                  │
                  └──< (N) SaleLineItem >── (1) Product (reference only, snapshot ราคา/ชื่อ)

Promotion ── (0..1) Product   [เมื่อ Scope = Item]
```

## Staff

พนักงาน/ผู้จัดการที่ล็อกอินเข้าใช้งาน (FR-007–FR-009, FR-029)

| Field | Type | Constraints |
|---|---|---|
| Id | Guid | PK |
| Name | string | required |
| Username | string | required, unique |
| PasswordHash | string | required (จาก `PasswordHasher<Staff>` — ดู research.md #1) |
| Role | enum { Manager, Cashier } | required |

**Validation rules**:
- Username ต้องไม่ซ้ำกันทั้งระบบ
- Role กำหนดสิทธิ์การเข้าถึง: Manager เท่านั้นที่เข้าหน้าจัดการสต็อก/โปรโมชั่น/รายงานได้ (FR-029); Cashier เข้าได้เฉพาะ
  หน้าขายสินค้าและการค้นหา/สมัครสมาชิก

**State**: ไม่มี lifecycle — สร้าง/แก้ไขโดยตรง (การเปิด/ปิดการใช้งานบัญชีไม่อยู่ในขอบเขตสเปกนี้)

## Product

สินค้าที่ขายในร้าน (FR-001–FR-006, FR-015–FR-018)

| Field | Type | Constraints |
|---|---|---|
| Id | Guid | PK |
| Name | string | required |
| ImageUrl | string | required (ตาม FR-015 ต้องมีรูปภาพ) |
| Price | decimal | required, > 0 |
| Barcode | string? | optional, unique เมื่อมีค่า (ตาม Assumptions — ไม่บังคับทุกสินค้าต้องมี) |
| StockQuantity | int | required, >= 0, นับเป็นจำนวนเต็ม/หน่วยชิ้นเท่านั้น (ผลการ clarify #1) |
| LowStockThreshold | int | required, >= 0, มีค่าเริ่มต้นถ้าไม่ได้กำหนด (FR-018) |

**Computed (ไม่เก็บเป็นคอลัมน์แยก)**:
- `IsOutOfStock` = `StockQuantity == 0`
- `IsLowStock` = `StockQuantity > 0 && StockQuantity <= LowStockThreshold`

**Validation rules**:
- Price ต้องมากกว่า 0
- StockQuantity ต้องไม่ติดลบ ณ เวลาใด ๆ — การตัดสต็อกต้องเป็น atomic conditional update (research.md #2) เพื่อรองรับ
  หลายจุดขายพร้อมกัน (ผลการ clarify #5)
- Barcode ถ้ามีค่าต้องไม่ซ้ำกับสินค้าอื่น

**State**: ไม่มี lifecycle state — CRUD ตรงไปตรงมา (สร้าง/แก้ไข/ลบ ตาม FR-015) การลบสินค้าไม่กระทบบิลเก่าที่เคยขายไปแล้ว
เพราะ `SaleLineItem` เก็บ snapshot ของชื่อ/ราคาไว้แยกต่างหาก (ดู Edge Case ในสเปก)

## Member

สมาชิกร้าน (FR-010–FR-014)

| Field | Type | Constraints |
|---|---|---|
| Id | Guid | PK |
| Name | string | required |
| PhoneNumber | string | required, unique (FR-011) |
| AccumulatedPurchaseTotal | decimal | default 0, >= 0 |

**Validation rules**:
- PhoneNumber ต้องไม่ซ้ำกับสมาชิกที่มีอยู่แล้ว (FR-011)
- AccumulatedPurchaseTotal อัปเดตเฉพาะตอนบิลที่ผูกกับสมาชิกนี้ชำระเงินสำเร็จเท่านั้น (FR-014) — ใช้เพื่อติดตาม/แสดงผล
  เท่านั้น ไม่ปลดล็อกส่วนลดแบบขั้นบันไดอัตโนมัติ (Assumptions)

**State**: ไม่มี lifecycle — สมัครแล้วคงอยู่ถาวร ไม่มี tier/สถานะสมาชิก

## Promotion

โปรโมชั่นส่วนลด % (FR-019–FR-022)

| Field | Type | Constraints |
|---|---|---|
| Id | Guid | PK |
| Scope | enum { Item, Bill } | required |
| DiscountPercentage | decimal | required, > 0 และ <= 100 |
| ProductId | Guid? | required เมื่อ `Scope == Item`, ต้องเป็น null เมื่อ `Scope == Bill` |
| AppliesToMembersOnly | bool | required — แยกส่วนลดสมาชิก (FR-020) ออกจากโปรโมชั่นทั่วไป |
| StartDate | date | required |
| EndDate | date | required, >= StartDate |

**Validation rules**:
- `EndDate >= StartDate`
- `Scope == Item` ⇒ `ProductId` ต้องระบุ; `Scope == Bill` ⇒ `ProductId` ต้องเป็น null
- โปรโมชั่นถือว่า active เฉพาะเมื่อวันที่ปัจจุบันอยู่ในช่วง `[StartDate, EndDate]` (FR-021)

**Business rule — การเลือกส่วนลดสูงสุด (FR-022)**: เมื่อมีทั้งโปรโมชั่นทั่วไปที่ active และส่วนลดสมาชิกที่ active
ปรับใช้ได้กับ line item/บิลเดียวกัน ให้ `DiscountResolver` domain service (research.md #3) เลือกใช้เพียงรายการที่ให้
มูลค่าส่วนลดสูงสุดหนึ่งรายการ ไม่รวมหลายส่วนลดเข้าด้วยกัน

**State**: ไม่มี lifecycle state แยก — ความ "active" คำนวณจากวันที่ปัจจุบันเทียบกับ `StartDate`/`EndDate` เสมอ

## Sale (บิลขาย)

บิลขายที่ชำระเงินสำเร็จแล้วเท่านั้น (FR-023–FR-024) — ตะกร้าก่อนชำระเงินเป็น client-side state ชั่วคราว ไม่ persist
จนกว่าจะชำระเงินสำเร็จ

| Field | Type | Constraints |
|---|---|---|
| Id | Guid | PK |
| StaffId | Guid | required, FK → Staff (FR-008) |
| MemberId | Guid? | optional, FK → Member (FR-013) |
| CreatedAt | datetime (UTC) | required |
| SubtotalAmount | decimal | required, >= 0 (ผลรวมก่อนหักส่วนลด) |
| DiscountAmount | decimal | required, >= 0 (ผลรวมส่วนลดที่ใช้จริงหลังเลือก best-of ต่อรายการ/บิล) |
| TotalAmount | decimal | required, >= 0 (`SubtotalAmount - DiscountAmount`, ไม่มี VAT ตาม Assumptions) |
| LineItems | `SaleLineItem[]` | required, อย่างน้อย 1 รายการ (Edge Case: ห้ามชำระเงินตะกร้าว่าง) |

**Validation rules**:
- ต้องมี LineItems อย่างน้อยหนึ่งรายการ
- `TotalAmount = SubtotalAmount - DiscountAmount` ต้องสอดคล้องกันเสมอ

**State**: **Append-only / immutable หลังสร้างเสร็จ** — ผลการ clarify #2 ระบุว่าเวอร์ชันนี้ไม่รองรับการยกเลิก/แก้ไข/
คืนเงินบิลที่บันทึกสำเร็จแล้ว จึงไม่มี status field หรือ state transition ใด ๆ บนเอนทิตีนี้

## SaleLineItem

รายการสินค้าภายในบิล (ส่วนหนึ่งของ Sale aggregate)

| Field | Type | Constraints |
|---|---|---|
| Id | Guid | PK |
| SaleId | Guid | required, FK → Sale |
| ProductId | Guid | required, FK → Product (reference เท่านั้น) |
| ProductNameSnapshot | string | required — ชื่อสินค้า ณ เวลาขาย (คงอยู่แม้สินค้าถูกลบภายหลัง) |
| UnitPriceSnapshot | decimal | required, > 0 — ราคาต่อชิ้น ณ เวลาขาย |
| Quantity | int | required, > 0, จำนวนเต็ม (ผลการ clarify #1) |
| DiscountAmount | decimal | default 0, >= 0 — ส่วนลดที่ถูกเลือกใช้กับรายการนี้ (ผลจาก `DiscountResolver`) |
| LineTotal | decimal | required — `(UnitPriceSnapshot * Quantity) - DiscountAmount` |

**Rationale สำหรับ snapshot fields**: ป้องกัน Edge Case ที่สินค้าเดิมถูกลบ/แก้ไขราคาในภายหลัง — ประวัติบิลเก่าต้อง
แสดงชื่อ/ราคา ณ เวลาที่ขายจริงเสมอ ไม่ใช่ค่าปัจจุบันของ `Product`

## สรุป mapping กับ Functional Requirements

| Entity | FR ที่เกี่ยวข้อง |
|---|---|
| Product | FR-001–FR-006, FR-015–FR-018 |
| Staff | FR-007–FR-009, FR-029 |
| Member | FR-010–FR-014 |
| Promotion | FR-019–FR-022 |
| Sale / SaleLineItem | FR-016 (ตัดสต็อก), FR-022 (ส่วนลด), FR-023–FR-024, FR-030 (ใบเสร็จ) |
