# การคำนวณโปรโมชั่น / ส่วนลด

ส่วนลดทั้งหมดคำนวณ **ฝั่ง `api/` เท่านั้น** ตอนปิดบิล (`POST /api/v1/sales`) — ไม่มีการ preview
ส่วนลดที่แม่นยำระหว่างช้อป (ดู [`sales-screen.md`](./sales-screen.md) หัวข้อ 2)

โค้ดหลักที่เกี่ยวข้อง (ทั้งหมดอยู่ใต้ `api/src/`):

| ไฟล์ | หน้าที่ |
|---|---|
| `TaladPOS.Domain/Promotions/Promotion.cs` | Entity โปรโมชั่น + validation (`DiscountPercentage` ต้องอยู่ใน (0,100], `Scope=Item` ต้องมี `ProductId`, `Scope=Bill` ต้องไม่มี) |
| `TaladPOS.Domain/Promotions/DiscountResolver.cs` | **กฎการเลือกโปรโมชั่น** — static, pure function, ไม่แตะ DB |
| `TaladPOS.Application/Sales/CompleteSaleUseCase.cs` | Orchestrate ทั้ง flow ปิดบิล: ตัดสต็อก → เรียก DiscountResolver → กระจายส่วนลดบิลลง line → บันทึก |
| `TaladPOS.Infrastructure/Repositories/PromotionRepository.cs` | ดึงโปรโมชั่นที่ active วันนี้จาก DB |

## 1. โครงสร้างโปรโมชั่น (`Promotion`)

