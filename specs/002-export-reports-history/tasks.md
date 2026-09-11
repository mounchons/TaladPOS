---

description: "Task list template for feature implementation"
---

# Tasks: Export รายงานสต็อกคงเหลือและประวัติการขาย

**Input**: Design documents from `/specs/002-export-reports-history/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/export-routes.md](./contracts/export-routes.md), [quickstart.md](./quickstart.md)

**Tests**: ไม่มี automated test task ใน list นี้ — `web/` ไม่มี test framework ตั้งค่าไว้ในโปรเจกต์ปัจจุบัน
(ดู plan.md > Technical Context > Testing) และ constitution Principle III (test-first, NON-NEGOTIABLE) บังคับ
เฉพาะ business logic ใน `api/` เท่านั้น ซึ่งฟีเจอร์นี้ไม่แตะเลย (ไม่มี endpoint ใหม่ใน `api/`) การยืนยันความ
ถูกต้องจึงเป็น task "ตรวจสอบด้วยมือตาม quickstart.md" แทนที่ task ทดสอบอัตโนมัติ

**Organization**: จัดกลุ่ม task ตาม User Story (US1, US2 — ทั้งคู่ priority P1) จาก spec.md

## Format: `[ID] [P?] [Story] Description`

- **[P]**: รันขนานได้ (คนละไฟล์ ไม่มี dependency ค้าง)
- **[Story]**: user story ที่ task นี้สังกัด (US1, US2)
- ทุก task ระบุ path ไฟล์ตรง ๆ ตาม Project Structure ใน plan.md

## Path Conventions (จาก plan.md)

ทุกไฟล์อยู่ใต้ `web/` เท่านั้น — ฟีเจอร์นี้ไม่แก้ `api/` เลย (ดู plan.md > Summary)

---

## Phase 1: Setup

**Purpose**: เตรียม dependency ใหม่ที่ต้องใช้

- [ ] T001 เพิ่ม dependency `exceljs` ใน `web/package.json` (`cd web && npm install exceljs`) ตาม research.md ข้อ 1

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: กลไก export ที่ใช้ร่วมกันทั้งสอง User Story (Route Handler แปลงไฟล์ + client helper เรียกใช้ +
ปุ่ม UI กลาง) — ทั้ง US1 และ US2 พึ่งพาส่วนนี้ทั้งหมด ต้องเสร็จก่อนเริ่ม story ใด ๆ

**⚠️ CRITICAL**: ห้ามเริ่มงาน User Story ใดจนกว่า phase นี้จะเสร็จ

- [ ] T002 [P] สร้าง shared types `ExportColumn<T>` (data-model.md ข้อ 1) และ `XlsxExportRequest` (data-model.md
  ข้อ 5: `{ filenamePrefix, sheetName, columns: {header}[], rows: (string|number)[][] }`) ใน
  `web/src/lib/export/types.ts`
- [ ] T003 Implement Next.js Route Handler `POST /api/export/xlsx` ตาม contracts/export-routes.md ใน
  `web/src/app/api/export/xlsx/route.ts` (depends on T001, T002): validate `rows[i].length === columns.length`
  ทุกแถว (ไม่ตรงคืน `400 { "error": "invalid_export_request" }`), `rows` ว่าง (`[]`) ต้องสร้างไฟล์ที่มีแค่หัว
  คอลัมน์สำเร็จ (FR-011 — ไม่ error), สร้าง workbook ด้วย `exceljs`: sheet ชื่อตาม `sheetName`, แถวแรกเป็นหัว
  คอลัมน์ตัวหนา (bold), ปรับความกว้างคอลัมน์ตามความยาวเนื้อหาโดยประมาณ, ตอบกลับด้วย
  `Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` และ
  `Content-Disposition: attachment; filename="<filenamePrefix>-<YYYY-MM-DD-HHmm>.xlsx"` (FR-009, FR-010)
- [ ] T004 Implement client helper `triggerXlsxExport(request: XlsxExportRequest): Promise<void>` ใน
  `web/src/lib/export/xlsxClient.ts` ตาม research.md ข้อ 4 (depends on T002, T003): `fetch("POST", ...)` ไป
  `/api/export/xlsx` พร้อม JSON body, รับ response กลับมาเป็น `Blob`, สร้าง Object URL ชั่วคราวเพื่อ trigger
  การดาวน์โหลดผ่าน `<a download>` ที่สร้างขึ้นด้วย JavaScript แล้ว revoke Object URL ทันทีหลังคลิก, throw
  error ที่มีข้อความอ่านง่ายถ้า response ไม่ใช่ `200`
- [ ] T005 [P] สร้าง shared component `ExportButton` ใน `web/src/components/ExportButton.tsx`: รับ prop
  `onExport: () => Promise<void>`, จัดการ loading state ของตัวเอง (แสดงข้อความ "กำลังเตรียมไฟล์..." และ
  `disabled` ปุ่มระหว่างรอ กันกดซ้ำซ้อนตาม FR-008), จับ error จาก `onExport` แล้วแสดงข้อความ error ใต้ปุ่ม
  (ใช้พื้นที่ข้อความเดียวกันนี้แสดงข้อความ "เกินเพดาน" ของ US2 ได้ด้วย เพราะ `onExport` ของ US2 จะ throw
  ข้อความนั้นแทนที่จะเรียก `triggerXlsxExport`)

**Checkpoint**: กลไก export ใช้งานได้จริงแบบ end-to-end (ทดสอบยิง request ตรง ๆ ไป
`POST /api/export/xlsx` ด้วยข้อมูลตัวอย่างแล้วได้ไฟล์ .xlsx ที่เปิดได้) — เริ่มงาน User Story ได้

---

## Phase 3: User Story 1 - ผู้จัดการ export รายงานสต็อกคงเหลือ (Priority: P1) 🎯 MVP

**Goal**: ผู้จัดการกดปุ่มบนแท็บ "สต็อกคงเหลือ" ของหน้ารายงาน แล้วได้ไฟล์ Excel ที่มีข้อมูลตรงกับที่แสดงบนจอ
ทุกแถว

**Independent Test**: ล็อกอินด้วยบัญชีผู้จัดการ เข้าหน้ารายงาน สลับไปแท็บ "สต็อกคงเหลือ" กดปุ่ม export แล้ว
ตรวจว่าได้ไฟล์ที่มีจำนวนแถวและข้อมูลตรงกับที่แสดงบนตารางหน้าจอทุกแถว โดยไม่ต้องพึ่ง User Story 2

### Implementation for User Story 1

- [ ] T006 [US1] เพิ่มปุ่ม `<ExportButton>` บนแท็บ "สต็อกคงเหลือ" ใน
  `web/src/app/(protected)/reports/page.tsx` (depends on T004, T005): `onExport` แปลง state `stock`
  (`StockReportRow[]`) เป็น `columns`/`rows` ตาม data-model.md ข้อ 2 (ชื่อสินค้า, จำนวนคงเหลือ,
  สถานะ = `row.isLowStock ? "ใกล้หมด" : "ปกติ"`) แล้วเรียก `triggerXlsxExport({ filenamePrefix:
  "taladpos-stock-report", sheetName: "สต็อกคงเหลือ", columns, rows })` — ปุ่มอยู่ในเนื้อหาของ
  `activeTab === 3` เท่านั้น ใช้ guard `staff?.role !== "Manager"` ที่มีอยู่แล้วบนหน้านี้ควบคุมสิทธิ์
  (FR-007 — ไม่ต้องเขียนตรวจสิทธิ์ซ้ำ เพราะ Cashier เข้าหน้านี้ไม่ได้ตั้งแต่แรกอยู่แล้ว)
- [ ] T007 [US1] ตรวจสอบด้วยมือตาม quickstart.md หัวข้อ 1 ทั้งหมด (กรณีข้อมูลปกติ, ตารางว่าง 0 แถว
  ต้องได้ไฟล์ที่มีแค่หัวคอลัมน์ตาม FR-011, และยืนยันว่าบัญชี Cashier มองไม่เห็นปุ่มนี้เลยตาม SC-005)

**Checkpoint**: User Story 1 ใช้งานได้ครบและทดสอบผ่านอิสระจาก User Story 2 — นี่คือ MVP

---

## Phase 4: User Story 2 - พนักงาน export ประวัติการขาย (Priority: P1)

**Goal**: พนักงาน (ผู้จัดการหรือแคชเชียร์) กดปุ่ม export บนหน้าประวัติการขาย แล้วได้ไฟล์ที่มีบิลครบทุกรายการ
ที่ตรงกับตัวกรองปัจจุบัน ไม่ใช่แค่หน้าที่กำลังแสดงอยู่บนจอ

**Independent Test**: สร้างบิลขายให้มากกว่า 20 บิลในช่วงวันที่เดียวกัน ตั้งตัวกรองให้ครอบคลุมบิลทั้งหมดนั้น
กดปุ่ม export แล้วตรวจว่าจำนวนแถวในไฟล์เท่ากับจำนวนบิลทั้งหมดที่ตรงกับตัวกรอง ไม่ใช่แค่ 20 แถวของหน้าแรก
— ทดสอบแยกจาก User Story 1 ได้อิสระ (ไม่ต้องพึ่งแท็บรายงานสต็อกเลย)

### Implementation for User Story 2

- [ ] T008 [P] [US2] Implement `fetchAllSalesForExport(filters): Promise<FetchAllSalesResult>` ใน
  `web/src/lib/api/sales.ts` ตาม data-model.md ข้อ 4 และ research.md ข้อ 3: เรียกหน้าแรกด้วย
  `searchSalesPaged({ ...filters, page: 1, pageSize: 100 })`, ถ้า `totalCount > 10_000`
  (ค่าคงที่ `SALES_EXPORT_ROW_CAP`) คืน `{ status: "cap_exceeded", totalCount }` **ทันที** โดยไม่ยิงหน้า
  ถัดไปต่อ, ถ้าไม่เกินให้วนเรียกหน้าถัดไปจนครบ `totalPages` แล้วคืน `{ status: "ok", sales: [...ทุกแถว] }`
  (ใช้รูปแบบเดียวกับ `fetchAllPages()` ที่มีอยู่แล้วใน `web/src/lib/api/products.ts`)
- [ ] T009 [US2] เพิ่มปุ่ม `<ExportButton>` บนหน้า `web/src/app/(protected)/sales/history/page.tsx` (depends
  on T004, T005, T008): `onExport` เรียก `fetchAllSalesForExport()` ด้วยตัวกรองปัจจุบันบนจอ (`from`, `to`,
  `staffId` เมื่อติ๊ก "เฉพาะบิลของฉัน", `memberId`) — ถ้าผลเป็น `cap_exceeded` ให้ throw error ข้อความ
  "ตัวกรองนี้ตรงกับ {totalCount} บิล เกิน 10,000 บิล กรุณาแคบช่วงวันที่หรือตัวกรองลงก่อน" (FR-006, ไม่เรียก
  `triggerXlsxExport` เลยในเคสนี้) — ถ้าเป็น `ok` แปลง `sales` เป็น `columns`/`rows` ตาม data-model.md ข้อ 3
  (วันที่, พนักงาน, สมาชิก, จำนวนรายการ, ส่วนลด, ยอดรวม) แล้วเรียก `triggerXlsxExport({ filenamePrefix:
  "taladpos-sales-history", sheetName: "ประวัติการขาย", columns, rows })`
- [ ] T010 [US2] ตรวจสอบด้วยมือตาม quickstart.md หัวข้อ 2 ทั้งหมด (จำนวนแถวในไฟล์ตรงกับ pager เกิน 1 หน้าจอ,
  ตัวกรอง "เฉพาะบิลของฉัน" ถูกเคารพ, ตารางว่างได้ไฟล์หัวคอลัมน์อย่างเดียว, เคสเกินเพดานแสดงข้อความแทนที่จะ
  ดาวน์โหลดไฟล์บางส่วน)

**Checkpoint**: User Story 1 และ 2 ใช้งานได้อิสระต่อกันครบทั้งคู่

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: ตรวจสอบสิ่งที่ครอบคลุมทั้งสอง User Story พร้อมกัน

- [ ] T011 [P] รัน `npm run lint` และ `npm run build` ใน `web/` ให้ผ่านไม่มี error/type error จากไฟล์ใหม่และ
  ที่แก้ไขทั้งหมด (T002–T009)
- [ ] T012 ตรวจสอบด้วยมือตาม quickstart.md หัวข้อ 3 (กันกดปุ่ม export ซ้ำซ้อนระหว่างเตรียมไฟล์ — ทดสอบทั้ง
  สองหน้าจอ, FR-008) — ทำหลัง T007 และ T010 เสร็จแล้วทั้งคู่

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: ไม่มี dependency — เริ่มได้ทันที
- **Foundational (Phase 2)**: ขึ้นกับ Setup เสร็จ (T001) — บล็อก User Story ทุกตัว
- **User Stories (Phase 3–4)**: ขึ้นกับ Foundational (Phase 2) เสร็จทั้งหมด แล้วทำขนานกันได้ (คนละไฟล์ล้วน)
  หรือเรียงตามลำดับ US1 → US2 ก็ได้
- **Polish (Phase 5)**: ขึ้นกับ User Story ที่ต้องการ deliver ทั้งหมดเสร็จก่อน

### User Story Dependencies

- **User Story 1 (P1)**: เริ่มได้หลัง Foundational (Phase 2) เสร็จ — ไม่ขึ้นกับ User Story 2
- **User Story 2 (P1)**: เริ่มได้หลัง Foundational (Phase 2) เสร็จ — ไม่ขึ้นกับ User Story 1 (คนละหน้าจอ,
  คนละไฟล์ทั้งหมด นอกจากกลไกกลางใน Phase 2 ที่ใช้ร่วมกัน)

### Within Each User Story

- US1: T006 (implementation) → T007 (manual validation)
- US2: T008 (data-fetching helper) → T009 (wire ปุ่ม, ต้องใช้ T008) → T010 (manual validation)

### Parallel Opportunities

- Phase 2: T002 และ T005 รันขนานกันได้ (คนละไฟล์ ไม่ต้องรอกัน) — T003 ต้องรอ T001+T002, T004 ต้องรอ T002+T003
- Phase 3/4: T006 (US1) และ T008 (US2) รันขนานกันได้ทันทีที่ Foundational เสร็จ (คนละไฟล์คนละหน้าจอ)
- Phase 5: T011 รันขนานกับ T012 ได้ (คนละกิจกรรม ไม่ชนกัน)

---

## Parallel Example: Foundational + เริ่ม User Story พร้อมกัน

```bash
# หลัง T001 เสร็จ รันขนานกันได้:
Task: "สร้าง shared types ใน web/src/lib/export/types.ts"          # T002
Task: "สร้าง shared component ExportButton ใน web/src/components/ExportButton.tsx"  # T005

