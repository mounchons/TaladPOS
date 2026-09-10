# Implementation Plan: ระบบ POS สำหรับร้านค้าเดี่ยว (Single-Store POS)

**Branch**: `001-single-store-pos` | **Date**: 2026-09-10 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-single-store-pos/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

ระบบ POS สำหรับร้านค้าเดี่ยว (ร้านผลไม้/สินค้าทั่วไป) ครอบคลุมการขายหน้าร้าน (ค้นหา/สแกนบาร์โค้ด, ตะกร้า, ชำระเงิน),
การล็อกอินพนักงานพร้อมบทบาท (ผู้จัดการ vs แคชเชียร์), ระบบสมาชิกพร้อมยอดสะสม, การจัดการสต็อกพร้อมแจ้งเตือนสินค้าใกล้หมด,
โปรโมชั่นแบบเปอร์เซ็นต์ (รายสินค้า/ทั้งบิล/เฉพาะสมาชิก) ที่เลือกใช้ส่วนลดสูงสุดแบบไม่สะสม, การบันทึกบิลขายแบบไม่ลบ/ไม่แก้ไข
(append-only) พร้อมใบเสร็จอย่างง่าย, และรายงานสรุปยอดขาย/สินค้าขายดี/ยอดตามพนักงาน/สต็อกคงเหลือ

แนวทางเทคนิค: แยก backend (`api/`) เป็น ASP.NET Core Web API จัดโครงสร้างแบบ DDD (Domain/Application/Infrastructure/Api)
ใช้ EF Core กับ PostgreSQL และมี unit test ครอบคลุม business logic ทั้งหมด (ตาม constitution) ส่วน frontend (`web/`)
เป็น Next.js + Tailwind CSS เรียกข้อมูลผ่าน REST API เท่านั้น ไม่เข้าถึงฐานข้อมูลโดยตรง การตัดสต็อกใช้ optimistic
concurrency control (RowVersion) ระดับฐานข้อมูลเพื่อรองรับหลายจุดขายพร้อมกันตามที่ระบุไว้ในสเปก

## Technical Context

**Language/Version**: C# 12 / .NET 8 (LTS) สำหรับ `api/`; TypeScript 5 / Node.js 20 LTS สำหรับ `web/`

**Primary Dependencies**:
- `api/`: ASP.NET Core Web API (.NET 8), Entity Framework Core 8 + Npgsql (PostgreSQL provider), ASP.NET Core Identity
  หรือ cookie/JWT-based authentication สำหรับ FR-007 (ตัดสินใจใน research.md)
