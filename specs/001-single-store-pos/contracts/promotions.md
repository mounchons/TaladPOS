# Contract: Promotions

**Base path**: `/api/promotions`
**เกี่ยวข้องกับ**: FR-019–FR-022, FR-029

ทุก endpoint ในไฟล์นี้จำกัดสิทธิ์เฉพาะ **Manager** เท่านั้น (FR-029) — Cashier เรียกแล้วได้ 403 Forbidden

## `GET /api/promotions`

**Query parameters**:
| Param | Type | ความหมาย |
|---|---|---|
| `activeOnly` | bool | กรองเฉพาะโปรโมชั่นที่ active ณ วันนี้ (`startDate <= today <= endDate`) |

**Response 200 OK**:
```json
[
  {
    "id": "guid",
    "scope": "Item | Bill",
    "discountPercentage": 10.0,
    "productId": "guid | null",
    "appliesToMembersOnly": false,
    "startDate": "2026-09-01",
    "endDate": "2026-09-30",
    "isActive": true
  }
]
```

## `POST /api/promotions`

สร้างโปรโมชั่นใหม่ (FR-019, FR-020, FR-021)

**Request body**:
```json
{
  "scope": "Item | Bill",
  "discountPercentage": 10.0,
  "productId": "guid | null",
  "appliesToMembersOnly": false,
  "startDate": "2026-09-01",
  "endDate": "2026-09-30"
}
```

**Validation** (ดู data-model.md):
- `scope == "Item"` ⇒ `productId` ต้องระบุ; `scope == "Bill"` ⇒ `productId` ต้องเป็น `null`
- `discountPercentage` ต้องอยู่ในช่วง (0, 100]
- `endDate >= startDate`

**Response 201 Created**: PromotionDto ที่สร้างแล้ว
**Response 400**: validation error ตามด้านบน

## `PUT /api/promotions/{id}`

แก้ไขโปรโมชั่น — request/validation เหมือน POST
**Response 200 OK** / **404** / **400**

## `DELETE /api/promotions/{id}`

**Response 204 No Content**
**Response 404**: ไม่พบโปรโมชั่น

> หมายเหตุ: ตรรกะ "เลือกส่วนลดสูงสุดเมื่อมีทั้งโปรโมชั่นและส่วนลดสมาชิก" (FR-022) ไม่มี endpoint แยก — ระบบคำนวณ
> อัตโนมัติฝั่งเซิร์ฟเวอร์ตอนเรียก `POST /api/sales` (ดู `sales.md` และ research.md #3)
