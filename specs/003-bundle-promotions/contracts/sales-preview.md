# Contract: Cart Pricing Preview & Sale Result Changes

**Base path**: `/api/v1/sales`
**เกี่ยวข้องกับ**: FR-012–FR-022, FR-024, FR-025
**สิทธิ์**: ผู้ใช้ที่ล็อกอินแล้วทุกบทบาท (**Cashier และ Manager**) — `SalesController` ไม่มี role restriction
ระดับคลาส และ `Program.cs` ตั้ง `FallbackPolicy = RequireAuthenticatedUser()` ไว้

> นี่คือเหตุผลที่หน้าขายต้องใช้ endpoint นี้ ไม่ใช่ `/api/v1/conditional-promotions` ซึ่งเป็น Manager-only

---

## `POST /api/v1/sales/preview` *(ใหม่)*

ตั้งราคาตะกร้าโดย**ไม่**บันทึกอะไรและ**ไม่**ตัดสต็อก ใช้แสดงยอด ส่วนลด ของแถม และสิทธิ์ที่ยังไม่ได้ใช้
บนหน้าขายก่อนกดชำระเงิน (FR-013, FR-015)

ผลลัพธ์คำนวณด้วย `CartPricer` **ตัวเดียวกับที่ `POST /api/v1/sales` ใช้** ยอดจากพรีวิวจึงตรงกับยอดที่ตัดจริง
ทุกสตางค์ตราบใดที่ตะกร้า สมาชิก ราคาสินค้า และวันที่ไม่เปลี่ยน (FR-012, SC-006)

**Request body** — รูปร่างเดียวกับ `POST /api/v1/sales` เป๊ะ:

```json
{
  "memberId": "guid | null",
  "lineItems": [
    { "productId": "guid", "quantity": 2 }
  ]
}
```

**Response 200 OK** — `PricedCartDto`:

```json
{
  "lines": [
    {
      "productId": "guid",
      "productName": "น้ำส้มคั้น",
      "unitPrice": 100.00,
      "quantity": 2,
      "discountAmount": 0.00,
      "lineTotal": 200.00,
      "isGift": false
    },
    {
      "productId": "guid",
      "productName": "น้ำส้มคั้น",
      "unitPrice": 100.00,
      "quantity": 1,
      "discountAmount": 100.00,
      "lineTotal": 0.00,
      "isGift": true
    }
  ],
  "appliedPromotions": [
    {
      "promotionId": "guid",
      "description": "ซื้อ น้ำส้มคั้น 2 แถม น้ำส้มคั้น 1",
      "setCount": 1,
      "discountAmount": 100.00
    }
  ],
  "unclaimedGifts": [
    {
      "promotionId": "guid",
      "description": "ซื้อ บะหมี่ 1 + น้ำอัดลม 1 แถม ขนม 1",
      "giftProductId": "guid",
      "giftProductName": "ขนมขบเคี้ยว",
      "missingQuantity": 1
    }
  ],
  "subtotalAmount": 300.00,
  "discountAmount": 100.00,
  "totalAmount": 200.00
}
```

**กฎของผลลัพธ์**:

| กฎ | ข้อกำหนด |
|---|---|
| สินค้ารายการเดียวกันปรากฏได้สองบรรทัด คือบรรทัดที่จ่ายเงินและบรรทัดของแถม | FR-016 |
| บรรทัดที่ `isGift = true` มี `unitPrice` เป็นราคาปกติ ไม่ใช่ 0 และ `lineTotal` เป็น 0 เสมอ | FR-016 |
| `subtotalAmount` = ผลรวม `unitPrice × quantity` ของ**ทุก**บรรทัด รวมของแถม | data-model.md §5 |
| `discountAmount` = ผลรวม `discountAmount` ของทุกบรรทัด | data-model.md §5 |
| `totalAmount` = `subtotalAmount − discountAmount` เสมอ ไม่มีการปัดเศษเพิ่ม | — |
| `unclaimedGifts` ว่างเมื่อไม่มีสิทธิ์ค้าง และไม่กระทบยอดเงินใดๆ | FR-015 |
| ตะกร้าเดียวกันที่ส่ง `lineItems` คนละลำดับ ต้องได้ผลลัพธ์เหมือนกันทุกฟิลด์ | FR-012, SC-006 |
| รายการซ้ำ `productId` ใน request ต้องถูกรวมยอดก่อนคำนวณ | research.md #10 ข้อ 1 |

**Response 400 Bad Request**:

| กรณี | `error` |
|---|---|
| `lineItems` ว่างหรือไม่มี | `empty_cart` |
| `quantity <= 0` | `invalid_quantity` |

**Response 404 Not Found**: `productId` หรือ `memberId` ไม่มีในระบบ

