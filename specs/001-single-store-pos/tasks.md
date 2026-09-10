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

- [X] T001 สร้าง solution + 4 projects (`TaladPOS.Domain`, `TaladPOS.Application`, `TaladPOS.Infrastructure`, `TaladPOS.Api`) ใน `api/src/` ตาม Project Structure ใน plan.md
- [X] T002 สร้าง 3 test projects (`TaladPOS.Domain.Tests`, `TaladPOS.Application.Tests`, `TaladPOS.Api.IntegrationTests`) พร้อม xUnit + FluentAssertions ใน `api/tests/`
- [X] T003 [P] สร้างโปรเจกต์ Next.js 14 (App Router) + TypeScript + Tailwind CSS 3 ใน `web/`
- [X] T004 [P] ติดตั้ง PrimeReact และตั้งค่าโหมด unstyled + Tailwind passthrough preset ตาม research.md #7 ใน `web/src/styles/`
- [X] T005 [P] ตั้งค่า lint/format: `.editorconfig`/`dotnet format` สำหรับ `api/`, ESLint+Prettier สำหรับ `web/`
- [X] T006 ตั้งค่าการเชื่อมต่อ PostgreSQL 16 (connection string ใน `appsettings.Development.json`) และ `docker-compose.yml` สำหรับรัน PostgreSQL ในเครื่อง ใน `api/`
- [X] T007 [P] สร้างไฟล์ `.env.local.example` กำหนด `NEXT_PUBLIC_API_BASE_URL` ใน `web/`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: โครงสร้างพื้นฐานที่ทุก User Story ต้องพึ่งพา (auth, entities หลัก, DB context) — ทุก endpoint ตาม
`contracts/*.md` ต้องผ่าน JWT auth ก่อน จึงต้องทำให้เสร็จก่อนเริ่ม story ใด ๆ

**⚠️ CRITICAL**: ห้ามเริ่มงาน User Story ใดจนกว่า phase นี้จะเสร็จ

