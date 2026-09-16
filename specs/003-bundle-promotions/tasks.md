---

description: "Task list for Bundle & Gift Promotions"
---

# Tasks: Bundle & Gift Promotions (โปรโมชั่นแบบมีเงื่อนไข)

**Input**: Design documents from `/specs/003-bundle-promotions/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/](./contracts/)

**Tests**: **บังคับ ไม่ใช่ทางเลือก** — constitution Principle III (NON-NEGOTIABLE) กำหนดว่า business logic ทุกจุด
ต้องมี unit test ที่รันได้โดยไม่พึ่ง PostgreSQL จริง และ `plan.md` ส่วน Testing ระบุชุดเทสที่ต้องมีไว้แล้ว
งานเทสในไฟล์นี้จึงเขียน **ก่อน** งาน implementation ของเฟสเดียวกันเสมอ และต้องเห็นว่า fail ก่อนจึงเริ่มเขียนโค้ด

**Organization**: จัดกลุ่มตาม user story เพื่อให้แต่ละเรื่องส่งมอบและทดสอบได้อิสระ

## Format: `[ID] [P?] [Story] Description`

- **[P]**: รันขนานได้ (คนละไฟล์ ไม่มี dependency ค้าง)
- **[Story]**: US1 / US2 / US3 ตาม user story ใน spec.md
- ทุกงานระบุ path ไฟล์จริง

## Path Conventions

โปรเจกต์นี้เป็น web application แยก `api/` (ASP.NET Core DDD) กับ `web/` (Next.js) ตาม constitution
Repository Structure — path ทั้งหมดด้านล่างอ้างจาก repository root

> **หมายเหตุเรื่องขนาดของ Phase 2**: ฟีเจอร์นี้มี "แกนกลางร่วม" ก้อนใหญ่โดยธรรมชาติ — ทั้งสาม user story
> ใช้ aggregate เดียวกัน ตารางเดียวกัน migration ไฟล์เดียวกัน (data-model.md กำหนดให้เป็นไฟล์เดียว)
> และ `CartPricer` ตัวเดียวกัน Phase 2 จึงยาวกว่าปกติโดยเจตนา สิ่งที่แยกไปอยู่ในแต่ละ story คือ
> **การให้สิทธิ์แต่ละแบบที่จุดขาย** ซึ่งเป็นส่วนที่ทดสอบและส่งมอบแยกกันได้จริง

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: ยืนยันจุดตั้งต้นและเก็บค่าอ้างอิงไว้พิสูจน์ว่าของเดิมไม่พังทีหลัง (FR-026, SC-007)

- [X] T001 รัน `dotnet test api/TaladPOS.sln` ให้ผ่านทั้งหมดก่อนเริ่มงาน และบันทึกจำนวนเทสที่ผ่านไว้เป็นค่าตั้งต้น
- [X] T002 [P] บันทึก golden baseline ของยอดบิล โดยรัน `./scripts/reset-test-data.ps1` แล้วเก็บ `subtotalAmount` / `discountAmount` / `totalAmount` ของบิลตัวอย่างอย่างน้อย 5 บิลจาก `GET /api/v1/sales` ลงไฟล์ `specs/003-bundle-promotions/baseline-totals.json` เพื่อใช้ตรวจ regression ที่ T063

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: aggregate ใหม่ สคีมา repository CRUD และแกนกลางของ `CartPricer` ที่ทุก user story ต้องใช้

**⚠️ CRITICAL**: ห้ามเริ่ม Phase 3 ขึ้นไปจนกว่าเฟสนี้จะเสร็จ

### Tests (เขียนก่อน ต้องเห็น fail ก่อนเริ่ม implementation)

- [X] T003 [P] เขียนเทส validation ของ aggregate ใน `api/tests/TaladPOS.Domain.Tests/Promotions/ConditionalPromotionValidationTests.cs` ครอบคลุมทุกกฎในตาราง Validation ของ `contracts/conditional-promotions.md`: `name` ยาว 1–100 ตัวอักษร, `conditionLines` อย่างน้อย 1 แถว, ห้าม `productId` ซ้ำในเงื่อนไขเดียวกัน, `minimumQuantity >= 1`, `kind = Gift` ⇒ `giftProductId` ไม่เป็น null และ `giftQuantity >= 1`, `kind = Percentage` ⇒ `0 < discountPercentage <= 100`, ฟิลด์ของ reward อีกแบบต้องเป็น null, `endDate >= startDate`
- [X] T004 [P] เขียนเทสการนับชุดและความคงเส้นคงวาใน `api/tests/TaladPOS.Domain.Tests/Promotions/CartPricerTests.cs` ครอบคลุม FR-010 (จำนวนชุด = ค่าน้อยที่สุดของ `floor(จำนวนในตะกร้า ÷ จำนวนขั้นต่ำ)` ข้ามทุกรายการ) และ FR-012 (ตะกร้าเดียวกันสลับลำดับ input แล้วผลลัพธ์เท่ากันทุกฟิลด์) โดยยืนยันที่ `SetCount` ของ `AppliedPromotionResult` — **ห้ามยืนยันมูลค่าส่วนลดหรือ FR-018 ในงานนี้** เพราะยังไม่มี branch ของ reward ใดทำงาน จึงยังไม่มีส่วนลดให้ตรวจ (ไปอยู่ที่ T038)

### Domain — aggregate ใหม่

- [X] T005 [P] สร้าง `RewardKind` enum (`Gift`, `Percentage`) และ value object `Reward` ใน `api/src/TaladPOS.Domain/Promotions/Reward.cs` พร้อม validation: `Kind = Gift` ⇒ `GiftProductId` ไม่เป็น null, `GiftQuantity >= 1`, `DiscountPercentage` ต้องเป็น null; `Kind = Percentage` ⇒ `DiscountPercentage` อยู่ในช่วง `(0, 100]`, `GiftProductId` และ `GiftQuantity` ต้องเป็น null (FR-004, FR-005, FR-007)
- [X] T006 [P] สร้าง `ConditionLine` ใน `api/src/TaladPOS.Domain/Promotions/ConditionLine.cs` มี `ProductId` (Guid) และ `MinimumQuantity` (int, ต้อง `>= 1`) (FR-003)
- [X] T007 สร้าง aggregate root `ConditionalPromotion` ใน `api/src/TaladPOS.Domain/Promotions/ConditionalPromotion.cs` — ฟิลด์ `Id`, `Name` (required, 1–100 ตัวอักษร), `ConditionLines` (อย่างน้อย 1 แถว, `ProductId` ห้ามซ้ำ), `Reward` (required), `AppliesToMembersOnly`, `StartDate`, `EndDate` (`>= StartDate`); validation อยู่ใน constructor และ `Update()` เหมือนรูปแบบของ `Promotion.cs` เดิม; เพิ่ม `IsActive(DateOnly date)` และ `ReferencedProductIds` (FR-001, FR-002, FR-003, FR-006, FR-007) — ขึ้นกับ T005, T006
- [X] T008 เพิ่ม `ConditionalPromotion.Describe(IReadOnlyDictionary<Guid,string> productNames)` ใน `api/src/TaladPOS.Domain/Promotions/ConditionalPromotion.cs` คืนข้อความรูปแบบ `ซื้อ {ชื่อ} {จำนวน} [+ {ชื่อ} {จำนวน}]... {แถม {ชื่อ} {จำนวน} | ลด {x}%}` โดยรับชื่อสินค้าเป็นพารามิเตอร์ ไม่ดึงเอง (FR-008, research.md #11)
- [X] T009 เพิ่ม `Reward.ValuePerSet(IReadOnlyDictionary<Guid,decimal> prices, IReadOnlyList<ConditionLine> lines)` ใน `api/src/TaladPOS.Domain/Promotions/Reward.cs` — แบบของแถม = `จำนวนที่แถม × ราคาสินค้าที่แถม`, แบบเปอร์เซ็นต์ = `เปอร์เซ็นต์ × ผลรวม(จำนวนขั้นต่ำ × ราคา)` ของทุกรายการในเงื่อนไข (FR-019, research.md #9)

### Domain — โครงผลลัพธ์และแกนกลาง `CartPricer`

- [X] T010 [P] สร้างชนิดผลลัพธ์ `PricedCart`, `PricedLine`, `AppliedPromotionResult`, `UnclaimedGift` ใน `api/src/TaladPOS.Domain/Promotions/PricedCart.cs` ตามโครงใน `data-model.md` §6
- [X] T011 สร้าง `CartPricer` ใน `api/src/TaladPOS.Domain/Promotions/CartPricer.cs` เป็น static/pure ไม่เรียก repository ไม่อ่านนาฬิกา รับพารามิเตอร์ 5 ตัวตาม `research.md` #2 (รายการตะกร้าที่รวมยอดแล้ว, ตารางค้นหาสินค้าที่ครอบคลุมสินค้าในตะกร้า ∪ สินค้าที่โปรโมชั่นอ้างถึง, โปรโมชั่นเปอร์เซ็นต์ที่มีผล, โปรโมชั่นแบบมีเงื่อนไขที่ใช้ได้, `hasMember` + `DateOnly`) แล้วทำขั้นตอนที่ 1–3 ของ `research.md` #10: รวมยอดตาม `productId` และเรียง, กรองตามวันที่และเงื่อนไขสมาชิก, เรียงลำดับสามชั้น (มูลค่าต่อชุดมาก→น้อย, `StartDate` เก่า→ใหม่, `Id` น้อย→มาก) — ขึ้นกับ T009, T010
- [X] T012 เพิ่มขั้นตอนที่ 4 (ส่วนการนับชุดและหักคงเหลือ) ลงใน `api/src/TaladPOS.Domain/Promotions/CartPricer.cs` — คำนวณ `sets` ตาม FR-010 ข้ามทุกรายการในเงื่อนไข, ข้ามโปรโมชั่นเมื่อ `sets == 0`, หักจำนวนที่ใช้ออกจากคงเหลือเพื่อไม่ให้หน่วยเดิมถูกนับซ้ำ (FR-018) และบันทึก `AppliedPromotionResult` โดยกรอก `SetCount` ไว้ก่อน ส่วน `DiscountAmount` ให้ branch ของ reward แต่ละแบบเป็นผู้เติม — **ยังไม่ทำ branch ของ reward ใดในงานนี้** (จะทำที่ T042 สำหรับของแถม, T051–T052 สำหรับของแถมที่เป็นสินค้าเดียวกับเงื่อนไข และ T055 สำหรับส่วนลดเปอร์เซ็นต์)
- [X] T013 เพิ่มขั้นตอนที่ 5–6 ลงใน `api/src/TaladPOS.Domain/Promotions/CartPricer.cs` — นำหน่วยที่เหลือของแต่ละสินค้าไปเข้า `DiscountResolver.ResolveItemDiscount` ตามกติกาเดิม แล้วประกอบ `PricedLine` ของบรรทัดที่จ่ายเงิน (`IsGift = false`) — ขึ้นกับ T012
- [X] T014 เพิ่มขั้นตอนที่ 7 ลงใน `api/src/TaladPOS.Domain/Promotions/CartPricer.cs` — ฐานส่วนลดระดับบิลคิดจากผลรวม `ราคา × จำนวน` ของ**บรรทัดที่จ่ายเงินเท่านั้น** เรียก `DiscountResolver.ResolveBillDiscount` แล้วกระจายลงเฉพาะบรรทัดที่จ่ายเงินตามสัดส่วน ยกเศษการปัดไปบรรทัดสุดท้าย (FR-021, research.md #5) — ขึ้นกับ T013

### Domain — ส่วนขยายฝั่งบิลขาย (สคีมาต้องพร้อมก่อนทำ migration ไฟล์เดียว)

- [X] T015 [P] เพิ่มพรอเพอร์ตี้ `IsGift` (bool) ใน `api/src/TaladPOS.Domain/Sales/SaleLineItem.cs` และเพิ่มพารามิเตอร์ `bool isGift = false` ต่อท้าย constructor — **ห้ามแก้ invariant เดิม** (`UnitPriceSnapshot > 0`, `Quantity > 0`, `DiscountAmount >= 0`) เพราะบรรทัดของแถมสอดคล้องกับทั้งสามข้ออยู่แล้ว (FR-016, research.md #4)
- [X] T016 [P] สร้าง `SaleAppliedPromotion` ใน `api/src/TaladPOS.Domain/Sales/SaleAppliedPromotion.cs` — `Id`, `SaleId`, `PromotionId` (ไม่มี FK), `DescriptionSnapshot` (required), `SetCount` (`>= 1`), `DiscountAmount` (`>= 0`) (FR-024)
- [X] T017 เพิ่ม collection `AppliedPromotions` (read-only) ใน `api/src/TaladPOS.Domain/Sales/Sale.cs` โดยรับเข้าทาง constructor พร้อม line items — **ห้ามเพิ่ม mutation API** บิลยังเป็น append-only และ `SubtotalAmount`/`DiscountAmount`/`TotalAmount` ยังคำนวณจาก `_lineItems` เหมือนเดิม — ขึ้นกับ T016

### Infrastructure — สคีมาและ repository

- [X] T018 [P] สร้าง `ConditionalPromotionConfiguration` ใน `api/src/TaladPOS.Infrastructure/Configurations/ConditionalPromotionConfiguration.cs` — ตาราง `conditional_promotions` (`name varchar(100) not null`, `reward_kind varchar(10) not null` เก็บเป็นสตริงแบบเดียวกับ `promotions.scope`, `gift_product_id uuid null`, `gift_quantity integer null`, `discount_percentage numeric(5,2) null`, `applies_to_members_only boolean not null`, `start_date date not null`, `end_date date not null`) และ owned collection ลงตาราง `conditional_promotion_lines` (PK ประกอบ `(conditional_promotion_id, product_id)`, `minimum_quantity integer not null`, FK ไป `conditional_promotions` แบบ cascade) — **ไม่ตั้ง FK ไป `products`** (research.md #7)
- [X] T019 [P] แก้ `api/src/TaladPOS.Infrastructure/Configurations/SaleLineItemConfiguration.cs` เพิ่มคอลัมน์ `is_gift boolean not null default false`
- [X] T020 แก้ `api/src/TaladPOS.Infrastructure/Configurations/SaleConfiguration.cs` เพิ่ม owned collection `AppliedPromotions` ลงตาราง `sale_applied_promotions` (FK ไป `sales` แบบ cascade, `promotion_id uuid not null` ไม่มี FK, `description_snapshot` required, `set_count integer`, `discount_amount numeric`) — ขึ้นกับ T017
- [X] T021 เพิ่ม `DbSet<ConditionalPromotion>` และลงทะเบียน configuration ใหม่ใน DbContext ที่ `api/src/TaladPOS.Infrastructure/Persistence/` — ขึ้นกับ T018
- [X] T022 สร้าง migration ชื่อ `AddConditionalPromotions` ใน `api/src/TaladPOS.Infrastructure/Persistence/Migrations/` ครอบคลุม 4 การเปลี่ยนแปลงตาม `data-model.md` §Migration ในไฟล์เดียว: สร้าง `conditional_promotions`, `conditional_promotion_lines`, `sale_applied_promotions` และ `ALTER TABLE sale_line_items ADD COLUMN is_gift boolean NOT NULL DEFAULT false` — ต้องเป็น additive ล้วน ไม่มี `DROP` ไม่แตะตาราง `promotions` และย้อนกลับได้ — ขึ้นกับ T019, T020, T021
- [X] T023 [P] สร้าง `IConditionalPromotionRepository` ใน `api/src/TaladPOS.Application/Promotions/IConditionalPromotionRepository.cs` — `GetByIdAsync`, `ListAsync(activeOnly, today)`, `GetActiveOnAsync(date)`, `AddAsync`, `UpdateAsync`, `DeleteAsync` ตามรูปแบบของ `IPromotionRepository` เดิม
- [X] T024 สร้าง `ConditionalPromotionRepository` ใน `api/src/TaladPOS.Infrastructure/Repositories/ConditionalPromotionRepository.cs` โดยโหลด `ConditionLines` มาด้วยเสมอ — ขึ้นกับ T023, T021

### Application + API — CRUD ของโปรโมชั่นแบบมีเงื่อนไข

- [X] T025 [P] สร้าง `CreateConditionalPromotionUseCase`, `UpdateConditionalPromotionUseCase`, `DeleteConditionalPromotionUseCase` ใน `api/src/TaladPOS.Application/Promotions/` — ตรวจว่าสินค้าทุกตัวที่อ้างถึงมีอยู่จริงก่อนบันทึก (คืน `product_not_found`) และบันทึกทั้ง aggregate ใน transaction เดียว ห้ามบันทึกบางส่วน (FR-007) — ขึ้นกับ T023
- [X] T026 สร้าง `ListConditionalPromotionsQuery` ใน `api/src/TaladPOS.Application/Promotions/ListConditionalPromotionsQuery.cs` — โหลดชื่อสินค้าที่ถูกอ้างถึงแบบ batch แล้วประกอบ `description` ด้วย `Describe()` พร้อมคำนวณ `isUsable` และ `unusableReason` เมื่อมีสินค้าที่ถูกลบ และคืน `productName`/`giftProductName` เป็น null สำหรับสินค้าที่หายไป (FR-008, FR-023) — ขึ้นกับ T008, T024
- [X] T027 สร้าง `ConditionalPromotionsController` ใน `api/src/TaladPOS.Api/Controllers/ConditionalPromotionsController.cs` ที่ `[Route("api/v1/conditional-promotions")]` และ `[Authorize(Roles = nameof(StaffRole.Manager))]` ครบทั้ง 4 endpoint ตาม `contracts/conditional-promotions.md` พร้อมรหัส error ตามตาราง Validation — ขึ้นกับ T025, T026

### Application + API — พรีวิวตะกร้าและการรีแฟกเตอร์ checkout

- [X] T028 สร้าง `PreviewSaleUseCase` ใน `api/src/TaladPOS.Application/Sales/PreviewSaleUseCase.cs` — โหลดสินค้าแบบ batch ครอบคลุม **สินค้าในตะกร้า ∪ สินค้าทุกตัวที่โปรโมชั่นแบบมีเงื่อนไขที่มีผลอ้างถึง**, ตัดโปรโมชั่นที่มีสินค้าหายไปแม้แต่ตัวเดียวออก, แล้วเรียก `CartPricer` — **ห้ามเปิด transaction ห้ามตัดสต็อก ห้ามบันทึกอะไร** (research.md #3, #7) — ขึ้นกับ T014, T024
- [X] T029 เพิ่ม `POST /api/v1/sales/preview` ใน `api/src/TaladPOS.Api/Controllers/SalesController.cs` รับ body รูปร่างเดียวกับ `POST /api/v1/sales` คืน `PricedCartDto` ตาม `contracts/sales-preview.md` — ไม่ใส่ `[Authorize(Roles=...)]` เพื่อให้แคชเชียร์เรียกได้ผ่าน `FallbackPolicy` เดิม — ขึ้นกับ T028
- [X] T030 รีแฟกเตอร์ `api/src/TaladPOS.Application/Sales/CompleteSaleUseCase.cs` ให้ใช้ `CartPricer` ตัวเดียวกับพรีวิว และ **สลับลำดับ** เป็น โหลดสินค้า → ตั้งราคา → ตัดสต็อก → บันทึกบิล → อัปเดตยอดสะสมสมาชิก → commit โดยยังอยู่ใน transaction เดียวและยังโยน `InsufficientStockException` ด้วย signature เดิม (research.md #8) — ขึ้นกับ T028

### Web — CRUD ฝั่งหน้าจอ

- [X] T031 [P] สร้าง `web/src/lib/api/conditionalPromotions.ts` — type และฟังก์ชัน `listConditionalPromotions`, `createConditionalPromotion`, `updateConditionalPromotion`, `deleteConditionalPromotion` ตาม DTO ใน `contracts/conditional-promotions.md`
- [X] T032 [P] เพิ่ม `previewSale()` และ type `PricedCart` ใน `web/src/lib/api/sales.ts` ตาม `contracts/sales-preview.md` และเพิ่มฟิลด์ `isGift` กับ `appliedPromotions` ใน type ของ `Sale`
- [X] T033 แก้ `web/src/components/PromotionFormDialog.tsx` ให้สลับระหว่างโปรโมชั่นเปอร์เซ็นต์เดิมกับแบบมีเงื่อนไขได้ พร้อมตัวแก้ไขรายการเงื่อนไข (เพิ่ม/ลบแถว สินค้า + จำนวนขั้นต่ำ) และตัวเลือกสิ่งตอบแทนสองแบบ — ช่องเลือกสินค้าทุกช่องใช้ prop `products` ที่ component นี้รับเข้ามาอยู่แล้ว **ไม่ต้องเพิ่มการโหลดสินค้าเอง** เพราะการไล่อ่านให้ครบทุกหน้าตาม `001/FR-037` เป็นความรับผิดชอบของ `promotions/page.tsx` ที่ทำไว้แล้ว (FR-009) — ขึ้นกับ T031
- [X] T034 แก้ `web/src/app/(protected)/promotions/page.tsx` ให้แสดงโปรโมชั่นทั้งสองชนิดในตารางเดียว โดยแถวของแบบมีเงื่อนไขแสดงฟิลด์ `description` ที่ server ส่งมา (ห้ามประกอบข้อความเอง) และแสดงป้ายเตือนเมื่อ `isUsable = false` พร้อม `unusableReason` (FR-008, FR-023) — ขึ้นกับ T031, T033
- [X] T035 แก้ `web/src/app/(protected)/sales/page.tsx` ให้ถือ state ของผลพรีวิว และเรียก `previewSale()` แบบ debounce ประมาณ 250 มิลลิวินาทีทุกครั้งที่ `cartLines` หรือ `selectedMember` เปลี่ยน แล้วส่ง `PricedCart` ลงไปให้ `<Cart>` เป็น prop — การ fetch ต้องอยู่ที่หน้านี้ไม่ใช่ใน `Cart.tsx` เพราะ state ทั้งสองตัวอยู่ที่นี่ และการเปลี่ยนสมาชิกมีผลต่อโปรโมชั่นเฉพาะสมาชิก (FR-006) — ขึ้นกับ T032
- [X] T036 แก้ `web/src/components/Cart.tsx` ให้รับ prop ผลพรีวิวและใช้ยอดจากผลนั้นแทนการบวกราคาเองที่บรรทัด `const subtotal = lines.reduce(...)` เดิม โดยแสดงยอดเดิมค้างไว้ระหว่างรอผลใหม่ ไม่ใช่แสดงค่าว่าง (FR-013, constitution Principle I) — ขึ้นกับ T035

### Integration test ของแกนกลาง

- [X] T037 [P] เพิ่ม integration test ใน `api/tests/TaladPOS.Api.IntegrationTests/` ครอบคลุม CRUD ของ `/api/v1/conditional-promotions` และยืนยันว่าแคชเชียร์เรียกแล้วได้ `403 Forbidden` ขณะที่ `POST /api/v1/sales/preview` เรียกได้ทั้งแคชเชียร์และผู้จัดการ — ขึ้นกับ T027, T029

**Checkpoint**: ผู้จัดการสร้าง/แก้/ลบโปรโมชั่นแบบมีเงื่อนไขได้ครบ และพรีวิวตะกร้ากับ checkout เรียก `CartPricer`
ตัวเดียวกันแล้ว (การพิสูจน์ว่ายอดทั้งสองทางเท่ากันในกรณีที่มีสิทธิ์เกิดขึ้นจริงอยู่ที่ T061 เพราะตอนนี้ยังไม่มี reward ใดทำงาน)
แต่**ยังไม่มีสิทธิ์ใดถูกใช้** เพราะ branch ของ reward แต่ละแบบอยู่ในเฟสถัดไป

---

## Phase 3: User Story 1 - ซื้อสินค้าครบชุดแล้วได้ของแถม (Priority: P1) 🎯 MVP

**Goal**: ตะกร้าที่มีสินค้าครบตามเงื่อนไขได้รับของแถมโดยแคชเชียร์ไม่ต้องแก้ราคา ของแถมขึ้นใบเสร็จเป็นรายการแยก
ยอดสุทธิ 0 และตัดสต็อกจริง

**Independent Test**: สร้างโปรโมชั่น "ซื้อ A 1 + B 1 แถม C 1" ที่มีผลวันนี้ ทำรายการขายที่มี A 1, B 1 และ C 1
ตรวจว่าใบเสร็จมีรายการ C ยอดสุทธิ 0 บาทและสต็อก C ลดลง 1 ชิ้น — ทำได้โดยไม่ต้องรอ US2 และ US3

### Tests for User Story 1 (เขียนก่อน ต้องเห็น fail)

- [X] T038 [P] [US1] เพิ่มเทสของ acceptance scenario ทั้ง 7 ข้อของ User Story 1 ลงใน `api/tests/TaladPOS.Domain.Tests/Promotions/CartPricerTests.cs` โดยยืนยัน**ตัวเลขจริง** ไม่ใช่แค่ว่ามีส่วนลดเกิดขึ้น — โดยเฉพาะ scenario 3 (A 3, B 2, C 3 → ได้ 2 ชุด, C แถม 2 เหลือ C จ่าย 1), scenario 4 (เฉพาะสมาชิกแต่ไม่ผูกสมาชิก → ไม่มีสิทธิ์), scenario 5 (นอกช่วงวันที่ → ไม่มีสิทธิ์), scenario 6 และ 7 (เข้าเงื่อนไขแต่ไม่มีของแถมในตะกร้า → มี `UnclaimedGift` และยอดไม่เปลี่ยน) และเพิ่มข้อยืนยันตาม FR-013 ว่าสิทธิ์ถูกใช้ทันทีที่เงื่อนไขครบ โดย `PricedCart.AppliedPromotions` มีรายการนั้นทันทีจากอินพุตตะกร้าอย่างเดียว ไม่มีพารามิเตอร์ยืนยันหรือ flag เปิดใช้โปรโมชั่นใดใน signature ของ `CartPricer`
- [X] T039 [P] [US1] เพิ่มเทสใน `api/tests/TaladPOS.Application.Tests/Sales/` ยืนยันว่า `PreviewSaleUseCase` และ `CompleteSaleUseCase` ตัดโปรโมชั่นที่อ้างถึงสินค้าที่ถูกลบออกก่อนตั้งราคา จึงไม่เกิดข้อความเตือนให้หยิบสินค้าที่ไม่มีอยู่แล้ว (FR-023, research.md #7)
- [X] T040 [P] [US1] เพิ่มเทสใน `api/tests/TaladPOS.Application.Tests/Sales/` ยืนยันว่า `CompleteSaleUseCase` ตัดสต็อกของสินค้าที่แถมตามจำนวนที่แถมจริง (FR-017, SC-003) และยอดซื้อสะสมของสมาชิกไม่เพิ่มขึ้นจากมูลค่าของแถม (FR-022)
- [X] T041 [P] [US1] เพิ่มเทสใน `api/tests/TaladPOS.Domain.Tests/Promotions/CartPricerTests.cs` ยืนยันว่า `CartPricer` **ไม่สร้างสินค้าขึ้นมาเอง** ตามประโยคบังคับของ FR-014 ("ระบบต้องไม่เพิ่มหรือลบสินค้าในตะกร้าด้วยตัวเอง") — สำหรับทุก `productId` ผลรวม `Quantity` ของทุกบรรทัดใน `PricedCart.Lines` (นับทั้งบรรทัดที่จ่ายเงินและบรรทัดของแถม) ต้องเท่ากับจำนวนที่ส่งเข้ามาในตะกร้าพอดี และต้องไม่มีบรรทัดของสินค้าที่ไม่ได้อยู่ในตะกร้าเลย แม้ตะกร้านั้นจะเข้าเงื่อนไขโปรโมชั่นที่แถมสินค้าดังกล่าวก็ตาม (กรณีนั้นต้องออกมาเป็น `UnclaimedGift` แทน) — ฝั่งหน้าจอมี `quickstart.md` S1 ข้อ 3 และ 7 ตรวจด้วยมืออยู่แล้ว

### Implementation for User Story 1

- [X] T042 [US1] เพิ่ม branch ของ reward แบบของแถมกรณี**สินค้าที่แถมไม่ได้อยู่ในเงื่อนไข** ลงในขั้นตอนที่ 4 ของ `api/src/TaladPOS.Domain/Promotions/CartPricer.cs` — `giftApplied = min(sets × จำนวนที่แถม, คงเหลือ[สินค้าที่แถม])` แล้วหักออกจากคงเหลือ; ส่วนที่ขาดบันทึกเป็น `UnclaimedGift` พร้อม `MissingQuantity` — **จำนวนชุดต้องไม่ถูกจำกัดด้วยการมีของแถมในตะกร้าหรือไม่** (FR-015, research.md #10 ข้อ 4)
- [X] T043 [US1] เพิ่มการสร้างบรรทัดของแถมในขั้นตอนที่ 6 ของ `api/src/TaladPOS.Domain/Promotions/CartPricer.cs` — `Quantity` = จำนวนที่แถม, `DiscountAmount` = `ราคา × จำนวนที่แถม`, `IsGift = true`, และบรรทัดที่จ่ายเงินมี `Quantity` = จำนวนในตะกร้า − จำนวนที่แถม โดยไม่สร้างบรรทัดที่จ่ายเงินเมื่อจำนวนเป็น 0 (FR-016, SC-002) — ขึ้นกับ T042
- [X] T044 [US1] แก้ `api/src/TaladPOS.Application/Sales/CompleteSaleUseCase.cs` ให้แปลง `PricedCart` เป็น `SaleLineItem` (ส่ง `isGift` ตามผลลัพธ์) และ `SaleAppliedPromotion` พร้อมตัดสต็อกครบทุกบรรทัดรวมของแถม (FR-017, FR-024) — ขึ้นกับ T043, T030
- [X] T045 [US1] เพิ่มฟิลด์ `isGift` ใน `SaleLineItemDto` และฟิลด์ `appliedPromotions` ใน `SaleDto` ที่ `api/src/TaladPOS.Api/Controllers/SalesController.cs` ให้คืนครบทั้ง `POST /api/v1/sales`, `GET /api/v1/sales/{id}` และ `GET /api/v1/sales/{id}/receipt` โดยบิลเก่าคืน `isGift = false` และ `appliedPromotions` เป็นอาร์เรย์ว่าง (FR-016, FR-024) — ขึ้นกับ T044
- [X] T046 [US1] แก้ `web/src/components/Cart.tsx` ให้แสดงบรรทัดของแถมพร้อมป้ายกำกับและยอดสุทธิ 0 บาท และแสดงข้อความเตือนจาก `unclaimedGifts` ว่าบิลนี้มีสิทธิ์แถมสินค้าใดที่ยังไม่ได้ใช้และขาดอีกกี่ชิ้น โดยไม่ขัดขวางปุ่มชำระเงิน (FR-014, FR-015) — ขึ้นกับ T035, T045
- [X] T047 [US1] แก้ `web/src/components/Receipt.tsx` ให้แสดงบรรทัดของแถมด้วยราคาปกติพร้อมส่วนลดเท่ากับราคานั้น (ไม่ใช่แสดง 0 บาทเป็นราคาต่อชิ้น) และแสดงรายการชื่อโปรโมชั่นที่ถูกใช้จาก `appliedPromotions` เพื่อให้อ่านใบเสร็จแล้วรู้ที่มาของส่วนลดโดยไม่ต้องเปิดหน้าจัดการโปรโมชั่น (FR-016, FR-024, SC-008) — ขึ้นกับ T045
- [X] T048 [P] [US1] เพิ่มโปรโมชั่นตัวอย่างแบบ "ซื้อ A 1 + B 1 แถม C 1" ลงใน `api/tools/TaladPOS.TestData/TestDataSpec.cs` และ seeding logic ใน `api/tools/TaladPOS.TestData/TestDataBuilder.cs` เพื่อให้ `scripts/reset-test-data.ps1` สร้างข้อมูลพร้อมทดสอบ (research.md #12)

**Checkpoint**: User Story 1 ทำงานครบและทดสอบแยกได้ — S1 ใน `quickstart.md` ต้องผ่านทั้งหมด

---

## Phase 4: User Story 2 - ซื้อสินค้าเดิมครบจำนวนแล้วได้เพิ่มฟรี (Priority: P2)

**Goal**: โปรโมชั่นแบบ "ซื้อ y แถม x" ที่สินค้าที่แถมเป็นรายการเดียวกับเงื่อนไข คิดเงินเฉพาะจำนวนที่ต้องจ่าย
และแยกบรรทัดของแถมบนใบเสร็จแม้เป็นสินค้าเดียวกัน

**Independent Test**: สร้าง "ซื้อ A 2 แถม A 1" แล้วสแกน A 3 ชิ้น ตรวจว่าลูกค้าจ่ายเท่ากับ A 2 ชิ้น
และสต็อก A ลดลง 3 ชิ้น — ทดสอบได้โดยไม่ต้องมี US1 หรือ US3 ทำงานอยู่

### Tests for User Story 2 (เขียนก่อน ต้องเห็น fail)

- [X] T049 [P] [US2] เพิ่มเทสของ acceptance scenario ทั้ง 5 ข้อของ User Story 2 ลงใน `api/tests/TaladPOS.Domain.Tests/Promotions/CartPricerTests.cs` ด้วยตัวเลขจริงตามตารางตรวจใน `research.md` #10: A 3 → จ่าย 200, A 5 → จ่าย 400 (1 ชุด เศษ 2), A 6 → จ่าย 400 (2 ชุด), A 2 → จ่าย 200 (ไม่มีสิทธิ์), และ A 5 ที่มีโปรโมชั่นลดรายชิ้น 10% ร่วมด้วย → จ่าย 380
- [X] T050 [P] [US2] เพิ่มเทสใน `api/tests/TaladPOS.Domain.Tests/Promotions/CartPricerTests.cs` ยืนยันว่ากรณีสินค้าที่แถมเป็นรายการเดียวกับเงื่อนไข ผลลัพธ์ประกอบด้วย **สองบรรทัด** สำหรับสินค้าเดียวกัน คือบรรทัดที่จ่ายเงินและบรรทัดของแถม (FR-016)

### Implementation for User Story 2

- [X] T051 [US2] เพิ่มการปรับจำนวนที่ใช้ต่อชุดตาม FR-011 ลงในขั้นตอนที่ 4 ของ `api/src/TaladPOS.Domain/Promotions/CartPricer.cs` — สำหรับรายการเงื่อนไขที่เป็นสินค้าเดียวกับของแถม จำนวนที่ใช้ต่อชุด = `จำนวนขั้นต่ำ + จำนวนที่แถม` แทนที่จะเป็นจำนวนขั้นต่ำอย่างเดียว จากนั้น `sets` ยังคำนวณด้วยกติกา FR-010 เดิม (ค่าน้อยที่สุดข้ามทุกรายการ) ไม่ใช่สูตรแยก — ขึ้นกับ T012
- [X] T052 [US2] เพิ่มการกำหนดจำนวนของแถมกรณีสินค้าที่แถมอยู่ในเงื่อนไขด้วย ลงใน `api/src/TaladPOS.Domain/Promotions/CartPricer.cs` — จำนวนที่แถม = `sets × จำนวนที่แถม` ซึ่งถูกหักออกจากคงเหลือไปแล้วในขั้นตอนหักคงเหลือ **ห้ามหักซ้ำ** และกรณีนี้ต้องไม่เกิด `UnclaimedGift` — ขึ้นกับ T051, T042
- [X] T053 [P] [US2] เพิ่มโปรโมชั่นตัวอย่างแบบ "ซื้อ A 2 แถม A 1" ลงใน `api/tools/TaladPOS.TestData/TestDataSpec.cs` และ seeding logic ที่ `api/tools/TaladPOS.TestData/TestDataBuilder.cs`

**Checkpoint**: User Story 1 และ 2 ทำงานได้อิสระทั้งคู่ — S2 ใน `quickstart.md` ต้องผ่าน

---

## Phase 5: User Story 3 - ซื้อสินค้าครบชุดแล้วได้ส่วนลดเป็นเปอร์เซ็นต์ (Priority: P3)

**Goal**: โปรโมชั่นชุดที่ให้ส่วนลดเปอร์เซ็นต์แทนของแถม ลดเฉพาะสินค้าในชุดตามจำนวนที่ครบชุด
ไม่กระทบสินค้ารายการอื่นในบิลและยังทับซ้อนกับส่วนลดระดับบิลได้ตามเดิม

**Independent Test**: สร้าง "ซื้อ A 1 + B 1 ลด 15%" แล้วขาย A 1 + B 1 ตรวจว่าส่วนลดเท่ากับ 15% ของ
(ราคา A + ราคา B) พอดี — ไม่ต้องพึ่งกลไกของแถมของ US1/US2 เลย

### Tests for User Story 3 (เขียนก่อน ต้องเห็น fail)

- [X] T054 [P] [US3] เพิ่มเทสของ acceptance scenario ทั้ง 4 ข้อของ User Story 3 ลงใน `api/tests/TaladPOS.Domain.Tests/Promotions/CartPricerTests.cs` — A 100 บาท, B 200 บาท, D 50 บาท: A1+B1+D1 → ส่วนลด 45 และ D เต็ม 50; A2+B1 → ได้ 1 ชุด ส่วนลด 45; A2+B2 → 2 ชุด ส่วนลด 90; A1+B1 พร้อมส่วนลดทั้งบิล 5% → รวมจ่าย 240 (ฐานบิลคือ 300 ไม่ใช่ 255)

### Implementation for User Story 3

- [X] T055 [US3] เพิ่ม branch ของ reward แบบเปอร์เซ็นต์ลงในขั้นตอนที่ 4 ของ `api/src/TaladPOS.Domain/Promotions/CartPricer.cs` — ส่วนลดของสินค้าแต่ละรายการในเงื่อนไข = `round(เปอร์เซ็นต์ ÷ 100 × จำนวนขั้นต่ำ × sets × ราคา, 2)` คิดเฉพาะจำนวนที่ครบชุด ไม่รวมชิ้นที่เกิน (FR-020) — ขึ้นกับ T012
- [X] T056 [US3] ยืนยันว่าส่วนลดชุดแบบเปอร์เซ็นต์ถูกนำไปรวมกับส่วนลดรายชิ้นของหน่วยที่เหลือในบรรทัดเดียวกันอย่างถูกต้องที่ขั้นตอนที่ 6 ของ `api/src/TaladPOS.Domain/Promotions/CartPricer.cs` และบรรทัดนั้นยังนับเข้าฐานส่วนลดระดับบิลที่ราคาเต็มตามขั้นตอนที่ 7 (FR-021, research.md #5) — ขึ้นกับ T055, T014
- [X] T057 [P] [US3] เพิ่มโปรโมชั่นตัวอย่างแบบ "ซื้อ A 1 + B 1 ลด 15%" ลงใน `api/tools/TaladPOS.TestData/TestDataSpec.cs` และ seeding logic ที่ `api/tools/TaladPOS.TestData/TestDataBuilder.cs`

**Checkpoint**: ทั้งสาม user story ทำงานได้อิสระ — S1, S2, S3 ใน `quickstart.md` ผ่านครบ

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: edge case กติกาการทับซ้อน การพิสูจน์ว่าของเดิมไม่พัง และการตรวจตาม constitution

- [X] T058 [P] เพิ่มเทส edge case ทั้ง 10 ข้อจาก spec.md ลงใน `api/tests/TaladPOS.Domain.Tests/Promotions/CartPricerTests.cs` โดยข้อที่สำคัญที่สุดคือ **"ซื้อ A 1 + B 1 แถม A 1"** (ของแถมเป็นสินค้าในเงื่อนไขและเงื่อนไขมีหลายรายการ): ตะกร้า A 3, B 1 → จำนวนที่ใช้ต่อชุดของ A = 2, ของ B = 1, `sets = min(floor(3/2), 1) = 1`, A จ่าย 2 แถม 1, B จ่าย 1
- [X] T059 [P] เพิ่มเทสลำดับการจัดสรรใน `api/tests/TaladPOS.Domain.Tests/Promotions/CartPricerTests.cs` — โปรโมชั่นสองตัวที่แย่งสินค้าชิ้นเดียวกันต้องถูกจัดสรรตามลำดับสามชั้น (มูลค่าต่อชุด → `StartDate` → `Id`) และโปรโมชั่นที่มูลค่าต่อชุดและวันเริ่มเท่ากันต้องยังให้ผลเดิมทุกครั้ง (FR-012, FR-019, SC-006)
- [X] T060 [P] เพิ่มเทสส่วนลด 100% ใน `api/tests/TaladPOS.Domain.Tests/Promotions/CartPricerTests.cs` ยืนยันว่าบรรทัดที่ได้ส่วนลดชุด 100% มียอดสุทธิ 0 แต่ `IsGift` ยังเป็น `false` ซึ่งต่างจากบรรทัดของแถม (research.md #4)
- [X] T061 [P] เพิ่ม integration test ใน `api/tests/TaladPOS.Api.IntegrationTests/` ยืนยันว่า `POST /api/v1/sales/preview` กับ `POST /api/v1/sales` ที่ส่ง body เดียวกันให้ `totalAmount` เท่ากัน และการสลับลำดับ `lineItems` ไม่ทำให้ยอดเปลี่ยน (FR-012, SC-006)
- [X] T062 [P] เพิ่ม integration test ใน `api/tests/TaladPOS.Api.IntegrationTests/` ยืนยันว่าบิลที่บันทึกก่อนฟีเจอร์นี้ยังอ่านได้ปกติผ่าน `GET /api/v1/sales/{id}` และ `GET /api/v1/sales/{id}/receipt` โดยทุกบรรทัดคืน `isGift = false` และ `appliedPromotions` เป็นอาร์เรย์ว่าง
- [X] T063 ตรวจ regression ของโปรโมชั่นเดิมโดยรัน checkout ที่ไม่เข้าเงื่อนไขโปรโมชั่นแบบมีเงื่อนไขใดเลย แล้วเทียบยอดกับ `specs/003-bundle-promotions/baseline-totals.json` จาก T002 ให้ตรงทุกบิล (FR-026, SC-007) — ขึ้นกับ T002, T030
- [X] T064 ตรวจด้วย `git diff` ว่าไฟล์เหล่านี้ **ไม่ถูกแก้แม้แต่บรรทัดเดียว**: `api/src/TaladPOS.Domain/Promotions/Promotion.cs`, `api/src/TaladPOS.Domain/Promotions/DiscountResolver.cs`, `api/src/TaladPOS.Infrastructure/Configurations/PromotionConfiguration.cs`, `api/src/TaladPOS.Infrastructure/Repositories/PromotionRepository.cs`, `api/src/TaladPOS.Api/Controllers/PromotionsController.cs` (FR-026)
- [X] T065 รัน `grep -rnE '\* *0\.|/ *100|discountPercentage|Math\.floor' web/src` แล้วตรวจว่าผลลัพธ์มีเฉพาะ type definition ใน `web/src/lib/api/*.ts` เท่านั้น — ถ้ามี hit ใน `web/src/components/` หรือ `web/src/app/` แปลว่ามีการคำนวณส่วนลดหรือการนับชุดหลุดเข้าไปฝั่ง frontend ถือว่าไม่ผ่าน เพราะตัวเลขทุกตัวต้องมาจาก API (constitution Principle I) — ขึ้นกับ T035, T036, T046, T047
- [X] T066 [P] ตรวจหน้าจอทั้งสามที่กระทบ (โปรโมชั่น, ขาย, ใบเสร็จ) ด้วย Chrome DevTools device toolbar ที่ความกว้าง 360 / 768 / 1024 / 1440 px — เกณฑ์ผ่านคือ `document.documentElement.scrollWidth <= window.innerWidth` ทุกความกว้าง (ไม่มีการเลื่อนแนวนอนของทั้งหน้า) และปุ่ม/ช่องกรอกที่เพิ่มใหม่ทุกตัวมีความสูงจริงไม่น้อยกว่า 44 px เมื่อจอกว้างไม่เกิน 767 px วัดจาก `getBoundingClientRect().height` — เก็บ screenshot ความกว้างละ 1 รูปต่อหน้าจอไว้แนบใน PR เป็นหลักฐาน (`001/FR-031`, `001/FR-033`)
- [X] T067 [P] เพิ่มเอกสารอธิบายโปรโมชั่นแบบมีเงื่อนไขและวิธีรีเซ็ตข้อมูลทดสอบชุดใหม่ลงใน `docs/reset-test-data.md` และอ้างอิง contract ใหม่จาก `api/README.md`
- [X] T068 [P] ตรวจว่ารายงานยอดขายเดิมรวมส่วนลดจากโปรโมชั่นแบบมีเงื่อนไขไว้แล้วโดยไม่ต้องเพิ่มรายงานประเภทใหม่ — ขายบิลที่ได้ทั้งของแถมและส่วนลดชุด แล้วเทียบยอดส่วนลดใน `GET /api/v1/reports/*` กับผลรวม `DiscountAmount` ของบิลนั้น ถ้าไม่ตรงคือ `Sale.DiscountAmount` ถูกแก้สูตรไปโดยไม่ตั้งใจ (FR-025) — ขึ้นกับ T044
- [X] T069 สร้างบิลทดสอบอย่างน้อย 100 บิลด้วยโปรโมชั่นที่ซ้อนทับกัน แล้วตรวจว่าไม่มีบิลใดที่สินค้าชิ้นเดียวกันถูกนับเข้าโปรโมชั่นแบบมีเงื่อนไขมากกว่าหนึ่งรายการ โดยเทียบผลรวม `SetCount × จำนวนที่ใช้ต่อชุด` ของทุก `SaleAppliedPromotion` กับจำนวนสินค้าจริงในบิล (FR-018, SC-004) — ขึ้นกับ T048, T053, T057
- [X] T070 ตรวจว่า migration ย้อนกลับได้สะอาดด้วย `dotnet ef migrations remove` แล้วสคีมากลับสู่สถานะก่อนหน้าโดยตาราง `promotions` ไม่ถูกแตะ — ขึ้นกับ T022
- [X] T071 [P] เพิ่มเทสใน `api/tests/TaladPOS.Application.Tests/Reports/` ยืนยันความหมายของตัวเลขในรายงานเดิมเมื่อบิลมีของแถมตาม FR-027 — ขายบิลที่มีของแถม 1 ชิ้นราคา 100 บาท แล้วตรวจว่า (ก) `QuantitySold` ของรายงานสินค้าขายดีเท่ากับ จำนวนที่จ่ายเงิน + จำนวนที่แถม (ข) `TotalDiscountAmount` ของรายงานยอดขายเพิ่มขึ้น 100 บาท (ค) `TotalSalesAmount` ไม่เพิ่มขึ้นจากของแถม — **ไม่ต้องแก้โค้ดรายงานเดิม** งานนี้คือการล็อกพฤติกรรมที่เป็นอยู่ให้มีเทสกำกับ ถ้าเทสไม่ผ่านแปลว่ามีคนไปแก้สูตรรวมยอดเข้า — ขึ้นกับ T015, T016, T017
- [X] T072 รัน `quickstart.md` ทั้งไฟล์ตั้งแต่ข้อ 1 ถึงเช็กลิสต์สุดท้าย ให้ S1–S8 ผ่านครบทุกข้อ และจับเวลาการสร้างโปรโมชั่นทั้งสามรูปแบบว่าไม่เกินรูปแบบละ 2 นาที พร้อมยืนยันว่าแคชเชียร์ปิดการขายได้โดยไม่ต้องแก้ราคาด้วยมือเลย (SC-001, SC-005) — ขึ้นกับทุกงานข้างบน

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: เริ่มได้ทันที
- **Foundational (Phase 2)**: ต้องรอ Setup — **บล็อกทุก user story**
- **User Stories (Phase 3–5)**: ทุกเรื่องต้องรอ Phase 2 เสร็จ จากนั้นทำขนานกันได้ หรือทำเรียงตาม P1 → P2 → P3
- **Polish (Phase 6)**: ต้องรอ user story ที่ต้องการส่งมอบเสร็จก่อน

### User Story Dependencies

- **US1 (P1)**: เริ่มได้หลัง Phase 2 ไม่พึ่ง story อื่น
- **US2 (P2)**: เริ่มได้หลัง Phase 2 — T052 แตะโค้ดก้อนเดียวกับ T042 ของ US1 จึงควรทำหลัง US1 ถ้าอยู่คนเดียว
  แต่ตัว story ยังทดสอบแยกได้ด้วยโปรโมชั่นของตัวเอง
- **US3 (P3)**: เริ่มได้หลัง Phase 2 — **ไม่แตะโค้ดของแถมเลย** จึงทำขนานกับ US1/US2 ได้สนิทที่สุด

### ลำดับภายในแต่ละ story

- เทสเขียนก่อนและต้องเห็น fail → domain → application → API → web
- งานที่แตะไฟล์เดียวกัน (`CartPricer.cs`) ห้ามทำขนาน แม้อยู่คนละ story

### Parallel Opportunities

- Phase 2: T003/T004 ขนานกันได้, T005/T006 ขนานกันได้, T015/T016 ขนานกันได้,
  T018/T019 ขนานกันได้, T023 กับ T025 ขนานกันได้, T031/T032 ขนานกันได้
- Phase 3–5: งานเทสที่ขึ้นต้นด้วย [P] ของแต่ละ story เขียนขนานกันได้ และงาน TestData
  (T048, T053, T057) ขนานกับงาน domain ของ story ตัวเองได้
- Phase 6: T058–T062 และ T066/T067/T068/T071 ขนานกันได้ทั้งหมด
- ทีมหลายคน: หลัง Phase 2 เสร็จ ให้คนหนึ่งทำ US1 อีกคนทำ US3 ได้ทันทีเพราะไม่แตะ branch เดียวกัน
  ส่วน US2 ควรรอ US1 เพราะแก้ฟังก์ชันเดียวกัน

---

## Parallel Example: Phase 2 Foundational

```bash
# เขียนเทสสองไฟล์พร้อมกัน
Task: "เทส validation ของ aggregate ใน api/tests/TaladPOS.Domain.Tests/Promotions/ConditionalPromotionValidationTests.cs"
Task: "เทสการนับชุดและ determinism ใน api/tests/TaladPOS.Domain.Tests/Promotions/CartPricerTests.cs"

# สร้าง value object และ owned entity พร้อมกัน
Task: "Reward + RewardKind ใน api/src/TaladPOS.Domain/Promotions/Reward.cs"
Task: "ConditionLine ใน api/src/TaladPOS.Domain/Promotions/ConditionLine.cs"

# EF configuration สองไฟล์พร้อมกัน
Task: "ConditionalPromotionConfiguration ใน api/src/TaladPOS.Infrastructure/Configurations/ConditionalPromotionConfiguration.cs"
Task: "เพิ่ม is_gift ใน api/src/TaladPOS.Infrastructure/Configurations/SaleLineItemConfiguration.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 เท่านั้น)

1. ทำ Phase 1 ให้เสร็จ
2. ทำ Phase 2 ให้เสร็จ (**สำคัญที่สุด — บล็อกทุกอย่าง**)
3. ทำ Phase 3 (US1)
4. **หยุดและตรวจ**: รัน S1 ใน `quickstart.md` ให้ผ่าน
5. ส่งมอบ/สาธิตได้

MVP นี้ให้คุณค่าครบในตัวเอง — ร้านจัดโปรโมชั่น "ซื้อคู่แถม" ได้จริงโดยยังไม่ต้องมีอีกสองรูปแบบ

### Incremental Delivery

1. Setup + Foundational → แกนกลางพร้อม (ผู้จัดการตั้งโปรโมชั่นได้ พรีวิวทำงาน แต่ยังไม่ให้สิทธิ์)
2. + US1 → ทดสอบแยก → ส่งมอบ (MVP)
3. + US2 → ทดสอบแยก → ส่งมอบ
4. + US3 → ทดสอบแยก → ส่งมอบ
5. ปิดท้ายด้วย Phase 6 ซึ่งมีงานพิสูจน์ว่าของเดิมไม่พัง (T063, T064) ที่ควรรันซ้ำทุกครั้งที่ส่งมอบ

### Parallel Team Strategy

1. ทีมทำ Setup + Foundational ร่วมกันก่อน
2. หลัง Phase 2 เสร็จ:
   - คนที่ 1: US1 (ของแถม)
   - คนที่ 2: US3 (ส่วนลดชุด) — ไม่ชนกับคนที่ 1 เพราะคนละ branch ใน `CartPricer`
   - คนที่ 3: งาน Phase 6 ที่เป็น [P] และงานเอกสาร
3. US2 เข้ามาหลัง US1 เสร็จ เพราะแก้ฟังก์ชันเดียวกัน

---

## สถานะการทำจริง (2026-09-16)

ทำเสร็จครบ **72 จาก 72 งาน**

**เรื่องพอร์ตฐานข้อมูล**: พอร์ต 5432 ถูกคอนเทนเนอร์ `n8n-simple-postgres-1` ของอีกโปรเจกต์ยึดอยู่
`docker-compose.yml` ของ TaladPOS ผูก `5432:5432` จึงสตาร์ตไม่ได้ อาการที่เห็นคือ
`password authentication failed for user "taladpos"` ซึ่งเป็นพอร์ตชนไม่ใช่รหัสผ่านผิด
**ไม่ได้หยุดคอนเทนเนอร์ของโปรเจกต์อื่น** แต่เปิดคอนเทนเนอร์ postgres ชั่วคราวของ TaladPOS เองบนพอร์ต 5435
แล้วชี้ connection string ผ่าน environment variable โดยไม่แก้ไฟล์ใน repo — งานที่ต้องใช้ฐานข้อมูลจึงทำได้ครบ

ถ้าจะรันเองให้ทำแบบเดียวกัน:

```bash
docker run -d --name taladpos-tmp-5435 -e POSTGRES_DB=taladpos -e POSTGRES_USER=taladpos   -e POSTGRES_PASSWORD=taladpos_dev_password -p 5435:5432 postgres:16

export ConnectionStrings__TaladPOSDb="Host=localhost;Port=5435;Database=taladpos;Username=taladpos;Password=taladpos_dev_password"
export TEST_DB_CONNECTION="Host=localhost;Port=5435;Database=taladpos_test;Username=taladpos;Password=taladpos_dev_password"
```

**ผลการตรวจ**: build ทั้ง solution ผ่าน · เทส 173 ตัวผ่านหมด (Domain 106 / Application 49 / Integration 18)
· `npm run build` ฝั่ง web ผ่าน · migration ใช้กับฐานข้อมูลเปล่าได้และย้อนกลับได้
· quickstart S1–S8 ตรวจผ่าน API จริงแล้วทุกข้อ

**T066 (responsive)**: ตรวจบนเบราว์เซอร์จริงที่ 360 / 768 / 1024 / 1440 px ทั้งหน้าโปรโมชั่น หน้าขาย ใบเสร็จ
และ dialog โปรโมชั่นแบบมีเงื่อนไข — ไม่มีการเลื่อนแนวนอนของทั้งหน้าในทุกกรณี
(หน้าต่าง Chrome ถูก maximize อยู่จึง `resizeTo` ไม่ได้ ใช้ iframe กำหนดความกว้างแทน ซึ่ง media query ทำงานตามจริง)

พบและแก้ไป 1 จุด: ปุ่ม "+ เพิ่มสินค้าในเงื่อนไข" สูงเพียง 20 px ที่จอ 360 px เพราะเป็น text button
ที่ไม่มีคลาส `.btn` กฎ 44 px ใน `globals.css` จึงไม่ครอบถึง แก้ด้วย `min-h-[44px] md:min-h-0` แล้ววัดใหม่ได้ 44 px

หมายเหตุที่ไม่ได้แก้เพราะเป็นของเดิม: checkbox สูง 20 px (แต่ `<label>` ที่ห่อสูง 44 px ซึ่งเป็นเป้าแตะจริง
ตามที่ `globals.css` ตั้งใจไว้) และปุ่มท้าย dialog ของ `promotion-form` เดิมสูง 42 px ส่วนของ
`conditional-promotion-form` ได้ 44 px

## Notes

- [P] = คนละไฟล์ ไม่มี dependency ค้าง
- `CartPricer.cs` เป็นไฟล์ที่หลาย story แตะร่วมกัน — **ห้ามทำขนานข้าม story บนไฟล์นี้**
- เทสต้องเห็น fail ก่อนเริ่ม implementation (constitution Principle III)
- commit หลังจบแต่ละงานหรือแต่ละกลุ่มที่สมเหตุสมผล
- หยุดที่ checkpoint ใดก็ได้เพื่อตรวจว่า story นั้นทำงานแยกได้จริง
- ห้ามแตะ `Promotion.cs`, `DiscountResolver.cs` และตาราง `promotions` ตลอดทั้งฟีเจอร์ (FR-026)
