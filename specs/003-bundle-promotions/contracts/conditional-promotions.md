# Contract: Conditional Promotions

**Base path**: `/api/v1/conditional-promotions`
**เกี่ยวข้องกับ**: FR-001–FR-009, FR-023
**สิทธิ์**: **Manager เท่านั้น** — Cashier เรียกแล้วได้ `403 Forbidden` (รูปแบบเดียวกับ `001/contracts/promotions.md`
ตาม `001/FR-029`)

> endpoint ชุดนี้ **แยกจาก** `/api/v1/promotions` เดิมโดยสิ้นเชิง ของเดิมไม่ถูกแก้แม้แต่ฟิลด์เดียว (FR-026)

---

## รูปร่างข้อมูลร่วม

### `ConditionalPromotionDto`

```json
{
  "id": "guid",
  "name": "ซื้อคู่สุดคุ้ม",
  "conditionLines": [
    { "productId": "guid", "productName": "บะหมี่กึ่งสำเร็จรูป", "minimumQuantity": 1 },
    { "productId": "guid", "productName": "น้ำอัดลม", "minimumQuantity": 1 }
  ],
  "reward": {
    "kind": "Gift",
    "giftProductId": "guid",
    "giftProductName": "ขนมขบเคี้ยว",
    "giftQuantity": 1,
    "discountPercentage": null
  },
  "appliesToMembersOnly": false,
  "startDate": "2026-09-01",
  "endDate": "2026-09-30",
  "isActive": true,
  "description": "ซื้อ บะหมี่กึ่งสำเร็จรูป 1 + น้ำอัดลม 1 แถม ขนมขบเคี้ยว 1",
  "isUsable": true,
  "unusableReason": null
}
```

- `reward.kind` เป็น `"Gift"` หรือ `"Percentage"` เท่านั้น
  - `"Gift"` ⇒ `giftProductId` และ `giftQuantity` มีค่า, `discountPercentage` เป็น `null`
  - `"Percentage"` ⇒ `discountPercentage` มีค่า, `giftProductId`/`giftQuantity`/`giftProductName` เป็น `null`
- `description` ประกอบโดย server (research.md #11) — `web/` ต้องไม่ประกอบข้อความนี้เอง (FR-008)
- `isUsable` เป็น `false` เมื่อมีสินค้าที่ถูกอ้างถึงถูกลบไปแล้ว พร้อม `unusableReason` เป็นข้อความอธิบาย
  เช่น `"สินค้าที่อ้างถึงถูกลบออกจากระบบแล้ว"` (FR-023)
- `productName` / `giftProductName` เป็น `null` เมื่อสินค้านั้นถูกลบไปแล้ว

### `ConditionalPromotionRequestDto`

```json
{
  "name": "ซื้อคู่สุดคุ้ม",
  "conditionLines": [
    { "productId": "guid", "minimumQuantity": 1 },
    { "productId": "guid", "minimumQuantity": 1 }
  ],
  "reward": {
    "kind": "Gift",
    "giftProductId": "guid",
    "giftQuantity": 1,
    "discountPercentage": null
  },
  "appliesToMembersOnly": false,
  "startDate": "2026-09-01",
  "endDate": "2026-09-30"
}
```

---

## `GET /api/v1/conditional-promotions`

**Query parameters**:

| Param | Type | ความหมาย |
|---|---|---|
| `activeOnly` | bool | กรองเฉพาะที่มีผล ณ วันนี้ (`startDate <= today <= endDate`) |

**Response 200 OK**: `ConditionalPromotionDto[]`

---

## `POST /api/v1/conditional-promotions`

สร้างโปรโมชั่นแบบมีเงื่อนไข (FR-001–FR-006)

**Request body**: `ConditionalPromotionRequestDto`

**Validation** (ดู data-model.md):

| กฎ | ข้อกำหนด | ข้อความเมื่อผิด |
|---|---|---|
| `name` ยาว 1–100 ตัวอักษร | FR-002 | `name_required` / `name_too_long` |
| `conditionLines` มีอย่างน้อย 1 แถว | FR-003 | `condition_lines_required` |
| `productId` ห้ามซ้ำใน `conditionLines` | FR-003 | `duplicate_condition_product` |
| `minimumQuantity >= 1` ทุกแถว | FR-003 | `invalid_minimum_quantity` |
| `kind = "Gift"` ⇒ `giftProductId` มีค่า และ `giftQuantity >= 1` | FR-004 | `gift_reward_incomplete` |
| `kind = "Percentage"` ⇒ `0 < discountPercentage <= 100` | FR-005 | `invalid_discount_percentage` |
| ฟิลด์ของ reward อีกแบบต้องเป็น `null` | FR-004, FR-005 | `reward_fields_mismatch` |
| `endDate >= startDate` | FR-006 | `invalid_date_range` |
| สินค้าทุกตัวที่อ้างถึงต้องมีอยู่จริงในระบบ ณ เวลาที่สร้าง | FR-007 | `product_not_found` |

การปฏิเสธต้องไม่บันทึกโปรโมชั่นบางส่วน — ทั้ง aggregate ถูกสร้างใน transaction เดียว (FR-007)

**Response 201 Created**: `ConditionalPromotionDto` ที่สร้างแล้ว
**Response 400 Bad Request**: `{ "error": "<รหัสจากตารางข้างบน>" }`
**Response 403 Forbidden**: ผู้เรียกไม่ใช่ Manager

---

## `PUT /api/v1/conditional-promotions/{id}`

แก้ไขโปรโมชั่น — request body และ validation เหมือน `POST` ทุกประการ
รายการเงื่อนไขถูกแทนที่ทั้งชุด (replace) ไม่ใช่ merge ทีละแถว

**Response 200 OK**: `ConditionalPromotionDto` ที่แก้แล้ว
**Response 400 / 403 / 404**

---

## `DELETE /api/v1/conditional-promotions/{id}`

ลบโปรโมชั่นและรายการเงื่อนไขทั้งหมดของมัน (cascade)
บิลที่เคยใช้โปรโมชั่นนี้ไม่ได้รับผลกระทบ เพราะ `sale_applied_promotions` เก็บข้อความบรรยายเป็น snapshot
และไม่มี FK ผูกกลับมา (FR-024)

**Response 204 No Content**
**Response 403 / 404**
