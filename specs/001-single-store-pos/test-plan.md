# Test Plan: ระบบ POS ร้านค้าเดี่ยว (TaladPOS)

**Branch**: `qa-precheck` · **สร้างเมื่อ**: 2026-09-10 · **สถานะ**: ร่างเพื่อตรวจทาน (ยังไม่ลงมือเขียนเทสต์)

เอกสารนี้แจกแจง **test case ที่ต้องมี** เพื่อครอบคลุมการทำงานทั้งระบบ รวมถึง CRUD ผ่าน UI ทุกหน้าจอ
อ้างอิง `spec.md` (FR-001–FR-037, SC-001–SC-010), `contracts/*.md` และโค้ดจริงใน `api/` + `web/`

> **วิธีใช้เอกสารนี้**: ตรวจทานทีละหัวข้อ แล้วทำเครื่องหมาย/แก้/เพิ่มบรรทัดได้เลย
> คอลัมน์ **สถานะ** = `มีแล้ว` (เขียนไว้ในโค้ดเทสต์ปัจจุบัน) · `ใหม่` (ต้องเขียนเพิ่ม) · `N/A` (ไม่มีฟีเจอร์นี้ตามสเปก)
> ยังไม่เริ่มเขียนโค้ดเทสต์จนกว่าหัวข้อ 8 (สิ่งที่ต้องตัดสินใจก่อน) จะได้คำตอบ

---

## 0. ขอบเขตและกติกา

**ครอบคลุม**: Domain logic, Application use case, REST API ทุก endpoint, UI ทุกหน้าจอ (CRUD + filter + paging + responsive + RBAC)

**ไม่ครอบคลุม (ตามสเปกไม่มีฟีเจอร์)**: VAT/ใบกำกับภาษี, ขายแบบชั่งน้ำหนัก, ยกเลิก/คืนบิล, แก้ไข/ลบสมาชิก, หลายสาขา, load/stress test

**ID scheme**: `TC-<LAYER>-<AREA>-NNN` · LAYER = `DOM` | `APP` | `API` | `UI` | `NFR`

**ระดับความสำคัญ**: `P1` = บล็อกการปล่อยของ · `P2` = ควรมี · `P3` = ทำเมื่อมีเวลา

**ยอดรวมในเอกสารนี้**: 265 เคส — Domain 23 · Application 29 · API 89 · UI 111 · Non-functional 13
แบ่งตามความสำคัญ: P1 217 · P2 42 · P3 6 · แบ่งตามสถานะ: **มีอยู่แล้ว 47 · ต้องเขียนใหม่ 218**

---

## 1. สถานะการทดสอบปัจจุบัน (inventory)

ตัวเลขด้านล่างมาจากการรัน `dotnet test TaladPOS.sln` จริงเมื่อ 2026-09-10 (ผ่านทั้งหมด 93/93)

| ชั้น | โปรเจกต์ | จำนวนจริง | ครอบคลุมอะไรอยู่ | ช่องว่างหลัก |
|---|---|---|---|---|
| Domain | `api/tests/TaladPOS.Domain.Tests` | **45** | Product (stock/status/validation), Promotion (validation/IsActive/DiscountResolver), Sale + SaleLineItem, Member accumulation | ค่อนข้างครบ — เหลือเคสขอบ |
| Application | `api/tests/TaladPOS.Application.Tests` | **39** | PagedResult, RegisterMember, Product use cases, CompleteSale, SalesReport, BestSelling | **ไม่มีเลย**: Promotion use cases (Create/Update/Delete), GetSalesByStaff, GetStockReport, GetSalesHistory |
| API Integration | `api/tests/TaladPOS.Api.IntegrationTests` | **9** | 401 เมื่อไม่มี token, สร้างบิลผูก staff จาก JWT, concurrency 2 เคส, OpenAPI 4 เคส | **ไม่มีเลย**: CRUD สินค้า/โปรโมชั่น/สมาชิก, รายงาน, ประวัติการขาย, RBAC 403, pagination 400 |
| UI / E2E | — | **0** | ไม่มี test framework ใน `web/` เลย (ไม่มี Playwright/Vitest/Jest ใน `package.json`) | ทุกอย่าง |

> จำนวนจริง 93 มากกว่าที่ระบุใน `tasks.md` (74) เพราะมีเทสต์ที่เพิ่มระหว่างทางโดยไม่ได้อัปเดต tasks.md
> และรวม `UnitTest1.Test1` ซึ่งเป็น template ว่าง 2 ตัวที่ควรลบ
>
> **93 เมธอด ≠ 47 แถวในแผน** — เอกสารนี้นับเป็น "แถว" ซึ่งหนึ่งแถวมักครอบหลาย `[Fact]`
> เช่น TC-DOM-PROMO-004 แถวเดียวครอบ `IsActive` 6 กรณี ดังนั้นตัวเลขสองชุดนี้วัดคนละอย่าง ไม่ได้ขัดกัน

**หนี้เล็กที่ควรเก็บพร้อมกัน**

- `api/tests/TaladPOS.Application.Tests/UnitTest1.cs` และ `api/tests/TaladPOS.Domain.Tests/UnitTest1.cs` เป็น template ว่าง → ลบ
- `web/scripts/verify-fetch-all.cjs` เป็นสคริปต์ตรวจ FR-037/SC-010 ที่ใช้ได้จริงอยู่แล้ว → แปลงเป็นเทสต์ในชุดใหม่แทนการเขียนซ้ำ

---

## 2. เมทริกซ์ CRUD ต่อหน้าจอ

ตอบตรงคำถาม "CRUD ทุกหน้าจอ" — ช่องที่เป็น N/A **ไม่ใช่การข้าม** แต่คือยืนยันแล้วว่าสเปกไม่มีฟีเจอร์นั้น

