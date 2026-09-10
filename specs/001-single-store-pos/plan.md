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


---

# รอบที่ 2 — daisyUI + Responsive + Server Paging

> เพิ่มเข้ามาหลังจากฟีเจอร์ 001 ปิดครบทุก task แล้ว (83/83) จึงเขียนต่อในแผนเดิมตามที่
> `setup-plan.ps1` ชี้มา ไม่ใช่ฟีเจอร์ใหม่แยกโฟลเดอร์ — ตามที่เคยตกลงกันไว้ตอนบันทึกงาน UI หน้า sales เข้า 001
> **ทางเลือกอื่นที่ไม่ได้เลือก**: แยกเป็น `specs/002-daisyui-responsive/` ซึ่งเป็นรูปแบบมาตรฐานของ speckit
> สำหรับงานขนาดนี้ (เปลี่ยน UI framework + แก้สัญญา API แบบ breaking) — ถ้าต้องการแยกภายหลังยังทำได้

## Summary (รอบที่ 2)

สี่งานที่ผูกกันเป็นชุดเดียว เรียงตามลำดับที่ต้องทำ:

1. **อัปเกรด Tailwind CSS 3.4.1 → 4.3.3** — เป็นเงื่อนไขของ daisyUI 5 ต้องทำก่อนและพิสูจน์ให้จบก่อนแตะอย่างอื่น
2. **เปลี่ยน UI library เป็น daisyUI 5** แทน PrimeReact ทั้งหมด (15 ไฟล์)
3. **เพิ่ม server paging** ที่ `GET /api/v1/products` และ `GET /api/v1/sales` — เป็น breaking change ของ `v1`
4. **ทำให้ทุกหน้า responsive ตามเกณฑ์ที่วัดได้** และให้ทุก datagrid กรองข้อมูลได้

รายละเอียดการตัดสินใจทั้งหมดอยู่ใน `research.md` ข้อ 8–12

## หมายเหตุสำคัญ — อาการในภาพที่ผู้ใช้ส่งมายังทำซ้ำไม่ได้

ภาพที่ส่งมาแสดงหน้า `/sales` ที่เนื้อหาถูกตัดขาดที่ขอบขวา (แผงแคชเชียร์และชื่อผู้ใช้โดนตัด) แต่การวัดด้วย
Playwright บน dev server ที่รันอยู่ **ไม่พบ horizontal overflow เลย** ทั้งที่ 375px, 900px และ 1280px ทุกหน้า
(`scrollWidth - clientWidth = 0`) และหน้า `/stock` ที่ 375px แสดงตารางเป็นการ์ดซ้อนแนวตั้งได้ถูกต้อง

ความเป็นไปได้ที่อธิบายภาพนั้นได้: เป็นภาพจากหน้าเวอร์ชันก่อนการแก้ UI รอบที่แล้ว, เป็นภาพที่จับตอนกำลังลาก
ย่อหน้าต่าง (Chrome บน Windows จะยืดภาพเฟรมเดิมไว้ระหว่างลาก), หรือเบราว์เซอร์ตั้ง zoom ไว้

**เรื่องนี้ไม่บล็อกแผน** เพราะรอบนี้จะเขียน layout ใหม่ด้วย daisyUI อยู่แล้ว และเกณฑ์ responsive ที่กำหนดไว้
(research.md ข้อ 12) ครอบอาการแบบในภาพไว้ด้วย — แต่ถ้าอาการยังอยู่หลังทำเสร็จ **รบกวนแจ้งความกว้างหน้าต่าง
และระดับ zoom** เพื่อให้ทำซ้ำได้

## Technical Context (รอบที่ 2 — เฉพาะส่วนที่เปลี่ยน)

| หัวข้อ | เดิม | รอบนี้ |
|---|---|---|
| CSS framework | Tailwind CSS 3.4.1 | **Tailwind CSS 4.3.3** (CSS-first config) |
| PostCSS plugin | `tailwindcss` | **`@tailwindcss/postcss` 4.3.3** |
| UI component library | PrimeReact 10.9.9 (unstyled + passthrough) | **daisyUI 5.7.32** (Tailwind plugin) |
| ที่เก็บธีม | `web/tailwind.config.ts` | `@theme` + `@plugin "daisyui/theme"` ใน `globals.css` (ลบ config ทิ้ง) |
| Datagrid | `DataTable` ของ PrimeReact | `web/src/components/DataTable.tsx` เขียนเอง (ไม่เพิ่ม dependency) |
| รูปแบบ response ของ list | array เปล่า | envelope `{items,page,pageSize,totalCount,totalPages}` (2 endpoint) |
| Next.js | 14.2.35 | คงเดิม — เป็นความเสี่ยงที่ต้องพิสูจน์ ดู research.md ข้อ 8 |

**ไม่เปลี่ยน**: .NET 8, EF Core 8, PostgreSQL 16, โครง DDD ของ `api/`, กลไก auth, และ domain model ทั้งหมด

## Constitution Check (รอบที่ 2)