**หมายเหตุสำคัญ**: endpoint นี้ **ไม่ตรวจสต็อก** — การตรวจสต็อกเกิดที่ `POST /api/v1/sales` ตามเดิม
พรีวิวจึงตอบได้แม้จำนวนในตะกร้าเกินสต็อก (หน้าขายมีกฎห้ามเพิ่มเกินสต็อกอยู่แล้วตาม `001`)

**ประสิทธิภาพ**: `web/` เรียก endpoint นี้แบบ debounce ~250 ms หลังตะกร้าเปลี่ยน และแสดงยอดเดิมค้างไว้
ระหว่างรอผลใหม่ ไม่ใช่แสดงค่าว่าง

---

## `POST /api/v1/sales` *(เดิม — เพิ่มฟิลด์ในผลลัพธ์)*

Request body **ไม่เปลี่ยน** พฤติกรรมเดิมทุกอย่างคงเดิม (ตัดสต็อก บันทึกบิล อัปเดตยอดสะสมสมาชิก
ทั้งหมดใน transaction เดียวตาม `001/FR-016`)

**สิ่งที่เพิ่มใน `SaleDto`**:

```json
{
  "lineItems": [
    { "...": "ฟิลด์เดิมทั้งหมด", "isGift": false }
  ],
  "appliedPromotions": [
    { "promotionId": "guid", "description": "...", "setCount": 1, "discountAmount": 100.00 }
  ]
}
```

- `lineItems[].isGift` — ฟิลด์ใหม่ บิลเก่าที่บันทึกก่อนฟีเจอร์นี้คืนค่า `false` ทุกบรรทัด (FR-016)
- `appliedPromotions` — ฟิลด์ใหม่ อาร์เรย์ว่างเมื่อบิลไม่ได้ใช้โปรโมชั่นแบบมีเงื่อนไข (FR-024)

**ลำดับการทำงานที่เปลี่ยนภายใน** (ไม่กระทบสัญญาภายนอก แต่ต้องรู้ตอน implement — research.md #8):
โหลดสินค้า → `CartPricer` → ตัดสต็อกทุกบรรทัดรวมของแถม → บันทึกบิล → อัปเดตยอดสะสม → commit
`InsufficientStockException` ยังคืน error รูปแบบเดิม เพียงเกิดขึ้นช้ากว่าเดิมในลำดับ

**ยอดซื้อสะสมของสมาชิก** ยังคิดจาก `totalAmount` (ยอดสุทธิ) เหมือนเดิม ของแถมจึงไม่เพิ่มยอดสะสม
โดยอัตโนมัติเพราะบรรทัดของแถมมี `lineTotal = 0` (FR-022)

---

## `GET /api/v1/sales/{id}` และ `GET /api/v1/sales/{id}/receipt` *(เดิม — เพิ่มฟิลด์)*

คืนฟิลด์ใหม่ชุดเดียวกับ `POST /api/v1/sales` คือ `lineItems[].isGift` และ `appliedPromotions`
เพื่อให้ใบเสร็จแสดงบรรทัดของแถมและชื่อโปรโมชั่นที่ถูกใช้ได้ (FR-016, FR-024, `001/FR-030`)

---

## `GET /api/v1/sales` และ endpoint รายงาน *(เดิม — ไม่เปลี่ยนสัญญา)*

ยอดส่วนลดในประวัติการขายและรายงานรวมส่วนลดจากโปรโมชั่นแบบมีเงื่อนไขไว้แล้วโดยอัตโนมัติ
เพราะส่วนลดทุกชนิดอยู่ใน `SaleLineItem.DiscountAmount` และ `Sale.DiscountAmount` คำนวณจากที่นั่นเหมือนเดิม
**ไม่ต้องเพิ่มรายงานประเภทใหม่** (FR-025)

**ความหมายของตัวเลขเมื่อบิลมีของแถม** (FR-027) — สัญญาไม่เปลี่ยน แต่ค่าที่ได้เปลี่ยนความหมาย จึงระบุไว้ให้ชัด:

| ตัวเลข | ของแถมมีผลอย่างไร |
|---|---|
| `TotalSalesAmount` (รายงานยอดขาย) | **ไม่เพิ่ม** เพราะคิดจาก `Sale.TotalAmount` ซึ่งบรรทัดของแถมมียอดสุทธิ 0 |
| `TotalDiscountAmount` (รายงานยอดขาย) | **เพิ่มตามราคาปกติของของแถม** เพราะบรรทัดของแถมมี `DiscountAmount` เท่ากับราคาเต็ม |
| `QuantitySold` (รายงานสินค้าขายดี) | **รวมจำนวนที่แถม** ทำให้ตรงกับจำนวนที่สต็อกลดลงตาม FR-017 |
| `TotalSalesAmount` (รายงานสินค้าขายดี) | **ไม่เพิ่ม** เพราะคิดจาก `LineTotal` ซึ่งเป็น 0 สำหรับของแถม |