# หลัง Foundational (T002-T005) เสร็จทั้งหมด รันขนานกันได้ข้าม story:
Task: "เพิ่มปุ่ม export บนแท็บสต็อกคงเหลือ ใน web/src/app/(protected)/reports/page.tsx"      # T006 (US1)
Task: "Implement fetchAllSalesForExport ใน web/src/lib/api/sales.ts"                          # T008 (US2)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001)
2. Complete Phase 2: Foundational (T002–T005) — บล็อกทุก story
3. Complete Phase 3: User Story 1 (T006–T007)
4. **STOP and VALIDATE**: ทดสอบ export สต็อกคงเหลือด้วยมือตาม quickstart.md หัวข้อ 1
5. Deploy/demo ได้ทันทีถ้าต้องการส่งมอบ US1 ก่อน US2

### Incremental Delivery

1. Setup + Foundational เสร็จ → กลไก export พร้อมใช้
2. เพิ่ม User Story 1 → ทดสอบอิสระ → Deploy/Demo (MVP!)
3. เพิ่ม User Story 2 → ทดสอบอิสระ → Deploy/Demo
4. Polish (T011–T012) → ส่งมอบฉบับสมบูรณ์

---

## Notes

- [P] tasks = คนละไฟล์ ไม่มี dependency ค้างกัน
- [Story] label เชื่อม task กับ user story ที่สังกัดตาม spec.md เพื่อ traceability
- ไม่มี task ใน `api/` เลยสักตัว — ยืนยันแล้วใน plan.md ว่าฟีเจอร์นี้ไม่ต้องแก้ backend
- Commit หลังทำแต่ละ task หรือกลุ่ม task ที่เกี่ยวข้องกันเสร็จ
- หยุดที่ checkpoint ของแต่ละ phase เพื่อตรวจสอบ story นั้นแยกจาก story อื่นก่อนไปต่อ
