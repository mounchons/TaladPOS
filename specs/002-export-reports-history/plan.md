# Implementation Plan: Export รายงานสต็อกคงเหลือและประวัติการขาย

**Branch**: `002-export-reports-history` | **Date**: 2026-09-11 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/002-export-reports-history/spec.md`

## Summary

เพิ่มปุ่ม export เป็นไฟล์ Excel (.xlsx) ที่ 2 จุด: แท็บ "สต็อกคงเหลือ" ในหน้ารายงาน (Manager-only) และหน้า
"ประวัติการขาย" (`sales/history`, Manager+Cashier) โดย export ประวัติการขายต้องดึงข้อมูล**ทุกบิลที่ตรงกับ
ตัวกรอง** ข้ามข้อจำกัด pagination 20 รายการ/หน้าบนจอ แต่มีเพดานสูงสุด 10,000 บิลต่อครั้ง

แนวทางทางเทคนิค: ฟีเจอร์นี้ **ไม่ต้องแก้ไข `api/` เลย** — ทุก endpoint ที่ต้องใช้ (`GET /api/v1/reports/stock`,
`GET /api/v1/sales`) มีอยู่แล้วและคืนข้อมูลครบพอสำหรับ export ทั้งสองจุด งานทั้งหมดอยู่ฝั่ง `web/`:
1. ฝั่ง client ดึงข้อมูลให้ครบตามตัวกรอง (เพจของ `/api/v1/sales` วนเก็บจนครบ หรือหยุดถ้าเกิน 10,000)
2. ส่งข้อมูลที่ได้ (ซึ่งผ่านการตรวจสิทธิ์จาก `api/` มาแล้วตั้งแต่ตอน fetch) ไปให้ Next.js Route Handler ของ
   `web/` เอง แปลงเป็นไฟล์ `.xlsx` ที่มีการจัดรูปแบบหัวตาราง (ใช้ ExcelJS ซึ่งรันได้เต็มรูปแบบใน Node.js
   runtime ของ Route Handler โดยไม่ต้องพึ่ง browser bundle/polyfill)
3. Browser ดาวน์โหลดไฟล์ที่ได้กลับมา

วิธีนี้ทำให้ไม่ต้องเปิด endpoint ใหม่ใน `api/`, ไม่ต้องแตะ business logic/DDD layer ใด ๆ, และสิทธิ์การเข้าถึง
ข้อมูล (FR-007) ได้มาฟรีโดยอัตโนมัติ — เพราะข้อมูลที่จะ export ต้องผ่าน `apiFetch()` (ซึ่งแนบ JWT และถูก
`api/` ตรวจ role ตามปกติ) มาก่อนเสมอ ถ้าไม่มีสิทธิ์ก็จะได้ 401/403 ตั้งแต่ขั้นตอนดึงข้อมูล ก่อนจะถึงขั้นตอน
สร้างไฟล์ด้วยซ้ำ

## Technical Context

**Language/Version**: TypeScript (Next.js 14 App Router, React 18) — ตรงกับ `web/` เดิม ไม่มีอะไรใหม่

**Primary Dependencies**: `exceljs` (ไลบรารีใหม่ที่ต้องเพิ่มใน `web/package.json`) — ดูเหตุผลการเลือกใน
`research.md`

**Storage**: N/A — ไม่มีข้อมูลใหม่ที่ต้อง persist ฟีเจอร์นี้อ่านข้อมูลที่มีอยู่แล้วผ่าน REST endpoint เดิม
ทั้งหมด (`GET /api/v1/reports/stock`, `GET /api/v1/sales`) และไม่เขียนอะไรกลับเข้า PostgreSQL

**Testing**: ไม่มี automated test framework ตั้งค่าไว้ใน `web/` ในปัจจุบัน (ไม่มี `test` script, ไม่มีไฟล์
`*.test.*`/`*.spec.*`) — Constitution Principle III (test-first, NON-NEGOTIABLE) บังคับเฉพาะ business logic
ใน `api/` เท่านั้น ซึ่งฟีเจอร์นี้ไม่แตะเลย จึงไม่ผิด gate ใด ๆ การยืนยันความถูกต้องใช้ manual QA ตาม
`quickstart.md` แทน (ไม่ใช่การเลี่ยง testing โดยไม่มีเหตุผล — เป็นสภาพเดิมของโปรเจกต์ที่ฟีเจอร์นี้ไม่ได้
เปลี่ยนแปลง)

**Target Platform**: Web browser (existing `web/` deployment) — ไม่มีแพลตฟอร์มใหม่

**Project Type**: Web application (ของเดิม: `api/` + `web/` สองโปรเจกต์แยกกันตาม constitution) — ฟีเจอร์นี้
แก้ไขเฉพาะฝั่ง `web/`

**Performance Goals**: ตาม SC-004 ของ spec — สร้างไฟล์เสร็จภายใน 5 วินาทีสำหรับข้อมูลไม่เกินเพดานสูงสุด
(10,000 บิลสำหรับประวัติการขาย, ไม่จำกัดสำหรับสต็อกคงเหลือเพราะเป็นชุดข้อมูลขอบเขตจำกัดอยู่แล้ว)

**Constraints**: เพดาน 10,000 บิลต่อครั้งสำหรับ export ประวัติการขาย (FR-006, ยืนยันแล้วใน Clarifications
รอบที่ 2) — เกินกว่านี้ต้องปฏิเสธพร้อมข้อความ ไม่ export บางส่วนเงียบ ๆ

**Scale/Scope**: 2 หน้าจอที่ต้องแก้ (`reports/page.tsx`, `sales/history/page.tsx`), 1 Route Handler ที่ใช้ร่วมกัน
ทั้งสองจุด, 1 dependency ใหม่ (`exceljs`)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | ผลตรวจสอบ | เหตุผล |
|---|---|---|
| I. Separation of API and Frontend | **PASS** | Route Handler ใหม่อยู่ใน `web/` เท่านั้น ไม่แตะ PostgreSQL/EF Core โดยตรงเลย รับแค่ JSON ที่ browser ดึงมาจาก `api/` REST endpoint ที่มีอยู่แล้วผ่าน `apiFetch()` แล้วแปลงเป็นไฟล์เท่านั้น ไม่มีการเรียก DB ใหม่จุดใด |
| II. API Architecture & Technology Stack (DDD) | **PASS / N/A** | ไม่แก้ `api/` เลยสักไฟล์ — ไม่มี layer ใหม่ ไม่มี business rule ใหม่ใน domain/application layer |
| III. Test-First for Business Logic (NON-NEGOTIABLE) | **PASS / N/A** | ไม่มี business logic ใหม่ใน `api/` (domain entities, domain services, use-case logic) ให้ต้องเขียน unit test ตามหลักนี้ — งานทั้งหมดเป็น presentation-layer ใน `web/` ซึ่งอยู่นอกขอบเขตของ principle นี้ |
| IV. Frontend Technology Stack | **PASS** | ใช้ Next.js + Tailwind/daisyUI เดิมทั้งหมด ปุ่ม export เป็น React component ธรรมดา, Route Handler เป็นกลไกมาตรฐานของ Next.js App Router ไม่ใช่ stack ใหม่ |
| Repository Structure | **PASS** | ทุกไฟล์ใหม่อยู่ใต้ `web/src/` เท่านั้น `api/` ไม่ถูกแตะ ไม่มีการแชร์ source code/dependency graph ข้าม boundary |
| Development Workflow & Quality Gates | **PASS** | ไม่มีการเข้าถึง DB/ORM ใหม่ฝั่ง `web/` ให้ reviewer ต้องปฏิเสธ — Route Handler เรียกแค่ `exceljs` (in-memory transform) ไม่เชื่อมต่อฐานข้อมูลใด ๆ |

ไม่มีข้อขัดแย้งกับ constitution ข้อใด — ไม่ต้องกรอก Complexity Tracking (ไม่มี violation ให้ justify)

**Re-check หลัง Phase 1 (design เสร็จแล้ว)**: ผลลัพธ์ของ Phase 0/1 (`research.md`, `data-model.md`,
`contracts/export-routes.md`) ยืนยันแนวทางเดิมทุกประการ — Route Handler ใหม่ไม่เรียก DB/ORM ใด ๆ, ไม่มี
endpoint ใหม่ใน `api/`, ไม่มี business logic ใหม่ในโดเมน ผลตรวจสอบทั้ง 6 แถวข้างบนยังคง **PASS** เหมือนเดิม
ไม่มีข้อค้นพบใหม่ระหว่าง design ที่ทำให้ gate ใดเปลี่ยนสถานะ

## Project Structure

### Documentation (this feature)

```text
specs/002-export-reports-history/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
│   └── export-routes.md
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
web/
├── src/
│   ├── app/
│   │   ├── (protected)/
│   │   │   ├── reports/
│   │   │   │   └── page.tsx                  # แก้ไข — เพิ่มปุ่ม export บนแท็บ "สต็อกคงเหลือ"
│   │   │   └── sales/history/
│   │   │       └── page.tsx                  # แก้ไข — เพิ่มปุ่ม export ที่เคารพตัวกรองปัจจุบัน
│   │   └── api/
│   │       └── export/
│   │           └── xlsx/
│   │               └── route.ts              # ใหม่ — Next.js Route Handler: รับ {filename, sheetName,
│   │                                          #        columns, rows} คืน .xlsx binary (ดู contracts/)
│   ├── lib/
│   │   ├── api/
│   │   │   └── sales.ts                      # แก้ไข — เพิ่มฟังก์ชันดึงประวัติการขายทุกหน้าตามตัวกรอง
│   │   │                                      #        พร้อมเช็คเพดาน 10,000 (รูปแบบเดียวกับ fetchAllPages
│   │   │                                      #        ที่มีอยู่แล้วใน products.ts)
│   │   └── export/
│   │       └── xlsxClient.ts                 # ใหม่ — helper ฝั่ง client: เรียก Route Handler, รับ blob,
│   │                                          #        สั่งดาวน์โหลด
│   └── components/
│       └── ExportButton.tsx                  # ใหม่ — ปุ่ม export ใช้ร่วมกัน 2 จุด (สถานะ loading, กันกดซ้ำ
│                                              #        ตาม FR-008, โชว์ error message ตอนเกินเพดาน)
└── package.json                              # แก้ไข — เพิ่ม dependency `exceljs`
```

**Structure Decision**: ทุกไฟล์อยู่ใต้ `web/` ทั้งหมด ไม่มีโปรเจกต์ใหม่/โฟลเดอร์ระดับ root ใหม่ — ใช้โครงสร้าง
App Router เดิม (`(protected)/...` สำหรับหน้าจอ, `app/api/...` สำหรับ Route Handler) และ `lib/`/`components/`
เดิมสำหรับ logic/UI ที่ใช้ร่วมกัน ไม่มี Option 1/2/3 อื่นให้เลือกเพราะ repository structure ถูกกำหนดตาย
ตัวไว้แล้วโดย constitution (`api/` + `web/`) และฟีเจอร์นี้ไม่จำเป็นต้องแตะ `api/`