> **สรุปสั้น — โปรโมชั่นมีกี่แบบ เก็บไว้ที่ไหน:** มี **2 ชนิดตาม `Scope`** เท่านั้น คือ `Item`
> (ลดสินค้าชิ้นเดียว) กับ `Bill` (ลดทั้งบิล) — เก็บทั้งคู่ในตารางเดียวกันคือ **`promotions`** ใน
> PostgreSQL (ดูคอลัมน์ทั้งหมดที่ [`architecture.md`](./architecture.md#promotions)) ไม่ได้แยกตาราง
> ตามชนิด ส่วน "เฉพาะสมาชิก" (`AppliesToMembersOnly`) **ไม่ใช่ชนิดที่ 3** แต่เป็น flag แยกที่ผูกกับ
> โปรโมชั่นชนิดไหนก็ได้ (เช่น "Item ลด 10% เฉพาะสมาชิก" หรือ "Bill ลด 5% เฉพาะสมาชิก")

| Field | ความหมาย |
|---|---|
| `Scope` | `Item` (ผูกกับสินค้าชิ้นเดียว) หรือ `Bill` (ลดทั้งบิล) — enum เก็บเป็น string ในคอลัมน์ `Scope` |
| `DiscountPercentage` | เปอร์เซ็นต์ส่วนลด (0, 100] |
| `ProductId` | บังคับมีค่าเมื่อ `Scope=Item`, ต้องเป็น `null` เมื่อ `Scope=Bill` |
| `AppliesToMembersOnly` | ถ้า `true` ใช้ได้เฉพาะบิลที่ระบุ `memberId` เท่านั้น — เป็น flag ไม่ใช่ scope แยก (ดูกล่องสรุปด้านบน) |
| `StartDate` / `EndDate` | active เมื่อ `StartDate <= วันนี้ <= EndDate` (รวมทั้งสองวัน) |

Entity: `api/src/TaladPOS.Domain/Promotions/Promotion.cs` — validation ทั้งหมดอยู่ใน `Promotion.Validate()`
(บรรทัด 71-94) เช่น `DiscountPercentage` ต้องอยู่ในช่วง (0,100], `EndDate >= StartDate`

จัดการผ่านหน้า **"โปรโมชั่น"** (`web/src/app/(protected)/promotions/page.tsx` + `PromotionFormDialog.tsx`)
ซึ่งเป็นเมนูที่จำกัดเฉพาะ **Manager** เท่านั้น (`[Authorize(Roles = "Manager")]` ทั้ง controller) ผ่าน
endpoint `GET/POST/PUT/DELETE /api/v1/promotions` (`PromotionsController.cs`)

## 2. กฎการเลือกโปรโมชั่น (`DiscountResolver`)

> **กฎเดียว ท่องไว้ตัวเดียว**: ภายใน scope เดียวกัน (Item หรือ Bill) ถ้ามีโปรโมชั่นเข้าเงื่อนไขพร้อมกัน
> หลายตัว **เลือกตัวที่คำนวณแล้วให้จำนวนเงินส่วนลดมากที่สุดเพียงตัวเดียว ไม่บวกสะสมกัน**
> แต่ส่วนลดระดับ **Item** และระดับ **Bill** เป็นคนละ scope กัน จึง **รวมกันได้** บน line เดียวกัน

### 2.1 ส่วนลดระดับสินค้า (Item-scope) — ต่อ 1 line item

```mermaid
flowchart TD
    A["สำหรับ line item: productId, lineSubtotal = price × quantity"] --> B["กรองโปรโมชั่นทั้งหมดที่ active วันนี้ (จาก GetActiveOnAsync)\nด้วยเงื่อนไข:\n• Scope == Item\n• ProductId ตรงกับสินค้านี้\n• AppliesToMembersOnly == false หรือ บิลนี้มี memberId"]
    B --> C{"มีตัวที่เข้าเงื่อนไข\nอย่างน้อย 1 ตัว?"}
    C -- "ไม่มี" --> D["ItemDiscount = 0"]
    C -- "มี" --> E["สำหรับแต่ละตัว: discount = round(lineSubtotal × DiscountPercentage / 100, 2)"]
    E --> F["ItemDiscount = ค่ามากที่สุดในกลุ่มนี้ (Max)"]
```

### 2.2 ส่วนลดระดับบิล (Bill-scope) — คำนวณครั้งเดียวต่อทั้งบิล

```mermaid
flowchart TD
    A["billSubtotal = Σ (price × quantity) ทุก line\n← ยอดรวมก่อนหักส่วนลดใด ๆ ทั้งสิ้น"] --> B["กรองโปรโมชั่นที่ active วันนี้ด้วยเงื่อนไข:\n• Scope == Bill\n• AppliesToMembersOnly == false หรือ บิลนี้มี memberId"]
    B --> C{"มีตัวที่เข้าเงื่อนไข?"}
    C -- "ไม่มี" --> D["BillDiscount = 0"]
    C -- "มี" --> E["สำหรับแต่ละตัว: discount = round(billSubtotal × DiscountPercentage / 100, 2)"]
    E --> F["BillDiscount = ค่ามากที่สุดในกลุ่มนี้ (Max)"]
```

**ข้อสังเกตสำคัญ:** `billSubtotal` ที่ใช้คำนวณ Bill-scope คือยอดรวม**ก่อน**หักส่วนลด Item-scope ใด ๆ
(ดู `CompleteSaleUseCase.cs` บรรทัด `billSubtotal = pendingLines.Sum(l => l.Subtotal)` ซึ่ง `Subtotal`
คือ `UnitPrice × Quantity` เท่านั้น ไม่ลบ `ItemDiscount` ออกก่อน)

### 2.3 กระจายส่วนลดระดับบิลลงแต่ละ line (`DistributeBillDiscount`)

เพราะ `SaleLineItem` แต่ละแถวเก็บ `DiscountAmount` เป็นตัวเลขเดียว (ไม่มีคอลัมน์แยก item/bill) และ
`Sale.DiscountAmount` = ผลรวม `DiscountAmount` ของทุก line (ไม่ใช่คอลัมน์ที่เก็บจริง — เป็น computed
property) ส่วนลดระดับบิลจึงต้องถูก "หาร" ลงแต่ละ line ตามสัดส่วนยอดของ line นั้น:

```
share(line) = round(BillDiscount × line.Subtotal / billSubtotal, 2)   — สำหรับทุก line ยกเว้น line สุดท้าย
share(line สุดท้าย) = BillDiscount − Σ share ของ line ก่อนหน้าทั้งหมด  — กันเศษสตางค์จากการปัดเศษหาย/เกิน

line.DiscountAmount = ItemDiscount(line) + share(line)
```

การให้ line สุดท้ายรับเศษที่เหลือ ทำให้ `Σ line.DiscountAmount == BillDiscount + Σ ItemDiscount` เป๊ะ
เสมอ ไม่มีเศษสตางค์หายหรือเกินจากการปัดเศษ (มี unit test คุมพฤติกรรมนี้โดยตรงใน
`CompleteSaleUseCaseTests.ExecuteAsync_WithActiveBillPromotion_DistributesDiscountAcrossAllLines`)

หากไม่มี Bill-scope discount เลย (`BillDiscount == 0`) จะข้ามการกระจายทั้งหมด แต่ละ line ใช้แค่
`ItemDiscount` ของตัวเอง

## 3. Sequence Diagram: ปิดบิล (Checkout) แบบเต็ม

```mermaid
sequenceDiagram
    actor Cashier
    participant UI as SalesPage (web)
    participant API as SalesController
    participant UC as CompleteSaleUseCase
    participant ProdRepo as ProductRepository
    participant PromoRepo as PromotionRepository
    participant Resolver as DiscountResolver
    participant SaleRepo as SaleRepository
    participant MemRepo as MemberRepository
    participant DB as PostgreSQL

    Cashier->>UI: กดปุ่ม "ชำระเงิน"
    UI->>API: POST /api/v1/sales\n{ memberId, lineItems[] }  (JWT Bearer)
    API->>API: staffId = JWT claim "staffId"
    API->>UC: ExecuteAsync(CompleteSaleRequest)
    UC->>DB: BEGIN TRANSACTION

    alt ระบุ memberId มา
        UC->>MemRepo: GetByIdAsync(memberId)
        MemRepo-->>UC: Member หรือ null
        Note over UC: ถ้าไม่พบ → throw KeyNotFoundException → 404 (rollback)
    end

    UC->>PromoRepo: GetActiveOnAsync(today)
    PromoRepo->>DB: SELECT * FROM promotions\nWHERE StartDate<=today AND today<=EndDate
    DB-->>PromoRepo: แถวโปรโมชั่นที่ active วันนี้
    PromoRepo-->>UC: activePromotions (ทั้งหมด ยังไม่กรอง scope)

    loop ทุก line item ในตะกร้า
        UC->>ProdRepo: GetByIdAsync(productId)
        ProdRepo-->>UC: Product (ราคา ณ ปัจจุบัน, สต็อก)
        UC->>ProdRepo: TryDecreaseStockAsync(productId, qty)
        ProdRepo->>DB: UPDATE products SET "StockQuantity" = "StockQuantity" - qty\nWHERE "Id"=productId AND "StockQuantity">=qty
        DB-->>ProdRepo: affectedRows (atomic conditional update — กันแข่งขายพร้อมกันหลายจุดขาย)
        alt affectedRows == 0 (สต็อกไม่พอ)
            ProdRepo-->>UC: false
            UC->>DB: ROLLBACK
            UC-->>API: throw InsufficientStockException(productId)
            API-->>UI: 409 { error: "insufficient_stock", productId }
        else สำเร็จ
            ProdRepo-->>UC: true
            UC->>Resolver: ResolveItemDiscount(activePromotions, productId, hasMember, lineSubtotal, today)
            Note right of Resolver: กรอง Scope=Item + ProductId ตรง + members-only check<br/>→ เลือกส่วนลด % ที่มากสุด → คูณ lineSubtotal → ปัด 2 ตำแหน่ง
            Resolver-->>UC: itemDiscount
        end
    end

    UC->>UC: billSubtotal = Σ (price × qty) ทุก line (ยังไม่หัก itemDiscount)
    UC->>Resolver: ResolveBillDiscount(activePromotions, hasMember, billSubtotal, today)
    Note right of Resolver: กรอง Scope=Bill + members-only check<br/>→ เลือกส่วนลด % ที่มากสุด → คูณ billSubtotal → ปัด 2 ตำแหน่ง
    Resolver-->>UC: billDiscount

    UC->>UC: DistributeBillDiscount(lines, billDiscount)\n→ กระจายตามสัดส่วน subtotal, เศษไปลงที่ line สุดท้าย\n→ line.DiscountAmount = itemDiscount + share

    UC->>SaleRepo: AddAsync(new Sale(staffId, memberId, lineItems))
    SaleRepo->>DB: INSERT INTO sales (...)\nINSERT INTO sale_line_items (...) ×N

    opt มี memberId
        UC->>MemRepo: IncreaseAccumulatedPurchaseTotalAsync(memberId, sale.TotalAmount)
        MemRepo->>DB: UPDATE members SET "AccumulatedPurchaseTotal" += totalAmount
        Note right of MemRepo: totalAmount ที่บวกคือยอด "หลังหักส่วนลดแล้ว" (FR-014)
    end

    UC->>DB: COMMIT TRANSACTION
    UC-->>API: Sale (มี Id, LineItems, Subtotal/Discount/TotalAmount ที่คำนวณจาก LineItems)
    API-->>UI: 201 Created — SaleDto
    UI->>UI: ล้าง cartLines, reload สินค้า (สต็อกอัปเดตแล้ว)
    UI->>Cashier: แสดง ReceiptDialog (ยอดก่อนลด / ส่วนลด / ยอดสุทธิ จริงจาก server)
```

ทุกขั้นตอนข้างบน (ตัดสต็อก, บันทึกบิล, อัปเดตยอดสะสมสมาชิก) อยู่ใน **database transaction เดียวกัน**
— ถ้าขั้นใดขั้นหนึ่งล้มเหลว (เช่น สต็อกไม่พอสำหรับสินค้าตัวที่ 2 ในตะกร้า) ทุกอย่างจะ rollback หมด
ไม่มีสถานะครึ่ง ๆ กลาง ๆ (ตัดสต็อกไปแล้วแต่ไม่บันทึกบิล เป็นต้น)

## 4. ตัวอย่างตัวเลขจริง (จาก unit test ใน `CompleteSaleUseCaseTests.cs`)

### ตัวอย่างที่ 1 — โปรโมชั่นระดับสินค้า (Item)

ตะกร้า: มะม่วง 45.00 × 2, แอปเปิ้ล 60.00 × 1 — มีโปรโมชั่น "มะม่วงลด 10%" (`Scope=Item`, ผูกกับมะม่วง)

| | คำนวณ | ผลลัพธ์ |
|---|---|---|
| Line มะม่วง subtotal | 45.00 × 2 | 90.00 |
| Line มะม่วง discount | 90.00 × 10% | **9.00** |
| Line แอปเปิ้ล discount | ไม่มีโปรโมชั่นของสินค้านี้ | **0.00** |
| `Sale.DiscountAmount` | 9.00 + 0.00 | **9.00** |
| `Sale.TotalAmount` | (90.00+60.00) − 9.00 | **141.00** |

### ตัวอย่างที่ 2 — โปรโมชั่นระดับบิล (Bill)

ตะกร้าเดิม (รวม 150.00) — เปลี่ยนเป็นโปรโมชั่น "ลดทั้งบิล 10%" (`Scope=Bill`, ไม่ผูก member)

| | คำนวณ | ผลลัพธ์ |
|---|---|---|
| `billSubtotal` | 90.00 + 60.00 | 150.00 |
| `BillDiscount` | 150.00 × 10% | 15.00 |
| ส่วนแบ่งมะม่วง | round(15.00 × 90/150, 2) | 9.00 |
| ส่วนแบ่งแอปเปิ้ล (line สุดท้าย = เศษที่เหลือ) | 15.00 − 9.00 | 6.00 |
| `Sale.DiscountAmount` | 9.00 + 6.00 | **15.00** (ตรงกับ `BillDiscount` เป๊ะ) |
| `Sale.TotalAmount` | 150.00 − 15.00 | **135.00** |

### ตัวอย่างที่ 3 — โปรโมชั่นเฉพาะสมาชิก (`AppliesToMembersOnly = true`)

โปรโมชั่น "สมาชิกลด 20%" ผูกกับมะม่วง (45.00 × 1):

- **บิลไม่ผูกสมาชิก** (`memberId = null`) → `hasMember = false` → โปรโมชั่นนี้ไม่เข้าเงื่อนไข →
  `DiscountAmount = 0.00`
- **บิลผูกสมาชิก** → `hasMember = true` → เข้าเงื่อนไข → `DiscountAmount = 45.00 × 20% = 9.00`

## 5. เคสที่มักเข้าใจผิด

| คำถาม | คำตอบ |
|---|---|
| ถ้ามีโปรโมชั่น Item 10% + Bill 5% พร้อมกัน ลดรวมกี่ %? | ลดทั้งสองซ้อนกันได้ (คนละ scope) — line ที่โดนทั้งคู่จะได้ `itemDiscount + ส่วนแบ่ง billDiscount` |
| มีโปรโมชั่น Item ของสินค้าเดียวกัน 2 ตัวพร้อมกัน (เช่น 10% กับ 15%) จะได้ลดกี่ %? | ได้แค่ตัวที่ให้ส่วนลด**เป็นเงิน**มากกว่า (ในที่นี้คือ 15%) ตัวเดียว ไม่ใช่ 25% |
| โปรโมชั่นที่ `EndDate` เป็นเมื่อวาน ยังใช้ได้ไหม? | ไม่ได้ — เช็คด้วย `StartDate <= today <= EndDate` แบบ inclusive ที่ **`today` ฝั่งเซิร์ฟเวอร์ (UTC)** ตอนกดชำระเงินจริง ไม่ใช่ตอนเปิดหน้าตะกร้า |
| หน้าตะกร้าโชว์ราคาหลังหักส่วนลดระหว่างช้อปไหม? | **ไม่** — โชว์แค่ราคารวมดิบ ส่วนลดจริงรู้ตอนได้ response จาก `POST /api/v1/sales` เท่านั้น |