- `web/`: Next.js 14+ (App Router), React 18, Tailwind CSS 3, **PrimeReact** (https://primereact.dev/) สำหรับ
  UI component สำเร็จรูป (DataTable, Dialog, Button ฯลฯ) แทนการเขียน component เองทั้งหมด, fetch-based REST client
  (ไม่มี ORM/DB client ฝั่ง frontend)

**Storage**: PostgreSQL 16 (ตาม constitution) — ตาราง Product, Staff, Member, Sale, SaleLineItem, Promotion

**Testing**:
- `api/`: xUnit + FluentAssertions สำหรับ unit test ของ business logic ทุกจุด (NON-NEGOTIABLE ตาม constitution
  Principle III) — เฉพาะ domain/application layer, ไม่พึ่ง PostgreSQL จริง (ใช้ EF Core InMemory/fake repository)
- `web/`: ไม่บังคับโดย constitution แต่แนะนำ manual/quickstart validation ตาม `quickstart.md`

**Target Platform**: Web (เบราว์เซอร์บนคอมพิวเตอร์/แท็บเล็ตหน้าร้าน) เรียก API ที่ host แบบ Linux server/container

**Project Type**: Web application แยก frontend + backend (`api/` + `web/` ตาม constitution Repository Structure)

**Performance Goals**: ทำรายการขายตะกร้า 5 ชิ้นเสร็จภายใน 1 นาที (SC-001); รายงานยอดขายรายวันไม่มีความล่าช้าของข้อมูล (SC-004);
รองรับหลายจุดขายพร้อมกันโดยไม่มี stock ติดลบ (จากผลการ clarify)

**Constraints**: frontend ต้องเรียกผ่าน REST API เท่านั้น ห้ามเข้าถึง PostgreSQL โดยตรง (constitution Principle I);
การตัดสต็อกต้อง concurrency-safe ข้ามหลายจุดขาย; บิลขายที่ชำระสำเร็จเป็น append-only (ไม่มี void/refund ในเวอร์ชันนี้);
ไม่มีการคำนวณ VAT

**Scale/Scope**: ร้านค้าเดี่ยว 1 สาขา, พนักงาน/แคชเชียร์หลักหน่วยถึงหลักสิบคน, จุดขาย (เครื่องคิดเงิน) มากกว่า 1 เครื่องพร้อมกันได้,
แคตตาล็อกสินค้าระดับร้อยถึงพันรายการ, ปริมาณบิลขายระดับหลักร้อยถึงหลักพันบิล/เดือน

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate (จาก `.specify/memory/constitution.md`) | สถานะ | หมายเหตุ |
|---|---|---|
| I. Separation of API and Frontend — frontend เรียกผ่าน REST เท่านั้น ห้ามเข้าถึง DB ตรง **และข้อมูลต้องเปิดผ่าน endpoint ที่มี version** | PASS | `web/` เป็น Next.js เรียก `api/` ผ่าน REST endpoints ตาม `contracts/`; ไม่มี DB driver/connection string ใน `web/`; ทุก endpoint อยู่ใต้ prefix `api/v1/` ตามข้อกำหนด "versioned REST endpoint" ของ constitution บรรทัด 29 |
| II. API Architecture & Technology Stack (DDD) — .NET Core/ASP.NET Core Web API, EF Core, PostgreSQL, DDD | PASS | `api/` แบ่งเป็น Domain/Application/Infrastructure/Api layers; EF Core + Npgsql ต่อ PostgreSQL; domain rules อยู่ใน Domain layer เท่านั้น |
| III. Test-First for Business Logic (NON-NEGOTIABLE) — ต้องมี unit test สำหรับ business logic, แยกจาก DB จริง | PASS | ทุก business rule (คำนวณส่วนลด, ตัดสต็อกแบบ concurrency-safe, ยอดสะสมสมาชิก, สถานะโปรโมชั่นตามช่วงวันที่) ต้องมี unit test ใน `api/tests/*.Domain.Tests` ก่อน merge; ใช้ fake repository ไม่พึ่ง PostgreSQL จริง |
| IV. Frontend Technology Stack — Next.js + Tailwind CSS, REST-only | PASS | `web/` ใช้ Next.js + Tailwind CSS ตามที่กำหนด; PrimeReact เป็น component library เสริมสำหรับ UI (DataTable/Dialog/Button) ไม่ใช่ตัวแทน Tailwind และไม่กระทบการเรียก REST-only (ดู research.md #7) |
| Repository Structure — แยก `api/` และ `web/` ชัดเจน ไม่แชร์โค้ด/build output | PASS | โครงสร้างโฟลเดอร์ระดับบนสุดคือ `api/` และ `web/` ตาม Project Structure ด้านล่าง จุดเชื่อมต่อเดียวคือ REST contract ใน `specs/001-single-store-pos/contracts/` |
| Development Workflow & Quality Gates | PASS (บังคับใช้ตอน PR) | ระบุไว้ใน tasks.md/PR review ว่าต้องตรวจ unit test coverage และการไม่เข้าถึง DB ตรงจาก `web/` |

**แก้ไขภายหลัง (พบโดย `/speckit-analyze`)**: การประเมิน gate I รอบแรกดูเฉพาะข้อ "REST-only / ห้ามต่อ DB ตรง"
แล้วสรุปว่า PASS โดย**ไม่ได้ประเมินประโยคสุดท้ายของ Principle I** ที่ระบุว่า "Any data the frontend needs MUST be
exposed through a **versioned** REST endpoint" ตอนนั้น endpoint จริงเป็น `api/<resource>` ไม่มี version จึงถือว่า
**ละเมิด constitution มาตลอด** ทั้งที่ตารางขึ้น PASS

แก้แล้วโดยย้ายทุก endpoint ไปอยู่ใต้ `api/v1/` (controllers, `contracts/*.md`, `web/src/lib/api/`, integration test
และเอกสารทั้งหมด) — ดู tasks.md T081 บทเรียนคือ gate ที่มีหลายประโยคต้องประเมินให้ครบทุกประโยค ไม่ใช่แค่ประโยคที่เด่นที่สุด

ไม่มี violation ที่ต้องกรอกใน Complexity Tracking

**Post-Design Re-check** (หลัง Phase 1 — research.md, data-model.md, contracts/, quickstart.md):
- Entities ทั้งหมดใน `data-model.md` อยู่ใน `TaladPOS.Domain` ไม่มี field ผูกกับ EF Core/ASP.NET → ยังตรง Principle II
- `contracts/*.md` ทุกไฟล์ระบุชัดว่า `web/` เข้าถึงข้อมูลผ่าน REST endpoint เท่านั้น (auth ผ่าน JWT, ไม่มี DB
  connection string ฝั่ง `web/`) → ยังตรง Principle I
- `research.md` #1 และ #3 เลือกออกแบบ auth (`PasswordHasher<Staff>`) และส่วนลด (`DiscountResolver`) ให้เป็น
  business logic ที่ unit test ได้โดยไม่พึ่ง PostgreSQL จริง → ยังตรง Principle III
- ไม่มีการเพิ่ม project/โฟลเดอร์ระดับบนสุดนอกเหนือจาก `api/` และ `web/` → ยังตรง Repository Structure
- สรุป: **ผ่านทุก gate ไม่มี violation ใหม่เกิดขึ้นจากการออกแบบ**

**Re-check เพิ่มเติม (เพิ่ม PrimeReact เป็น UI component library)**:
- PrimeReact เป็น library ฝั่ง UI ล้วน ๆ ไม่มีความสามารถเข้าถึงฐานข้อมูลหรือเรียก backend เอง จึงไม่กระทบ Principle I
  (REST-only) — การเรียกข้อมูลยังผ่าน `lib/api/` ตามเดิม
- ใช้แบบ unstyled + Tailwind (research.md #7) ทำให้ Tailwind ยังเป็นแหล่ง styling เดียวตาม Principle IV ไม่มี
  ธีม CSS ของ PrimeReact มาปะทะ
- ผลสรุป: **PASS ไม่ต้องบันทึกใน Complexity Tracking** เพราะเป็นการเพิ่ม UI library ปกติ ไม่ใช่การเบี่ยงเบนจาก
  architecture ที่ constitution กำหนด

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
api/
├── src/
│   ├── TaladPOS.Domain/            # Entities, value objects, domain services, business rules (no EF/ASP.NET deps)
│   │   ├── Products/               # Product aggregate: stock, low-stock threshold, concurrency token
│   │   ├── Staff/                  # Staff aggregate: credentials (hashed), role (Manager/Cashier)
│   │   ├── Members/                # Member aggregate: phone, name, accumulated purchase total
│   │   ├── Sales/                  # Sale aggregate + SaleLineItem, discount resolution (best-of rule)
│   │   └── Promotions/             # Promotion entity: item/bill scope, member-only flag, date range
│   ├── TaladPOS.Application/       # Use cases (CompleteSale, RegisterMember, CreatePromotion, Reports...), DTOs, interfaces
│   ├── TaladPOS.Infrastructure/    # EF Core DbContext, entity configurations, repositories, PostgreSQL migrations
│   └── TaladPOS.Api/               # ASP.NET Core Web API: controllers/endpoints, auth, DI composition root
└── tests/
    ├── TaladPOS.Domain.Tests/          # Unit tests for business logic (NON-NEGOTIABLE, Constitution III)
    ├── TaladPOS.Application.Tests/     # Unit tests for use-case orchestration (mocked repositories)
    └── TaladPOS.Api.IntegrationTests/  # Contract-level tests against the REST endpoints (test server + test DB)

web/
├── src/
│   ├── app/                # Next.js App Router pages: /login, /sales, /stock, /members, /promotions, /reports
│   ├── components/         # ProductCard, Cart, MemberSearch, PromotionForm, ReportTable — สร้างจาก PrimeReact
│   │                       # primitives (DataTable, Dialog, Button, InputText ฯลฯ) ในโหมด unstyled + Tailwind
│   │                       # utility classes ผ่าน `pt` (passthrough) prop (ดู research.md #7)
│   ├── lib/api/            # Typed REST client wrappers calling api/ endpoints only (no DB access)
│   └── styles/             # Tailwind config/global styles + PrimeReact passthrough/theme preset ร่วม
└── tests/                  # Optional component/e2e tests (not constitution-mandated)
```

**Structure Decision**: Web application แยกสองโปรเจกต์ตาม constitution Repository Structure — `api/` (ASP.NET Core
Web API, DDD layering) และ `web/` (Next.js + Tailwind CSS + PrimeReact) เป็นโฟลเดอร์ระดับบนสุดของ repo แยกจากกันสมบูรณ์
จุดเชื่อมต่อเดียวระหว่างสองฝั่งคือ REST API ตามสัญญาที่นิยามไว้ใน `specs/001-single-store-pos/contracts/`
PrimeReact ใช้เฉพาะเพื่อประกอบ UI component (`components/`) เท่านั้น ไม่มีส่วนเกี่ยวข้องกับการเรียกข้อมูลหรือ business
logic

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

ไม่มี — Constitution Check ทุกข้อผ่านโดยไม่ต้องมี exception ใด ๆ ตารางนี้จึงว่างโดยตั้งใจ
