# Contract: Sales

**Base path**: `/api/v1/sales`
**เกี่ยวข้องกับ**: FR-005, FR-006, FR-008, FR-013, FR-014, FR-016, FR-022, FR-023, FR-024, FR-030

## `POST /api/v1/sales`

ปิดบิล/ชำระเงิน (checkout) — จุดที่ตัดสต็อก, คำนวณส่วนลดสูงสุด, บันทึกบิล, และอัปเดตยอดสะสมสมาชิก ทั้งหมดในหนึ่ง
database transaction

**Auth required**: Manager หรือ Cashier — `staffId` ผู้ขายมาจาก JWT claim เท่านั้น (ดู `auth.md`) ไม่รับจาก request body

**Request body**:
```json
{
  "memberId": "guid | null",
  "lineItems": [
    { "productId": "guid", "quantity": 2 }
  ]
}
```

**การประมวลผล (server-side)**:
1. ตรวจว่า `lineItems` มีอย่างน้อย 1 รายการ (Edge Case: ห้ามชำระเงินตะกร้าว่าง) — ไม่ผ่านคืน 400
2. สำหรับแต่ละ line item: ตัดสต็อกด้วย atomic conditional update (research.md #2) — ถ้าสต็อกไม่พอสำหรับสินค้าใด
   ให้ยกเลิกทั้ง transaction คืน 409 พร้อมระบุ `productId` ที่มีปัญหา
3. ใช้ `DiscountResolver` (research.md #3) เลือกส่วนลดสูงสุดต่อ line item/บิล จากโปรโมชั่นที่ active วันนี้ (FR-021)
   และส่วนลดสมาชิก (ถ้ามี `memberId` และมีส่วนลดสมาชิกที่ active) — ไม่สะสมหลายส่วนลด (FR-022)
4. บันทึก snapshot ชื่อ/ราคาสินค้าลงใน `SaleLineItem` (ดู data-model.md)
5. ถ้ามี `memberId` — เพิ่ม `accumulatedPurchaseTotal` ของสมาชิกนั้นด้วย `totalAmount` ของบิล (FR-014)
6. บันทึก `Sale` พร้อม `staffId` จาก JWT (FR-008)

**Response 201 Created**:
```json
{
  "id": "guid",
  "createdAt": "2026-09-10T10:15:00Z",
  "staff": { "id": "guid", "name": "string" },
  "member": { "id": "guid", "name": "string" } ,
  "lineItems": [
    {
      "productId": "guid",
      "productNameSnapshot": "มะม่วง",
      "unitPriceSnapshot": 45.00,
      "quantity": 2,
      "discountAmount": 9.00,
      "lineTotal": 81.00
    }
  ],
  "subtotalAmount": 90.00,
  "discountAmount": 9.00,
  "totalAmount": 81.00
}
```

**Response 400 Bad Request**: ตะกร้าว่าง หรือ `quantity <= 0`
**Response 404**: `productId` หรือ `memberId` ที่ระบุไม่มีอยู่จริง
**Response 409 Conflict**: สต็อกไม่พอสำหรับสินค้าที่ระบุ (รวมถึงกรณีแข่งกันขายจากหลายจุดขายพร้อมกัน — ผลการ clarify #5)
```json
{ "error": "insufficient_stock", "productId": "guid" }
```

## `GET /api/v1/sales`

ประวัติบิลขายย้อนหลัง (FR-024)

**Auth required**: Manager หรือ Cashier

**Query parameters** (ทั้งหมด optional):
| Param | Type | ความหมาย |
|---|---|---|
| `from` | date | วันเริ่มต้นของช่วง (รวมวันนี้) |
| `to` | date | วันสิ้นสุดของช่วง (รวมวันนี้) |
| `staffId` | guid | กรองเฉพาะบิลของพนักงานคนนี้ |
| `memberId` | guid | กรองเฉพาะบิลของสมาชิกคนนี้ |
| `page` | int | เลขหน้า เริ่มที่ **1** (default `1`) |
| `pageSize` | int | จำนวนต่อหน้า (default `20`, **สูงสุด 100**) |

**Response 200 OK**:
```json
{
  "items": [ ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 412,
  "totalPages": 21
}
```
`items` คือ array ของ SaleDto (รูปแบบเดียวกับ response ของ `POST /api/v1/sales`)

> **รูปแบบ response เปลี่ยนแล้ว (รอบที่ 2)** — เดิมคืน array เปล่า ตอนนี้คืน envelope
> `{items, page, pageSize, totalCount, totalPages}` ดู research.md ข้อ 11 สำหรับเหตุผลและขอบเขต
> `items` คือ array แบบเดิมทุกประการ ไม่มี field ใดในตัว object เปลี่ยน

**Response 400 Bad Request**: `page < 1`, `pageSize < 1` หรือ `pageSize > 100`
```json
{ "error": "invalid_pagination" }
```
ค่านอกช่วงตอบ 400 ไม่ปัดเงียบ ๆ ให้เข้าช่วง — ไม่งั้น client ที่ส่งค่าผิดจะเข้าใจว่าได้ข้อมูลครบแล้วทั้งที่ไม่ครบ

`totalCount` นับ **หลังกรอง** ด้วย `from`/`to`/`staffId`/`memberId` แล้ว

เรียงจากบิลใหม่สุดไปเก่าสุด (`soldAt` มาก → น้อย) การเรียงต้องคงที่ ไม่งั้นบิลเดียวกันจะโผล่ซ้ำหรือหายไป
ตอนเปลี่ยนหน้า

## `GET /api/v1/sales/{id}`

**Response 200 OK**: SaleDto เดียว
**Response 404**: ไม่พบบิล

## `GET /api/v1/sales/{id}/receipt`

ข้อมูลใบเสร็จอย่างง่ายสำหรับแสดง/พิมพ์ (FR-030, research.md #5) — ไม่มีการคำนวณ VAT

**Response 200 OK**: เหมือน SaleDto (ข้อมูลเพียงพอให้ `web/` render เป็นหน้าใบเสร็จแล้วสั่งพิมพ์ผ่านเบราว์เซอร์)
**Response 404**: ไม่พบบิล

> หมายเหตุ: ไม่มี endpoint สำหรับยกเลิก/แก้ไข/ลบบิล — Sale เป็น append-only ตามผลการ clarify #2 และ data-model.md
