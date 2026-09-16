# Phase 1 Data Model: Bundle & Gift Promotions

**Feature**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md) | **Research**: [research.md](./research.md)

> ข้อมูลเดิมของ 001 ที่ **ไม่ถูกแตะเลย**: ตาราง `promotions`, `products`, `members`, `staff`, `sales`
> (ยกเว้นคอลัมน์เดียวที่เพิ่มบน `sale_line_items` ตามที่ระบุด้านล่าง)

---

## 1. ConditionalPromotion (aggregate root, ใหม่)

โปรโมชั่นหนึ่งข้อที่ผู้จัดการตั้งขึ้น — ตาราง `conditional_promotions`

| Field | ชนิด | ข้อจำกัด | ข้อกำหนดที่อ้าง |
|---|---|---|---|
| `Id` | Guid | PK | — |
| `Name` | string | required, 1–100 ตัวอักษร | FR-002 |
| `ConditionLines` | collection ของ `ConditionLine` | อย่างน้อย 1 แถว, `ProductId` ห้ามซ้ำ | FR-003 |
| `Reward` | `Reward` (value object) | required, เป็นแบบของแถม **หรือ** แบบเปอร์เซ็นต์ อย่างใดอย่างหนึ่ง | FR-001, FR-004, FR-005 |
| `AppliesToMembersOnly` | bool | required | FR-006 |
| `StartDate` | DateOnly | required | FR-006 |
| `EndDate` | DateOnly | required, `>= StartDate` | FR-006 |

**พฤติกรรมของ aggregate**:
- `IsActive(DateOnly date)` → `date >= StartDate && date <= EndDate` (รูปแบบเดียวกับ `Promotion.IsActive` เดิม)
- `Describe(IReadOnlyDictionary<Guid,string> productNames)` → ข้อความบรรยายบรรทัดเดียวตาม research.md #11
  รับชื่อสินค้าเข้ามาเป็นพารามิเตอร์ เพราะ domain layer ต้องไม่ไปดึงข้อมูลสินค้าเอง
