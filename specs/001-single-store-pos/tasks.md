---

description: "Task list template for feature implementation"
---

# Tasks: ระบบ POS สำหรับร้านค้าเดี่ยว (Single-Store POS)

**Input**: Design documents from `/specs/001-single-store-pos/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/](./contracts/), [quickstart.md](./quickstart.md)

**Tests**: รวม task ทดสอบไว้ **เฉพาะ unit test ของ business logic** เพราะ constitution Principle III
("Test-First for Business Logic") ระบุไว้เป็น **NON-NEGOTIABLE** — ทุก business rule ต้องมี unit test ก่อน merge
ไม่ใช่ทางเลือก ส่วน contract/integration test อื่นถูกจำกัดไว้เฉพาะจุดที่สเปกระบุชัดว่าต้องพิสูจน์ได้ (การปฏิเสธ
ผู้ใช้ที่ไม่ล็อกอิน, การป้องกัน stock ติดลบเมื่อขายพร้อมกันหลายจุดขาย) ไม่ได้เพิ่ม contract test ให้ครบทุก endpoint
โดยไม่จำเป็น

**Organization**: จัดกลุ่ม task ตาม User Story (P1–P6) จาก spec.md เพื่อให้ implement/ทดสอบแต่ละเรื่องแยกจากกันได้

## Format: `[ID] [P?] [Story] Description`

- **[P]**: รันขนานได้ (คนละไฟล์ ไม่มี dependency ค้าง)
- **[Story]**: user story ที่ task นี้สังกัด (US1–US6)
- ทุก task ระบุ path ไฟล์ตรง ๆ ตาม Project Structure ใน plan.md

## Path Conventions (จาก plan.md)

- Backend: `api/src/TaladPOS.{Domain,Application,Infrastructure,Api}/...`, tests ที่ `api/tests/TaladPOS.{Domain,Application,Api.Integration}Tests/...`
- Frontend: `web/src/{app,components,lib}/...`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: เตรียมโครงสร้างโปรเจกต์ `api/` และ `web/` ตาม constitution Repository Structure

- [ ] T001 สร้าง solution + 4 projects (`TaladPOS.Domain`, `TaladPOS.Application`, `TaladPOS.Infrastructure`, `TaladPOS.Api`) ใน `api/src/` ตาม Project Structure ใน plan.md
- [ ] T002 สร้าง 3 test projects (`TaladPOS.Domain.Tests`, `TaladPOS.Application.Tests`, `TaladPOS.Api.IntegrationTests`) พร้อม xUnit + FluentAssertions ใน `api/tests/`
- [ ] T003 [P] สร้างโปรเจกต์ Next.js 14 (App Router) + TypeScript + Tailwind CSS 3 ใน `web/`
- [ ] T004 [P] ติดตั้ง PrimeReact และตั้งค่าโหมด unstyled + Tailwind passthrough preset ตาม research.md #7 ใน `web/src/styles/`
- [ ] T005 [P] ตั้งค่า lint/format: `.editorconfig`/`dotnet format` สำหรับ `api/`, ESLint+Prettier สำหรับ `web/`
- [ ] T006 ตั้งค่าการเชื่อมต่อ PostgreSQL 16 (connection string ใน `appsettings.Development.json`) และ `docker-compose.yml` สำหรับรัน PostgreSQL ในเครื่อง ใน `api/`
- [ ] T007 [P] สร้างไฟล์ `.env.local.example` กำหนด `NEXT_PUBLIC_API_BASE_URL` ใน `web/`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: โครงสร้างพื้นฐานที่ทุก User Story ต้องพึ่งพา (auth, entities หลัก, DB context) — ทุก endpoint ตาม
`contracts/*.md` ต้องผ่าน JWT auth ก่อน จึงต้องทำให้เสร็จก่อนเริ่ม story ใด ๆ

**⚠️ CRITICAL**: ห้ามเริ่มงาน User Story ใดจนกว่า phase นี้จะเสร็จ

- [ ] T008 สร้าง `TaladPOSDbContext` (EF Core) และโครง migration เริ่มต้นใน `api/src/TaladPOS.Infrastructure/`
- [ ] T009 [P] Implement `Staff` domain entity (Name, Username unique, PasswordHash, Role enum `Manager`/`Cashier`) ตาม data-model.md ใน `api/src/TaladPOS.Domain/Staff/Staff.cs`
- [ ] T010 [P] Implement `Product` domain entity (Name required, ImageUrl required, Price > 0, Barcode optional-unique, StockQuantity int >= 0, LowStockThreshold int >= 0, computed `IsLowStock`/`IsOutOfStock`) ตาม data-model.md ใน `api/src/TaladPOS.Domain/Products/Product.cs`
- [ ] T011 Implement credential verification ด้วย `PasswordHasher<Staff>` (research.md #1) ใน `api/src/TaladPOS.Application/Auth/StaffAuthenticator.cs`
- [ ] T012 Implement JWT issuing service (claims: `staffId`, `role`, อายุจำกัดตาม `expiresAt`) ตาม contracts/auth.md ใน `api/src/TaladPOS.Application/Auth/JwtTokenService.cs` (depends on T009)
- [ ] T013 Implement `POST /api/auth/login` ตาม contracts/auth.md (คืน 401 `invalid_credentials` เมื่อ username/password ผิด) ใน `api/src/TaladPOS.Api/Controllers/AuthController.cs` (depends on T011, T012)
- [ ] T014 ตั้งค่า JWT bearer authentication middleware + authorization policy แยก `Manager`/`Cashier` (FR-029) ใน `api/src/TaladPOS.Api/Program.cs` (depends on T012)
- [ ] T015 [P] Implement EF Core entity configuration + migration สำหรับ `Staff` และ `Product` (unique index บน `Username` และ `Barcode`) ใน `api/src/TaladPOS.Infrastructure/Configurations/` (depends on T008, T009, T010)
- [ ] T016 [P] สร้าง global error-handling middleware (validation → 400, not found → 404, conflict → 409) ใน `api/src/TaladPOS.Api/Middleware/ErrorHandlingMiddleware.cs`
- [ ] T017 สร้าง seed script (1 บัญชี Manager, 1 บัญชี Cashier, สินค้าตัวอย่าง 2 รายการ ตาม quickstart.md ข้อ 3) ใน `api/src/TaladPOS.Infrastructure/Seed/DevelopmentSeeder.cs` (depends on T015)
- [ ] T018 [P] สร้าง REST API client base (fetch wrapper แนบ `Authorization: Bearer` header อัตโนมัติ, อ่าน base URL จาก `NEXT_PUBLIC_API_BASE_URL`) ใน `web/src/lib/api/client.ts`
- [ ] T019 [P] ตั้งค่า `PrimeReactProvider` + root layout (โหลด Tailwind globals) ใน `web/src/app/layout.tsx`

**Checkpoint**: Auth ใช้งานได้จริง, entity หลักพร้อม, DB migration พร้อม — เริ่มงาน User Story ได้

---

## Phase 3: User Story 1 - พนักงานทำการขายสินค้าให้ลูกค้า (Priority: P1) 🎯 MVP

**Goal**: พนักงานค้นหาสินค้า (ชื่อ/บาร์โค้ด) เพิ่ม/ลด/ลบในตะกร้า แล้วชำระเงินปิดบิล ระบบตัดสต็อกและบันทึกบิลอัตโนมัติ

**Independent Test**: seed สินค้าไว้ล่วงหน้า → ค้นหา → เพิ่มลงตะกร้า → ปรับจำนวน → ชำระเงิน → ตรวจสอบสต็อกลดลงตรงและมี
บิลถูกบันทึก (ไม่ต้องพึ่งสมาชิกหรือโปรโมชั่น)

### Tests for User Story 1 (business logic — NON-NEGOTIABLE ตาม constitution Principle III)

- [ ] T020 [P] [US1] Unit test: การตัดสต็อกต้องถูกปฏิเสธเมื่อจำนวนที่ขอเกินกว่า `StockQuantity` ที่มีอยู่ ใน `api/tests/TaladPOS.Domain.Tests/Products/ProductStockTests.cs`
- [ ] T021 [P] [US1] Unit test: `Sale` ต้องมีอย่างน้อย 1 `SaleLineItem` เสมอ (ปฏิเสธตะกร้าว่าง ตาม Edge Case ในสเปก) ใน `api/tests/TaladPOS.Domain.Tests/Sales/SaleTests.cs`
- [ ] T022 [P] [US1] Unit test: `SaleLineItem` เก็บ `ProductNameSnapshot`/`UnitPriceSnapshot` แยกจากค่าปัจจุบันของ `Product` (คงอยู่แม้สินค้าถูกลบ/แก้ไขภายหลัง) ใน `api/tests/TaladPOS.Domain.Tests/Sales/SaleLineItemTests.cs`

### Implementation for User Story 1

- [ ] T023 [P] [US1] Implement `Sale` และ `SaleLineItem` domain entities (append-only ตาม data-model.md) ใน `api/src/TaladPOS.Domain/Sales/` (depends on T020-T022)
- [ ] T024 [US1] Implement `CompleteSaleUseCase`: ตรวจตะกร้าไม่ว่าง, ตัดสต็อกแบบ atomic conditional update (`UPDATE ... WHERE stock_quantity >= @qty`, research.md #2), ตั้ง `DiscountAmount = 0` ชั่วคราว (ยังไม่มีโปรโมชั่น/สมาชิกใน US1), บันทึก `Sale` ทั้งหมดใน transaction เดียว ใน `api/src/TaladPOS.Application/Sales/CompleteSaleUseCase.cs` (depends on T023)
- [ ] T025 [US1] Implement repository + EF Core configuration สำหรับ `Sale`/`SaleLineItem` และ migration ใน `api/src/TaladPOS.Infrastructure/Repositories/SaleRepository.cs` (depends on T010, T023)
- [ ] T026 [US1] Implement `GET /api/products` (query `search`, `barcode`, `lowStockOnly`) ตาม contracts/products.md ใน `api/src/TaladPOS.Api/Controllers/ProductsController.cs` (depends on T010, T014)
- [ ] T027 [US1] Implement `POST /api/sales` และ `GET /api/sales/{id}` ตาม contracts/sales.md (400 ตะกร้าว่าง, 409 `insufficient_stock`) ใน `api/src/TaladPOS.Api/Controllers/SalesController.cs` (depends on T024)
- [ ] T028 [P] [US1] สร้าง ProductCard grid + search bar (ชื่อ/บาร์โค้ด) ด้วย PrimeReact ใน `web/src/components/ProductCard.tsx` และ `web/src/app/sales/page.tsx`
- [ ] T029 [P] [US1] สร้าง Cart component (เพิ่ม/ลด/ลบรายการ, ยอดรวมย่อยแบบเรียลไทม์) ด้วย PrimeReact Button/InputNumber ใน `web/src/components/Cart.tsx`
- [ ] T030 [US1] เชื่อมปุ่มชำระเงินกับ `POST /api/sales` ผ่าน `web/src/lib/api/sales.ts` พร้อมแสดง error เมื่อสต็อกไม่พอ (depends on T027, T029)

**Checkpoint**: US1 ใช้งานได้ครบวงจรด้วยตัวเอง — ขายของได้จริง ตัดสต็อกถูกต้อง บันทึกบิล

---

## Phase 4: User Story 2 - พนักงานล็อกอินก่อนใช้งานและถูกบันทึกว่าเป็นผู้ขาย (Priority: P2)

**Goal**: บังคับล็อกอินก่อนเข้าหน้าขาย และบันทึกผู้ขายในทุกบิลจาก JWT claim (ป้องกันการปลอมแปลง)

**Independent Test**: เข้าหน้าขายโดยไม่ล็อกอิน → ถูกปฏิเสธ; ล็อกอินแล้วขาย → บิลระบุชื่อพนักงานถูกต้อง; สลับผู้ใช้ →
บิลใหม่ระบุผู้ขายคนใหม่ถูกต้อง

### Tests for User Story 2

- [ ] T031 [P] [US2] Integration test: เรียก `POST /api/sales` โดยไม่แนบ `Authorization` header ต้องได้ 401 ใน `api/tests/TaladPOS.Api.IntegrationTests/AuthTests.cs`
- [ ] T032 [P] [US2] Integration test: บิลที่สร้างจาก request ที่ล็อกอินแล้ว ต้องบันทึก `StaffId` ตรงกับ claim ใน JWT เท่านั้น (ไม่รับจาก request body) ใน `api/tests/TaladPOS.Api.IntegrationTests/SalesAttributionTests.cs`

### Implementation for User Story 2

- [ ] T033 [P] [US2] สร้างหน้า `/login` (ฟอร์ม PrimeReact InputText/Password/Button) เรียก `POST /api/auth/login` ใน `web/src/app/login/page.tsx`
- [ ] T034 [P] [US2] Implement auth context (`AuthContext`) เก็บ JWT + ข้อมูลพนักงาน `{id, name, role}`, เปิด/ปิด session ใน `web/src/lib/auth/AuthContext.tsx`
- [ ] T035 [US2] สร้าง protected-route wrapper ที่ redirect ไป `/login` เมื่อไม่มี token ใน `web/src/app/(protected)/layout.tsx` (depends on T034)
- [ ] T036 [US2] เพิ่มปุ่มล็อกเอาต์ (ล้าง token แล้ว redirect ไป `/login`) ใน `web/src/components/AppShell.tsx` (depends on T034)
- [ ] T037 [US2] แสดงชื่อพนักงานที่ล็อกอินอยู่บนหน้าขายสินค้า ใน `web/src/app/sales/page.tsx` (depends on T034)

**Checkpoint**: US1+US2 ทำงานร่วมกัน — บังคับล็อกอิน และผู้ขายถูกบันทึกถูกต้องเสมอ

---

## Phase 5: User Story 3 - ผู้จัดการดูแลสต็อกสินค้า (Priority: P3)

**Goal**: ผู้จัดการเพิ่ม/แก้ไข/ลบสินค้าพร้อมรูป/ราคา/จำนวน และเห็นการแจ้งเตือนสินค้าใกล้หมด

**Independent Test**: เพิ่มสินค้าใหม่ → ปรากฏในหน้าขาย; แก้ราคา/จำนวน → หน้าขายอัปเดตตาม; ลดจำนวนถึงเกณฑ์ → เห็นการแจ้งเตือน

### Tests for User Story 3

- [ ] T038 [P] [US3] Unit test: `Product` ปฏิเสธ `Price <= 0` และ `StockQuantity` ติดลบ ใน `api/tests/TaladPOS.Domain.Tests/Products/ProductValidationTests.cs`
- [ ] T039 [P] [US3] Unit test: `IsLowStock` เป็น true เมื่อ `0 < StockQuantity <= LowStockThreshold`, เป็น false เมื่อ `StockQuantity == 0` (ใช้ `IsOutOfStock` แทน) ใน `api/tests/TaladPOS.Domain.Tests/Products/ProductStockStatusTests.cs`
- [ ] T040 [P] [US3] Unit test: use case สร้าง/แก้ไขสินค้าปฏิเสธ `Barcode` ที่ซ้ำกับสินค้าอื่นที่มีอยู่แล้ว ใน `api/tests/TaladPOS.Application.Tests/Products/ProductUseCaseTests.cs`

### Implementation for User Story 3

- [ ] T041 [US3] Implement `CreateProductUseCase`/`UpdateProductUseCase`/`DeleteProductUseCase` ใน `api/src/TaladPOS.Application/Products/` (depends on T038-T040)
- [ ] T042 [US3] Implement `POST`/`PUT`/`DELETE /api/products` พร้อม `[Authorize(Roles = "Manager")]` (FR-029) และคืน 409 เมื่อ barcode ซ้ำ ตาม contracts/products.md ใน `api/src/TaladPOS.Api/Controllers/ProductsController.cs` (depends on T041)
- [ ] T043 [P] [US3] สร้างหน้า `/stock` แสดงรายการสินค้าด้วย PrimeReact DataTable พร้อม badge สินค้าใกล้หมด ใน `web/src/app/stock/page.tsx`
- [ ] T044 [P] [US3] สร้าง Dialog ฟอร์มเพิ่ม/แก้ไขสินค้า (PrimeReact Dialog, InputText, InputNumber, ช่อง URL รูปภาพ) ใน `web/src/components/ProductFormDialog.tsx`
- [ ] T045 [US3] จำกัดสิทธิ์เข้าหน้า `/stock` เฉพาะ role `Manager` (FR-029) ใน `web/src/app/stock/page.tsx` (depends on T035, T043)

**Checkpoint**: ผู้จัดการจัดการสต็อกได้ครบวงจร มีการแจ้งเตือนสินค้าใกล้หมด

---

## Phase 6: User Story 4 - ลูกค้าสมัครสมาชิกและรับส่วนลดสมาชิกตอนซื้อสินค้า (Priority: P4)

**Goal**: สมัครสมาชิกด้วยเบอร์โทร/ชื่อ ผูกบิลกับสมาชิก และสะสมยอดซื้อ

**Independent Test**: สมัครสมาชิกใหม่ → ค้นหาเจอทันที; ผูกบิลกับสมาชิก → ยอดสะสมเพิ่มขึ้นตามยอดบิลหลังชำระสำเร็จ

### Tests for User Story 4

- [ ] T046 [P] [US4] Unit test: การสมัครสมาชิกปฏิเสธ `PhoneNumber` ที่ซ้ำกับสมาชิกที่มีอยู่แล้ว (FR-011) ใน `api/tests/TaladPOS.Application.Tests/Members/RegisterMemberUseCaseTests.cs`
- [ ] T047 [P] [US4] Unit test: `AccumulatedPurchaseTotal` ของสมาชิกเพิ่มขึ้นเท่ากับ `TotalAmount` ของบิลพอดี เมื่อบิลนั้นผูกกับสมาชิก (FR-014) ใน `api/tests/TaladPOS.Domain.Tests/Members/MemberAccumulationTests.cs`

### Implementation for User Story 4

- [ ] T048 [P] [US4] Implement `Member` domain entity (Name, PhoneNumber unique, AccumulatedPurchaseTotal >= 0 default 0) ตาม data-model.md ใน `api/src/TaladPOS.Domain/Members/Member.cs`
- [ ] T049 [US4] Implement `RegisterMemberUseCase` พร้อมตรวจสอบเบอร์โทรซ้ำ ใน `api/src/TaladPOS.Application/Members/RegisterMemberUseCase.cs` (depends on T048, T046)
- [ ] T050 [US4] ขยาย `CompleteSaleUseCase` ให้รับ `memberId` (optional), ผูก `Sale.MemberId`, และเพิ่ม `Member.AccumulatedPurchaseTotal` แบบ atomic ใน transaction เดียวกัน ใน `api/src/TaladPOS.Application/Sales/CompleteSaleUseCase.cs` (depends on T024, T049, T047)
- [ ] T051 [US4] Implement EF Core configuration + migration สำหรับ `Member` (unique index บน `PhoneNumber`) ใน `api/src/TaladPOS.Infrastructure/Configurations/MemberConfiguration.cs` (depends on T048)
- [ ] T052 [US4] Implement `GET`/`POST /api/members` ตาม contracts/members.md ใน `api/src/TaladPOS.Api/Controllers/MembersController.cs` (depends on T049)
- [ ] T053 [P] [US4] สร้าง member search component (PrimeReact AutoComplete ค้นหาด้วยเบอร์โทร/ชื่อ) ใน `web/src/components/MemberSearch.tsx`
- [ ] T054 [P] [US4] สร้าง Dialog ฟอร์มสมัครสมาชิกใหม่ (PrimeReact Dialog/InputText) ใน `web/src/components/MemberFormDialog.tsx`
- [ ] T055 [US4] รวม member search/สมัครสมาชิกเข้ากับขั้นตอนชำระเงินในหน้าขาย ส่ง `memberId` ไปกับ `POST /api/sales` ใน `web/src/app/sales/page.tsx` (depends on T030, T053, T054)

**Checkpoint**: สมัคร/ค้นหาสมาชิกได้ ผูกบิลกับสมาชิกได้ ยอดสะสมอัปเดตถูกต้อง

---

## Phase 7: User Story 5 - ผู้จัดการตั้งโปรโมชั่นส่วนลด (Priority: P5)

**Goal**: ตั้งส่วนลด % รายสินค้า/ทั้งบิล พร้อมช่วงวันที่ และเลือกใช้ส่วนลดสูงสุดเมื่อชนกับส่วนลดสมาชิก (FR-022)

**Independent Test**: สร้างโปรโมชั่นครอบคลุมวันนี้ → ขายแล้วเห็นส่วนลดอัตโนมัติ; โปรโมชั่นหมดอายุ → ไม่ถูกใช้; มีทั้ง
โปรโมชั่นและส่วนลดสมาชิก → ใช้เฉพาะค่าที่มากกว่า

### Tests for User Story 5

- [ ] T056 [P] [US5] Unit test: `Promotion` ปฏิเสธ `EndDate < StartDate`, `DiscountPercentage` นอกช่วง `(0, 100]`, และกรณี `Scope == Item` ไม่มี `ProductId` หรือ `Scope == Bill` มี `ProductId` ใน `api/tests/TaladPOS.Domain.Tests/Promotions/PromotionValidationTests.cs`
- [ ] T057 [P] [US5] Unit test: `Promotion.IsActive(date)` เป็น true เฉพาะเมื่อ `date` อยู่ในช่วง `[StartDate, EndDate]` แบบ inclusive ใน `api/tests/TaladPOS.Domain.Tests/Promotions/PromotionActiveTests.cs`
- [ ] T058 [US5] Unit test: `DiscountResolver` เลือกส่วนลดที่มีมูลค่าสูงสุดเพียงรายการเดียวเสมอจากผู้สมัคร (โปรโมชั่นรายสินค้า/ทั้งบิลที่ active + ส่วนลดสมาชิก) ไม่สะสมรวมกัน ครอบคลุมเคส: มีแต่โปรโมชั่น, มีแต่ส่วนลดสมาชิก, มีทั้งคู่ค่าเท่ากัน, โปรโมชั่นนอกช่วงวันที่ถูกตัดออก ใน `api/tests/TaladPOS.Domain.Tests/Promotions/DiscountResolverTests.cs` (depends on T057)

### Implementation for User Story 5

- [ ] T059 [US5] Implement `Promotion` domain entity ตาม data-model.md ใน `api/src/TaladPOS.Domain/Promotions/Promotion.cs` (depends on T056)
- [ ] T060 [US5] Implement `DiscountResolver` domain service (กติกาเลือกส่วนลดสูงสุด, FR-022) ใน `api/src/TaladPOS.Domain/Promotions/DiscountResolver.cs` (depends on T059, T058)
- [ ] T061 [US5] Implement `CreatePromotionUseCase`/`UpdatePromotionUseCase`/`DeletePromotionUseCase` (Manager-only) ใน `api/src/TaladPOS.Application/Promotions/` (depends on T059)
- [ ] T062 [US5] แทนที่ `DiscountAmount = 0` ชั่วคราวใน `CompleteSaleUseCase` ด้วยการเรียก `DiscountResolver` จริง (พิจารณาโปรโมชั่น active + ส่วนลดสมาชิก) ใน `api/src/TaladPOS.Application/Sales/CompleteSaleUseCase.cs` (depends on T050, T060)
- [ ] T063 [US5] Implement EF Core configuration + migration สำหรับ `Promotion` ใน `api/src/TaladPOS.Infrastructure/Configurations/PromotionConfiguration.cs` (depends on T059)
- [ ] T064 [US5] Implement CRUD `/api/promotions` (Manager-only) ตาม contracts/promotions.md ใน `api/src/TaladPOS.Api/Controllers/PromotionsController.cs` (depends on T061)
- [ ] T065 [P] [US5] สร้างหน้า `/promotions` (PrimeReact DataTable + Dialog ฟอร์มด้วย Calendar สำหรับวันที่, Dropdown สำหรับ scope, InputNumber สำหรับ %) ใน `web/src/app/promotions/page.tsx` และ `web/src/components/PromotionFormDialog.tsx`
- [ ] T066 [US5] แสดงส่วนลดที่ถูกใช้ต่อรายการและยอดรวมบิลใน Cart/หน้าชำระเงิน ใน `web/src/components/Cart.tsx` (depends on T029, T062)

**Checkpoint**: โปรโมชั่นทำงานอัตโนมัติ กติกาเลือกส่วนลดสูงสุดถูกต้อง มองเห็นในหน้าขาย

---

## Phase 8: User Story 6 - ผู้จัดการดูประวัติการขายและรายงานสรุป (Priority: P6)

**Goal**: ดูประวัติบิลย้อนหลัง และรายงานยอดขายรายวัน/รายเดือน สินค้าขายดี ยอดตามพนักงาน สต็อกคงเหลือ

**Independent Test**: มีบิลสะสมจากหลายพนักงานหลายวัน → ประวัติและรายงานทั้ง 4 ประเภทตรงกับข้อมูลจริง

### Tests for User Story 6

- [ ] T067 [P] [US6] Unit test: รายงานยอดขายรายวัน/รายเดือนรวม `totalSalesAmount` และ `billCount` ถูกต้องตามช่วงวันที่ที่ระบุ ใน `api/tests/TaladPOS.Application.Tests/Reports/SalesReportTests.cs`
- [ ] T068 [P] [US6] Unit test: รายงานสินค้าขายดีเรียงลำดับตาม `quantitySold` จากมากไปน้อยถูกต้อง ใน `api/tests/TaladPOS.Application.Tests/Reports/BestSellingProductsReportTests.cs`

### Implementation for User Story 6

- [ ] T069 [P] [US6] Implement application queries: `GetSalesHistoryQuery`, `GetDailyOrMonthlySalesReportQuery`, `GetBestSellingProductsReportQuery`, `GetSalesByStaffReportQuery`, `GetStockReportQuery` (research.md #6 — query ตรงจาก EF Core ไม่มี read-model แยก) ใน `api/src/TaladPOS.Application/Reports/` (depends on T067, T068)
- [ ] T070 [US6] Implement `GET /api/sales` (ประวัติพร้อม filter `from`/`to`/`staffId`/`memberId`) และ `GET /api/sales/{id}/receipt` ตาม contracts/sales.md ใน `api/src/TaladPOS.Api/Controllers/SalesController.cs` (depends on T027)
- [ ] T071 [US6] Implement `ReportsController` พร้อม `GET /api/reports/sales`, `/best-selling-products`, `/sales-by-staff`, `/stock` (Manager-only) ตาม contracts/reports.md ใน `api/src/TaladPOS.Api/Controllers/ReportsController.cs` (depends on T069)
- [ ] T072 [P] [US6] สร้างหน้าประวัติการขายด้วย PrimeReact DataTable พร้อม filter วันที่/พนักงาน/สมาชิก ใน `web/src/app/sales/history/page.tsx`
- [ ] T073 [P] [US6] สร้างหน้า `/reports` ด้วย PrimeReact TabView สลับ 4 ประเภทรายงาน แต่ละแท็บแสดงผลด้วย DataTable/summary card ใน `web/src/app/reports/page.tsx`
- [ ] T074 [P] [US6] สร้าง Receipt component พร้อมปุ่มพิมพ์ (`window.print()`, research.md #5) ใน `web/src/components/Receipt.tsx`

**Checkpoint**: ครบทั้ง 6 User Story ทำงานร่วมกันได้สมบูรณ์

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: ตรวจสอบ non-functional requirements ที่กระทบหลาย story และเตรียมส่งมอบ

- [ ] T075 [P] Integration test: ยิง `POST /api/sales` สองคำขอพร้อมกันขอซื้อสินค้าชิ้นสุดท้ายชิ้นเดียวกัน (`stockQuantity == 1`) ต้องมีเพียงคำขอเดียวได้ 201 อีกคำขอต้องได้ 409 `insufficient_stock` (research.md #2, quickstart.md ข้อ 5) ใน `api/tests/TaladPOS.Api.IntegrationTests/ConcurrencyTests.cs`
- [ ] T076 [P] ตรวจสอบว่า `web/` ไม่มี PostgreSQL driver/connection string หรือ dependency เข้าถึงฐานข้อมูลโดยตรงใด ๆ (constitution Principle I) และลบออกถ้าพบ
- [ ] T077 [P] สร้างเอกสาร OpenAPI/Swagger สำหรับ `api/` ใน `api/src/TaladPOS.Api/Program.cs`
- [ ] T078 รัน quickstart.md ครบทุก validation scenario (US1–US6 + concurrency + ไม่มี VAT) แล้วบันทึกผล
- [ ] T079 [P] เขียนคำแนะนำการติดตั้ง/รัน (README) สำหรับ `api/` และ `web/` อ้างอิง quickstart.md

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: ไม่มี dependency — เริ่มได้ทันที
- **Foundational (Phase 2)**: ต้องรอ Setup เสร็จ — บล็อกทุก User Story
- **User Stories (Phase 3–8)**: ต้องรอ Foundational เสร็จก่อนทั้งหมด จากนั้นเรียงตาม priority P1→P6 ได้ หรือขนานกันถ้ามีกำลังคนพอ
- **Polish (Phase 9)**: รอ User Story ที่ต้องการ deliver ครบก่อน

### User Story Dependencies

- **US1 (P1)**: เริ่มได้หลัง Foundational — ไม่พึ่ง story อื่น (MVP)
- **US2 (P2)**: เริ่มได้หลัง Foundational — ใช้ auth infrastructure ที่มีอยู่แล้ว, ทดสอบอิสระได้แต่ควรทำหลัง US1 เพื่อยืนยัน sale attribution จริง
- **US3 (P3)**: เริ่มได้หลัง Foundational — อิสระจาก US1/US2 (ใช้ `Product` entity เดียวกันจาก Foundational)
- **US4 (P4)**: แก้ไข `CompleteSaleUseCase` ร่วมกับ US1 (T024) — ควรทำหลัง US1 เสร็จ
- **US5 (P5)**: แก้ไข `CompleteSaleUseCase` ต่อจาก US4 (T050) — ควรทำหลัง US4 เสร็จ (เพราะต้องมีส่วนลดสมาชิกให้เทียบ FR-022)
- **US6 (P6)**: อ่านข้อมูลจาก US1/US4/US5 (Sale, Member, Promotion) — ควรทำหลังสุด เพื่อให้รายงานมีข้อมูลครบ

### Within Each User Story

- Test ต้องเขียนและ "แดง" (fail) ก่อน แล้วค่อย implement ให้ผ่าน
- Domain entity → Application use case → Infrastructure/API → UI
- Story หนึ่งเสร็จสมบูรณ์ก่อนเริ่ม story ถัดไป (หรือขนานกันถ้าทีมพอ)

### Parallel Opportunities

- Setup: T003, T004, T005, T007 รันขนานกันได้ (คนละไฟล์/โฟลเดอร์)
- Foundational: T009, T010 ขนานกันได้; T015 ขนานกับ T016; T018, T019 ขนานกัน (ฝั่ง `web/` แยกจาก `api/`)
- ในแต่ละ Story: test ที่มี `[P]` รันขนานกันได้ก่อนเริ่ม implementation
- US3 ขนานกับ US1/US2 ได้ทั้งหมด (คนละไฟล์ ไม่แก้ `CompleteSaleUseCase` ร่วมกัน)

---

## Parallel Example: User Story 1

```bash
# รัน unit tests ของ US1 พร้อมกัน:
Task: "Unit test: การตัดสต็อกต้องถูกปฏิเสธเมื่อจำนวนที่ขอเกินกว่า StockQuantity ใน api/tests/TaladPOS.Domain.Tests/Products/ProductStockTests.cs"
Task: "Unit test: Sale ต้องมีอย่างน้อย 1 SaleLineItem เสมอ ใน api/tests/TaladPOS.Domain.Tests/Sales/SaleTests.cs"
Task: "Unit test: SaleLineItem เก็บ snapshot ชื่อ/ราคาแยกจาก Product ใน api/tests/TaladPOS.Domain.Tests/Sales/SaleLineItemTests.cs"

# รัน UI component ของ US1 พร้อมกัน (คนละไฟล์):
Task: "สร้าง ProductCard grid + search bar ใน web/src/components/ProductCard.tsx"
Task: "สร้าง Cart component ใน web/src/components/Cart.tsx"
```

---

## Implementation Strategy

### MVP First (User Story 1 เท่านั้น)

1. Phase 1: Setup
2. Phase 2: Foundational (จำเป็น — บล็อกทุก story)
3. Phase 3: User Story 1
4. **หยุดและตรวจสอบ**: ทดสอบ US1 แบบอิสระตาม Independent Test
5. Deploy/demo ได้ถ้าพร้อม

### Incremental Delivery

1. Setup + Foundational เสร็จ → พื้นฐานพร้อม
2. เพิ่ม US1 → ทดสอบอิสระ → Deploy/Demo (MVP!)
3. เพิ่ม US2 → ทดสอบอิสระ → Deploy/Demo
4. เพิ่ม US3 → ทดสอบอิสระ → Deploy/Demo
5. เพิ่ม US4 → ทดสอบอิสระ → Deploy/Demo
6. เพิ่ม US5 → ทดสอบอิสระ → Deploy/Demo
7. เพิ่ม US6 → ทดสอบอิสระ → Deploy/Demo
8. Phase 9: Polish

### Parallel Team Strategy

1. ทีมทำ Setup + Foundational ร่วมกันก่อน
2. เมื่อ Foundational เสร็จ:
   - คนที่ 1: US1 → US4 → US5 (สาย `CompleteSaleUseCase`)
   - คนที่ 2: US2 (auth UI)
   - คนที่ 3: US3 (จัดการสต็อก, อิสระเต็มที่)
   - คนที่ 4: US6 (เริ่มโครง query/report ล่วงหน้าได้ แต่รอข้อมูลจริงจาก US1/US4/US5 ก่อนตรวจผลสุดท้าย)

---

## Notes

- [P] = คนละไฟล์ ไม่มี dependency ค้าง
- [Story] label ใช้ตรวจสอบว่า task นี้เพื่อ story ไหน
- ทุก unit test ของ business logic ต้อง "แดง" ก่อน แล้วค่อย implement ให้ "เขียว" (constitution Principle III)
- Commit หลังจบแต่ละ task หรือกลุ่มงานที่สัมพันธ์กัน
- หยุดที่ checkpoint ของแต่ละ story เพื่อตรวจสอบความอิสระก่อนไปต่อ
- หลีกเลี่ยง: task คลุมเครือ, แก้ไฟล์เดียวกันพร้อมกันหลาย task, ทำให้ story ต้องพึ่งพา story อื่นเกินจำเป็น
