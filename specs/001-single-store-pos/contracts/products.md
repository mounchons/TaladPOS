# Contract: Products

**Base path**: `/api/v1/products`
**เกี่ยวข้องกับ**: FR-001–FR-006, FR-015–FR-018, FR-029

## `GET /api/v1/products`

ค้นหา/แสดงรายการสินค้า (FR-001, FR-002) — ใช้ทั้งหน้าขายสินค้าและหน้าจัดการสต็อก

**Auth required**: Manager หรือ Cashier

**Query parameters** (ทั้งหมด optional):
| Param | Type | ความหมาย |
|---|---|---|
| `search` | string | ค้นหาจากชื่อสินค้า (contains, case-insensitive) |
| `barcode` | string | ค้นหาแบบตรงเป๊ะจากบาร์โค้ด (สำหรับสแกน) |
| `lowStockOnly` | bool | กรองเฉพาะสินค้าที่ `IsLowStock == true` (FR-017) |
| `page` | int | เลขหน้า เริ่มที่ **1** (default `1`) |
| `pageSize` | int | จำนวนต่อหน้า (default `20`, **สูงสุด 100**) |

> **ตัวกรองทุกตัว AND กัน ไม่ใช่ OR** — ส่ง `search` คู่กับ `barcode` ได้ผลเป็น "ชื่อมีคำนี้ **และ**
> บาร์โค้ดตรงเป๊ะค่านี้" ซึ่งแทบไม่มีสินค้าใดเข้าเงื่อนไข ตรวจได้ตรง ๆ:
> `?search=ทุเรียน` → 7 แถว · `?barcode=9990lnj7o` → 1 แถว · `?search=ทุเรียน&barcode=ทุเรียน` → **0 แถว**
>
> client ที่ต้องการ "ชื่อ **หรือ** บาร์โค้ด" — ช่องค้นหาช่องเดียวในหน้าขาย (FR-002, SC-010) —
> ต้องยิงสองคำขอแล้วรวมผลเอง ดู `searchProductsByNameOrBarcode()` ใน `web/src/lib/api/products.ts`
> การส่งคำเดียวกันไปทั้งสองพารามิเตอร์เคยทำให้หน้าขายพิมพ์ชื่อสินค้าแล้วได้ 0 รายการมาตลอด

**Response 200 OK**:
```json
{
  "items": [
    {
      "id": "guid",
      "name": "มะม่วง",
      "imageUrl": "https://...",
      "price": 45.00,
      "barcode": "8850000000012",
      "stockQuantity": 12,
      "lowStockThreshold": 5,
      "isLowStock": false,
      "isOutOfStock": false
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 137,
  "totalPages": 7
}
```

> **รูปแบบ response เปลี่ยนแล้ว (รอบที่ 2)** — เดิมคืน array เปล่า ตอนนี้คืน envelope
> `{items, page, pageSize, totalCount, totalPages}` ดู research.md ข้อ 11 สำหรับเหตุผลและขอบเขต
> `items` คือ array แบบเดิมทุกประการ ไม่มี field ใดในตัว object เปลี่ยน

**Response 400 Bad Request**: `page < 1`, `pageSize < 1` หรือ `pageSize > 100`
```json
{ "error": "invalid_pagination" }
```
ค่านอกช่วงตอบ 400 ไม่ปัดเงียบ ๆ ให้เข้าช่วง — ไม่งั้น client ที่ส่งค่าผิดจะเข้าใจว่าได้ข้อมูลครบแล้วทั้งที่ไม่ครบ

`totalCount` นับ **หลังกรอง** ด้วย `search`/`barcode`/`lowStockOnly` แล้ว ไม่ใช่จำนวนสินค้าทั้งร้าน — ไม่งั้น
ตัวแบ่งหน้าจะแสดงจำนวนหน้าที่กดไปแล้วว่าง

หน้าขายสินค้า (`/sales`) และช่องเลือกสินค้าในหน้าโปรโมชั่น (`/promotions`) ใช้ endpoint เดียวกันแต่ไม่แสดง
ตัวแบ่งหน้า — **"ไม่มีปุ่มเลขหน้า" ไม่ได้แปลว่า "ไม่แบ่งหน้า"** สองหน้านี้ตั้งใจแสดง *ทุกรายการ* จึงต้องไล่อ่าน
ทีละหน้าจนครบ (`page` เดินจาก 1 ถึง `totalPages`) ไม่ใช่ยิงครั้งเดียวที่ `pageSize=100` แล้วอ่านเฉพาะ `items`
ซึ่งจะตัดสินค้าชิ้นที่ 101 ทิ้งเงียบ ๆ (FR-037) ดู `fetchAllPages()` ใน `web/src/lib/api/client.ts`
พฤติกรรมการค้นหาและสแกนบาร์โค้ดไม่เปลี่ยน

## `GET /api/v1/products/{id}`

**Auth required**: Manager หรือ Cashier
**Response 200 OK**: object เดียวตามรูปแบบข้างบน
**Response 404**: ไม่พบสินค้า

## `POST /api/v1/products`

เพิ่มสินค้าใหม่ (FR-015)

**Auth required**: Manager เท่านั้น (FR-029) — Cashier เรียกแล้วได้ 403

**Request body**:
```json
{
  "name": "string",
  "imageUrl": "string",
  "price": 45.00,
  "barcode": "string | null",
  "stockQuantity": 12,
  "lowStockThreshold": 5
}
```

**Response 201 Created**: ProductDto ที่สร้างแล้ว
**Response 400**: validation error (เช่น `price <= 0`)
**Response 409**: `barcode` ซ้ำกับสินค้าอื่นที่มีอยู่แล้ว

## `PUT /api/v1/products/{id}`

แก้ไขสินค้า (FR-015) — รวมถึงปรับ `stockQuantity`/`lowStockThreshold` (FR-018)

**Auth required**: Manager เท่านั้น
**Request body**: เหมือน POST
**Response 200 OK**: ProductDto ที่อัปเดตแล้ว
**Response 404 / 400 / 409**: เหมือนด้านบน

## `DELETE /api/v1/products/{id}`

ลบสินค้า (FR-015) — ไม่กระทบบิลเก่าที่เคยขายสินค้านี้ไปแล้ว เพราะ `SaleLineItem` เก็บ snapshot แยก (ดู data-model.md)

**Auth required**: Manager เท่านั้น
**Response 204 No Content**
**Response 404**: ไม่พบสินค้า
