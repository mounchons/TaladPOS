# Quickstart: ระบบ POS สำหรับร้านค้าเดี่ยว (Single-Store POS)

คู่มือนี้ใช้ตรวจสอบว่าฟีเจอร์ทำงานได้จริงแบบ end-to-end ตาม acceptance scenarios ใน [spec.md](./spec.md) ไม่ใช่
คู่มือ implementation — รายละเอียด endpoint ดู [contracts/](./contracts/), รายละเอียดโครงสร้างข้อมูลดู
[data-model.md](./data-model.md)

## Prerequisites

- .NET 8 SDK
- Node.js 20 LTS + npm
- PostgreSQL 16 (รันในเครื่องหรือผ่าน Docker) พร้อม database ว่างชื่อ `taladpos`

## 1. ตั้งค่าและรัน `api/`

```bash
docker compose up -d postgres   # เริ่ม PostgreSQL 16 ตาม docker-compose.yml ที่ root ของ repo
cd api
dotnet restore
dotnet ef database update --project src/TaladPOS.Infrastructure --startup-project src/TaladPOS.Api
dotnet run --project src/TaladPOS.Api
```

API ควรพร้อมใช้งานที่ `http://localhost:5054` (ตาม `Properties/launchSettings.json` โปรไฟล์ `http`; ใช้ `dotnet run --launch-profile https` แทนถ้าต้องการ HTTPS ที่ `https://localhost:7096`)

**รัน unit test ของ business logic ก่อนเริ่มพัฒนาเพิ่ม (constitution Principle III — NON-NEGOTIABLE)**:

```bash
dotnet test api/tests/TaladPOS.Domain.Tests
dotnet test api/tests/TaladPOS.Application.Tests
```

ทั้งสองชุดต้องผ่าน 100% ก่อน merge งานใด ๆ ที่แตะ business logic

## 2. ตั้งค่าและรัน `web/`

```bash
cd web
npm install
npm run dev
```

`web/` ควรพร้อมใช้งานที่ `http://localhost:3000` และเรียก `api/` ผ่านตัวแปรแวดล้อม `NEXT_PUBLIC_API_BASE_URL`
เท่านั้น (ไม่มีการเชื่อมต่อ PostgreSQL โดยตรงจากฝั่งนี้ — ตรวจสอบได้จากการไม่มี connection string ใด ๆ ใน `web/`)

## 3. Seed ข้อมูลตั้งต้น

ก่อนทดสอบ ต้องมีข้อมูลอย่างน้อย:
- พนักงาน 1 คน role `Manager` และ 1 คน role `Cashier` (username/password สำหรับทดสอบล็อกอิน)
- สินค้าตัวอย่างอย่างน้อย 2 ชิ้น เช่น "มะม่วง" (มีบาร์โค้ด, ราคา 45, สต็อก 10) และ "แอปเปิ้ล" (ไม่มีบาร์โค้ด, ราคา 60, สต็อก 3)

(รายละเอียดวิธี seed — ผ่าน migration seed data หรือ script แยก — กำหนดตอน implementation ใน tasks.md)

## 4. Validation Scenarios (อ้างอิงตาม User Story ใน spec.md)

### US1 — พนักงานทำการขายสินค้า (P1)