- `ReferencedProductIds` → `ProductId` ของทุก condition line บวก `GiftProductId` (ถ้ามี) —
  ใช้โดย use case เพื่อตรวจการมีอยู่ของสินค้าแบบ batch (research.md #7)
- validation ทั้งหมดอยู่ใน constructor และ `Update()` เหมือนรูปแบบของ `Promotion` เดิม
  โยน `ArgumentException` / `ArgumentOutOfRangeException` เมื่อผิดกฎ (FR-007)

**การ map ลงฐานข้อมูล**: `Reward` เป็น value object ที่แบนราบลงคอลัมน์ของ `conditional_promotions` เอง
(`reward_kind`, `gift_product_id`, `gift_quantity`, `discount_percentage`) ตามรูปแบบ owned type ของ EF Core

| คอลัมน์ | ชนิด PostgreSQL | หมายเหตุ |
|---|---|---|
| `Id` | uuid | PK |
| `Name` | varchar(100) | not null |
| `RewardKind` | varchar(10) | not null — `Gift` หรือ `Percentage` เก็บเป็นสตริงเหมือน `promotions.Scope` |
| `GiftProductId` | uuid | null เมื่อ `RewardKind = 'Percentage'` |
| `GiftQuantity` | integer | null เมื่อ `RewardKind = 'Percentage'` |
| `DiscountPercentage` | numeric(5,2) | null เมื่อ `RewardKind = 'Gift'` |
| `AppliesToMembersOnly` | boolean | not null |
| `StartDate` | date | not null |
| `EndDate` | date | not null |

> **ข้อตกลงการตั้งชื่อ**: โปรเจกต์นี้ตั้งชื่อ**ตาราง**เป็น snake_case แต่ปล่อยชื่อ**คอลัมน์**ไว้ตามชื่อพรอเพอร์ตี้
> (ดู `promotions`: `Id`, `Scope`, `DiscountPercentage`) ตารางใหม่จึงทำแบบเดียวกัน ฉบับแรกของเอกสารนี้เขียน
> คอลัมน์เป็น snake_case ซึ่งไม่ตรงกับของเดิม และพบตอนรันกับฐานข้อมูลจริงว่าได้ตารางที่ปนสองแบบ

**ไม่มี FK ไปที่ `products`** โดยเจตนา — ดูเหตุผลที่ research.md #7

---

## 2. ConditionLine (owned entity, ใหม่)

หนึ่งแถวของเงื่อนไขการซื้อ — ตาราง `conditional_promotion_lines`

| Field | ชนิด | ข้อจำกัด | ข้อกำหนดที่อ้าง |
|---|---|---|---|
| `Id` | Guid | PK (surrogate) | — |
| `ProductId` | Guid | required, ห้ามซ้ำภายในโปรโมชั่นเดียวกัน | FR-003 |
| `MinimumQuantity` | int | `>= 1` | FR-003 |

| คอลัมน์ | ชนิด PostgreSQL | หมายเหตุ |
|---|---|---|
| `Id` | uuid | PK |
| `ConditionalPromotionId` | uuid | FK → `conditional_promotions.Id`, ON DELETE CASCADE |
| `ProductId` | uuid | not null, ไม่มี FK ไป `products` |
| `MinimumQuantity` | integer | not null |

**Unique index** บน (`ConditionalPromotionId`, `ProductId`) — บังคับกฎ "ห้ามระบุสินค้าซ้ำ" ที่ระดับฐานข้อมูลด้วย
ไม่ใช่แค่ที่ระดับ domain

> **ทำไมเป็น unique index ไม่ใช่ PK ประกอบ**: การแก้ไขโปรโมชั่นแทนที่รายการเงื่อนไขทั้งชุด ถ้าคีย์เป็น
> (โปรโมชั่น, สินค้า) แบบธรรมชาติ สินค้าที่ผู้จัดการคงไว้เหมือนเดิมจะถูก EF Core มองเป็นแถวเก่าที่ถูกลบ
> บวกแถวใหม่ที่มีคีย์เดียวกัน แล้วปฏิเสธที่จะ track ทั้งคู่ — พังตอนกดแก้ไขครั้งแรก การใช้ surrogate key
> คู่กับ unique index ให้ข้อบังคับเดียวกันโดยไม่ชนกับ change tracker

---

## 3. Reward (value object, ใหม่)

สิ่งตอบแทนหนึ่งอย่างต่อโปรโมชั่นหนึ่งข้อ มีสองแบบที่แยกจากกันเด็ดขาด (FR-001)

```
Reward
├── Kind: RewardKind  { Gift, Percentage }
├── GiftProductId: Guid?      — required เมื่อ Kind = Gift, ต้องเป็น null เมื่อ Kind = Percentage
├── GiftQuantity: int?        — required และ >= 1 เมื่อ Kind = Gift, ต้องเป็น null เมื่อ Kind = Percentage
└── DiscountPercentage: decimal?  — required และอยู่ในช่วง (0, 100] เมื่อ Kind = Percentage,
                                    ต้องเป็น null เมื่อ Kind = Gift
```

**กฎที่ validate**: ชุดคอลัมน์ต้องสอดคล้องกับ `Kind` เสมอ (FR-004, FR-005, FR-007) —
รูปแบบเดียวกับกฎ `Scope == Item ⇒ ProductId != null` ของ `Promotion` เดิม

**`ValuePerSet(IReadOnlyDictionary<Guid,decimal> prices, IReadOnlyList<ConditionLine> lines)`** →
มูลค่าส่วนลดต่อหนึ่งชุด ใช้จัดลำดับตาม research.md #9

---

## 4. SaleLineItem (เดิม, เพิ่มหนึ่งคอลัมน์)

| Field | การเปลี่ยนแปลง | ข้อกำหนดที่อ้าง |
|---|---|---|
| `IsGift` | **ใหม่** — bool, default `false` | FR-016 |

คอลัมน์ `IsGift boolean not null default false` บน `sale_line_items` — เป็น additive migration ล้วน
แถวเดิมทั้งหมดได้ค่า `false` โดยอัตโนมัติ ไม่ต้องเขียน data migration

**invariant เดิมไม่เปลี่ยนเลย**: `UnitPriceSnapshot > 0`, `Quantity > 0`, `DiscountAmount >= 0`
บรรทัดของแถมสอดคล้องกับทั้งสามข้อ เพราะเก็บที่ราคาปกติแล้วหักส่วนลดเต็มจำนวน (research.md #4)
constructor เดิมจึงใช้ต่อได้ เพียงรับพารามิเตอร์ `isGift` เพิ่มโดยมีค่าเริ่มต้นเป็น `false`

**บิลหนึ่งใบมีบรรทัดของสินค้าเดียวกันได้สองบรรทัด** (บรรทัดจ่ายเงิน + บรรทัดของแถม) ซึ่ง `Sale` เดิมรองรับอยู่แล้ว
เพราะไม่มี unique constraint บน (`sale_id`, `product_id`)

---

## 5. SaleAppliedPromotion (owned entity, ใหม่)

บันทึกสิทธิ์ที่ถูกใช้ในบิล — ตาราง `sale_applied_promotions` (FR-024)

| Field | ชนิด | ข้อจำกัด |
|---|---|---|
| `Id` | Guid | PK |
| `SaleId` | Guid | FK → `sales.id`, ON DELETE CASCADE |
| `PromotionId` | Guid | ไม่มี FK — โปรโมชั่นอาจถูกลบไปแล้ว |
| `DescriptionSnapshot` | string | required, ข้อความบรรยาย ณ เวลาที่ขาย |
| `SetCount` | int | `>= 1` |
| `DiscountAmount` | decimal | `>= 0` |

`Sale` ได้ collection ใหม่ `AppliedPromotions` (read-only) ที่รับเข้ามาทาง constructor พร้อม line items
**ไม่มี mutation API เพิ่ม** — บิลยังเป็น append-only ตามเจตนาเดิมของ `Sale`

`Sale.SubtotalAmount` / `DiscountAmount` / `TotalAmount` **ยังคำนวณจาก `_lineItems` เหมือนเดิมทุกประการ**
ตารางนี้เป็นข้อมูลอธิบายที่มา ไม่ใช่แหล่งของยอดเงิน — ยอดของแถมและส่วนลดชุดอยู่ในบรรทัดอยู่แล้ว

---

## 6. PricedCart (ผลลัพธ์ของ `CartPricer`, ไม่ลงฐานข้อมูล)

โครงสร้างผลลัพธ์ที่ `CartPricer` คืน ใช้ร่วมกันทั้งพรีวิวและ checkout (research.md #2)

```
PricedCart
├── Lines: PricedLine[]
│   ├── ProductId, ProductName, UnitPrice
│   ├── Quantity
│   ├── DiscountAmount        — ส่วนลดชุด + ส่วนลดรายชิ้น + ส่วนแบ่งส่วนลดระดับบิล
│   ├── LineTotal             — UnitPrice × Quantity − DiscountAmount
│   └── IsGift
├── AppliedPromotions: AppliedPromotionResult[]
│   └── PromotionId, Description, SetCount, DiscountAmount
├── UnclaimedGifts: UnclaimedGift[]      — FR-015
│   └── PromotionId, Description, GiftProductId, GiftProductName, MissingQuantity
├── SubtotalAmount, DiscountAmount, TotalAmount
```

`UnclaimedGifts` เกิดขึ้นเมื่อสินค้าที่แถมไม่ได้อยู่ในเงื่อนไขและมีในตะกร้าน้อยกว่าสิทธิ์ที่ได้ (research.md #10 ข้อ 4)
เป็นข้อมูลสำหรับแสดงผลอย่างเดียว ไม่กระทบยอดเงินใดๆ

`CompleteSaleUseCase` แปลง `PricedCart` เป็น `SaleLineItem[]` + `SaleAppliedPromotion[]` ตรงๆ
ส่วน `PreviewSaleUseCase` คืนออกไปทาง API ทั้งก้อน — ยอดเงินจากสองเส้นทางจึงเท่ากันโดยโครงสร้าง (FR-012, SC-006)

---

## ความสัมพันธ์โดยรวม

```
ConditionalPromotion 1 ──< ConditionLine        (owned, cascade delete)
ConditionalPromotion 1 ──  Reward               (owned, flattened columns)
ConditionalPromotion    ··> Product             (อ้างด้วย id เท่านั้น ไม่มี FK — research.md #7)
Reward                  ··> Product             (อ้างด้วย id เท่านั้น ไม่มี FK)

Sale 1 ──< SaleLineItem          (เดิม, เพิ่มคอลัมน์ is_gift)
Sale 1 ──< SaleAppliedPromotion  (ใหม่, owned, cascade delete)
SaleAppliedPromotion    ··> ConditionalPromotion  (อ้างด้วย id เท่านั้น — โปรโมชั่นอาจถูกลบแล้ว)
```

`──<` = owned collection มี FK จริง | `··>` = อ้างถึงด้วย id เปล่า ไม่มี FK

---

## Migration

ไฟล์เดียว: `AddConditionalPromotions`

1. `CREATE TABLE conditional_promotions`
2. `CREATE TABLE conditional_promotion_lines` (PK ประกอบ + FK cascade)
3. `CREATE TABLE sale_applied_promotions` (FK cascade ไป `sales`)
4. `ALTER TABLE sale_line_items ADD COLUMN "IsGift" boolean NOT NULL DEFAULT false`

เป็น additive ทั้งหมด ไม่มี `DROP`, ไม่มีการแก้ไขข้อมูลเดิม, ไม่แตะตาราง `promotions`
migration ย้อนกลับได้ด้วยการ drop สามตารางและคอลัมน์เดียวที่เพิ่ม