| หลักการ | ผล | เหตุผล |
|---|---|---|
| I. แยก API กับ Frontend | **ผ่าน** | daisyUI เป็น CSS ล้วน ไม่แตะการเรียกข้อมูล paging ทำที่ API แล้วส่งผลผ่าน REST เท่านั้น `web/` ยังไม่มีทางเข้าถึง DB |
| II. DDD + EF Core + PostgreSQL | **ผ่าน** | logic การแบ่งหน้าอยู่ที่ Application layer (query object) ไม่มี business rule รั่วลงไปที่ controller และไม่มี domain rule ใหม่ |
| III. Test-First (NON-NEGOTIABLE) | **ผ่าน — เป็น gate ที่เข้มที่สุดของรอบนี้** | การแบ่งหน้าเป็น logic ของ Application layer จึง **ต้องมี unit test ที่รันได้โดยไม่มี PostgreSQL** ครอบ: ค่า default, ค่าเกินเพดาน, `page` เกินจำนวนหน้าจริง, และการนับ `totalCount` ตอนมี filter |
| IV. Next.js + Tailwind CSS | **ผ่าน** | daisyUI คือ Tailwind plugin ไม่ใช่ CSS framework คู่แข่ง การถอด PrimeReact ทำให้เหลือ styling system เดียว = **ตรงเจตนาของหลักการนี้มากกว่าเดิม** |
| Repository Structure | **ผ่าน** | ไม่มีการแชร์โค้ดใหม่ระหว่าง `api/` กับ `web/` จุดเชื่อมเดียวยังเป็นสัญญาใน `contracts/` |

**ไม่มีข้อยกเว้นที่ต้องขออนุมัติ** ตาราง Complexity Tracking ข้างบนจึงยังว่างอยู่เหมือนเดิม

## ลำดับงานที่บังคับ (สำคัญกว่ารายละเอียด)

ลำดับนี้ไม่ใช่ความชอบ แต่มาจากการที่งานหลังพังเงียบถ้างานก่อนยังไม่จบ:

```text
1. Tailwind 4  ──► ต้องรัน `npm run build` ผ่านและเปิดครบทุกหน้าก่อน
                   (ไม่งั้นแยกไม่ออกว่าที่พังเป็นเพราะ Tailwind 4 หรือ daisyUI)
        │
2. API envelope + unit tests + contracts + integration tests
        │          (ทำก่อน UI ไม่งั้น DataTable ต้องเขียนสองรอบ)
        │
3. web/src/lib/api/*.ts อ่าน envelope
        │
4. DataTable.tsx + ถอด PrimeReact ทีละหน้า
        │
5. Responsive + filter บนหน้าที่เขียนใหม่แล้ว
        │
6. วัดผลตามเกณฑ์ research.md ข้อ 12 ที่ 360/768/1024/1440
```

**ข้อควรระวังจากรอบก่อน**: `npm run build` ทับ `.next/` ของ dev server ที่รันอยู่จนทุก chunk กลายเป็น 404
ต้องหยุด dev server ก่อน build แล้วค่อยเริ่มใหม่ (บันทึกไว้แล้วใน `web/README.md`)

## Project Structure — ส่วนที่เปลี่ยนใน `web/`

```text
web/
├── postcss.config.mjs          # แก้: tailwindcss -> @tailwindcss/postcss
├── tailwind.config.ts          # ลบทิ้ง — ธีมย้ายไปอยู่ใน globals.css
├── src/
│   ├── app/globals.css         # แก้ใหญ่: @import "tailwindcss" + @plugin daisyui + @theme
│   ├── components/
│   │   ├── DataTable.tsx       # เพิ่มใหม่ — generic table + server paging + filter slot
│   │   ├── MemberSearch.tsx    # เขียนใหม่ — autocomplete เองแทน PrimeReact AutoComplete
│   │   └── *.tsx               # ที่เหลือ: เปลี่ยน PrimeReact -> class ของ daisyUI
│   ├── lib/api/
│   │   ├── products.ts         # แก้: อ่าน envelope + ส่ง page/pageSize
│   │   └── sales.ts            # แก้: อ่าน envelope + ส่ง page/pageSize
│   └── styles/
│       └── primereact-passthrough.ts   # ลบทิ้งทั้งไฟล์
```

## Project Structure — ส่วนที่เปลี่ยนใน `api/`

```text
api/src/
├── TaladPOS.Application/
│   └── Common/PagedResult.cs   # เพิ่มใหม่ — envelope ที่ใช้ร่วมกันทั้งสอง use case
├── TaladPOS.Api/Controllers/
│   ├── ProductsController.cs   # แก้: รับ page/pageSize, คืน PagedResult, 400 ถ้านอกช่วง
│   └── SalesController.cs      # แก้: เหมือนกัน
api/tests/
├── TaladPOS.Application.Tests/ # เพิ่ม unit test ของการแบ่งหน้า (Constitution III)
└── TaladPOS.Api.IntegrationTests/  # แก้ test ที่ deserialize เป็น List<T> + OpenApiDocumentTests
```

## Phase 1 artifacts ของรอบนี้

| ไฟล์ | สถานะ |
|---|---|
| `research.md` ข้อ 8–12 | เขียนแล้ว (ข้อ 7 ถูกทำเครื่องหมายว่าถูกแทนที่) |
| `contracts/products.md` | แก้แล้ว — envelope + `page`/`pageSize` + 400 |
| `contracts/sales.md` | แก้แล้ว — envelope + `page`/`pageSize` + 400 |
| `data-model.md` | **ไม่เปลี่ยน** — ไม่มี entity, field หรือ invariant ใหม่ การแบ่งหน้าเป็นเรื่องของชั้น query ล้วน ๆ |
| `quickstart.md` หัวข้อ 4–6 + สคริปต์ T080 | **ต้องแก้ให้ส่ง `pageSize` ชัดเจน** — ไม่งั้นการตรวจจะอ่อนลงเงียบ ๆ (ดู 7.6) |
| `quickstart.md` หัวข้อ 7 | เพิ่มแล้ว — scenario ตรวจ paging, filter และ responsive |