1. ล็อกอินด้วยบัญชี Cashier ผ่าน `POST /api/v1/auth/login` (ดู `contracts/auth.md`) → ได้ JWT token
2. `GET /api/v1/products?search=มะม่วง` → ต้องเจอสินค้า "มะม่วง" (สอดคล้อง Acceptance Scenario #1)
3. `GET /api/v1/products?barcode=<บาร์โค้ดมะม่วง>` → ต้องเจอสินค้าเดียวกัน (Acceptance Scenario #2)
4. `POST /api/v1/sales` พร้อม `lineItems: [{ productId: <มะม่วง>, quantity: 2 }]` (ดู `contracts/sales.md`) → ได้ 201
   พร้อม `totalAmount` ถูกต้อง และ `GET /api/v1/products/<มะม่วง>` ต้องแสดง `stockQuantity` ลดลง 2 (Acceptance Scenario #4)
5. `POST /api/v1/sales` ด้วย `lineItems: []` → ต้องได้ 400 (Edge Case: ห้ามชำระเงินตะกร้าว่าง)
6. **ตรวจฝั่ง `web/` (FR-030)**: ที่ `http://localhost:3000/sales` แตะสินค้าแล้วกด "ชำระเงิน" → ใบเสร็จต้องเด้งเป็น
   modal ทันที (หัวข้อ "ขายสำเร็จ") แสดงรายการ/ส่วนลด/ยอดสุทธิตรงกับ response ของ `POST /api/v1/sales` และมีปุ่ม
   "พิมพ์ใบเสร็จ" — สั่งพิมพ์แล้วต้องได้ **เฉพาะตัวใบเสร็จหน้าเดียว** ไม่มีแถบเมนู ตารางสินค้า แผงตะกร้า หรือปุ่มใน
   dialog ติดออกมา (ดู research.md #5.1); กดปิดแล้วลิงก์ "ใบเสร็จบิลล่าสุด" ใต้ปุ่มชำระเงินต้องเปิดใบเสร็จเดิมซ้ำได้
   และการแตะนอก dialog ต้องไม่ปิดใบเสร็จ

### US2 — พนักงานล็อกอินและถูกบันทึกว่าเป็นผู้ขาย (P2)

1. เรียก `POST /api/v1/sales` โดยไม่แนบ `Authorization` header → ต้องได้ 401
2. ล็อกอินสำเร็จแล้วทำ Sale ตาม US1 → `GET /api/v1/sales/{id}` ต้องแสดง `staff.name` ตรงกับบัญชีที่ล็อกอิน
   (Acceptance Scenario #2)

### US3 — ผู้จัดการดูแลสต็อกสินค้า (P3)

1. ล็อกอินด้วยบัญชี Manager → `POST /api/v1/products` เพิ่มสินค้าใหม่ (ดู `contracts/products.md`) → 201
2. `GET /api/v1/products` (ไม่ล็อกอินเป็น Cashier ก็ต้องเห็นสินค้านี้ในหน้าขาย) → เจอสินค้าที่เพิ่งเพิ่ม (Acceptance Scenario #1)
3. `PUT /api/v1/products/{id}` แก้ราคา → `GET /api/v1/products/{id}` ต้องแสดงราคาที่อัปเดตแล้ว (Acceptance Scenario #2)
4. ตั้ง `stockQuantity` ให้ <= `lowStockThreshold` (เช่นขายจนเหลือน้อย) → `GET /api/v1/products?lowStockOnly=true` ต้องเจอ
   สินค้านี้ (Acceptance Scenario #3 / SC-003)
5. ลองสั่งซื้อ (`POST /api/v1/products` โดย login เป็น Cashier) → ต้องได้ 403 (FR-029)

### US4 — สมัครสมาชิกและรับส่วนลดสมาชิก (P4)

1. `POST /api/v1/members` ด้วยเบอร์โทร/ชื่อใหม่ (ดู `contracts/members.md`) → 201
2. `POST /api/v1/members` ด้วยเบอร์โทรเดิม → ต้องได้ 409 (FR-011)
3. `GET /api/v1/members?search=<เบอร์โทร>` → เจอสมาชิกที่เพิ่งสมัคร (Acceptance Scenario #2)
4. `POST /api/v1/sales` พร้อม `memberId` ของสมาชิกนั้น → หลังสำเร็จ `GET /api/v1/members/{id}` ต้องแสดง
   `accumulatedPurchaseTotal` เพิ่มขึ้นตาม `totalAmount` ของบิล (Acceptance Scenario #3)

### US5 — ผู้จัดการตั้งโปรโมชั่นส่วนลด (P5)

1. `POST /api/v1/promotions` สร้างโปรโมชั่นรายสินค้า % ให้ครอบคลุมวันนี้ (ดู `contracts/promotions.md`)
2. ทำ Sale ของสินค้านั้น → `discountAmount` ใน response ต้อง > 0 ตรงกับ % ที่ตั้งไว้ (Acceptance Scenario #1)
3. สร้างโปรโมชั่นที่ `endDate` เป็นอดีต แล้วทำ Sale สินค้าเดียวกัน → `discountAmount` ต้องเป็น 0 จากโปรโมชั่นนี้
   (Acceptance Scenario #3)
4. ทดสอบเคสมีทั้งโปรโมชั่นและส่วนลดสมาชิกพร้อมกัน (ผูก `memberId` ที่มีส่วนลดสมาชิก active) → `discountAmount` ต้องเท่ากับ
   ค่าที่มากกว่าเพียงค่าเดียว ไม่ใช่ผลรวมของทั้งสอง (FR-022)

### US6 — ดูประวัติการขายและรายงานสรุป (P6)

1. `GET /api/v1/sales?from=<วันนี้>&to=<วันนี้>` → เห็นบิลที่ทำไปใน US1–US5 (Acceptance Scenario #1)
2. `GET /api/v1/reports/sales?period=daily&date=<วันนี้>` (ดู `contracts/reports.md`) → `totalSalesAmount` ต้องตรงกับ
   ผลรวมบิลจริงในวันนั้น (Acceptance Scenario #2 / SC-004)
3. `GET /api/v1/reports/sales-by-staff?from=...&to=...` → ยอดของพนักงานแต่ละคนตรงกับบิลที่คนนั้นขาย (Acceptance Scenario #3)
4. `GET /api/v1/reports/stock` → จำนวนคงเหลือตรงกับที่เห็นใน `GET /api/v1/products` (Acceptance Scenario #4)
5. `GET /api/v1/reports/best-selling-products?from=<วันนี้>&to=<วันนี้>&limit=10` (ดู `contracts/reports.md`) →
   อันดับสินค้าต้องตรงกับที่รวม `quantity` เองจากบิลจริงในช่วงนั้น และเรียงจากมากไปน้อย (FR-026) — ข้อนี้คู่กับข้อ 3
   ทำให้ SC-006 ครบทั้งสองครึ่ง (สินค้าขายดีที่สุด + พนักงานยอดขายสูงสุด) โดยผู้จัดการไม่ต้องดึงข้อมูลดิบไปคำนวณเอง

## 5. ตรวจสอบ Success Criteria (SC) จาก spec.md

SC ส่วนใหญ่ถูกพิสูจน์โดย scenario ที่มีอยู่แล้วข้างบน ตารางนี้คือจุดผูกให้ชัดว่าข้อไหนพิสูจน์ด้วยอะไร
(ก่อนหน้านี้ไม่มี scenario ใดอ้าง SC เลย ทำให้ไม่รู้ว่าเกณฑ์ความสำเร็จถูกตรวจครบหรือยัง)

| SC | พิสูจน์ด้วย | เกณฑ์ผ่าน |
|---|---|---|
| SC-001 | **ตรวจใหม่ด้านล่าง (SC-001)** | ทำรายการตะกร้า 5 ชิ้นครบวงจรภายใน ≤ 60 วินาที |
| SC-002 | **ตรวจใหม่ด้านล่าง (SC-002)** | สต็อกที่หายไปของทุกสินค้า = ผลรวมจำนวนที่ขายจริง ไม่คลาดเคลื่อนแม้แต่ชิ้นเดียว |
| SC-003 | US3 ข้อ 4 | สินค้าที่คงเหลือ ≤ เกณฑ์ ปรากฏใน `?lowStockOnly=true` ทันทีที่เรียกครั้งถัดไป |
| SC-004 | US6 ข้อ 2 | `totalSalesAmount` ของวันนี้ = ผลรวมบิลจริงทุกบิลจนถึงขณะนั้น |
| SC-005 | **ไม่ตรวจอัตโนมัติ** | เป็นผลลัพธ์ด้านการใช้งานของพนักงานจริง วัดได้จากการสังเกตหน้างานเท่านั้น ไม่ใช่สิ่งที่ scenario พิสูจน์ได้ |
| SC-006 | US6 ข้อ 3 + ข้อ 5 | ระบุสินค้าขายดีที่สุดและพนักงานยอดขายสูงสุดได้จาก endpoint รายงานโดยตรง |

**SC-001 — ความเร็วในการทำรายการ**: จับเวลาตั้งแต่เริ่มค้นหาสินค้าชิ้นแรกจนได้ 201 จาก `POST /api/v1/sales`
สำหรับตะกร้าที่มีสินค้ารวม 5 ชิ้น → ต้องไม่เกิน 60 วินาที (วัดเฉพาะเวลาที่ระบบใช้ ไม่รวมเวลาที่คนคิด)

**SC-002 — ความถูกต้องของการตัดสต็อก**: บันทึก `stockQuantity` ของสินค้าที่จะขายไว้ก่อน แล้วทำหลายบิล
จากนั้นอ่านค่าใหม่ → ส่วนต่างของแต่ละสินค้าต้องเท่ากับผลรวม `quantity` ที่ขายไปพอดีทุกตัว

## 6. ตรวจสอบ Non-Functional จากผลการ Clarify

- **Concurrency**: จำลองสองคำขอ `POST /api/v1/sales` พร้อมกันที่ขอซื้อสินค้าชิ้นสุดท้ายชิ้นเดียวกัน (`stockQuantity == 1`)
  → ต้องมีคำขอเดียวที่ได้ 201 อีกคำขอต้องได้ 409 `insufficient_stock` (ไม่ใช่ทั้งคู่สำเร็จหรือสต็อกติดลบ)
- **Append-only**: ต้องไม่มี endpoint ใดใน `contracts/sales.md` ที่แก้ไข/ลบบิลที่สร้างแล้ว
- **ไม่มี VAT**: `totalAmount` ทุกบิลต้องเท่ากับ `subtotalAmount - discountAmount` พอดี ไม่มีการบวกภาษีเพิ่ม