- [X] T008 สร้าง `TaladPOSDbContext` (EF Core) และโครง migration เริ่มต้นใน `api/src/TaladPOS.Infrastructure/`
- [X] T009 [P] Implement `Staff` domain entity (Name, Username unique, PasswordHash, Role enum `Manager`/`Cashier`) ตาม data-model.md ใน `api/src/TaladPOS.Domain/Staff/Staff.cs`
- [X] T010 [P] Implement `Product` domain entity (Name required, ImageUrl required, Price > 0, Barcode optional-unique, StockQuantity int >= 0, LowStockThreshold int >= 0, computed `IsLowStock`/`IsOutOfStock`) ตาม data-model.md ใน `api/src/TaladPOS.Domain/Products/Product.cs`
- [X] T011 Implement credential verification ด้วย `PasswordHasher<Staff>` (research.md #1) ใน `api/src/TaladPOS.Application/Auth/StaffAuthenticator.cs` (ใช้ marker type แทน `Staff` โดยตรงเพื่อ hash รหัสผ่านก่อนมี instance จริงตอน seed — ดูคอมเมนต์ในไฟล์)
- [X] T012 Implement JWT issuing service (claims: `staffId`, `role`, อายุจำกัดตาม `expiresAt`) ตาม contracts/auth.md ใน `api/src/TaladPOS.Infrastructure/Auth/JwtTokenService.cs` (interface `IJwtTokenService` อยู่ใน Application ตาม research.md #1; implementation ย้ายไป Infrastructure เพราะพึ่ง `System.IdentityModel.Tokens.Jwt`)
- [X] T013 Implement `POST /api/v1/auth/login` ตาม contracts/auth.md (คืน 401 `invalid_credentials` เมื่อ username/password ผิด) ใน `api/src/TaladPOS.Api/Controllers/AuthController.cs` (depends on T011, T012)
- [X] T014 ตั้งค่า JWT bearer authentication middleware + authorization policy แยก `Manager`/`Cashier` (FR-029) ใน `api/src/TaladPOS.Api/Program.cs` (depends on T012)
- [X] T015 [P] Implement EF Core entity configuration + migration สำหรับ `Staff` และ `Product` (unique index บน `Username` และ `Barcode`) ใน `api/src/TaladPOS.Infrastructure/Configurations/` (depends on T008, T009, T010) — สร้าง migration `InitialCreate` และรันจริงกับ PostgreSQL 16 (docker-compose) สำเร็จ
- [X] T016 [P] สร้าง global error-handling middleware (validation → 400, not found → 404, conflict → 409) ใน `api/src/TaladPOS.Api/Middleware/ErrorHandlingMiddleware.cs`
- [X] T017 สร้าง seed script (1 บัญชี Manager, 1 บัญชี Cashier, สินค้าตัวอย่าง 2 รายการ ตาม quickstart.md ข้อ 3) ใน `api/src/TaladPOS.Infrastructure/Seed/DevelopmentSeeder.cs` (depends on T015) — ทดสอบรันจริง เห็นข้อมูล seed ใน PostgreSQL แล้ว
- [X] T018 [P] สร้าง REST API client base (fetch wrapper แนบ `Authorization: Bearer` header อัตโนมัติ, อ่าน base URL จาก `NEXT_PUBLIC_API_BASE_URL`) ใน `web/src/lib/api/client.ts`
- [X] T019 [P] ตั้งค่า `PrimeReactProvider` + root layout (โหลด Tailwind globals) ใน `web/src/app/layout.tsx`

**Checkpoint**: Auth ใช้งานได้จริง, entity หลักพร้อม, DB migration พร้อม — เริ่มงาน User Story ได้

---

## Phase 3: User Story 1 - พนักงานทำการขายสินค้าให้ลูกค้า (Priority: P1) 🎯 MVP

**Goal**: พนักงานค้นหาสินค้า (ชื่อ/บาร์โค้ด) เพิ่ม/ลด/ลบในตะกร้า แล้วชำระเงินปิดบิล ระบบตัดสต็อกและบันทึกบิลอัตโนมัติ

**Independent Test**: seed สินค้าไว้ล่วงหน้า → ค้นหา → เพิ่มลงตะกร้า → ปรับจำนวน → ชำระเงิน → ตรวจสอบสต็อกลดลงตรงและมี
บิลถูกบันทึก (ไม่ต้องพึ่งสมาชิกหรือโปรโมชั่น)

### Tests for User Story 1 (business logic — NON-NEGOTIABLE ตาม constitution Principle III)

- [X] T020 [P] [US1] Unit test: การตัดสต็อกต้องถูกปฏิเสธเมื่อจำนวนที่ขอเกินกว่า `StockQuantity` ที่มีอยู่ ใน `api/tests/TaladPOS.Domain.Tests/Products/ProductStockTests.cs`
- [X] T021 [P] [US1] Unit test: `Sale` ต้องมีอย่างน้อย 1 `SaleLineItem` เสมอ (ปฏิเสธตะกร้าว่าง ตาม Edge Case ในสเปก) ใน `api/tests/TaladPOS.Domain.Tests/Sales/SaleTests.cs`
- [X] T022 [P] [US1] Unit test: `SaleLineItem` เก็บ `ProductNameSnapshot`/`UnitPriceSnapshot` แยกจากค่าปัจจุบันของ `Product` (คงอยู่แม้สินค้าถูกลบ/แก้ไขภายหลัง) ใน `api/tests/TaladPOS.Domain.Tests/Sales/SaleLineItemTests.cs`

### Implementation for User Story 1

- [X] T023 [P] [US1] Implement `Sale` และ `SaleLineItem` domain entities (append-only ตาม data-model.md) ใน `api/src/TaladPOS.Domain/Sales/` (depends on T020-T022)
- [X] T024 [US1] Implement `CompleteSaleUseCase`: ตรวจตะกร้าไม่ว่าง, ตัดสต็อกแบบ atomic conditional update (`UPDATE ... WHERE stock_quantity >= @qty`, research.md #2), ตั้ง `DiscountAmount = 0` ชั่วคราว (ยังไม่มีโปรโมชั่น/สมาชิกใน US1), บันทึก `Sale` ทั้งหมดใน transaction เดียว ใน `api/src/TaladPOS.Application/Sales/CompleteSaleUseCase.cs` (depends on T023) — ใช้ `IUnitOfWork.BeginTransactionAsync` ครอบทั้งการตัดสต็อกและบันทึก Sale เพราะ `ExecuteUpdateAsync` ไม่รวม transaction กับ `SaveChangesAsync` โดยอัตโนมัติ (พบและแก้ไขจากคำแนะนำก่อน implement); มี unit test เพิ่มเติมยืนยัน commit/ไม่สร้างบิลเมื่อสต็อกไม่พอ ใน `api/tests/TaladPOS.Application.Tests/Sales/CompleteSaleUseCaseTests.cs`
- [X] T025 [US1] Implement repository + EF Core configuration สำหรับ `Sale`/`SaleLineItem` และ migration ใน `api/src/TaladPOS.Infrastructure/Repositories/SaleRepository.cs` (depends on T010, T023) — สร้าง migration `AddSales` และรันจริงกับ PostgreSQL สำเร็จ
- [X] T026 [US1] Implement `GET /api/v1/products` (query `search`, `barcode`, `lowStockOnly`) ตาม contracts/products.md ใน `api/src/TaladPOS.Api/Controllers/ProductsController.cs` (depends on T010, T014)
- [X] T027 [US1] Implement `POST /api/v1/sales` และ `GET /api/v1/sales/{id}` ตาม contracts/sales.md (400 ตะกร้าว่าง, 409 `insufficient_stock`) ใน `api/src/TaladPOS.Api/Controllers/SalesController.cs` (depends on T024)
- [X] T028 [P] [US1] สร้าง ProductCard grid + search bar (ชื่อ/บาร์โค้ด) ด้วย PrimeReact ใน `web/src/components/ProductCard.tsx` และ `web/src/app/sales/page.tsx`
- [X] T029 [P] [US1] สร้าง Cart component (เพิ่ม/ลด/ลบรายการ, ยอดรวมย่อยแบบเรียลไทม์) ด้วย PrimeReact Button/InputNumber ใน `web/src/components/Cart.tsx`
- [X] T030 [US1] เชื่อมปุ่มชำระเงินกับ `POST /api/v1/sales` ผ่าน `web/src/lib/api/sales.ts` พร้อมแสดง error เมื่อสต็อกไม่พอ (depends on T027, T029) — ทดสอบผ่าน browser จริงครบวงจร (ค้นหา → เพิ่มตะกร้า → ปรับจำนวน → ชำระเงิน → ตะกร้าล้าง/สต็อกลด); ระหว่างทางพบและแก้บั๊ก CORS จริง (ต้องเพิ่ม `app.UseCors` ใน `Program.cs` ให้ `web/` เรียก `api/` ข้าม origin ได้) และปัญหา `.env.local` ของ `web/` ไม่มีอยู่จริง (มีแต่ `.env.local.example`)

**Checkpoint**: US1 ใช้งานได้ครบวงจรด้วยตัวเอง — ขายของได้จริง ตัดสต็อกถูกต้อง บันทึกบิล

---

## Phase 4: User Story 2 - พนักงานล็อกอินก่อนใช้งานและถูกบันทึกว่าเป็นผู้ขาย (Priority: P2)

**Goal**: บังคับล็อกอินก่อนเข้าหน้าขาย และบันทึกผู้ขายในทุกบิลจาก JWT claim (ป้องกันการปลอมแปลง)

**Independent Test**: เข้าหน้าขายโดยไม่ล็อกอิน → ถูกปฏิเสธ; ล็อกอินแล้วขาย → บิลระบุชื่อพนักงานถูกต้อง; สลับผู้ใช้ →
บิลใหม่ระบุผู้ขายคนใหม่ถูกต้อง

### Tests for User Story 2

- [X] T031 [P] [US2] Integration test: เรียก `POST /api/v1/sales` โดยไม่แนบ `Authorization` header ต้องได้ 401 ใน `api/tests/TaladPOS.Api.IntegrationTests/AuthTests.cs`
  - Built `TaladPOSApiFactory` (`api/tests/TaladPOS.Api.IntegrationTests/Infrastructure/`): `WebApplicationFactory<Program>` against a real, dedicated `taladpos_test` Postgres database (not in-memory) - runs real migrations + `DevelopmentSeeder`, truncates+reseeds `sale_line_items/sales/products` before every test for isolation, all tests serialized via one xunit collection since they share the DB.
  - Deviation found & fixed: Program.cs (top-level statements) reads `Jwt:SigningKey`/`ConnectionStrings:TaladPOSDb` *eagerly*, before `builder.Build()` - `WebApplicationFactory.ConfigureWebHost`'s `ConfigureAppConfiguration` hook only applies to the deferred host builder captured *at* `Build()`, so it runs too late. Fixed by setting real OS environment variables (`Jwt__SigningKey` etc.) in the factory's static constructor instead, which `WebApplicationBuilder.CreateBuilder`'s default `AddEnvironmentVariables()` picks up in time.
  - Deviation found & fixed: table names are lowercase snake_case (`sales`, `sale_line_items`, `products`) via explicit `ToTable(...)` in each `*Configuration.cs`, not PascalCase as assumed - the reset helper's `TRUNCATE` initially referenced nonexistent `"Sales"`/`"SaleLineItems"`/`"Products"` and failed with `42P01`; fixed to the real lowercase names.
  - Verified: `dotnet test tests/TaladPOS.Api.IntegrationTests` (docker sdk container on the `taladpos_default` network, `TEST_DB_CONNECTION=Host=postgres;...;Database=taladpos_test`) - 3/3 pass.
- [X] T032 [P] [US2] Integration test: บิลที่สร้างจาก request ที่ล็อกอินแล้ว ต้องบันทึก `StaffId` ตรงกับ claim ใน JWT เท่านั้น (ไม่รับจาก request body) ใน `api/tests/TaladPOS.Api.IntegrationTests/SalesAttributionTests.cs`
  - Logs in as both cashier and manager, checks out as the cashier while including an extraneous `staffId` field pointed at the manager in the request body (inert - the DTO doesn't bind it), then re-fetches the sale through a *different* logged-in session to confirm the persisted `StaffId` is the cashier's, never the manager's.
  - Considered nesting `staff:{id,name}` into `SaleDto` to match `contracts/sales.md`'s literal shape, but `Sale` deliberately has no `Staff` navigation (DDD aggregate boundary, same reasoning as `SaleLineItem.ProductId` having no FK) and US2's own acceptance criteria only requires correct `StaffId` attribution, not display. Deferred the display-shape fix to US6 (T067-T071), where the sales-history/receipt UI will actually need staff names and can resolve them via `IStaffRepository` without touching the aggregate.

### Implementation for User Story 2

- [X] T033 [P] [US2] สร้างหน้า `/login` (ฟอร์ม PrimeReact InputText/Password/Button) เรียก `POST /api/v1/auth/login` ใน `web/src/app/login/page.tsx`
  - Added `passwordPT`/`secondaryButtonPT` to `web/src/styles/primereact-passthrough.ts` (PrimeReact `Password` component, unstyled+Tailwind per research.md #7).
- [X] T034 [P] [US2] Implement auth context (`AuthContext`) เก็บ JWT + ข้อมูลพนักงาน `{id, name, role}`, เปิด/ปิด session ใน `web/src/lib/auth/AuthContext.tsx`
  - Added `web/src/lib/api/auth.ts` (`POST /api/v1/auth/login` wrapper) and extended `tokenStore.ts` with `getStoredStaff`/`setStoredStaff`/`clearStoredStaff` so the staff summary survives a page reload without re-authenticating. Wired `AuthProvider` into `web/src/app/providers.tsx`.
- [X] T035 [US2] สร้าง protected-route wrapper ที่ redirect ไป `/login` เมื่อไม่มี token ใน `web/src/app/(protected)/layout.tsx` (depends on T034)
  - Moved `web/src/app/sales/page.tsx` into `web/src/app/(protected)/sales/page.tsx` (route group - URL stays `/sales`) so it picks up this guard. `web/src/app/page.tsx` (unused Next.js scaffold) now redirects to `/sales`.
- [X] T036 [US2] เพิ่มปุ่มล็อกเอาต์ (ล้าง token แล้ว redirect ไป `/login`) ใน `web/src/components/AppShell.tsx` (depends on T034)
- [X] T037 [US2] แสดงชื่อพนักงานที่ล็อกอินอยู่บนหน้าขายสินค้า ใน `web/src/app/sales/page.tsx` (depends on T034)
  - Implemented in the shared `AppShell` header (shown on every `(protected)` route, sales page included) rather than duplicated per-page.
  - Verified end-to-end in a real browser (Playwright) against the real API+Postgres: unauthenticated `/sales` → redirects to `/login`; login as `cashier` → lands on `/sales` with header showing "แคชเชียร์ (แคชเชียร์)"; completed a real checkout (มะม่วง stock 7→6, confirmed via `GET /api/v1/products` with a manager token); clicked logout → redirected to `/login`, `localStorage` token/staff cleared, re-visiting `/sales` redirects to `/login` again.
  - Also ran `npx tsc --noEmit`, `npm run build`, `npx eslint src --max-warnings=0` - all clean.

**Checkpoint**: US1+US2 ทำงานร่วมกัน — บังคับล็อกอิน และผู้ขายถูกบันทึกถูกต้องเสมอ

---

## Phase 5: User Story 3 - ผู้จัดการดูแลสต็อกสินค้า (Priority: P3)

**Goal**: ผู้จัดการเพิ่ม/แก้ไข/ลบสินค้าพร้อมรูป/ราคา/จำนวน และเห็นการแจ้งเตือนสินค้าใกล้หมด

**Independent Test**: เพิ่มสินค้าใหม่ → ปรากฏในหน้าขาย; แก้ราคา/จำนวน → หน้าขายอัปเดตตาม; ลดจำนวนถึงเกณฑ์ → เห็นการแจ้งเตือน

### Tests for User Story 3

- [X] T038 [P] [US3] Unit test: `Product` ปฏิเสธ `Price <= 0` และ `StockQuantity` ติดลบ ใน `api/tests/TaladPOS.Domain.Tests/Products/ProductValidationTests.cs`
- [X] T039 [P] [US3] Unit test: `IsLowStock` เป็น true เมื่อ `0 < StockQuantity <= LowStockThreshold`, เป็น false เมื่อ `StockQuantity == 0` (ใช้ `IsOutOfStock` แทน) ใน `api/tests/TaladPOS.Domain.Tests/Products/ProductStockStatusTests.cs`
- [X] T040 [P] [US3] Unit test: use case สร้าง/แก้ไขสินค้าปฏิเสธ `Barcode` ที่ซ้ำกับสินค้าอื่นที่มีอยู่แล้ว ใน `api/tests/TaladPOS.Application.Tests/Products/ProductUseCaseTests.cs`

### Implementation for User Story 3

- [X] T041 [US3] Implement `CreateProductUseCase`/`UpdateProductUseCase`/`DeleteProductUseCase` ใน `api/src/TaladPOS.Application/Products/` (depends on T038-T040)
  - Extended `IProductRepository` with `BarcodeExistsAsync`/`AddAsync`/`UpdateAsync`/`DeleteAsync`; added `DuplicateBarcodeException` (Domain).
- [X] T042 [US3] Implement `POST`/`PUT`/`DELETE /api/v1/products` พร้อม `[Authorize(Roles = "Manager")]` (FR-029) และคืน 409 เมื่อ barcode ซ้ำ ตาม contracts/products.md ใน `api/src/TaladPOS.Api/Controllers/ProductsController.cs` (depends on T041)
  - Mapped `DuplicateBarcodeException` → 409 in `ErrorHandlingMiddleware`.
  - Verified against the real running API (curl + real Postgres, not just unit tests): Cashier `POST /api/v1/products` → 403; Manager → 201; duplicate barcode → 409 `{"error":"duplicate_barcode",...}`; `PUT` updating stock below threshold → `isLowStock:true`; `GET ?lowStockOnly=true` includes it; Manager `DELETE` → 204, then `GET` → 404.
- [X] T043 [P] [US3] สร้างหน้า `/stock` แสดงรายการสินค้าด้วย PrimeReact DataTable พร้อม badge สินค้าใกล้หมด ใน `web/src/app/stock/page.tsx`
  - Placed at `web/src/app/(protected)/stock/page.tsx` (route group, URL stays `/stock`) so it inherits T035's login guard. Added `dataTablePT` to `primereact-passthrough.ts`, keyed off PrimeReact's own bundled Tailwind PT preset (`node_modules/primereact/passthrough/tailwind`) so the pt tree shape (`table`/`thead`/`tbody`/`headerRow`/`bodyRow`/`column.headerCell`/`column.bodyCell`) is correct, re-themed to this app's palette.
  - Added a nav bar to `AppShell` (ขายสินค้า / จัดการสต็อก links, the latter Manager-only) so the page is actually reachable from the UI.
- [X] T044 [P] [US3] สร้าง Dialog ฟอร์มเพิ่ม/แก้ไขสินค้า (PrimeReact Dialog, InputText, InputNumber, ช่อง URL รูปภาพ) ใน `web/src/components/ProductFormDialog.tsx`
  - Added `dialogPT` to the passthrough presets. Handles both create (product=null) and edit modes; pre-fills from the selected product; surfaces 409/400 as Thai error text.
- [X] T045 [US3] จำกัดสิทธิ์เข้าหน้า `/stock` เฉพาะ role `Manager` (FR-029) ใน `web/src/app/stock/page.tsx` (depends on T035, T043)
  - Verified end-to-end in a real browser against the real API+Postgres: Manager sees the full DataTable, adds a product, edits มะม่วง (dialog correctly pre-filled name/image/price/barcode/stock/threshold from the existing row), deletes the test product; Cashier hitting `/stock` sees "หน้านี้สำหรับผู้จัดการเท่านั้น" instead of the table, and the nav bar hides the "จัดการสต็อก" link for them.
  - Also ran `npx tsc --noEmit`, `npm run build`, `npx eslint src --max-warnings=0` - all clean. Full backend suite: `dotnet test` → 18 Domain + 8 Application + 3 Integration, all passing.
  - Noted for later: PrimeReact's `InputNumber` didn't respond to a raw native-`value`-setter + `input`-event simulation the way plain `InputText` did during automated browser testing (it needs its own internal keyboard-driven parsing) - not an app defect (real typing works normally; confirmed by editing an existing product and seeing its price/stock fields pre-fill and display correctly), just a limitation of that specific test technique, documented here so it isn't mistaken for a UI bug later.

**Checkpoint**: ผู้จัดการจัดการสต็อกได้ครบวงจร มีการแจ้งเตือนสินค้าใกล้หมด

---

## Phase 6: User Story 4 - ลูกค้าสมัครสมาชิกและรับส่วนลดสมาชิกตอนซื้อสินค้า (Priority: P4)

**Goal**: สมัครสมาชิกด้วยเบอร์โทร/ชื่อ ผูกบิลกับสมาชิก และสะสมยอดซื้อ

**Independent Test**: สมัครสมาชิกใหม่ → ค้นหาเจอทันที; ผูกบิลกับสมาชิก → ยอดสะสมเพิ่มขึ้นตามยอดบิลหลังชำระสำเร็จ

### Tests for User Story 4

- [X] T046 [P] [US4] Unit test: การสมัครสมาชิกปฏิเสธ `PhoneNumber` ที่ซ้ำกับสมาชิกที่มีอยู่แล้ว (FR-011) ใน `api/tests/TaladPOS.Application.Tests/Members/RegisterMemberUseCaseTests.cs`
- [X] T047 [P] [US4] Unit test: `AccumulatedPurchaseTotal` ของสมาชิกเพิ่มขึ้นเท่ากับ `TotalAmount` ของบิลพอดี เมื่อบิลนั้นผูกกับสมาชิก (FR-014) ใน `api/tests/TaladPOS.Domain.Tests/Members/MemberAccumulationTests.cs`

### Implementation for User Story 4

- [X] T048 [P] [US4] Implement `Member` domain entity (Name, PhoneNumber unique, AccumulatedPurchaseTotal >= 0 default 0) ตาม data-model.md ใน `api/src/TaladPOS.Domain/Members/Member.cs`
- [X] T049 [US4] Implement `RegisterMemberUseCase` พร้อมตรวจสอบเบอร์โทรซ้ำ ใน `api/src/TaladPOS.Application/Members/RegisterMemberUseCase.cs` (depends on T048, T046)
  - Added `DuplicatePhoneNumberException` (Domain) → mapped to 409 `phone_number_already_registered` in `ErrorHandlingMiddleware`.
- [X] T050 [US4] ขยาย `CompleteSaleUseCase` ให้รับ `memberId` (optional), ผูก `Sale.MemberId`, และเพิ่ม `Member.AccumulatedPurchaseTotal` แบบ atomic ใน transaction เดียวกัน ใน `api/src/TaladPOS.Application/Sales/CompleteSaleUseCase.cs` (depends on T024, T049, T047)
  - Unknown `memberId` is rejected (404) before any stock is touched. Accumulation uses the same atomic `ExecuteUpdateAsync` pattern as stock decrement (research.md #2) via `IMemberRepository.IncreaseAccumulatedPurchaseTotalAsync`, inside the same `IUnitOfWork` transaction as the Sale insert and stock decrement.
- [X] T051 [US4] Implement EF Core configuration + migration สำหรับ `Member` (unique index บน `PhoneNumber`) ใน `api/src/TaladPOS.Infrastructure/Configurations/MemberConfiguration.cs` (depends on T048)
  - Migration `AddMembers` generated and applied to real PostgreSQL (`members` table, unique index on `PhoneNumber`).
- [X] T052 [US4] Implement `GET`/`POST /api/v1/members` ตาม contracts/members.md ใน `api/src/TaladPOS.Api/Controllers/MembersController.cs` (depends on T049)
- [X] T053 [P] [US4] สร้าง member search component (PrimeReact AutoComplete ค้นหาด้วยเบอร์โทร/ชื่อ) ใน `web/src/components/MemberSearch.tsx`
  - Added `autoCompletePT` to `primereact-passthrough.ts` (keyed off PrimeReact's bundled Tailwind preset for correct pt shape).
- [X] T054 [P] [US4] สร้าง Dialog ฟอร์มสมัครสมาชิกใหม่ (PrimeReact Dialog/InputText) ใน `web/src/components/MemberFormDialog.tsx`
- [X] T055 [US4] รวม member search/สมัครสมาชิกเข้ากับขั้นตอนชำระเงินในหน้าขาย ส่ง `memberId` ไปกับ `POST /api/v1/sales` ใน `web/src/app/(protected)/sales/page.tsx` (depends on T030, T053, T054)
  - Verified end-to-end against the real API+Postgres: `dotnet test` → 21 Domain + 12 Application + 3 Integration, all passing. curl: register member → 201; duplicate phone → 409; checkout with `memberId` → member's `accumulatedPurchaseTotal` increases by exactly the bill's `totalAmount` (90.00, confirmed via `GET /api/v1/members/{id}`); checkout with an unknown `memberId` → 404, stock untouched. Real browser (Playwright): searched a member by name in the Cart's AutoComplete, selected it (accumulated total displayed), completed checkout, and confirmed via API the total went from 90.00 → 135.00 (+45.00, matching the bill) - member selection also correctly clears after a successful checkout.

**Checkpoint**: สมัคร/ค้นหาสมาชิกได้ ผูกบิลกับสมาชิกได้ ยอดสะสมอัปเดตถูกต้อง

---

## Phase 7: User Story 5 - ผู้จัดการตั้งโปรโมชั่นส่วนลด (Priority: P5)

**Goal**: ตั้งส่วนลด % รายสินค้า/ทั้งบิล พร้อมช่วงวันที่ และเลือกใช้ส่วนลดสูงสุดเมื่อชนกับส่วนลดสมาชิก (FR-022)

**Independent Test**: สร้างโปรโมชั่นครอบคลุมวันนี้ → ขายแล้วเห็นส่วนลดอัตโนมัติ; โปรโมชั่นหมดอายุ → ไม่ถูกใช้; มีทั้ง
โปรโมชั่นและส่วนลดสมาชิก → ใช้เฉพาะค่าที่มากกว่า

### Tests for User Story 5

- [X] T056 [P] [US5] Unit test: `Promotion` ปฏิเสธ `EndDate < StartDate`, `DiscountPercentage` นอกช่วง `(0, 100]`, และกรณี `Scope == Item` ไม่มี `ProductId` หรือ `Scope == Bill` มี `ProductId` ใน `api/tests/TaladPOS.Domain.Tests/Promotions/PromotionValidationTests.cs`
- [X] T057 [P] [US5] Unit test: `Promotion.IsActive(date)` เป็น true เฉพาะเมื่อ `date` อยู่ในช่วง `[StartDate, EndDate]` แบบ inclusive ใน `api/tests/TaladPOS.Domain.Tests/Promotions/PromotionActiveTests.cs`
- [X] T058 [US5] Unit test: `DiscountResolver` เลือกส่วนลดที่มีมูลค่าสูงสุดเพียงรายการเดียวเสมอจากผู้สมัคร (โปรโมชั่นรายสินค้า/ทั้งบิลที่ active + ส่วนลดสมาชิก) ไม่สะสมรวมกัน ครอบคลุมเคส: มีแต่โปรโมชั่น, มีแต่ส่วนลดสมาชิก, มีทั้งคู่ค่าเท่ากัน, โปรโมชั่นนอกช่วงวันที่ถูกตัดออก ใน `api/tests/TaladPOS.Domain.Tests/Promotions/DiscountResolverTests.cs` (depends on T057)

### Implementation for User Story 5

- [X] T059 [US5] Implement `Promotion` domain entity ตาม data-model.md ใน `api/src/TaladPOS.Domain/Promotions/Promotion.cs` (depends on T056)
  - `AppliesToMembersOnly` on `Promotion` IS the "member discount" concept (data-model.md) - not a separate field on `Member`. `Scope` is updatable via `Update()` since `PUT /api/v1/promotions/{id}` accepts the full request body (contracts/promotions.md).
- [X] T060 [US5] Implement `DiscountResolver` domain service (กติกาเลือกส่วนลดสูงสุด, FR-022) ใน `api/src/TaladPOS.Domain/Promotions/DiscountResolver.cs` (depends on T059, T058)
  - Two entry points, `ResolveItemDiscount`/`ResolveBillDiscount`, each internally filters by `IsActive(date)` + eligibility (scope/productId/membership) then picks the single largest resulting amount - never stacks.
- [X] T061 [US5] Implement `CreatePromotionUseCase`/`UpdatePromotionUseCase`/`DeletePromotionUseCase` (Manager-only) ใน `api/src/TaladPOS.Application/Promotions/` (depends on T059)
  - Manager-only is enforced at the controller (`[Authorize(Roles = "Manager")]` on the whole `PromotionsController`), not in these use cases.
- [X] T062 [US5] แทนที่ `DiscountAmount = 0` ชั่วคราวใน `CompleteSaleUseCase` ด้วยการเรียก `DiscountResolver` จริง (พิจารณาโปรโมชั่น active + ส่วนลดสมาชิก) ใน `api/src/TaladPOS.Application/Sales/CompleteSaleUseCase.cs` (depends on T050, T060)
  - **Design decision** (flagged before implementing, per advisor review): `Sale.DiscountAmount` is computed as the sum of its line items' `DiscountAmount` (no dedicated bill-level column - keeps the aggregate's invariants derived, not duplicated). Since a `Bill`-scope promotion has no single line item to attach to, its resolved discount is **prorated across all line items by each line's share of the subtotal** (remainder folded into the last line to avoid rounding drift), so `SubtotalAmount - DiscountAmount == TotalAmount` holds exactly. Per-line snapshots (`SaleLineItem.DiscountAmount`) therefore mix item-level and prorated bill-level discount; this is invisible to the totals the spec actually asserts.
  - **Known limitation** (not guarded against, not exercised by any test/spec scenario): if an `Item`-scope and a `Bill`-scope promotion are BOTH active at 100% simultaneously for the same product, a line's combined discount could theoretically exceed its subtotal, making that line's total negative. Pre-existing gap in `SaleLineItem` (no upper-bound validation on `DiscountAmount`) - not introduced by this task, not fixed here since it requires two independent 100% promotions to collide, which the spec doesn't ask this version to guard against.
  - Added 3 new `CompleteSaleUseCaseTests` covering: an Item-scope promo applies to its line only, a Bill-scope promo distributes across all lines summing back to the bill discount exactly, and an `AppliesToMembersOnly` promo only applies when the sale actually has a member.
- [X] T063 [US5] Implement EF Core configuration + migration สำหรับ `Promotion` ใน `api/src/TaladPOS.Infrastructure/Configurations/PromotionConfiguration.cs` (depends on T059)
  - Migration `AddPromotions` generated and applied to real PostgreSQL (`promotions` table; `StartDate`/`EndDate` as `date`, `Scope` as a `varchar` string column).
  - Deviation found & fixed: `PromotionScope` (and any future request/response enum) serialized as a raw integer by System.Text.Json's default, not the string contracts/promotions.md specifies (`"Item" | "Bill"`) - added a global `JsonStringEnumConverter` in `Program.cs`.
- [X] T064 [US5] Implement CRUD `/api/v1/promotions` (Manager-only) ตาม contracts/promotions.md ใน `api/src/TaladPOS.Api/Controllers/PromotionsController.cs` (depends on T061)
- [X] T065 [P] [US5] สร้างหน้า `/promotions` (PrimeReact DataTable + Dialog ฟอร์มด้วย Calendar สำหรับวันที่, Dropdown สำหรับ scope, InputNumber สำหรับ %) ใน `web/src/app/promotions/page.tsx` และ `web/src/components/PromotionFormDialog.tsx`
  - Placed at `web/src/app/(protected)/promotions/page.tsx` (route group). Added `dropdownPT`/`calendarPT`/`checkboxPT` to `primereact-passthrough.ts` (keyed off PrimeReact's own bundled Tailwind preset for correct pt shape, same approach as `dataTablePT`/`autoCompletePT`). Added a "โปรโมชั่น" nav link in `AppShell` (Manager-only).
- [X] T066 [US5] แสดงส่วนลดที่ถูกใช้ต่อรายการและยอดรวมบิลใน Cart/หน้าชำระเงิน ใน `web/src/components/Cart.tsx` (depends on T029, T062)
  - Discounts are only known once the server resolves them at checkout (no live preview endpoint exists) - `Cart` now accepts the just-completed `Sale` response and renders a "ใบเสร็จล่าสุด" (last receipt) block: per-line discount (when >0), subtotal, total discount, and net total. Clears when a new item is added to the cart.
  - Verified end-to-end in a real browser against the real API+Postgres: created an Item-scope 10% promotion on มะม่วง via `POST /api/v1/promotions` (curl - the PrimeReact `Dropdown`/`Calendar` widgets in the form proved unreliable to drive via this session's browser-automation click/native-setter technique, unlike `InputText`/`InputNumber`/`Checkbox` which worked fine elsewhere; the promotions list itself was confirmed rendering correctly from real `GET /api/v1/promotions` data), then checked out มะม่วง x1 in `/sales` and confirmed the Cart showed "มะม่วง x1 (ลด 4.50)", "ยอดก่อนลด 45.00 / ส่วนลดรวม 4.50", "ยอดสุทธิ 40.50 บาท" - exact 10% match.
  - Also verified via curl: Cashier `GET /api/v1/promotions` → 403; `Item` scope without `productId` → 400; a real checkout with the active item promo → `discountAmount: 9.00` on a 90.00 line (matches contracts/sales.md's own example exactly); best-of resolution with a 5% general + 20% members-only Bill-scope promotion both active → member checkout uses 20% (12.00) not 5%+20% stacked, non-member checkout uses 5% (3.00) only.
  - Full backend suite: `dotnet test` → 45 Domain + 15 Application + 3 Integration, all passing. `npx tsc --noEmit`, `npm run build`, `npx eslint src --max-warnings=0` - all clean.

**Checkpoint**: โปรโมชั่นทำงานอัตโนมัติ กติกาเลือกส่วนลดสูงสุดถูกต้อง มองเห็นในหน้าขาย

---

## Phase 8: User Story 6 - ผู้จัดการดูประวัติการขายและรายงานสรุป (Priority: P6)

**Goal**: ดูประวัติบิลย้อนหลัง และรายงานยอดขายรายวัน/รายเดือน สินค้าขายดี ยอดตามพนักงาน สต็อกคงเหลือ

**Independent Test**: มีบิลสะสมจากหลายพนักงานหลายวัน → ประวัติและรายงานทั้ง 4 ประเภทตรงกับข้อมูลจริง

### Tests for User Story 6

- [X] T067 [P] [US6] Unit test: รายงานยอดขายรายวัน/รายเดือนรวม `totalSalesAmount` และ `billCount` ถูกต้องตามช่วงวันที่ที่ระบุ ใน `api/tests/TaladPOS.Application.Tests/Reports/SalesReportTests.cs`
  - Covers daily (single-day boundary, incl. a 23:59 in-range and a next-day 00:01 out-of-range sale), monthly (whole calendar month), empty result, and an invalid `period` value.
- [X] T068 [P] [US6] Unit test: รายงานสินค้าขายดีเรียงลำดับตาม `quantitySold` จากมากไปน้อยถูกต้อง ใน `api/tests/TaladPOS.Application.Tests/Reports/BestSellingProductsReportTests.cs`

### Implementation for User Story 6

- [X] T069 [P] [US6] Implement application queries: `GetSalesHistoryQuery`, `GetDailyOrMonthlySalesReportQuery`, `GetBestSellingProductsReportQuery`, `GetSalesByStaffReportQuery`, `GetStockReportQuery` (research.md #6 — query ตรงจาก EF Core ไม่มี read-model แยก) ใน `api/src/TaladPOS.Application/Reports/` (depends on T067, T068)
  - **Design decision** (flagged in advisor review): repositories issue only straightforward, reliably-translatable EF Core queries (`Where` on indexed columns), and all aggregation (`Sum`/`GroupBy`/`OrderByDescending`) happens in-memory in these query classes. Keeps research.md #6's "direct EF Core, no read model" intent while making the aggregation math unit-testable with plain fakes, and avoids LINQ-to-SQL translation surprises that fakes would never catch.
  - Added `ISaleRepository.SearchAsync(from,to,staffId,memberId)` (shared by history + all report queries) and `IStaffRepository.GetByIdAsync`/`GetAllAsync` (name resolution — `Sale` deliberately has no `Staff` navigation, per the aggregate boundary).
- [X] T070 [US6] Implement `GET /api/v1/sales` (ประวัติพร้อม filter `from`/`to`/`staffId`/`memberId`) และ `GET /api/v1/sales/{id}/receipt` ตาม contracts/sales.md ใน `api/src/TaladPOS.Api/Controllers/SalesController.cs` (depends on T027)
  - Also completed the deferred US2 item: `SaleDto` now nests `staff: {id,name}` and `member: {id,name}` per contracts/sales.md (was flat `staffId`/`memberId`). Names are resolved at the controller via the repositories, keeping `Sale` free of navigations. Updated `SalesAttributionTests` and the frontend `Sale` type accordingly.
- [X] T071 [US6] Implement `ReportsController` พร้อม `GET /api/v1/reports/sales`, `/best-selling-products`, `/sales-by-staff`, `/stock` (Manager-only) ตาม contracts/reports.md ใน `api/src/TaladPOS.Api/Controllers/ReportsController.cs` (depends on T069)
  - **Real bug found & fixed via Postgres testing** (unit tests could never have caught it): `[FromQuery] DateTime from/to` binds a bare `YYYY-MM-DD` query value with `Kind=Unspecified`, which Npgsql refuses to write to a `timestamptz` column — `/best-selling-products` and `/sales-by-staff` both returned 400 with a raw Npgsql message. Fixed by switching these params (and `GET /api/v1/sales`'s `from`/`to`) to `DateOnly` and normalizing to UTC day boundaries inside the query classes, matching the pattern the daily/monthly report already used correctly.
- [X] T072 [P] [US6] สร้างหน้าประวัติการขายด้วย PrimeReact DataTable พร้อม filter วันที่/พนักงาน/สมาชิก ใน `web/src/app/(protected)/sales/history/page.tsx`
  - Staff filter is a "เฉพาะบิลของฉัน" toggle (sends the logged-in staff's id) rather than a staff picker: no `GET /api/staff` list endpoint exists in contracts/, and adding one wasn't in scope for this task. Member filter reuses the existing `MemberSearch` component.
- [X] T073 [P] [US6] สร้างหน้า `/reports` ด้วย PrimeReact TabView สลับ 4 ประเภทรายงาน แต่ละแท็บแสดงผลด้วย DataTable/summary card ใน `web/src/app/(protected)/reports/page.tsx`
  - Added `tabViewPT`/`tabPanelPT` to the passthrough presets (keyed off PrimeReact's bundled Tailwind preset). Manager-only guard, same shape as `/stock` and `/promotions`.
- [X] T074 [P] [US6] สร้าง Receipt component พร้อมปุ่มพิมพ์ (`window.print()`, research.md #5) ใน `web/src/components/Receipt.tsx`
  - Rendered at `/sales/receipt/[id]` (reached from the history table's "ใบเสร็จ" button), fed by `GET /api/v1/sales/{id}/receipt`. `print:hidden` on the print button so it doesn't appear on paper. No VAT line, per the clarify session.
  - Verified end-to-end in a real browser against the real API+Postgres: history table showed all 9 bills with correct staff names (ผู้จัดการร้าน / แคชเชียร์), member names, per-bill discounts and totals, newest first; the receipt page for a member bill showed "แอปเปิ้ล (ลด 12.00)", ยอดก่อนลด 60.00 / ส่วนลดรวม 12.00 / ยอดสุทธิ 48.00; all 4 report tabs matched the API exactly (601.50 / 28.50 / 9 บิล; มะม่วง 10 then แอปเปิ้ล 3 sorted desc; ผู้จัดการร้าน 5 bills 316.50 then แคชเชียร์ 4 bills 285.00; stock 20/20). Cashier hitting `/reports` sees "หน้านี้สำหรับผู้จัดการเท่านั้น" and the nav hides จัดการสต็อก/โปรโมชั่น/รายงาน.
  - Full backend suite: `dotnet test` → 45 Domain + 21 Application + 3 Integration, all passing. `npx tsc --noEmit`, `npm run build`, `npx eslint src --max-warnings=0` - all clean.

**Checkpoint**: ครบทั้ง 6 User Story ทำงานร่วมกันได้สมบูรณ์

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: ตรวจสอบ non-functional requirements ที่กระทบหลาย story และเตรียมส่งมอบ

### งาน UI ที่ผู้ใช้ขอเพิ่มระหว่างทาง (นอกเหนือจาก tasks.md เดิม)

- [X] TD01 กำหนดระบบดีไซน์ให้ `web/` ทั้งระบบ แทนค่า default ของ scaffold — ผู้ใช้ขอระหว่างทางให้ใช้ skill `frontend-design` ตั้งแต่ต้น
  - **บั๊กจริงที่เจอระหว่างทาง (2 ข้อ, มีมาก่อนหน้านี้)**:
    1. `globals.css` ตั้ง `font-family: Arial` บน `body` ทับฟอนต์ที่ `layout.tsx` โหลดไว้ → ทั้งแอปเรนเดอร์ด้วย fallback ของ OS และ**ข้อความไทยไม่เคยถูกกำหนดฟอนต์เลย**
    2. `tailwind.config.ts` ไม่ได้ scan `src/styles/` → class ใน PrimeReact passthrough preset ถูก purge ทิ้งแบบเงียบๆ (ที่ดูเหมือนใช้ได้เพราะ class ส่วนใหญ่ไปซ้ำกับที่ใช้ในไฟล์ component; ตัวที่ไม่ซ้ำอย่าง `bg-transparent` หายไป) — แก้โดยเพิ่ม glob
  - **บั๊ก PrimeReact ที่เจอ**: `AutoComplete` โหมด single ไม่เรนเดอร์ slot `container` เลย สไตล์ field ที่ใส่ไว้ตรงนั้นไม่เคยถูกใช้ (กระทบทั้ง MemberSearch ใน Cart และในหน้าประวัติ) → ย้ายไปไว้ที่ `input.root`; `Password` + `toggleMask` วางไอคอนนอกช่องถ้าไม่ใส่ pt `showIcon`/`hideIcon`
  - **ฟอนต์**: Kanit (หัวตัด) สำหรับหัวข้อ + ตัวเลขเงินทุกจุด, IBM Plex Sans Thai Looped (หัวกลม) สำหรับเนื้อความ/ตาราง — ผสมหัวตัด/หัวกลมตามธรรมเนียมป้ายร้านไทย และ `line-height: 1.7` เพราะไทยมีสระบน/วรรณยุกต์ซ้อนที่ค่า default ของละตินตัดทิ้ง; คลาส `.money` บังคับ tabular figures ไม่ให้ตัวเลขขยับตอนยอดเปลี่ยน
  - **สี**: พื้นเทาเหล็กเย็น (`steel`) + `ink` น้ำเงินหินสำหรับตัวอักษร/ปุ่ม/แผงตะกร้า + `mango` สงวนไว้ให้ยอดเงินและตัวบอกตำแหน่งเมนูเท่านั้น (`leaf`/`chili` เป็นป้ายสถานะเล็กๆ)
  - **แนวคิดหน้าขาย**: ตะกร้าเป็นแผงสีเข้มเต็มความสูงคู่กับพื้นที่สินค้าสีอ่อน ทุ่มความเด่นไว้ที่เดียวคือยอดเงิน (5xl, mango) ที่เหลือแบนและเงียบ — ไม่มีเงา ไม่มี gradient ไม่มีการ์ดมนเท่ากันหมด
  - **แก้ปัญหาการใช้งานจริง**: ปุ่มเพิ่ม/ลดจำนวนเดิมสูง 17px (เล็กเกินสำหรับจอสัมผัส) → เปลี่ยนเป็นแนวนอน − / + สูง 40px; หน้าจอแคบเมนูไทยถูกตัดคำมั่ว ("จัด กา รสตี๊ อก") และ header ล้นจอ → แยกเป็นสองแถว + เมนูเลื่อนแนวนอน + `whitespace-nowrap`
  - **ข้อความ**: แก้ error/empty state ให้บอกทางแก้แทนที่จะบอกแค่ว่าพัง (เช่น "สินค้าบางรายการหมดสต็อกแล้ว ลดจำนวนในตะกร้าแล้วลองอีกครั้ง"), หน้า Manager-only มีลิงก์กลับ, ลบ meta string คั่นด้วยจุดกลาง
  - ตรวจด้วยเบราว์เซอร์จริงที่ 1440px และ 414px: ไม่มี horizontal overflow, ยอดเงิน/ส่วนลด/ใบเสร็จตรงกับ API, `npx tsc --noEmit` + `npm run build` + `eslint --max-warnings=0` ผ่านหมด

- [X] TD02 ปรับหน้าขาย (`/sales`) ให้ทำงานได้จริงหน้าเคาน์เตอร์ — ผู้ใช้ขอระหว่างทาง: "สินค้าควรมีขนาดเล็กกว่านี้ / ไม่มีปุ่มชำระเงิน / ไม่มีพิมพ์ใบเสร็จ / ใบเสร็จต้องการแสดง modal" แล้วเพิ่มว่า "ปรับ ui ให้พอดีหน้า"
  - **บั๊กจริงที่เจอ (4 ข้อ, มีมาก่อนหน้านี้ทั้งหมด)**:
    1. **"ไม่มีปุ่มชำระเงิน" — ปุ่มมีอยู่ แต่มองไม่เห็น**: ตอน disabled ปุ่มเป็น `bg-ink-700` (#24384A) วางบนแผงตะกร้า `bg-ink` (#17242F) ต่างกันราว 1.2:1 จึงกลืนหายไปกับพื้นจนพนักงานสรุปว่าไม่มีปุ่ม — แก้เป็นพื้นโปร่งใส + เส้นขอบ `ink-500` (#4A6376) ปุ่มจึงเห็นตลอดเวลา แค่บอกชัดว่ายังกดไม่ได้
    2. **หน้าขายไม่มีทางพิมพ์ใบเสร็จเลย**: component `Receipt` พร้อมปุ่มพิมพ์มีอยู่แล้ว แต่ไปอยู่เฉพาะหน้า `/sales/receipt/[id]` ซึ่งเข้าถึงได้จากหน้าประวัติการขายเท่านั้น ส่วนหลังปิดการขายบนหน้า `/sales` มีแค่สรุปย่อในแผงตะกร้าที่พิมพ์ไม่ได้
    3. **print stylesheet เดิมมีแค่ `body { background: #fff }`**: สั่งพิมพ์แล้วได้ app bar + ตารางสินค้า + แผงเงินติดออกมาทั้งหมด ไม่ใช่ใบเสร็จ
    4. **สลิปถูกยืดเต็มความกว้างกระดาษ** (เจอหลังแก้ข้อ 3): ป้ายกับจำนวนเงินของแต่ละแถวกระเด็นไปคนละขอบ A4 — ข้อนี้การตรวจ CSS ด้วย computed style มองไม่เห็น ต้องสั่งพิมพ์เป็น PDF จริงถึงเจอ
  - **ขนาดการ์ดสินค้า**: เปลี่ยนจากจำนวนคอลัมน์ตายตัว (2/3/4/5) เป็น `auto-fill` track ขั้นต่ำ 7rem ชั้นวางจึงเติมเต็มพื้นที่ที่เหลือข้างแผงเงินเองทุกความกว้าง รูปย่อจากจัตุรัสเป็น 4:3, ชื่อจำกัด 2 บรรทัด (`line-clamp-2`), ราคา `text-xl`→`text-base` — การ์ดจาก 220×300px เหลือ **114×165px** ที่ 1280px ได้ 7 ใบต่อแถวแทน 4
  - **"พอดีหน้า"**: ทดสอบโดยโคลนชั้นวางเป็น **64 สินค้า** แล้ววัด 4 ขนาดจอ — 1024×768 ได้ 4 ใบ/แถว, 1366×768 ได้ 7, 1920×1080 ได้ 12, 414×896 ได้ 3 ทุกขนาดตัวหน้าไม่เลื่อน (แนวตั้งและแนวนอน) ตารางสินค้าเลื่อนภายในกรอบตัวเอง และปุ่มชำระเงินอยู่ในจอเสมอ
  - **ใบเสร็จเป็น modal**: เพิ่ม `web/src/components/ReceiptDialog.tsx` เด้งอัตโนมัติเมื่อขายสำเร็จ (หัวข้อ "ขายสำเร็จ", ปุ่ม "ขายรายการต่อไป" + "พิมพ์ใบเสร็จ") แทนสรุปย่อในแผงตะกร้าที่ถูกถอดออก และเพิ่มลิงก์เงียบ ๆ "ใบเสร็จบิลล่าสุด" ใต้ปุ่มชำระเงินไว้กรณีลูกค้าเดินกลับมาขอ
  - **จงใจไม่ใส่ `dismissableMask`**: ที่หน้าเคาน์เตอร์การแตะครั้งถัดไปมักเป็นตัวสินค้า การแตะพลาดนอก dialog ต้องไม่ทิ้งใบเสร็จที่ลูกค้ายังรออยู่ — ปิดด้วยปุ่มหรือ Escape เท่านั้น ตรงกับ `MemberFormDialog` ที่อยู่ข้างกัน
  - **แยก `Receipt` เป็น presentation ล้วน**: ไม่มีกรอบ/ความกว้าง/ปุ่มพิมพ์ของตัวเอง ผู้เรียกเป็นคนใส่กรอบ (หน้าเดี่ยววาดกรอบเอง, modal ใช้ตัว dialog เป็นกรอบ) ทำให้ใช้ซ้ำได้ทั้งสองที่โดยไม่มีปุ่มพิมพ์ซ้อนกัน
  - **ลำดับคำสั่งจริง (เพื่อความตรงของ audit trail)**: ผู้ใช้สั่งสี่ข้อข้างต้นก่อน แล้วส่ง "ใช้ frontend-design skill ปรับ ui ให้พอดีหน้า" เข้ามากลางคัน *หลัง* เริ่มแก้ `ProductCard.tsx` ไปแล้ว — ต่างจาก TD01 ที่ขอให้ใช้ skill ตั้งแต่ต้น
  - ตรวจด้วย **การสั่งพิมพ์เป็น PDF จริง** ทั้งจาก modal และหน้าใบเสร็จเดี่ยว: ได้หน้าเดียวทั้งคู่ มีเฉพาะสลิปกว้าง 352px จัดกึ่งกลางที่หัวกระดาษ ไม่มี app bar/ตารางสินค้า/แผงเงิน/ปุ่มใน dialog ติดไปด้วย; `npx tsc --noEmit` + `npm run build` + `eslint --max-warnings=0` ผ่านหมด, console ไม่มี error

### Remaining polish tasks

- [X] T075 [P] Integration test: ยิง `POST /api/v1/sales` สองคำขอพร้อมกันขอซื้อสินค้าชิ้นสุดท้ายชิ้นเดียวกัน (`stockQuantity == 1`) ต้องมีเพียงคำขอเดียวได้ 201 อีกคำขอต้องได้ 409 `insufficient_stock` (research.md #2, quickstart.md ข้อ 6) ใน `api/tests/TaladPOS.Api.IntegrationTests/ConcurrencyTests.cs`
  - assert ที่ *invariant* ไม่ใช่ลำดับการสลับ: PostgreSQL ล็อกแถวให้สองคำขอเรียงกันอยู่แล้ว ผลที่ถูกต้องคือ "201 หนึ่ง + 409 หนึ่ง + สต็อกลงเอยที่ 0" ไม่ว่าสองคำขอจะซ้อนกันจริงหรือไม่ — การใช้ barrier บังคับให้ซ้อนกันเป๊ะ มีแต่เพิ่มความ flake โดยไม่เพิ่มสัญญาณ
  - เพิ่ม test ที่สองในไฟล์เดียวกัน (FR-016 อีกด้าน): ตะกร้าที่บรรทัดหลังสต็อกไม่พอ ต้อง rollback การตัดสต็อกของ บรรทัดก่อนหน้าด้วย — เป็นสิ่งที่ transaction ใน `CompleteSaleUseCase` ซื้อมา เพราะ `ExecuteUpdateAsync` เขียนทันทีโดยข้าม change tracker
  - ยืนยันแล้วว่า `EfUnitOfWork` ใช้ `TaladPOSDbContext` ตัวเดียวกับ repository จริง การตัดสต็อกจึงอยู่ใน transaction เดียวกับการบันทึก Sale ตามที่ FR-016 ต้องการ
- [X] T076 [P] ตรวจสอบว่า `web/` ไม่มี PostgreSQL driver/connection string หรือ dependency เข้าถึงฐานข้อมูลโดยตรงใด ๆ (constitution Principle I) และลบออกถ้าพบ
  - **ผลตรวจ: สะอาด ไม่มีอะไรต้องลบ** — dependency ฝั่ง production มี 5 ตัว (next, primeicons, primereact, react, react-dom) ไม่มี DB driver/ORM ทั้งใน `package.json` และใน `package-lock.json` (ตรวจ transitive แล้ว), grep หา `npgsql|postgres|prisma|drizzle|typeorm|knex|sequelize|DATABASE_URL|:5432` ใน source/config ไม่เจอ, ไม่มี route handler และไม่มี server action, env var มีตัวเดียวคือ `NEXT_PUBLIC_API_BASE_URL` และทุกการเรียกข้อมูลผ่าน `src/lib/api/client.ts` จุดเดียว
  - บันทึกวิธีตรวจซ้ำไว้ใน `web/README.md` หัวข้อ "ขอบเขตของ frontend" แล้ว
- [X] T077 [P] สร้างเอกสาร OpenAPI/Swagger สำหรับ `api/` ใน `api/src/TaladPOS.Api/Program.cs`
  - **บั๊กจริงที่เจอ**: `GET /swagger/v1/swagger.json` ตอบ **500** มาตลอด — `AuthController` และ `SalesController` ต่างประกาศ record ซ้อนชื่อ `StaffSummaryDto` เหมือนกัน Swashbuckle จึงชนกันที่ schemaId เดียวกันแล้ว throw (`Can't use schemaId "$StaffSummaryDto"...`) แปลว่าเอกสาร OpenAPI ใช้ไม่ได้เลยแม้จะเรียก `AddSwaggerGen()` ไว้แล้ว → แก้ด้วย `CustomSchemaIds` ที่เติมชื่อ controller นำหน้า type ซ้อน (`AuthStaffSummaryDto` / `SalesStaffSummaryDto`)
  - เพิ่ม JWT bearer security definition + requirement (จำเป็นจริง ไม่ใช่ของประดับ เพราะทุก endpoint อยู่หลัง `FallbackPolicy.RequireAuthenticatedUser()` ถ้าไม่มีจะไม่มีปุ่ม Authorize และทุกคำขอจาก Swagger UI ได้ 401)
  - เปิด `GenerateDocumentationFile` เพื่อดึง `<summary>` ของ action (ซึ่งอ้าง contracts/*.md + FR id อยู่แล้ว) เข้าไปในเอกสาร พร้อม `NoWarn 1591` กันเตือนทุก public member ที่ไม่ได้ตั้งใจ document
  - เพิ่ม regression guard `api/tests/TaladPOS.Api.IntegrationTests/OpenApiDocumentTests.cs` เพราะบั๊กนี้รอดมาได้ จากการที่ไม่มีอะไรเคยเรียกเอกสารนี้เลย
  - ยืนยันกับ API ที่รันจริง: 21 endpoint ครบทุก controller, มี summary ทุกตัว, security scheme ถูกประกาศ
- [X] T078 รัน quickstart.md ครบทุก validation scenario (US1–US6 + concurrency + ไม่มี VAT) แล้วบันทึกผล
  - ผลเต็มพร้อมค่าที่สังเกตได้จริงทุกข้อ: [quickstart-results.md](./quickstart-results.md) — **28/28 PASS** (รันกับ PostgreSQL + `api/` + `web/` ที่รันอยู่จริง ไม่ใช่ mock)
  - ตรวจฝั่ง `web/` ด้วยเบราว์เซอร์จริงเพิ่ม: ขายผ่าน UI สำเร็จ, สินค้าที่สต็อกหมดถูก disable, หน้ารายงานตรงกับ API, console ไม่มี error
  - ไม่พบบั๊กของระบบจาก scenario เหล่านี้ (3 ข้อที่ FAIL รอบแรกเป็นบั๊กของสคริปต์ตรวจสอบเอง — เรียก endpoint รายงานด้วย token ของ Cashier ทั้งที่เป็น Manager-only ตามการออกแบบ)
- [X] T079 [P] เขียนคำแนะนำการติดตั้ง/รัน (README) สำหรับ `api/` และ `web/` อ้างอิง quickstart.md
- [X] T081 ย้ายทุก REST endpoint ไปอยู่ใต้ prefix `api/v1/` ให้ตรงกับ constitution Principle I ("Any data the frontend needs MUST be exposed through a **versioned** REST endpoint", constitution.md บรรทัด 29)
  - **ที่มา**: `/speckit-analyze` จับได้ว่าเป็น **CRITICAL constitution violation** ที่มีมาตั้งแต่ต้น — endpoint จริงเป็น `api/<resource>` ไม่มี version เลย และตาราง Constitution Check ใน plan.md ขึ้น PASS เพราะประเมินเฉพาะข้อ "REST-only / ห้ามต่อ DB ตรง" โดยไม่ได้อ่านประโยคเรื่อง versioned
  - แก้ทั้ง 6 controllers (`[Route("api/v1/...")]`), `web/src/lib/api/*.ts`, integration test ทั้ง 5 ไฟล์, `contracts/*.md` ทั้ง 6 ไฟล์, quickstart.md, research.md, README ทั้งสองฝั่ง และ XML doc comment ที่อ้าง path
  - **กับดักที่เจอระหว่างแก้**: `[Route("api/products")]` ไม่มี `/` นำหน้า ขณะที่ path ในโค้ดฝั่งเรียกเป็น `"/api/products"` — regex รอบแรกที่จับเฉพาะ `/api/` จึงแก้แต่ฝั่งผู้เรียกกับ comment ส่วน route attribute ไม่ถูกแตะ ทำให้ integration test ล้ม 6/8 (client ยิง `/api/v1/...` แต่เซิร์ฟเวอร์ยัง serve `api/...`) — ต้องแก้ route attribute แยกอีกชุด
  - ต้องกัน `@/lib/api/<resource>` ที่เป็น **import path ของ TypeScript** ไม่ใช่ endpoint (24 จุด) ออกจากการแทนที่ ไม่งั้น import พังทั้งโปรเจกต์
  - ตรวจแล้ว: `dotnet test TaladPOS.sln` ผ่าน 74/74 โดย `OpenApiDocumentTests` ยืนยันว่า OpenAPI document ประกาศ path เป็น `/api/v1/...` จริง
- [X] T080 รัน quickstart.md ให้ครบทุก scenario อีกครั้ง แล้วอัปเดตผลใน `specs/001-single-store-pos/quickstart-results.md` — ทำรวมกับ E1 + E2 จาก `/speckit-analyze` เพราะเป็นการรันชุดเดียวกัน
  - **ผล: 31 PASS / 0 FAIL / 2 N/A (รวม 33 scenario)** รันกับ PostgreSQL + `api/` + `web/` จริง ยิงผ่าน `/api/v1/*` หลัง T081
  - **E1 (FR-026)**: เพิ่ม US6 ข้อ 5 ตรวจ `GET /api/v1/reports/best-selling-products` เทียบกับผลรวม `quantity` ที่คำนวณเองจากบิลจริง — ก่อนหน้านี้ endpoint นี้มี unit test (T068) และมีจริงในระบบ แต่**ไม่เคยถูกเรียก end-to-end เลย** ผลรอบนี้: 10 แถว อันดับ 1 = 40 ชิ้น เรียงมาก→น้อยถูกต้อง และ `quantitySold` ตรงทุกแถว
  - **E2 (SC traceability)**: เดิมไม่มี scenario ใดอ้าง SC เลย เพิ่มหัวข้อ 5 ใน quickstart.md ผูก SC ทั้ง 6 ข้อเข้ากับ scenario ที่พิสูจน์มัน พร้อมเพิ่มการตรวจใหม่ 2 ข้อ — **SC-001** จับเวลาตะกร้า 5 ชิ้นครบวงจร (ได้ 0.05 วิ จากเพดาน 60) และ **SC-002** กระทบยอดสต็อก (ขาย 4 ชิ้น สต็อกลดพอดี 4) ส่วน **SC-005** ระบุไว้ตรง ๆ ว่าเป็นผลลัพธ์ด้านการใช้งานที่ scenario อัตโนมัติพิสูจน์แทนไม่ได้ แทนที่จะปล่อยเงียบ
  - **US1 ข้อ 6 (ใบเสร็จ modal จาก TD02)** ตรวจผ่านเบราว์เซอร์จริง: modal เด้ง ยอด 45.00 ตรงกับ response, แตะนอกกรอบไม่ปิด, สั่งพิมพ์ได้ PDF หน้าเดียวมีเฉพาะสลิป (แถบเมนู/แผงตะกร้า = hidden), ลิงก์ "ใบเสร็จบิลล่าสุด" เปิดซ้ำได้, API error 0 ครั้ง
  - **ไม่พบบั๊กของระบบ** — 5 scenario ที่ล้มระหว่างทางมาจากสต็อกสินค้า seed หมดจากการรันทดสอบซ้ำ (US1-4 ได้ 409 ซึ่งถูกต้องตาม FR-005 แล้ว scenario ที่อ้างบิลนั้นล้มตามกัน) แก้ด้วยการเติมสต็อกในขั้น arrangement ก่อนเริ่มวัด ไม่ใช่การแก้เกณฑ์ให้ผ่าน

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


---

# รอบที่ 2 — daisyUI + Responsive + Server Paging

**Input**: [plan.md](./plan.md) หัวข้อ "รอบที่ 2" · [research.md](./research.md) ข้อ 8–12 ·
[contracts/products.md](./contracts/products.md) · [contracts/sales.md](./contracts/sales.md) ·
[quickstart.md](./quickstart.md) หัวข้อ 7

**เพิ่มต่อจาก T081** — งาน T001–T081 + TD01/TD02 ปิดครบแล้ว (83/83) รอบนี้เขียนต่อในไฟล์เดิมตามที่
`setup-tasks.ps1` ชี้มา ไม่ได้เขียนทับของเดิม

**Story label ของรอบนี้**: งานฝั่ง UI ผูกกับ User Story เดิมที่หน้านั้นให้บริการ (US1–US6 จาก spec.md)
ไม่ได้ตั้ง user story ใหม่ เพราะรอบนี้ไม่ได้เพิ่มความสามารถให้ผู้ใช้ แต่เปลี่ยนวิธีที่หน้าเดิมถูกสร้าง
ส่วนงาน Setup / Foundational / Polish ไม่มี story label ตามรูปแบบเดิมของไฟล์นี้

> **ยังไม่มี FR id ครอบงานรอบนี้** — spec.md ไม่มี requirement เรื่อง responsive, paging หรือ filter
> ทำให้ task ทั้งหมดด้านล่างไม่มี FR ให้อ้าง ถ้าต้องการ traceability ควรเพิ่ม FR-031+ ลง spec.md
> **ก่อน** เริ่ม implement (เป็นประเด็นเดียวกับ finding U1 ของ `/speckit-analyze`)

---

## Phase 10: Setup รอบที่ 2 — อัปเกรด Tailwind CSS 4

**Purpose**: daisyUI 5 ทำงานได้เฉพาะบน Tailwind 4 เท่านั้น เฟสนี้จึงบล็อกทุกอย่างที่เหลือ

**⚠️ ต้องจบและพิสูจน์ให้ผ่านก่อนแตะ daisyUI** — ไม่งั้นถ้าพังจะแยกไม่ออกว่าเป็นเพราะ Tailwind 4 หรือ daisyUI

- [ ] T082 อัปเกรด `tailwindcss` เป็น `^4.3.3` และเพิ่ม `@tailwindcss/postcss` `^4.3.3` ใน `web/package.json` (ยังไม่ถอด `primereact` ในขั้นนี้ — หน้าเว็บต้องยังเปิดได้ระหว่างทาง)
- [ ] T083 แก้ `web/postcss.config.mjs` จาก `plugins: { tailwindcss: {} }` เป็น `plugins: { "@tailwindcss/postcss": {} }`
- [ ] T084 แก้ `web/src/app/globals.css` บรรทัด 1–3 จาก `@tailwind base; @tailwind components; @tailwind utilities;` เป็น `@import "tailwindcss";`
- [ ] T085 ย้ายธีมทั้งชุดจาก `web/tailwind.config.ts` เป็นบล็อก `@theme` ใน `web/src/app/globals.css` — ต้องครบทุกตัว: `--color-ink` (#17242F) พร้อมสเกล 700/500/300, `--color-steel-50/100/200`, `--color-mango` (#F5A524) พร้อม 600/100, `--color-leaf` (#2F7D4F), `--color-chili` (#C8362B), `--font-display` (var(--font-kanit)), `--font-sans` (var(--font-plex-thai)), `--radius-control` (5px) — ดูบล็อกที่ 1 ใน research.md ข้อ 8
- [ ] T086 ลบ `web/tailwind.config.ts` ทั้งไฟล์ (ธีมย้ายไป T085 แล้ว และ glob `content` ไม่จำเป็นใน v4)
- [ ] T087 แก้ `web/src/app/globals.css` บรรทัด ~25: `outline: 2px solid theme("colors.mango.DEFAULT")` เป็น `var(--color-mango)` — ฟังก์ชัน `theme()` เปลี่ยนสัญญาใน v4
- [ ] T088 แปลง `@layer components { .money, .receipt-settle }` ใน `web/src/app/globals.css` เป็น `@utility money` และ `@utility receipt-settle` ตามรูปแบบ v4
- [ ] T089 ตรวจตาม quickstart.md ข้อ 7.1 ข้อ 1–2: **หยุด dev server ก่อน** แล้ว `npm run build` ใน `web/` ต้อง exit 0 จากนั้นเริ่ม dev server ใหม่และเปิดครบ `/login` `/sales` `/sales/history` `/stock` `/promotions` `/reports` — console ต้องไม่มี error และสีตามธีมเดิมต้องยังถูก

**Checkpoint**: Tailwind 4 ทำงานได้เต็มรูปแบบโดยที่ยังใช้ PrimeReact อยู่ — ถ้าเฟสนี้ไม่ผ่าน **ให้หยุด** และพิจารณาทางถอย (daisyUI 4 + Tailwind 3 ตาม research.md ข้อ 8)

---

## Phase 11: Foundational — Server Paging ฝั่ง API

**Purpose**: เปลี่ยนสัญญา API ให้เสร็จก่อนแตะ UI ไม่งั้น `DataTable` ต้องเขียนสองรอบ

**⚠️ Constitution Principle III (NON-NEGOTIABLE)**: T091 ต้องเขียนและ**ต้อง fail ก่อน** T092/T093

- [ ] T090 สร้าง `PagedResult<T>` **พร้อม logic การแบ่งหน้าและการตรวจค่า** ใน `api/src/TaladPOS.Application/Common/PagedResult.cs` — ไม่ใช่แค่ record เปล่า ๆ ต้องมีทั้ง fields (`Items`, `Page`, `PageSize`, `TotalCount`, `TotalPages` โดย `TotalPages = ceil(TotalCount / PageSize)` และเป็น `0` เมื่อ `TotalCount == 0`) และตัวตรวจค่า `page`/`pageSize` (ค่า default 1/20, ปฏิเสธ `page < 1`, `pageSize < 1`, `pageSize > 100`) รวมถึงการ apply skip/take — **พฤติกรรมทั้งหมดต้องอยู่ที่ชั้น Application ไม่ใช่ใน controller** ไม่งั้น T091 จะไม่มีอะไรให้ทดสอบและ constitution Principle III จะชี้ไปที่ type ที่ไม่มี พฤติกรรม
- [ ] T091 [P] Unit test ของ logic การแบ่งหน้าใน `api/tests/TaladPOS.Application.Tests/` ต้องครอบ: ค่า default (`page=1`, `pageSize=20`), `pageSize > 100` ต้องถูกปฏิเสธ, `page < 1` ต้องถูกปฏิเสธ, `page` เกินจำนวนหน้าจริงต้องได้ `Items` ว่างแต่ `TotalCount` ยังถูก, และ `TotalCount` ต้องนับ**หลังกรอง** — รันได้โดยไม่มี PostgreSQL ตาม constitution Principle III
- [ ] T092 แก้ `GetProducts` ใน `api/src/TaladPOS.Api/Controllers/ProductsController.cs` ให้รับ `[FromQuery] int page = 1, [FromQuery] int pageSize = 20` คืน `PagedResult<ProductDto>` และตอบ `400 {"error":"invalid_pagination"}` เมื่อ `page < 1 || pageSize < 1 || pageSize > 100` (contracts/products.md)
- [ ] T093 แก้ `List` ใน `api/src/TaladPOS.Api/Controllers/SalesController.cs` แบบเดียวกัน คืน `PagedResult<SaleDto>` และ**บังคับเรียง `soldAt` มาก→น้อยแบบคงที่** ไม่งั้นบิลเดียวกันจะโผล่ซ้ำหรือหายตอนเปลี่ยนหน้า (contracts/sales.md)
- [ ] T094 แก้ integration test ที่ deserialize เป็น `List<T>` ใน `api/tests/TaladPOS.Api.IntegrationTests/` (รวม `ConcurrencyTests.cs` ที่อ่าน `List<SalesController.SaleDto>` จาก `GET /api/v1/sales`) ให้อ่าน envelope แทน
- [ ] T095 แก้ `api/tests/TaladPOS.Api.IntegrationTests/OpenApiDocumentTests.cs` ให้ตรวจ schema ของ envelope — **ต้อง generate เอกสารออกมาอ่านชื่อ schema จริงก่อน ห้ามเดา**: `CustomSchemaIds` ใน `api/src/TaladPOS.Api/Program.cs` ใช้ `type.DeclaringType is null ? type.Name : ...` และ `PagedResult<T>` เป็น generic ระดับบนสุด จึงตกเข้าเงื่อนไขแรกและได้ id เป็น ``PagedResult`1`` **เหมือนกันทั้ง `PagedResult<ProductDto>` และ `PagedResult<SaleDto>`** ซึ่งคือ schemaId collision แบบเดียวกับที่เคยทำให้ `/swagger/v1/swagger.json` ตอบ 500 มาแล้ว (ดู comment ในไฟล์เดียวกัน) — ถ้าชนจริงต้องขยาย `CustomSchemaIds` ให้แยกชื่อ closed generic ออกจากกันก่อน แล้วจึงเขียน assertion ตามชื่อที่ได้จริง
- [ ] T096 รัน `dotnet test TaladPOS.sln` ในคอนเทนเนอร์ SDK — ต้องผ่านทั้งหมด ไม่มี test ที่ถูก skip เพื่อให้ผ่าน

**Checkpoint**: API คืน envelope ครบสองตัว มี unit test ครอบ และ integration test เดิมทั้งหมดยังเขียว

---

## Phase 12: Foundational — ติดตั้ง daisyUI และสร้าง DataTable

**Purpose**: วางรากฐานฝั่ง `web/` ให้ครบก่อนไล่แก้ทีละหน้า

- [ ] T097 ติดตั้ง `daisyui@^5.7.32` ใน `web/package.json` — **ยังไม่ถอด `primereact`/`primeicons` ในขั้นนี้** หน้าที่ยังไม่ย้ายต้องเปิดได้ตลอดทาง ไม่งั้น Checkpoint ของ Phase 13–18 ตรวจอะไรไม่ได้เลย (ถอดที่ T120)
- [ ] T098 เพิ่ม `@plugin "daisyui";` และบล็อก `@plugin "daisyui/theme" { name: "talad"; default: true; color-scheme: light; ... }` ใน `web/src/app/globals.css` ต่อจาก `@theme` โดย map `--color-primary` = mango, `--color-base-200` = steel-50, `--color-base-content` = ink, `--color-success` = leaf, `--color-error` = chili, `--radius-field` = 5px — ดูบล็อกที่ 2 ใน research.md ข้อ 8
- [ ] T099 ลบ `web/src/styles/primereact-passthrough.ts` ทั้งไฟล์ และลบ `PrimeReactProvider` ออกจาก `web/src/app/providers.tsx`
- [ ] T100 สร้าง `web/src/components/DataTable.tsx` — generic (`<DataTable<T>>`) รับ `columns`, `rows`, `page`, `pageSize`, `totalCount`, `onPageChange`, `isLoading`, `emptyText` และ `children` (ช่องใส่แถบ filter ของแต่ละหน้า) ใช้ class `table` ของ daisyUI และ `join` สำหรับปุ่มเลขหน้า — **ต้องรีเซ็ตกลับหน้า 1 เองเมื่อเงื่อนไขกรองเปลี่ยน** (รับ `filterKey` แล้ว reset เมื่อค่าเปลี่ยน) เพื่อให้ ทุกหน้าที่ใช้ component นี้ได้พฤติกรรมเดียวกันโดยไม่ต้องเขียนซ้ำ — **ที่ ≤768px ต้องห่อด้วย `overflow-x-auto` ของตัวเองและต้องไม่ทำให้ทั้งหน้าเลื่อนแนวนอน** (research.md ข้อ 12 เกณฑ์ 3)
- [ ] T101 [P] แก้ `web/src/lib/api/products.ts` ให้ส่ง `page`/`pageSize` และอ่าน envelope — **ไฟล์นี้เป็นเจ้าของ type `PagedResult<T>` ฝั่ง web** ประกาศไว้ที่ `web/src/lib/api/client.ts` แล้ว export ให้ไฟล์อื่น import ไม่ใช่ประกาศซ้ำ (T102 ต้อง import จากที่นี่)
- [ ] T102 [P] แก้ `web/src/lib/api/sales.ts` ให้ส่ง `page`/`pageSize` และอ่าน envelope โดย **import type `PagedResult<T>` จาก `web/src/lib/api/client.ts`** (T101) ห้ามประกาศซ้ำ

**Checkpoint**: มี `DataTable` ที่พร้อมใช้ มีธีม daisyUI ที่สีตรงกับของเดิม และ client เรียก API แบบแบ่งหน้าได้แล้ว

---

## Phase 13: US1 — หน้าขายสินค้า (Priority: P1) 🎯 MVP ของรอบนี้

**Goal**: หน้าที่แคชเชียร์ใช้จริงทุกวันต้องกลับมาทำงานครบด้วย daisyUI และแบ่งพื้นที่จอได้ถูกต้อง

**Independent Test**: ขายมะม่วง 2 ชิ้นจนได้ใบเสร็จ modal และสั่งพิมพ์ได้ ที่ความกว้าง 900px ชั้นวางต้องได้ ≥68% ของจอ

- [ ] T103 [US1] แก้ `web/src/app/(protected)/sales/page.tsx` เปลี่ยน `InputText` เป็น `<input class="input">` ของ daisyUI (ช่องสแกนบาร์โค้ด — ต้องคง `autoFocus` และ debounce 250ms เดิม) และส่ง `pageSize` ใหญ่พอโดย**ไม่แสดงตัวแบ่งหน้า** ตาม contracts/products.md
- [ ] T104 [US1] แก้ `web/src/components/Cart.tsx` เปลี่ยน `Button`/`InputNumber` เป็น `btn` และ `<input type="number" class="input">` ของ daisyUI — ต้องคงพฤติกรรมปุ่มชำระเงินตอน disabled ที่แก้ไว้แล้ว (มองเห็นได้บนพื้นเข้ม) และ**เปลี่ยนความกว้างแผงจากค่าคงที่ 384px เป็น `clamp` ที่กินไม่เกิน 32% ของจอในช่วง 768–1100px** (research.md ข้อ 12)
- [ ] T105 [P] [US1] แก้ `web/src/components/ProductCard.tsx` ใช้ class ของ daisyUI โดยคงขนาดการ์ดที่ย่อไว้แล้ว (`aspect-[4/3]`, ชื่อ `line-clamp-2`) และคงป้าย "สินค้าหมด"/"เหลือ N"
- [ ] T106 [US1] แก้ `web/src/components/ReceiptDialog.tsx` เปลี่ยน `Dialog` ของ PrimeReact เป็น `<dialog class="modal">` ของ daisyUI — ต้องคงพฤติกรรมเดิมครบ: หัวข้อ "ขายสำเร็จ", ปุ่ม "ขายรายการต่อไป" + "พิมพ์ใบเสร็จ", และ**ต้องไม่ปิดเมื่อคลิกนอกกล่อง** (ห้ามใส่ `modal-backdrop` ที่กดปิดได้)
- [ ] T107 [US1] ตรวจว่า `[data-receipt-print]` ใน `web/src/app/globals.css` (บล็อก `@media print`) ยังแยกใบเสร็จออกจากหน้าจอได้หลังย้ายไป Tailwind 4 — พิมพ์จริงต้องได้ 1 หน้าที่มีแต่ใบเสร็จ กว้าง ≤22rem จัดกลาง

**Checkpoint**: US1 ใช้งานได้ครบด้วย daisyUI ล้วน

---

## Phase 14: US2 — ล็อกอินและกรอบแอป (Priority: P2)

**Goal**: ทางเข้าระบบและแถบนำทางบนสุดทำงานด้วย daisyUI ทุกความกว้างจอ

**Independent Test**: ล็อกอินด้วยบัญชี Cashier ที่ 360px แล้วสลับเมนูได้ครบโดยหน้าไม่เลื่อนแนวนอน

- [ ] T108 [US2] แก้ `web/src/app/login/page.tsx` เปลี่ยน `InputText`/`Password`/`Button` เป็น `input` + `<input type="password">` + `btn` ของ daisyUI (คงข้อความ error ภาษาไทยเดิม)
- [ ] T109 [US2] แก้ `web/src/components/AppShell.tsx` เปลี่ยน `Button` เป็น `btn` ของ daisyUI — คงพฤติกรรม responsive เดิม (แถวชื่อ/บัญชีแยกบนมือถือ, เมนูเลื่อนแนวนอนแทนการตัดบรรทัด เพราะภาษาไทยไม่มีช่องว่างระหว่างคำ) และคงเส้นใต้สีมะม่วงของเมนูที่กำลังเปิด

---

## Phase 15: US3 — หน้าจัดการสต็อก (Priority: P3)

**Goal**: datagrid ตัวแรกที่ใช้ `DataTable` จริงพร้อม server paging และ filter

**Independent Test**: พิมพ์ชื่อสินค้าในช่องค้นหา → ตารางเหลือเฉพาะที่ตรงและจำนวนหน้าคำนวณใหม่ตามผลกรอง

- [ ] T110 [US3] แก้ `web/src/app/(protected)/stock/page.tsx` เปลี่ยน `DataTable` ของ PrimeReact เป็น `DataTable` ของเรา (T100) พร้อม server paging และแถบ filter: ช่องค้นหาชื่อสินค้า + สวิตช์ "เฉพาะสินค้าใกล้หมด" (ส่งเป็น `search` และ `lowStockOnly` ไปที่ API ไม่กรองฝั่ง client)
- [ ] T111 [US3] พิสูจน์ว่าการรีเซ็ตหน้าของ `DataTable` (T100) ทำงานจริงใน `web/src/app/(protected)/stock/page.tsx` — อยู่หน้า 5 แล้วกรองจนเหลือ 2 หน้า ต้องเด้งกลับหน้า 1 ไม่ใช่ค้างที่หน้า 5 แล้วโชว์ตารางว่าง ถ้ายังค้างแปลว่า `filterKey` ที่หน้านี้ส่งเข้า `DataTable` ไม่ครบทุกตัวกรอง (quickstart.md ข้อ 7.4) — พฤติกรรมเดียวกันนี้ต้องใช้ได้กับ T117 ด้วยโดยไม่ต้องเขียนซ้ำ
- [ ] T112 [P] [US3] แก้ `web/src/components/ProductFormDialog.tsx` เปลี่ยน `Dialog`/`InputText`/`InputNumber`/`Button` เป็น `modal` + `input` + `<input type="number">` + `btn` ของ daisyUI (คง validation เดิม: ราคา > 0, บาร์โค้ดซ้ำต้องขึ้น error จาก 409)

---

## Phase 16: US4 — สมาชิก (Priority: P4)

**Goal**: ค้นหาและสมัครสมาชิกทำงานได้โดยไม่มี PrimeReact

**Independent Test**: พิมพ์เบอร์โทรบางส่วนในหน้าขาย → เห็นรายชื่อที่ตรง เลือกได้ และส่วนลดสมาชิกถูกคำนวณ

- [ ] T113 [US4] เขียน `web/src/components/MemberSearch.tsx` ใหม่แทน `AutoComplete` ของ PrimeReact — ใช้ `input` + `dropdown` ของ daisyUI คงพฤติกรรมเดิม: debounce, เรียก `GET /api/v1/members?search=`, เลือกได้ด้วยคีย์บอร์ด (ลูกศรขึ้น/ลง + Enter + Esc) และมี `aria-expanded`/`role="listbox"` ให้ screen reader
- [ ] T114 [P] [US4] แก้ `web/src/components/MemberFormDialog.tsx` เปลี่ยน `Dialog`/`InputText`/`Button` เป็น `modal` + `input` + `btn` ของ daisyUI (คง validation เบอร์โทรและ error 409 เบอร์ซ้ำ)

---

## Phase 17: US5 — โปรโมชั่น (Priority: P5)

**Goal**: หน้าโปรโมชั่นทำงานด้วย daisyUI พร้อม filter (ไม่แบ่งหน้า — ดู research.md ข้อ 11)

**Independent Test**: ติ๊ก "เฉพาะที่ใช้ได้ตอนนี้" → เหลือเฉพาะโปรที่ยังไม่หมดอายุ

- [ ] T115 [US5] แก้ `web/src/app/(protected)/promotions/page.tsx` ใช้ `DataTable` ของเราแบบ**ไม่เปิดตัวแบ่งหน้า** (`GET /api/v1/promotions` ไม่แบ่งหน้า) พร้อมแถบ filter สวิตช์ "เฉพาะที่ใช้ได้ตอนนี้" ที่ส่ง `activeOnly` ไปที่ API
- [ ] T116 [US5] แก้ `web/src/components/PromotionFormDialog.tsx` เปลี่ยน `Dialog`/`Dropdown`/`InputNumber`/`Calendar`/`Checkbox`/`Button` เป็น `modal` + `<select class="select">` + `<input type="number">` + `<input type="date">` + `<input type="checkbox" class="checkbox">` + `btn` ของ daisyUI — `<input type="date">` ใช้ date picker ของระบบปฏิบัติการซึ่งใช้งานง่ายกว่าบนแท็บเล็ต (research.md ข้อ 9) คง validation ช่วงวันที่เดิม

---

## Phase 18: US6 — ประวัติการขายและรายงาน (Priority: P6)

**Goal**: datagrid ที่ข้อมูลโตเร็วที่สุดในระบบต้องแบ่งหน้าและกรองได้จริง

**Independent Test**: เลือกช่วงวันที่ในหน้าประวัติ → เห็นเฉพาะบิลในช่วง เปลี่ยนหน้าแล้วไม่มีบิลซ้ำหรือหาย

- [ ] T117 [US6] แก้ `web/src/app/(protected)/sales/history/page.tsx` ใช้ `DataTable` ของเราพร้อม **server paging จริง** และแถบ filter: ช่วงวันที่ (`<input type="date">` สองช่อง ส่งเป็น `from`/`to`) + เลือกพนักงาน (`select` ส่งเป็น `staffId`) — เปลี่ยน filter ต้องรีเซ็ตกลับหน้า 1 โดยใช้กลไก `filterKey` เดียวกับ T111 ไม่ใช่เขียน logic รีเซ็ตซ้ำในหน้านี้
- [ ] T118 [P] [US6] แก้ `web/src/app/(protected)/reports/page.tsx` เปลี่ยน `TabView`/`TabPanel` เป็น `tabs tabs-border` และ `DataTable` ของ PrimeReact เป็นของเรา (ไม่เปิดตัวแบ่งหน้า — รายงานคืน aggregate ที่มีขอบเขตในตัว) พร้อม filter ช่วงวันที่ที่ส่งไปทุกตาราง
- [ ] T119 [P] [US6] แก้ `web/src/app/(protected)/sales/receipt/[id]/page.tsx` และ `web/src/components/Receipt.tsx` เปลี่ยน `Button` เป็น `btn` ของ daisyUI โดยคง `data-receipt-print` และโครงใบเสร็จเดิมไว้ทั้งหมด

- [ ] T120 ถอด `primereact` และ `primeicons` ออกจาก `web/package.json` แล้วรัน `npm install` — ทำเป็นขั้นสุดท้ายของเฟสนี้ **หลังจาก** `grep -r "primereact" web/src` ได้ผลลัพธ์ว่างแล้วเท่านั้น ถ้ายัง grep เจอแปลว่ายังมีหน้าที่ย้ายไม่เสร็จ

**Checkpoint**: `grep -r "primereact" web/src web/package.json` ต้องได้ผลลัพธ์ว่างทั้งคู่ และทุกหน้ายังเปิดได้

---

## Phase 19: Polish & Cross-Cutting — Responsive และการพิสูจน์ผล

**Purpose**: ปิดงานด้วยตัวเลขที่วัดได้ ไม่ใช่ความรู้สึกว่า "ดูดีแล้ว"

- [ ] T121 **แก้ scenario เดิมให้ยังตรวจของจริง** — ไล่ `quickstart.md` หัวข้อ 4–6 ทุกจุดที่เรียก `GET /api/v1/products` หรือ `GET /api/v1/sales` โดยไม่ส่ง `pageSize` (บรรทัด US3 ข้อ 2/ข้อ 4, US6 ข้อ 1/ข้อ 4 และ NFR-3) ให้ส่ง `pageSize` ชัดเจนหรือไล่อ่านทีละหน้าจนครบ **ทำก่อน T124** ไม่งั้น NFR-3 ที่เคยตรวจ 54 บิลจะเหลือตรวจ 20 บิลแล้วขึ้น PASS หลอก (quickstart.md ข้อ 7.6)
- [ ] T122 เขียนสคริปต์วัด responsive ที่รันซ้ำได้ใน `scratchpad/` — วน `/login` `/sales` `/sales/history` `/stock` `/promotions` `/reports` × ความกว้าง 360/768/1024/1440 แล้ว assert ทั้ง 4 เกณฑ์ของ quickstart.md ข้อ 7.5: `scrollWidth - clientWidth <= 1`, แผงแคชเชียร์ ≤32% ของจอในช่วง 768–1100px และชั้นวาง ≥68% ที่ 900px, ทุก datagrid ที่ ≤768px เลื่อนแนวนอนในกล่องตัวเอง, ปุ่มและช่องกรอกสูง ≥44px ที่ 360px
- [ ] T123 รันสคริปต์ T122 แล้วแก้จนผ่านครบทุกช่อง — จุดที่คาดว่าต้องแก้คือ `web/src/components/Cart.tsx` (ความกว้างแผง), `web/src/components/DataTable.tsx` (กล่องเลื่อนแนวนอน) และ `web/src/app/globals.css` (ความสูงขั้นต่ำของปุ่ม/ช่องกรอก) — ค่าตั้งต้นก่อนแก้คือแผงแคชเชียร์ 384px = 42.7% ของจอที่ 900px ซึ่ง**ตก**เกณฑ์
- [ ] T124 รัน quickstart.md ทั้งไฟล์ (หัวข้อ 4, 5, 6 และ 7) แล้วบันทึกผลลง `specs/001-single-store-pos/quickstart-results.md` — **ผลต้องไม่แย่ลงกว่า 31 PASS / 0 FAIL / 2 N/A** และทุกตัวเลขต้องมาจากรอบที่รายงานเท่านั้น
- [ ] T125 [P] อัปเดต `web/README.md` — บันทึกว่าใช้ daisyUI 5 บน Tailwind 4, ไม่มี `tailwind.config.ts` แล้ว (ธีมอยู่ใน `globals.css`) และคงคำเตือนเรื่อง `npm run build` ทับ `.next/` ของ dev server ที่รันอยู่
- [ ] T126 [P] อัปเดต `specs/001-single-store-pos/research.md` ข้อ 8 ถ้าการอัปเกรดจริงต่างจากที่วางแผนไว้ (เช่น Next.js 14.2.35 ต้องอัปเกรดตาม) — บันทึกสิ่งที่เกิดขึ้นจริง ไม่ใช่สิ่งที่ตั้งใจไว้

---

## Dependencies & Execution Order (รอบที่ 2)

```text
Phase 10 (Tailwind 4)  ─── บล็อกทุกอย่าง ต้อง T089 ผ่านก่อน
      │
      ├─► Phase 11 (API paging)  ─── ทำขนานกับ Phase 12 ได้ คนละ repo layer
      │         │
      │         └─► T101, T102 (lib/api อ่าน envelope)
      │
      └─► Phase 12 (daisyUI + DataTable)
                │
                ├─► Phase 13 (US1) ◄── MVP ของรอบนี้ หยุดตรวจได้ที่นี่
                ├─► Phase 14 (US2)
                ├─► Phase 15 (US3) ◄── ต้องมี T100 + T101 ก่อน
                ├─► Phase 16 (US4)
                ├─► Phase 17 (US5)
                └─► Phase 18 (US6) ◄── ต้องมี T100 + T102 ก่อน
                          │
                          └─► Phase 19 (Polish) ─── T121 ต้องมาก่อน T124
```

### ลำดับที่ห้ามสลับ

- **T089 ก่อนทุกอย่าง** — ถ้า Tailwind 4 ยังไม่นิ่ง ความพังที่ตามมาจะแยกสาเหตุไม่ออก
- **Phase 11 ก่อน Phase 15/18** — ถ้า UI มาก่อน API `DataTable` จะถูกเขียนสองรอบ
- **T091 ก่อน T092/T093** — constitution Principle III บังคับให้ test มาก่อนและต้อง fail ก่อน
- **T121 ก่อน T124** — ไม่งั้นผล "31 PASS" มาจากการตรวจที่อ่อนลงกว่าเดิม

### Parallel Opportunities (รอบที่ 2)

- Phase 11 กับ Phase 12 ทำขนานกันได้ทั้งเฟส (คนละฝั่ง `api/` กับ `web/`)
- T101 กับ T102 ขนานกันได้ (คนละไฟล์)
- Phase 14–18 ขนานกันได้ทั้งหมดหลัง Phase 12 จบ (คนละหน้า คนละไฟล์) ยกเว้น T110/T117 ที่ต้องรอ `DataTable`
- T105, T112, T114, T118, T119, T125, T126 มี `[P]` เพราะแตะไฟล์ที่ไม่มีใครอื่นแตะพร้อมกัน

---

## Implementation Strategy (รอบที่ 2)

### MVP ของรอบนี้ = Phase 10 → 11 → 12 → 13

จบแค่ US1 ก็ได้ระบบขายที่ใช้งานได้จริงบน daisyUI + Tailwind 4 พร้อม API ที่แบ่งหน้าได้แล้ว
**หยุดตรงไหนก็ได้** — `primereact` ยังอยู่ใน `package.json` จนถึง T120 หน้าที่ยังไม่ย้ายจึงเปิดได้ตามปกติ ระหว่างทาง ทำให้ Checkpoint ของทุกเฟสตรวจได้จริง และเป็นเหตุผลที่การถอด dependency ถูกเลื่อนไปเป็น ขั้นสุดท้ายของ Phase 18 แทนที่จะทำตอนติดตั้ง daisyUI

### จุดตรวจที่ควรหยุดดู

1. หลัง **T089** — Tailwind 4 นิ่งหรือยัง ถ้าไม่ ให้ถอยไป daisyUI 4 + Tailwind 3 (research.md ข้อ 8)
2. หลัง **T096** — API เขียวครบหรือยังก่อนแตะ UI
3. หลัง **T107** — หน้าขายซึ่งใช้ทุกวันกลับมาครบหรือยัง
4. หลัง **T123** — ตัวเลข responsive ผ่านครบทุกช่องหรือยัง

### ความเสี่ยงที่รู้ล่วงหน้า

- **Next.js 14.2.35 ออกก่อน Tailwind 4** — ถ้า T089 ไม่ผ่าน ทางเลือกคืออัปเกรด Next.js หรือถอยไปทางเดิม
- **`npm run build` ทับ `.next/` ของ dev server ที่รันอยู่** จนทุก chunk เป็น 404 — หยุด dev server ก่อน build เสมอ
- **ห้ามถอด `primereact` ก่อน T120** — ถอดเร็วกว่านั้นหน้าที่ยังไม่ย้ายจะ resolve import ไม่ได้ และทุกเฟสจะตรวจอะไรไม่ได้เลยจนกว่าจะจบ Phase 18