| หน้าจอ (route) | Create | Read | Update | Delete | สิทธิ์ |
|---|---|---|---|---|---|
| `/login` | — | ฟอร์มล็อกอิน | — | ล็อกเอาต์ (ทิ้ง token, ปุ่มอยู่ใน AppShell) | สาธารณะ |
| `/sales` (ชั้นวาง + ตะกร้า) | ปิดบิล `POST /sales` · สมัครสมาชิกใหม่ | ค้นหา/แสดงสินค้าครบทุกชิ้น | ปรับจำนวนในตะกร้า (ก่อนปิดบิล) | เอาสินค้าออกจากตะกร้า (ก่อนปิดบิล) | Cashier + Manager |
| `/sales` → MemberSearch / MemberFormDialog | สมัครสมาชิก | ค้นหาชื่อ/เบอร์ | **N/A** — ไม่มี `PUT /members` | **N/A** — ไม่มี `DELETE /members` | Cashier + Manager |
| `/sales/history` | N/A (บิลเกิดที่ `/sales`) | ตาราง + filter + server paging | **N/A** — บิล append-only (clarify #2) | **N/A** | Cashier + Manager |
| `/sales/receipt/[id]` | N/A | ใบเสร็จ + สั่งพิมพ์ | N/A | N/A | Cashier + Manager |
| `/stock` | + เพิ่มสินค้า (dialog) | ตาราง + ค้นหา + ใกล้หมด + paging | แก้ไข (dialog) | ลบ | **Manager เท่านั้น** |
| `/promotions` | + สร้างโปรโมชั่น (dialog) | ตาราง + filter `activeOnly` | แก้ไข (dialog) | ลบ | **Manager เท่านั้น** |
| `/reports` | N/A | 4 แท็บ (ยอดขายวันนี้ / ขายดี / ตามพนักงาน / สต็อก) | N/A | N/A | **Manager เท่านั้น** |
| `/` | — | redirect → `/sales` | — | — | — |

---

## 3. Domain unit tests — `TaladPOS.Domain.Tests`

| ID | เคส | FR | คาดหวัง | สถานะ | P |
|---|---|---|---|---|---|
| TC-DOM-PROD-001 | `DecreaseStock` เกินจำนวนคงเหลือ | FR-005 | โยน `InsufficientStockException` | มีแล้ว | P1 |
| TC-DOM-PROD-002 | `DecreaseStock` เท่ากับคงเหลือพอดี → เหลือ 0 | FR-016 | สำเร็จ | มีแล้ว | P1 |
| TC-DOM-PROD-003 | `Price <= 0` / `StockQuantity < 0` | FR-015 | โยน | มีแล้ว | P1 |
| TC-DOM-PROD-004 | `IsLowStock` ที่ >threshold / =threshold / <threshold / =0 | FR-017 | ตรงตามนิยาม (0 → `IsOutOfStock` แทน) | มีแล้ว | P1 |
| TC-DOM-PROD-005 | ไม่ระบุ `lowStockThreshold` → ใช้ค่า default | FR-018 | ค่า default ตามที่เอกสารระบุ | **ใหม่** | P2 |
| TC-DOM-PROD-006 | `SetStockQuantity` เป็นค่าติดลบ | FR-015 | โยน | มีแล้ว | P2 |
| TC-DOM-SALE-001 | `Sale` ไม่มี line item | edge case | โยน `ArgumentException` | มีแล้ว | P1 |
| TC-DOM-SALE-002 | คำนวณ Subtotal / Discount / Total จาก line items | FR-006 | ตรง | มีแล้ว | P1 |
| TC-DOM-SALE-003 | `SaleLineItem` เก็บ snapshot ชื่อ/ราคา คงอยู่แม้สินค้าถูกแก้/ลบ | FR-023 | snapshot ไม่เปลี่ยน | มีแล้ว | P1 |
| TC-DOM-SALE-004 | `LineTotal = unitPrice × qty − discount` | FR-006 | ตรง | มีแล้ว | P1 |
| TC-DOM-SALE-005 | `quantity <= 0` ใน line item | FR-004 | โยน | **ใหม่** | P2 |
| TC-DOM-SALE-006 | `DiscountAmount` เกิน `unitPrice × qty` (Item 100% + Bill 100% ชนกัน) | — | **ยังไม่มีการ์ด** — ดูประเด็น R-06 | **ใหม่** | P3 |
| TC-DOM-PROMO-001 | `EndDate < StartDate` | FR-021 | โยน | มีแล้ว | P1 |
| TC-DOM-PROMO-002 | `DiscountPercentage` นอกช่วง (0, 100] | FR-019 | โยน | มีแล้ว | P1 |
| TC-DOM-PROMO-003 | `Scope=Item` ไม่มี `ProductId` / `Scope=Bill` มี `ProductId` | FR-019 | โยนทั้งคู่ | มีแล้ว | P1 |
| TC-DOM-PROMO-004 | `IsActive` ที่วันเริ่ม / วันสิ้นสุด / ระหว่าง / ก่อน / หลัง / โปรฯ วันเดียว | FR-021 | inclusive ทั้งสองปลาย | มีแล้ว | P1 |
| TC-DOM-DISC-001 | `DiscountResolver` มีแต่โปรฯ ทั่วไป | FR-022 | คืนค่าโปรฯ นั้น | มีแล้ว | P1 |
| TC-DOM-DISC-002 | มีแต่ส่วนลดสมาชิก แต่บิลไม่มีสมาชิก | FR-020 | คืน 0 | มีแล้ว | P1 |
| TC-DOM-DISC-003 | มีทั้งคู่ → เลือกอันที่มากกว่าอันเดียว ไม่สะสม | FR-022 | ค่ามากสุด | มีแล้ว | P1 |
| TC-DOM-DISC-004 | มีทั้งคู่ค่าเท่ากัน | FR-022 | คืนค่านั้นครั้งเดียว | มีแล้ว | P1 |
| TC-DOM-DISC-005 | โปรฯ นอกช่วงวันที่ถูกตัดออก | FR-021 | ไม่ถูกใช้ | มีแล้ว | P1 |
| TC-DOM-DISC-006 | โปรฯ ของสินค้าอื่นไม่หลุดมาใช้ | FR-019 | ไม่ถูกใช้ | มีแล้ว | P1 |
| TC-DOM-MEM-001 | `IncreaseAccumulatedPurchaseTotal` บวกตรงยอด / สะสมข้ามบิล / ค่าติดลบโยน | FR-014 | ตรง | มีแล้ว | P1 |

---

## 4. Application unit tests — `TaladPOS.Application.Tests`

| ID | เคส | FR | คาดหวัง | สถานะ | P |
|---|---|---|---|---|---|
| TC-APP-PAGE-001 | `PagedResult` เก็บ count หลังกรอง ไม่ใช่ความยาวของหน้า | FR-034 | ตรง | มีแล้ว | P1 |
| TC-APP-PAGE-002 | `TotalPages` ปัดขึ้นครอบหน้าสุดท้ายที่ไม่เต็ม / เลยหน้าสุดท้าย → ว่างแต่ total ยังจริง / `Skip` ข้ามทุกหน้าก่อน | FR-034 | ตรง | มีแล้ว | P1 |
| TC-APP-PAGE-003 | `TryCreate` ค่า default (page 1, size 20) / นอกช่วงถูกปฏิเสธ / ที่ 100 ผ่าน | FR-034, FR-035 | ตรง | มีแล้ว | P1 |
| TC-APP-PROD-001 | สร้างสินค้าบาร์โค้ดซ้ำ → `DuplicateBarcodeException` | FR-015 | โยน | มีแล้ว | P1 |
| TC-APP-PROD-002 | สร้างสินค้าบาร์โค้ด `null` | FR-015 | สำเร็จ (null หลายตัวไม่ชนกัน) | มีแล้ว | P1 |
| TC-APP-PROD-003 | แก้ไขโดยคงบาร์โค้ดเดิมของตัวเอง | FR-015 | สำเร็จ | มีแล้ว | P1 |
| TC-APP-PROD-004 | แก้ไขไปชนบาร์โค้ดของสินค้าอื่น | FR-015 | โยน | มีแล้ว | P1 |
| TC-APP-PROD-005 | `DeleteProductUseCase` กับ id ที่ไม่มีอยู่ | FR-015 | `KeyNotFoundException` | **ใหม่** | P2 |
| TC-APP-PROMO-001 | `CreatePromotionUseCase` เคสถูกต้อง (Item / Bill) | FR-019 | บันทึกสำเร็จ | **ใหม่** | P1 |
| TC-APP-PROMO-002 | `CreatePromotionUseCase` `Scope=Item` แต่ `productId` ไม่มีอยู่จริง | FR-019 | ปฏิเสธ | **ใหม่** | P1 |
| TC-APP-PROMO-003 | `UpdatePromotionUseCase` id ไม่มีอยู่ | FR-019 | `KeyNotFoundException` | **ใหม่** | P1 |
| TC-APP-PROMO-004 | `DeletePromotionUseCase` id ไม่มีอยู่ | FR-019 | `KeyNotFoundException` | **ใหม่** | P1 |
| TC-APP-MEM-001 | สมัครเบอร์ซ้ำ → `DuplicatePhoneNumberException` | FR-011 | โยน | มีแล้ว | P1 |
| TC-APP-MEM-002 | สมัครเบอร์ใหม่ → ยอดสะสมเริ่มที่ 0 | FR-010 | ตรง | มีแล้ว | P1 |
| TC-APP-SALE-001 | สต็อกพอ → สร้างบิลและ commit transaction | FR-016 | ตรง | มีแล้ว | P1 |
| TC-APP-SALE-002 | สต็อกไม่พอ → โยน และไม่สร้างบิล | FR-005 | ไม่มีบิล | มีแล้ว | P1 |
| TC-APP-SALE-003 | `lineItems` ว่าง → โยนก่อนเปิด transaction | edge case | ตรง | มีแล้ว | P1 |
| TC-APP-SALE-004 | `memberId` ไม่มีอยู่ → โยน และไม่ตัดสต็อก | FR-013 | ตรง | มีแล้ว | P1 |
| TC-APP-SALE-005 | ผูกสมาชิก → ยอดสะสมเพิ่มเท่ากับ `totalAmount` พอดี | FR-014 | ตรง | มีแล้ว | P1 |
| TC-APP-SALE-006 | โปรฯ Item ลงเฉพาะบรรทัดนั้น | FR-019 | ตรง | มีแล้ว | P1 |
| TC-APP-SALE-007 | โปรฯ Bill กระจายทุกบรรทัดแล้วรวมกลับเท่ายอดส่วนลดบิลพอดี | FR-019 | ตรง | มีแล้ว | P1 |
| TC-APP-SALE-008 | โปรฯ `appliesToMembersOnly` ใช้เฉพาะบิลที่มีสมาชิก | FR-020 | ตรง | มีแล้ว | P1 |
| TC-APP-RPT-001 | รายงาน daily รวมเฉพาะวันนั้น / monthly รวมทั้งเดือน / `period` ผิด → โยน / ไม่มีบิล → ศูนย์ | FR-025 | ตรง | มีแล้ว | P1 |
| TC-APP-RPT-002 | สินค้าขายดีเรียง `quantitySold` มาก→น้อย และเคารพ `limit` | FR-026 | ตรง | มีแล้ว | P1 |
| TC-APP-RPT-003 | `GetSalesByStaffReportQuery` รวมยอด/นับบิลต่อพนักงานถูกต้อง | FR-027 | ตรง | **ใหม่** | P1 |
| TC-APP-RPT-004 | `GetSalesByStaffReportQuery` พนักงานที่ไม่มีบิลในช่วง | FR-027 | ไม่ปรากฏ / เป็นศูนย์ (ยืนยันพฤติกรรมที่ตั้งใจ) | **ใหม่** | P2 |
| TC-APP-RPT-005 | `GetStockReportQuery` คืนทุกสินค้าพร้อม `isLowStock` ถูกต้อง | FR-028 | ตรง | **ใหม่** | P1 |
| TC-APP-RPT-006 | `GetSalesHistoryQuery` filter `from` / `to` / `staffId` / `memberId` แบบ AND | FR-024 | ตรง | **ใหม่** | P1 |
| TC-APP-RPT-007 | `GetSalesHistoryQuery` เรียงใหม่→เก่า และลำดับคงที่ (tiebreak) | SC-008 | ตรง | **ใหม่** | P1 |

---

## 5. API integration tests — `TaladPOS.Api.IntegrationTests`

รันจริงบน PostgreSQL `taladpos_test` ผ่าน `TaladPOSApiFactory` ที่มีอยู่แล้ว
บัญชี seed: `manager` / `Manager123!` (Manager) และ `cashier` / `Cashier123!` (Cashier)

### 5.1 Auth — `/api/v1/auth`

| ID | เคส | คาดหวัง | สถานะ | P |
|---|---|---|---|---|
| TC-API-AUTH-001 | login manager ถูกต้อง | 200 + token + `staff.role="Manager"` + `expiresAt` | **ใหม่** | P1 |
| TC-API-AUTH-002 | login cashier ถูกต้อง | 200 + `role="Cashier"` | **ใหม่** | P1 |
| TC-API-AUTH-003 | รหัสผ่านผิด | 401 `{"error":"invalid_credentials"}` | **ใหม่** | P1 |
| TC-API-AUTH-004 | username ไม่มีอยู่ | 401 ข้อความเดียวกับข้อบน (ไม่บอกว่า user มี/ไม่มี) | **ใหม่** | P1 |
| TC-API-AUTH-005 | token มี claim `role` และ `staffId` | decode แล้วครบ | **ใหม่** | P1 |
| TC-API-AUTH-006 | เรียก endpoint อื่นโดยไม่มี header | 401 | มีแล้ว | P1 |
| TC-API-AUTH-007 | token ปลอม / ผิด signature | 401 | **ใหม่** | P2 |
| TC-API-AUTH-008 | token หมดอายุ | 401 | **ใหม่** | P3 |

### 5.2 Products — `/api/v1/products`

| ID | เคส | คาดหวัง | สถานะ | P |
|---|---|---|---|---|
| TC-API-PROD-001 | GET ไม่ระบุ param | 200 envelope `page=1, pageSize=20` | **ใหม่** | P1 |
| TC-API-PROD-002 | GET `?search=` | คืนเฉพาะชื่อที่ contains (case-insensitive) | **ใหม่** | P1 |
| TC-API-PROD-003 | GET `?barcode=` | ตรงเป๊ะ 1 รายการ | **ใหม่** | P1 |
| TC-API-PROD-004 | GET `?search=X&barcode=X` | 0 แถว (AND ไม่ใช่ OR ตาม contract) | **ใหม่** | P1 |
| TC-API-PROD-005 | GET `?lowStockOnly=true` | เฉพาะ `isLowStock=true` | **ใหม่** | P1 |
| TC-API-PROD-006 | GET `?page=0` / `?pageSize=0` / `?pageSize=101` | 400 `{"error":"invalid_pagination"}` **ไม่ปัดค่าให้เอง** (FR-035) | **ใหม่** | P1 |
| TC-API-PROD-007 | GET `?pageSize=100` | 200 | **ใหม่** | P1 |
| TC-API-PROD-008 | GET หน้าเลย `totalPages` | 200 `items` ว่าง แต่ `totalCount` ยังเป็นค่าจริง | **ใหม่** | P1 |
| TC-API-PROD-009 | `totalCount` นับหลังกรอง ไม่ใช่จำนวนสินค้าทั้งร้าน | ตรง | **ใหม่** | P1 |
| TC-API-PROD-010 | ไล่อ่านทุกหน้า → ไม่มีรายการซ้ำ/หาย และรวมได้เท่า `totalCount` | SC-008 | **ใหม่** | P1 |
| TC-API-PROD-011 | GET `{id}` มี / ไม่มี | 200 / 404 | **ใหม่** | P1 |
| TC-API-PROD-012 | POST โดย Manager | 201 + ProductDto | **ใหม่** | P1 |
| TC-API-PROD-013 | POST โดย **Cashier** | **403** (FR-029) | **ใหม่** | P1 |
| TC-API-PROD-014 | POST `price <= 0` | 400 | **ใหม่** | P1 |
| TC-API-PROD-015 | POST บาร์โค้ดซ้ำ | 409 `duplicate_barcode` | **ใหม่** | P1 |
| TC-API-PROD-016 | POST `barcode=null` | 201 | **ใหม่** | P1 |
| TC-API-PROD-017 | PUT แก้ราคา / สต็อก / threshold | 200 + ค่าใหม่ | **ใหม่** | P1 |
| TC-API-PROD-018 | PUT คงบาร์โค้ดเดิมของตัวเอง | 200 | **ใหม่** | P1 |
| TC-API-PROD-019 | PUT ไปชนบาร์โค้ดสินค้าอื่น | 409 | **ใหม่** | P1 |
| TC-API-PROD-020 | PUT / DELETE id ไม่มีอยู่ | 404 | **ใหม่** | P1 |
| TC-API-PROD-021 | PUT / DELETE โดย Cashier | 403 | **ใหม่** | P1 |
| TC-API-PROD-022 | DELETE โดย Manager | 204 แล้ว GET → 404 | **ใหม่** | P1 |
| TC-API-PROD-023 | DELETE สินค้าที่เคยขาย → บิลเก่ายังอ่านได้ ชื่อ/ราคาจาก snapshot | FR-023 | **ใหม่** | P1 |

### 5.3 Promotions — `/api/v1/promotions` (Manager เท่านั้นทุก verb)

| ID | เคส | คาดหวัง | สถานะ | P |
|---|---|---|---|---|
| TC-API-PROMO-001 | GET โดย Manager | 200 array | **ใหม่** | P1 |
| TC-API-PROMO-002 | GET / POST / PUT / DELETE โดย **Cashier** | **403 ทุก verb** (FR-029) | **ใหม่** | P1 |
| TC-API-PROMO-003 | GET `?activeOnly=true` | เฉพาะที่ครอบวันนี้ | **ใหม่** | P1 |
| TC-API-PROMO-004 | POST `Scope=Item` + `productId` | 201 | **ใหม่** | P1 |
| TC-API-PROMO-005 | POST `Scope=Bill` + `productId=null` | 201 | **ใหม่** | P1 |
| TC-API-PROMO-006 | POST `Scope=Item` ไม่มี `productId` | 400 | **ใหม่** | P1 |
| TC-API-PROMO-007 | POST `Scope=Bill` แต่ส่ง `productId` | 400 | **ใหม่** | P1 |
| TC-API-PROMO-008 | POST `discountPercentage` = 0 / 101 / ติดลบ | 400 ทั้งหมด | **ใหม่** | P1 |
| TC-API-PROMO-009 | POST `discountPercentage` = 100 | 201 (ขอบบน inclusive) | **ใหม่** | P2 |
| TC-API-PROMO-010 | POST `endDate < startDate` | 400 | **ใหม่** | P1 |
| TC-API-PROMO-011 | PUT แก้แล้วอ่านกลับ | 200 + ค่าใหม่ | **ใหม่** | P1 |
| TC-API-PROMO-012 | PUT / DELETE id ไม่มีอยู่ | 404 | **ใหม่** | P1 |
| TC-API-PROMO-013 | DELETE | 204 แล้ว GET ไม่เจอ | **ใหม่** | P1 |

### 5.4 Members — `/api/v1/members`

| ID | เคส | คาดหวัง | สถานะ | P |
|---|---|---|---|---|
| TC-API-MEM-001 | GET โดย Cashier | 200 (ไม่ใช่ manager-only) | **ใหม่** | P1 |
| TC-API-MEM-002 | GET โดยไม่มี token | 401 | **ใหม่** | P1 |
| TC-API-MEM-003 | GET `?search=` ด้วยชื่อ / ด้วยเบอร์ | เจอทั้งสองทาง (FR-012) | **ใหม่** | P1 |
| TC-API-MEM-004 | POST เบอร์ใหม่ | 201 + `accumulatedPurchaseTotal=0` | **ใหม่** | P1 |
| TC-API-MEM-005 | POST เบอร์ซ้ำ | 409 `phone_number_already_registered` | **ใหม่** | P1 |
| TC-API-MEM-006 | POST ชื่อว่าง / เบอร์ว่าง | 400 | **ใหม่** | P2 |
| TC-API-MEM-007 | GET `{id}` มี / ไม่มี | 200 / 404 | **ใหม่** | P1 |
| TC-API-MEM-008 | ไม่มี PUT / DELETE สำหรับสมาชิก | 404 หรือ 405 (ยืนยันว่าไม่มีทางแก้ยอดสะสมตรง ๆ) | **ใหม่** | P2 |

### 5.5 Sales — `/api/v1/sales`

| ID | เคส | คาดหวัง | สถานะ | P |
|---|---|---|---|---|
| TC-API-SALE-001 | POST มี token ถูกต้อง | 201 + SaleDto ครบ snapshot | มีแล้ว | P1 |
| TC-API-SALE-002 | POST ไม่มี token | 401 | มีแล้ว | P1 |
| TC-API-SALE-003 | `staffId` มาจาก JWT ไม่ใช่ body | บิลผูกกับผู้ล็อกอินจริง (FR-008) | มีแล้ว | P1 |
| TC-API-SALE-004 | POST `lineItems` ว่าง | 400 | **ใหม่** | P1 |
| TC-API-SALE-005 | POST `quantity <= 0` | 400 | **ใหม่** | P1 |
| TC-API-SALE-006 | POST `productId` ไม่มีอยู่ | 404 | **ใหม่** | P1 |
| TC-API-SALE-007 | POST `memberId` ไม่มีอยู่ | 404 **และสต็อกไม่ถูกตัด** | **ใหม่** | P1 |
| TC-API-SALE-008 | POST สต็อกไม่พอ | 409 `insufficient_stock` + ระบุ `productId` — ⚠️ รูปแบบ body ต้องรอข้อสรุป R-08 ก่อน | **ใหม่** | P1 |
| TC-API-SALE-009 | สองคำขอพร้อมกันแย่งชิ้นสุดท้าย | 201 หนึ่ง / 409 หนึ่ง | มีแล้ว | P1 |
| TC-API-SALE-010 | บรรทัดหลังสต็อกไม่พอ → rollback การตัดสต็อกบรรทัดก่อนหน้า | สต็อกเดิมครบ | มีแล้ว | P1 |
| TC-API-SALE-011 | POST สำเร็จ → สต็อกลดเท่าที่ขายพอดี | SC-002 | **ใหม่** | P1 |
| TC-API-SALE-012 | POST ผูกสมาชิก → ยอดสะสมเพิ่มเท่า `totalAmount` | FR-014 | **ใหม่** | P1 |
| TC-API-SALE-013 | POST ขณะมีทั้งโปรฯ ทั่วไปและส่วนลดสมาชิก → ใช้อันเดียวที่มากสุด | FR-022 end-to-end | **ใหม่** | P1 |
| TC-API-SALE-014 | POST ขณะมีโปรฯ ที่หมดอายุแล้ว | ไม่ถูกใช้ | **ใหม่** | P1 |
| TC-API-SALE-015 | GET list ไม่ระบุ param | 200 envelope, เรียงใหม่→เก่า | **ใหม่** | P1 |
| TC-API-SALE-016 | GET `?from&to` / `?staffId` / `?memberId` และรวมกันแบบ AND | ตรง | **ใหม่** | P1 |
| TC-API-SALE-017 | GET `?page=0` / `?pageSize=101` | 400 `invalid_pagination` | **ใหม่** | P1 |
| TC-API-SALE-018 | ไล่ทุกหน้าจากชุดข้อมูล > 2 หน้า → ไม่ซ้ำ/ไม่หาย ผลรวม = `totalCount` | SC-008 | **ใหม่** | P1 |
| TC-API-SALE-019 | `totalCount` นับหลังกรอง | ตรง | **ใหม่** | P1 |
| TC-API-SALE-020 | GET `{id}` มี / ไม่มี | 200 / 404 | **ใหม่** | P1 |
| TC-API-SALE-021 | GET `{id}/receipt` มี / ไม่มี | 200 (ข้อมูลพอ render ใบเสร็จ) / 404 | **ใหม่** | P1 |
| TC-API-SALE-022 | ไม่มี PUT / DELETE บิล | 404 หรือ 405 (append-only) | **ใหม่** | P2 |

### 5.6 Reports — `/api/v1/reports` (Manager เท่านั้น)

| ID | เคส | คาดหวัง | สถานะ | P |
|---|---|---|---|---|
| TC-API-RPT-001 | ทั้ง 4 endpoint โดย **Cashier** | **403 ทุกอัน** (FR-029) | **ใหม่** | P1 |
| TC-API-RPT-002 | `GET /sales?period=daily&date=` | 200 ยอดตรงกับบิลจริงของวันนั้น | **ใหม่** | P1 |
| TC-API-RPT-003 | `GET /sales?period=monthly` | รวมทั้งเดือนของวันที่อ้างอิง | **ใหม่** | P1 |
| TC-API-RPT-004 | `period` ค่าอื่น / ไม่ส่ง | 400 | **ใหม่** | P1 |
| TC-API-RPT-005 | `GET /best-selling-products?from&to` | 200 เรียงมาก→น้อย ตรงกับผลรวมที่คำนวณเองจากบิล | **ใหม่** | P1 |
| TC-API-RPT-006 | `?limit=3` | คืน 3 แถว | **ใหม่** | P2 |
| TC-API-RPT-007 | `from` / `to` เป็น `YYYY-MM-DD` ล้วน (regression: เคยพัง `DateOnly` / `timestamptz`) | 200 ไม่ใช่ 400 | **ใหม่** | P1 |
| TC-API-RPT-008 | `GET /sales-by-staff?from&to` | 200 `billCount` / `totalSalesAmount` ตรงต่อพนักงาน | **ใหม่** | P1 |
| TC-API-RPT-009 | `GET /stock` | 200 ทุกสินค้า + `isLowStock` ตรง | **ใหม่** | P1 |
| TC-API-RPT-010 | ขายบิลใหม่แล้วเรียกรายงานวันนี้ทันที | ยอดรวมนับบิลนั้นด้วย ไม่มี data lag (SC-004) | **ใหม่** | P1 |

### 5.7 Cross-cutting API

| ID | เคส | คาดหวัง | สถานะ | P |
|---|---|---|---|---|
| TC-API-X-001 | OpenAPI document สร้างได้ ไม่ชน schemaId ประกาศ JWT ครอบคลุม endpoint ตาม contract | ผ่าน | มีแล้ว | P1 |
| TC-API-X-002 | ทุก path เป็น `/api/v1/...` | ผ่าน | มีแล้ว | P1 |
| TC-API-X-003 | CORS preflight จาก `http://localhost:3000` | 204 + header ครบ | **ใหม่** | P2 |
| TC-API-X-004 | แยก 401 (ไม่ล็อกอิน) ออกจาก 403 (ล็อกอินแต่ผิดบทบาท) ให้ชัดทุก manager-only endpoint | ตรง | **ใหม่** | P1 |
| TC-API-X-005 | รูปแบบ error body สม่ำเสมอทั้งระบบ | ⚠️ ตอนนี้ยังไม่สม่ำเสมอ — controller ส่งแบน (`{error}`) ส่วน middleware ส่งซ้อน (`{error, details}`) ต้องรอข้อสรุป R-08 | **ใหม่** | P2 |

---

## 6. E2E UI tests (ต่อหน้าจอ)

รันบนเบราว์เซอร์จริงกับ API + Postgres จริง — **ต้องตัดสินใจหัวข้อ 8 ก่อนเริ่ม**

### 6.1 ล็อกอิน / สิทธิ์ / นำทาง

| ID | เคส | FR | คาดหวัง | P |
|---|---|---|---|---|
| TC-UI-AUTH-001 | ล็อกอิน `manager` | FR-007 | ไป `/sales` + ชื่อและบทบาทขึ้นบนแถบ | P1 |
| TC-UI-AUTH-002 | ล็อกอิน `cashier` | FR-007 | ไป `/sales` | P1 |
| TC-UI-AUTH-003 | รหัสผ่านผิด | FR-007 | ข้อความ "ชื่อผู้ใช้หรือรหัสผ่านไม่ถูกต้อง ลองใหม่อีกครั้ง" และไม่เปลี่ยนหน้า | P1 |
| TC-UI-AUTH-004 | เปิด `/sales` โดยไม่ล็อกอิน | FR-007 | redirect ไป `/login` | P1 |
| TC-UI-AUTH-005 | รีเฟรชหน้าหลังล็อกอิน | — | ยังล็อกอินอยู่ (กู้จาก localStorage) ไม่กระพริบไป `/login` | P1 |
| TC-UI-AUTH-006 | ล็อกเอาต์ | FR-009 | กลับ `/login`, `taladpos_token` ถูกลบ, กด back แล้วเข้า `/sales` ไม่ได้ | P1 |
| TC-UI-AUTH-007 | เปิด `/` | — | redirect ไป `/sales` | P2 |
| TC-UI-AUTH-008 | เมนูของ Manager | FR-029 | เห็นครบ: ขายสินค้า, ประวัติการขาย, จัดการสต็อก, โปรโมชั่น, รายงาน | P1 |
| TC-UI-AUTH-009 | เมนูของ Cashier | FR-029 | เห็นเฉพาะ ขายสินค้า, ประวัติการขาย — ไม่มี 3 ลิงก์ของผู้จัดการ | P1 |
| TC-UI-AUTH-010 | Cashier พิมพ์ URL `/stock`, `/promotions`, `/reports` ตรง ๆ | FR-029 | เห็น "หน้านี้เปิดให้เฉพาะผู้จัดการ" + ลิงก์กลับหน้าขาย ทั้ง 3 หน้า | P1 |

### 6.2 `/sales` — หน้าขาย (Create / Update / Delete ของตะกร้า + ปิดบิล)

| ID | เคส | FR | คาดหวัง | P |
|---|---|---|---|---|
| TC-UI-SALES-001 | เปิดหน้าโดยไม่ค้นหา | FR-001, FR-037 | เห็นสินค้า **ครบทุกชิ้น** (ทดสอบด้วยแคตตาล็อก > 100 รายการ ต้องเห็นชิ้นที่ 101) | P1 |
| TC-UI-SALES-002 | พิมพ์ชื่อสินค้าในช่องเดียว | FR-002, SC-010 | เจอสินค้าตามชื่อ | P1 |
| TC-UI-SALES-003 | ป้อนบาร์โค้ดในช่องเดียวกัน | FR-002, SC-010 | เจอสินค้าตามบาร์โค้ด โดยไม่ต้องสลับโหมด | P1 |
| TC-UI-SALES-004 | ค้นหาคำที่ไม่มี | — | ข้อความ `ไม่พบสินค้าที่ตรงกับ "…"` | P2 |
| TC-UI-SALES-005 | แตะการ์ดสินค้า | FR-003 | เพิ่มลงตะกร้าจำนวน 1 | P1 |
| TC-UI-SALES-006 | แตะการ์ดเดิมซ้ำ | FR-004 | จำนวนเป็น 2 ไม่ใช่แยกเป็น 2 บรรทัด | P1 |
| TC-UI-SALES-007 | ปุ่ม + / − | FR-004 | จำนวนขึ้น/ลง และยอดรวมอัปเดตทันที | P1 |
| TC-UI-SALES-008 | ปุ่ม − ที่จำนวน 1 | FR-004 | ปุ่ม disabled (ไม่ลงไป 0) | P1 |
| TC-UI-SALES-009 | ปุ่ม + เมื่อจำนวน = สต็อกคงเหลือ | FR-005 | ปุ่ม disabled | P1 |
| TC-UI-SALES-010 | พิมพ์จำนวนเกินสต็อกลงช่องตัวเลข | FR-005 | ถูก clamp ลงเท่าสต็อก | P1 |
| TC-UI-SALES-011 | ปุ่มกากบาทลบรายการ | FR-004 | บรรทัดหายจากตะกร้า ยอดรวมลดลง | P1 |
| TC-UI-SALES-012 | สินค้าที่สต็อก = 0 | FR-005 | การ์ด disabled + ป้าย "สินค้าหมด" กดไม่ได้ | P1 |
| TC-UI-SALES-013 | สินค้าใกล้หมด | FR-017 | การ์ดมีป้าย "เหลือ N" | P2 |
| TC-UI-SALES-014 | ตะกร้าว่าง | edge case | ปุ่ม "ชำระเงิน" disabled แต่ยังมองเห็นชัด | P1 |
| TC-UI-SALES-015 | ชำระเงินสำเร็จ | FR-023, FR-030 | ใบเสร็จเด้ง, ตะกร้าล้าง, สมาชิกถูกล้าง, ชั้นวางโหลดสต็อกใหม่ | P1 |
| TC-UI-SALES-016 | ชำระเงินแล้วสต็อกถูกจุดขายอื่นตัดไปก่อน (409) | FR-016 | ข้อความ "สินค้าบางรายการหมดสต็อกแล้ว ลดจำนวนในตะกร้าแล้วลองอีกครั้ง" และตะกร้ายังอยู่ | P1 |
| TC-UI-SALES-017 | API ล่ม / เน็ตหลุดตอนชำระเงิน | — | ข้อความ "ชำระเงินไม่สำเร็จ ตรวจการเชื่อมต่อ…" | P2 |
| TC-UI-SALES-018 | กด "ใบเสร็จบิลล่าสุด" | FR-030 | เปิดใบเสร็จบิลเดิมอีกครั้ง | P2 |
| TC-UI-SALES-019 | คลิกนอกกล่องใบเสร็จ | — | **ไม่ปิด** (จงใจตาม `Modal`) — Escape ปิดได้ | P2 |
| TC-UI-SALES-020 | ทำครบวงจร 5 ชิ้นภายใน 1 นาที | SC-001 | จับเวลาแล้วผ่าน | P2 |
| TC-UI-SALES-021 | ตะกร้าแสดง **ยอดรวมย่อย + ส่วนลด + ยอดสุทธิ** ก่อนกดชำระเงิน | FR-006 | ⚠️ **คาดว่าจะไม่ผ่านตอนนี้** — ดูประเด็น R-01 | P1 |

### 6.3 สมาชิก (ภายใน `/sales`)

| ID | เคส | FR | คาดหวัง | P |
|---|---|---|---|---|
| TC-UI-MEM-001 | พิมพ์ชื่อในช่องค้นหาสมาชิก | FR-012 | รายการแนะนำขึ้นหลัง debounce | P1 |
| TC-UI-MEM-002 | พิมพ์เบอร์โทร | FR-012 | เจอเช่นกัน | P1 |
| TC-UI-MEM-003 | ลูกศรลง/ขึ้น + Enter | — | เลือกได้ด้วยคีย์บอร์ด | P2 |
| TC-UI-MEM-004 | Escape / คลิกนอก | — | ปิดรายการแนะนำ | P2 |
| TC-UI-MEM-005 | เลือกสมาชิกแล้ว | FR-013 | แสดงชื่อ เบอร์ และยอดสะสม | P1 |
| TC-UI-MEM-006 | ปุ่ม "เอาออก" | — | ล้างสมาชิกออกจากบิล | P1 |
| TC-UI-MEM-007 | สมัครสมาชิกใหม่สำเร็จ | FR-010 | dialog ปิด + สมาชิกใหม่ถูกเลือกให้อัตโนมัติ | P1 |
| TC-UI-MEM-008 | สมัครด้วยเบอร์ซ้ำ | FR-011 | ข้อความ "เบอร์โทรศัพท์นี้ถูกใช้สมัครสมาชิกไปแล้ว" และ dialog ไม่ปิด | P1 |
| TC-UI-MEM-009 | เว้นชื่อ/เบอร์ว่างแล้วกดบันทึก | — | เบราว์เซอร์บล็อกด้วย `required` | P2 |
| TC-UI-MEM-010 | ปิดแล้วเปิด dialog ใหม่ | — | ฟอร์มว่าง ไม่ค้างค่าเดิม/ข้อความ error เดิม | P2 |
| TC-UI-MEM-011 | ขายบิลผูกสมาชิก แล้วเปิดดูสมาชิกอีกครั้ง | FR-014 | ยอดสะสมเพิ่มเท่ายอดบิลพอดี | P1 |
| TC-UI-MEM-012 | พนักงานใหม่ทำ TC-UI-MEM-001→011 ได้เองโดยไม่ต้องถามทีมเทคนิค | SC-005 | ผ่าน (usability walkthrough) | P3 |

### 6.4 `/stock` — CRUD สินค้า (Manager)

| ID | เคส | FR | คาดหวัง | P |
|---|---|---|---|---|
| TC-UI-STOCK-001 | Manager เปิดหน้า | FR-015 | เห็นตารางพร้อมคอลัมน์ รูป/ชื่อ/บาร์โค้ด/ราคา/คงเหลือ | P1 |
| TC-UI-STOCK-002 | Cashier เปิดหน้า | FR-029 | เห็น ManagerOnly ไม่เห็นตาราง | P1 |
| TC-UI-STOCK-003 | **C**: กด "+ เพิ่มสินค้า" กรอกครบแล้วบันทึก | FR-015 | dialog ปิด, แถวใหม่ปรากฏในตาราง | P1 |
| TC-UI-STOCK-004 | C: บาร์โค้ดซ้ำ | FR-015 | ข้อความ "บาร์โค้ดนี้ถูกใช้กับสินค้าอื่นแล้ว" และ dialog ไม่ปิด | P1 |
| TC-UI-STOCK-005 | C: ราคา 0 / ติดลบ | FR-015 | ถูกบล็อกด้วย `min=0.01` ก่อนยิง API | P1 |
| TC-UI-STOCK-006 | C: เว้นบาร์โค้ดว่าง | FR-015 | บันทึกได้ (ส่ง `null`) และตารางแสดง "—" | P2 |
| TC-UI-STOCK-007 | C: กดยกเลิก | — | ไม่มีอะไรถูกสร้าง | P2 |
| TC-UI-STOCK-008 | **U**: กด "แก้ไข" | FR-015 | dialog เติมค่าเดิมครบทั้ง 6 ฟิลด์ | P1 |
| TC-UI-STOCK-009 | U: แก้ราคา + จำนวนแล้วบันทึก | FR-015 | ตารางแสดงค่าใหม่ | P1 |
| TC-UI-STOCK-010 | U: ตั้ง `lowStockThreshold` ให้ ≥ คงเหลือ | FR-017, FR-018 | แถวขึ้นป้าย "ใกล้หมด" ทันทีที่ใช้งานหน้าถัดไป (SC-003) | P1 |
| TC-UI-STOCK-011 | U: ตั้งคงเหลือ = 0 | FR-017 | ป้าย "หมด" (ไม่ใช่ "ใกล้หมด") และการ์ดในหน้าขาย disabled | P1 |
| TC-UI-STOCK-012 | U: เปิด edit แล้วปิด แล้วกด "+ เพิ่มสินค้า" | — | ฟอร์มว่าง ไม่ค้างค่าของสินค้าที่เพิ่งแก้ | P2 |
| TC-UI-STOCK-013 | **D**: กด "ลบ" | FR-015 | แถวหายจากตาราง (⚠️ ไม่มีกล่องยืนยัน — ดู R-02) | P1 |
| TC-UI-STOCK-014 | D: ลบสินค้าที่เคยขายไปแล้ว | FR-023 | ลบได้ และบิลเก่ายังแสดงชื่อ/ราคาเดิมจาก snapshot | P1 |
| TC-UI-STOCK-015 | D: ลบไม่สำเร็จ (API error) | — | ข้อความ "ไม่สามารถลบสินค้าได้" | P2 |
| TC-UI-STOCK-016 | **R**: ค้นหาชื่อสินค้า | FR-036 | กรองที่เซิร์ฟเวอร์ (ไม่ใช่กรองเฉพาะ 20 แถวในมือ) | P1 |
| TC-UI-STOCK-017 | R: ติ๊ก "เฉพาะสินค้าใกล้หมด" | FR-017, FR-036 | เหลือเฉพาะรายการใกล้หมด | P1 |
| TC-UI-STOCK-018 | R: ไปหน้า 5 แล้วเปลี่ยนเงื่อนไขกรอง | FR-036 | **กลับไปหน้า 1** ไม่ค้างหน้าเดิมจนตารางว่าง | P1 |
| TC-UI-STOCK-019 | R: ตัวแบ่งหน้า | FR-034, SC-008 | "หน้า X จาก Y · ทั้งหมด N รายการ" ตรง และไล่ทุกหน้าไม่มีซ้ำ/หาย | P1 |
| TC-UI-STOCK-020 | R: กรองแล้วไม่เจอ | — | ข้อความ "ไม่พบสินค้าที่ตรงกับเงื่อนไข" | P2 |
| TC-UI-STOCK-021 | R: ร้านที่ยังไม่มีสินค้าเลย | — | ข้อความ "ยังไม่มีสินค้า กดเพิ่มสินค้าเพื่อเริ่ม" | P3 |
| TC-UI-STOCK-022 | เพิ่มสินค้าใหม่แล้วไปหน้าขาย | FR-015 | สินค้าใหม่ปรากฏบนชั้นวาง | P1 |

### 6.5 `/promotions` — CRUD โปรโมชั่น (Manager)

| ID | เคส | FR | คาดหวัง | P |
|---|---|---|---|---|
| TC-UI-PROMO-001 | Manager เปิดหน้า / Cashier เปิดหน้า | FR-029 | เห็นตาราง / เห็น ManagerOnly | P1 |
| TC-UI-PROMO-002 | **C**: สร้างแบบ "รายสินค้า" | FR-019 | เลือกสินค้าได้ และบันทึกสำเร็จ | P1 |
| TC-UI-PROMO-003 | C: เปลี่ยนขอบเขตเป็น "ทั้งบิล" | FR-019 | dropdown สินค้าหายไป และส่ง `productId=null` | P1 |
| TC-UI-PROMO-004 | C: dropdown เลือกสินค้า | FR-037 | แสดงสินค้า **ครบทุกชิ้น** (แคตตาล็อก > 100 ต้องเลือกชิ้นที่ 101 ได้) | P1 |
| TC-UI-PROMO-005 | C: ส่วนลด > 100 หรือ ≤ 0 | FR-019 | ถูกบล็อกด้วย `min` / `max` ก่อนยิง API | P1 |
| TC-UI-PROMO-006 | C: วันสิ้นสุดก่อนวันเริ่ม | FR-021 | เลือกไม่ได้ (`min` ผูกกับ `startDate`) | P1 |
| TC-UI-PROMO-007 | C: ติ๊ก "ส่วนลดสำหรับสมาชิกเท่านั้น" | FR-020 | คอลัมน์ "เงื่อนไข" แสดง "เฉพาะสมาชิก" | P1 |
| TC-UI-PROMO-008 | **R**: คอลัมน์สถานะ | FR-021 | โปรฯ ที่ครอบวันนี้ = "ใช้อยู่" · นอกช่วง = "ยังไม่เริ่ม/หมดอายุ" | P1 |
| TC-UI-PROMO-009 | R: ติ๊ก "เฉพาะที่ใช้ได้ตอนนี้" | FR-036 | เหลือเฉพาะที่ active | P1 |
| TC-UI-PROMO-010 | R: คอลัมน์ "ใช้กับ" | — | แสดงชื่อสินค้า ไม่ใช่ GUID (⚠️ ดู R-04) | P2 |
| TC-UI-PROMO-011 | **U**: กด "แก้ไข" | FR-019 | dialog เติมค่าเดิมครบ (ขอบเขต / สินค้า / % / วันที่ / เฉพาะสมาชิก) | P1 |
| TC-UI-PROMO-012 | U: แก้ % แล้วบันทึก | FR-019 | ตารางอัปเดต | P1 |
| TC-UI-PROMO-013 | **D**: กด "ลบ" | FR-019 | แถวหาย (⚠️ ไม่มีกล่องยืนยัน — ดู R-02) | P1 |
| TC-UI-PROMO-014 | D: ลบไม่สำเร็จ | — | ข้อความ "ไม่สามารถลบโปรโมชั่นได้" | P2 |
| TC-UI-PROMO-015 | สร้างโปรฯ Item ครอบวันนี้ แล้วไปขายสินค้านั้น | FR-019, FR-021 | ใบเสร็จแสดงส่วนลดของบรรทัดนั้น | P1 |
| TC-UI-PROMO-016 | สร้างโปรฯ Bill ครอบวันนี้ แล้วขาย | FR-019 | ส่วนลดกระจายทุกบรรทัด รวมเท่ายอดส่วนลดบิล | P1 |
| TC-UI-PROMO-017 | มีทั้งโปรฯ ทั่วไปและส่วนลดสมาชิกพร้อมกัน แล้วขายให้สมาชิก | FR-022 | ใบเสร็จใช้ **ส่วนลดเดียวที่มากที่สุด** ไม่บวกกัน | P1 |
| TC-UI-PROMO-018 | โปรฯ ที่หมดอายุแล้ว แล้วขาย | FR-021 | ไม่มีส่วนลด | P1 |

### 6.6 `/sales/history` — ประวัติการขาย

| ID | เคส | FR | คาดหวัง | P |
|---|---|---|---|---|
| TC-UI-HIST-001 | เปิดหน้า | FR-024 | ตารางเรียงบิลใหม่→เก่า | P1 |
| TC-UI-HIST-002 | กรอง "จากวันที่" / "ถึงวันที่" | FR-036 | กรองที่เซิร์ฟเวอร์ | P1 |
| TC-UI-HIST-003 | ติ๊ก "เฉพาะบิลของฉัน" | FR-008 | เหลือเฉพาะบิลของผู้ล็อกอิน | P1 |
| TC-UI-HIST-004 | กรองด้วยสมาชิก (ช่องค้นหาสมาชิก) | FR-013 | เหลือเฉพาะบิลของสมาชิกนั้น | P1 |
| TC-UI-HIST-005 | ใช้ตัวกรองหลายตัวพร้อมกัน | — | ผลเป็น AND | P2 |
| TC-UI-HIST-006 | ไปหน้า 3 แล้วเปลี่ยนตัวกรอง | FR-036 | กลับหน้า 1 | P1 |
| TC-UI-HIST-007 | ไล่ทุกหน้า | SC-008 | ไม่มีบิลซ้ำ/หาย และจำนวนที่นับได้ = ยอดรวมที่ระบบแจ้ง | P1 |
| TC-UI-HIST-008 | กรองแล้วไม่เจอ | — | "ไม่พบบิลขายในเงื่อนไขนี้ ลองขยายช่วงวันที่" | P2 |
| TC-UI-HIST-009 | กดปุ่ม "ใบเสร็จ" ในแถว | FR-030 | ไปหน้า `/sales/receipt/{id}` ของบิลนั้น | P1 |
| TC-UI-HIST-010 | Cashier เปิดหน้านี้ | FR-029 | เข้าได้ (ไม่ใช่ manager-only) — ⚠️ ยืนยันเจตนา ดู R-07 | P2 |
| TC-UI-HIST-011 | คอลัมน์ส่วนลด | — | บิลที่ไม่มีส่วนลดแสดง "—" ไม่ใช่ 0.00 | P3 |

### 6.7 `/sales/receipt/[id]` — ใบเสร็จ

| ID | เคส | FR | คาดหวัง | P |
|---|---|---|---|---|
| TC-UI-RCPT-001 | เปิดด้วย id ที่มีจริง | FR-030 | แสดงรายการ ราคาต่อชิ้น ส่วนลด และยอดสุทธิ ครบ | P1 |
| TC-UI-RCPT-002 | เปิดด้วย id ที่ไม่มี | — | "ไม่พบบิลขายนี้" | P2 |
| TC-UI-RCPT-003 | กด "พิมพ์ใบเสร็จ" | FR-030 | เรียก print ของเบราว์เซอร์ | P2 |
| TC-UI-RCPT-004 | ตอนพิมพ์ | FR-030 | สไตล์พิมพ์แสดงเฉพาะสลิป ไม่มีแถบเมนู/ปุ่ม | P2 |
| TC-UI-RCPT-005 | ใบเสร็จไม่มีบรรทัด VAT และไม่อ้างว่าเป็นใบกำกับภาษี | clarify #3 | ตรง | P2 |
| TC-UI-RCPT-006 | ใบเสร็จของบิลที่สินค้าถูกลบไปแล้ว | FR-023 | ยังแสดงชื่อ/ราคาเดิม | P1 |

### 6.8 `/reports` — รายงาน (Manager)

| ID | เคส | FR | คาดหวัง | P |
|---|---|---|---|---|
| TC-UI-RPT-001 | Cashier เปิดหน้า | FR-029 | ManagerOnly | P1 |
| TC-UI-RPT-002 | แท็บ "ยอดขายวันนี้" | FR-025 | 3 ตัวเลข (ยอดขายรวม / ส่วนลดรวม / จำนวนบิล) ตรงกับบิลจริงของวันนี้ | P1 |
| TC-UI-RPT-003 | ขายบิลใหม่แล้วกลับมาดู | SC-004 | ยอดรวมนับบิลนั้นทันที | P1 |
| TC-UI-RPT-004 | แท็บ "สินค้าขายดี" | FR-026 | เรียงมาก→น้อย และตรงกับยอดจริง | P1 |
| TC-UI-RPT-005 | แท็บ "ยอดขายตามพนักงาน" | FR-027 | จำนวนบิล / ยอดขายต่อคนตรง | P1 |
| TC-UI-RPT-006 | แท็บ "สต็อกคงเหลือ" | FR-028 | ทุกสินค้า + ป้าย "ใกล้หมด" ถูกต้อง | P1 |
| TC-UI-RPT-007 | เปลี่ยนช่วงวันที่ | FR-036 | แท็บขายดี / ตามพนักงานอัปเดตตาม | P1 |
| TC-UI-RPT-008 | แท็บสต็อกไม่มีตัวเลือกช่วงวันที่ | FR-028 | ถูกต้อง (เป็น snapshot) | P2 |
| TC-UI-RPT-009 | แท็บ "ยอดขายวันนี้" ไม่ผูกกับช่วงวันที่ที่เลือก | FR-025 | ⚠️ ยืนยันเจตนา — ดู R-03 | P2 |
| TC-UI-RPT-010 | ผู้จัดการหาสินค้าขายดีสุด + พนักงานยอดสูงสุดของเดือนที่แล้วได้จากหน้านี้ตรง ๆ | SC-006 | ผ่าน | P2 |
| TC-UI-RPT-011 | สลับแท็บไปมา | — | ไม่ยิงคำขอซ้ำเกินจำเป็น / ไม่ค้าง loading | P3 |

---

## 7. Non-functional: responsive + paging (FR-031–FR-037)

| ID | เคส | FR/SC | คาดหวัง | P |
|---|---|---|---|---|
| TC-NFR-RSP-001 | 7 หน้า × 4 ความกว้าง (360 / 768 / 1024 / 1440 px) = 28 scenario | FR-031, SC-007 | ไม่มีการเลื่อนแนวนอนของทั้งหน้า และไม่มีเนื้อหาถูกบัง | P1 |
| TC-NFR-RSP-002 | ตารางที่กว้างเกินจอ | FR-031 | เลื่อนแนวนอน **ภายในกรอบตาราง** เท่านั้น | P1 |
| TC-NFR-RSP-003 | แผงตะกร้าในหน้าขายที่ 768–1100 px | FR-032 | กินไม่เกิน 32% ของความกว้างหน้าจอ | P1 |
| TC-NFR-RSP-004 | พื้นที่ชั้นวางสินค้าที่ 900 px | FR-032 | ไม่น้อยกว่า 68% | P1 |
| TC-NFR-RSP-005 | ปุ่ม / ช่องกรอกที่ ≤ 767 px | FR-033 | สูง ≥ 44 px ทุกตัวที่ต้องแตะ | P1 |
| TC-NFR-RSP-006 | ตะกร้าบนมือถือ (แผ่นเลื่อนล่างจอ) | FR-032 | เปิด/ปิดได้ ปุ่มชำระเงินอยู่ในจอเสมอ ไม่บังแถวสินค้าสุดท้าย | P1 |
| TC-NFR-PAG-001 | ทุก endpoint ที่แบ่งหน้า: default `pageSize=20`, ช่วง 1–100 | FR-034 | ตรง | P1 |
| TC-NFR-PAG-002 | ค่านอกช่วง | FR-035 | 400 ไม่ปัดค่าเงียบ ๆ | P1 |
| TC-NFR-PAG-003 | `totalCount` เป็นจำนวนหลังกรอง | FR-034 | ตรงทั้ง products และ sales | P1 |
| TC-NFR-PAG-004 | เปลี่ยนตัวกรอง → กลับหน้า 1 ทุกตารางที่มีตัวแบ่งหน้า | FR-036 | ตรง | P1 |
| TC-NFR-PAG-005 | `fetchAllPages()` ไล่อ่านจนครบ `totalPages` ไม่ตัดที่ 100 | FR-037 | ตรง (แปลงจาก `web/scripts/verify-fetch-all.cjs`) | P1 |
| TC-NFR-PAG-006 | `searchProductsByNameOrBarcode()` รวมผลชื่อ + บาร์โค้ดโดยไม่ซ้ำ และผลบาร์โค้ดมาก่อน | SC-010 | ตรง (แปลงจากสคริปต์เดิม) | P1 |
| TC-NFR-PAG-007 | `fetchAllPages()` เจอหน้าว่างทั้งที่ `totalPages` ยังไม่หมด | — | หยุด ไม่วนไม่รู้จบ | P2 |

---

## 8. สิ่งที่ต้องตัดสินใจก่อนลงมือ (ต้องการคำตอบ)

| # | เรื่อง | ทางเลือก | ข้อเสนอ |
|---|---|---|---|
| D-1 | **เครื่องมือ E2E** — `web/` ไม่มี test framework ใด ๆ เลย | Playwright · Cypress · ไม่ทำ E2E (ทดสอบมือตาม quickstart) | **Playwright** — โปรเจกต์เคยใช้ผ่าน MCP มาแล้ว (มีโฟลเดอร์ `.playwright-mcp/`) และรองรับการวัดหลาย viewport สำหรับ FR-031–033 ได้ในตัว |
| D-2 | **วิธีเลือก element** — ตอนนี้มี `data-testid` **0 จุด** ทั้งโปรเจกต์ | (ก) เพิ่ม `data-testid` ในคอมโพเนนต์จริง · (ข) ใช้ `getByRole` + ข้อความไทยล้วน | **(ก)** สำหรับจุดที่ข้อความเปลี่ยนบ่อย (ปุ่มในแถวตาราง, ป้ายสถานะ, แถวตาราง, dialog) และ **(ข)** สำหรับปุ่มหลัก — แต่ (ก) แตะโค้ดแอปจริง จึงขอให้ตัดสินใจก่อน |
| D-3 | **Unit test ฝั่งหน้าเว็บ** — `DataTable` (page reset), `fetchAllPages`, `searchProductsByNameOrBarcode`, `tokenStore` เป็นลอจิกที่เทสต์ได้เร็วกว่ามากถ้าไม่ผ่านเบราว์เซอร์ | เพิ่ม Vitest + React Testing Library · ครอบด้วย E2E อย่างเดียว | **เพิ่ม Vitest** — ลอจิก paging/merge คุ้มที่จะเทสต์ระดับหน่วย ไม่ต้องรอเบราว์เซอร์ |
| D-4 | **ชุดข้อมูลทดสอบ** — seed ปัจจุบันมีสินค้า **2 รายการ** ทดสอบ paging / FR-037 ไม่ได้ | เขียน `TestDataSeeder` แยก (> 120 สินค้า, > 50 บิลข้ามวันข้ามพนักงาน, สมาชิกหลายคน, โปรฯ ทั้ง active และหมดอายุ) | **ทำ** — เป็นเงื่อนไขบังคับของ TC-UI-SALES-001, TC-UI-PROMO-004 และ TC-\*-PAG-\* ทั้งหมด |
| D-5 | **สภาพแวดล้อมรัน E2E** | รันในเครื่อง (Postgres `taladpos_test` + API + `next dev`) · docker-compose ชุดเทสต์แยก | เริ่มจากรันในเครื่อง โดยยืมรูปแบบ env var ของ `TaladPOSApiFactory` |
| D-6 | **ขอบเขตรอบแรก** — เอกสารนี้มี **265 เคส** (P1 217 · P2 42 · P3 6) ในจำนวนนี้ **มีอยู่แล้ว 47 · ต้องเขียนใหม่ 218** | ทำ P1 ทั้งหมดก่อน (217 เคส) · ทำเฉพาะ API + CRUD UI ก่อน (89 + 111 = 200 เคส) · ทำทั้งหมด (265) | ขอให้ระบุ — มีผลต่อการแบ่งงานและลำดับในหัวข้อ 10 |

---

## 9. ประเด็นที่พบระหว่างอ่านโค้ด (ต้องชี้ขาดก่อนเขียนเทสต์)

เทสต์ต้องยืนยัน "สิ่งที่ควรเป็น" ไม่ใช่ "สิ่งที่โค้ดทำอยู่" — 7 ข้อนี้ต้องได้ข้อสรุปก่อน ไม่งั้นจะเขียนเทสต์ล็อกพฤติกรรมที่อาจเป็นบั๊กเอาไว้

| # | ประเด็น | ที่มาในโค้ด | ทำไมต้องชี้ขาด |
|---|---|---|---|
| R-01 | **ตะกร้าแสดงแค่ "ยอดรวม" ที่เป็น subtotal** — ไม่มีบรรทัดส่วนลดและยอดสุทธิก่อนกดชำระเงิน ส่วนลดคำนวณฝั่งเซิร์ฟเวอร์ตอนปิดบิล ลูกค้าจึงเห็นยอดจริงครั้งแรกบนใบเสร็จ | `web/src/components/Cart.tsx:37` (คำนวณ), `Cart.tsx:222` (แสดงผล) | FR-006 เขียนว่า "คำนวณและแสดงยอดรวมย่อย **ส่วนลดที่เกี่ยวข้อง และยอดรวมสุทธิ** แบบเรียลไทม์" — ตอนนี้ไม่ตรง เป็นบั๊กหรือจะปรับสเปก? |
| R-02 | **ลบสินค้า/โปรโมชั่นไม่มีกล่องยืนยัน** — กดปุ่ม "ลบ" แล้วหายทันที | `web/src/app/(protected)/stock/page.tsx:75`, `web/src/app/(protected)/promotions/page.tsx:60` | ถ้าตั้งใจ เทสต์จะล็อกไว้แบบนี้ ถ้าไม่ตั้งใจควรแก้ก่อนเขียนเทสต์ |
| R-03 | **แท็บ "ยอดขายวันนี้" ไม่ผูกกับตัวกรองช่วงวันที่** — ใช้ `today` ตายตัว ขณะที่อีก 2 แท็บใช้ `from` / `to` | `web/src/app/(protected)/reports/page.tsx:63` | ผู้จัดการเปลี่ยนช่วงวันที่แล้วตัวเลขแท็บแรกไม่ขยับ อาจสับสน |
| R-04 | **คอลัมน์ "ใช้กับ" fallback เป็น GUID** เมื่อหาชื่อสินค้าไม่เจอในลิสต์ที่โหลดมา | `web/src/app/(protected)/promotions/page.tsx:72` | ต้องยืนยันว่าไล่อ่านครบทุกหน้าจริง ไม่งั้นโปรฯ ของสินค้าชิ้นที่ 101 จะโชว์ GUID |
| R-05 | **จำนวนสูงสุดในตะกร้าอิงสต็อกจากตอนโหลดหน้า** — ถ้าจุดขายอื่นขายไปก่อน ค่า clamp จะล้าสมัย | `web/src/components/Cart.tsx:187`, `web/src/app/(protected)/sales/page.tsx:42` | เซิร์ฟเวอร์กันไว้อยู่แล้ว (409) แต่ต้องยืนยันว่า UI แสดงข้อความให้เข้าใจ → TC-UI-SALES-016 |
| R-06 | **โปรฯ Item 100% + Bill 100% พร้อมกัน ทำให้ยอดบรรทัดติดลบได้** (บันทึกไว้แล้วใน `tasks.md` T060 ว่าเป็นช่องว่างที่ยังไม่กัน) | `SaleLineItem` ไม่มีเพดาน `DiscountAmount` | ตัดสินว่าจะกันในรอบนี้ (แล้วเขียน TC-DOM-SALE-006) หรือรับไว้เป็นหนี้ที่รู้ตัว |
| R-07 | **Cashier เข้า `/sales/history` ได้** — FR-029 ระบุจำกัดเฉพาะ stock/promotions/reports แต่ก็บอกว่าแคชเชียร์เข้าได้ "เฉพาะหน้าขายสินค้าและการค้นหา/สมัครสมาชิก" | `web/src/components/AppShell.tsx:13-14` (`CASHIER_LINKS`) | สองประโยคใน FR-029 ตีความได้ต่างกัน ต้องชี้ว่าประวัติการขายเป็นของแคชเชียร์ด้วยหรือไม่ |
| R-08 | **รูปแบบ error body ของ 409 ไม่ตรงกันระหว่างเอกสารกับโค้ด** — `contracts/sales.md` เขียน `{"error":"insufficient_stock","productId":…}` (แบน) แต่โค้ดส่ง `{"error":"insufficient_stock","details":{"productId":…}}` (ซ้อน) เคสเดียวกันนี้กระทบ `duplicate_barcode` ด้วย | `api/src/TaladPOS.Api/Middleware/ErrorHandlingMiddleware.cs:31,35` เทียบกับ `contracts/sales.md`, `contracts/products.md` | ต้องชี้ว่าฝั่งไหนคือฉบับจริง แล้วแก้อีกฝั่งให้ตรง ก่อนเขียน TC-API-SALE-008 / TC-API-X-005 — ไม่งั้นเทสต์จะล็อกรูปแบบที่ contract ไม่รับรอง<br>*(ตรวจแล้วว่า `invalid_pagination` และ `invalid_credentials` controller ส่งออกมาแบนตรงตาม contract จริง — ไม่ผ่าน middleware)* |

---

## 10. ลำดับการทำและวิธีรัน

**ลำดับที่เสนอ** (แต่ละเฟสจบแล้วรันได้จริงก่อนไปต่อ)

1. **เฟส 0 — เตรียม**: ตอบหัวข้อ 8, ชี้ขาดหัวข้อ 9, ลบ `UnitTest1.cs` ทั้ง 2 ไฟล์, เขียน `TestDataSeeder`
2. **เฟส 1 — Application** (เร็วสุด ไม่ต้องมี DB): TC-APP-PROMO-\*, TC-APP-RPT-003→007, TC-APP-PROD-005
3. **เฟส 2 — API integration** (ต่อยอดจาก `TaladPOSApiFactory` ที่มีอยู่): 5.1 → 5.7 เรียงตามลำดับ
4. **เฟส 3 — Unit ฝั่งเว็บ** (ถ้ารับ D-3): `fetchAllPages`, `searchProductsByNameOrBarcode`, `DataTable` page reset, `tokenStore`
5. **เฟส 4 — E2E CRUD** (ถ้ารับ D-1 / D-2): 6.1 → 6.8 เรียงตามลำดับ
6. **เฟส 5 — Non-functional**: หัวข้อ 7 (responsive + paging)
7. **เฟส 6**: อัปเดต `quickstart-results.md` ด้วยผลรันจริง

**คำสั่งรัน**

```bash
# ฐานข้อมูลสำหรับเทสต์
docker compose up -d postgres

# Domain + Application + API integration ทั้งหมด
cd api && dotnet test TaladPOS.sln

# เฉพาะชั้นเดียว
dotnet test tests/TaladPOS.Domain.Tests
dotnet test tests/TaladPOS.Application.Tests
dotnet test tests/TaladPOS.Api.IntegrationTests   # ต้องมี Postgres; override ด้วย TEST_DB_CONNECTION

# ฝั่งเว็บ (หลังเพิ่มเครื่องมือตาม D-1 / D-3)
cd web && npm run test          # Vitest
cd web && npm run test:e2e      # Playwright

# ตรวจสิ่งที่มีอยู่แล้ววันนี้
cd web && node scripts/verify-fetch-all.cjs   # FR-037 + SC-010 (ต้องมี API รันอยู่)
cd web && npx tsc --noEmit && npm run build && npx eslint src --max-warnings=0
```

**เกณฑ์ปิดงาน**: P1 ผ่านครบ 100% · P2 ผ่าน ≥ 90% · ประเด็น R-01…R-07 ทุกข้อมีข้อสรุปบันทึกไว้ · `quickstart-results.md` อัปเดตด้วยผลรันจริง
