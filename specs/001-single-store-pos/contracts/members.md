# Contract: Members

**Base path**: `/api/v1/members`
**เกี่ยวข้องกับ**: FR-010–FR-014

## `GET /api/v1/members`

ค้นหาสมาชิกตอนขาย (FR-012)

**Auth required**: Manager หรือ Cashier

**Query parameters**:
| Param | Type | ความหมาย |
|---|---|---|
| `search` | string | ค้นหาจากเบอร์โทรศัพท์หรือชื่อ (contains) |

**Response 200 OK**:
```json
[
  {
    "id": "guid",
    "name": "สมชาย ใจดี",
    "phoneNumber": "0812345678",
    "accumulatedPurchaseTotal": 1250.00
  }
]
```

## `POST /api/v1/members`

สมัครสมาชิกใหม่ (FR-010)

**Auth required**: Manager หรือ Cashier

**Request body**:
```json
{
  "name": "string",
  "phoneNumber": "string"
}
```

**Response 201 Created**: MemberDto ที่สร้างแล้ว (`accumulatedPurchaseTotal` เริ่มที่ 0)
**Response 409 Conflict**: เบอร์โทรศัพท์นี้ถูกใช้สมัครสมาชิกไปแล้ว (FR-011)
```json
{ "error": "phone_number_already_registered" }
```

## `GET /api/v1/members/{id}`

**Auth required**: Manager หรือ Cashier
**Response 200 OK**: MemberDto เดียว
**Response 404**: ไม่พบสมาชิก

> หมายเหตุ: `accumulatedPurchaseTotal` ไม่มี endpoint แก้ไขตรง ๆ — ค่านี้ถูกอัปเดตเป็นผลข้างเคียงของ
> `POST /api/v1/sales` เท่านั้น (ดู `sales.md`) ตาม FR-014
