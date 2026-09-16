# Implementation Plan: Bundle & Gift Promotions (โปรโมชั่นแบบมีเงื่อนไข)

**Branch**: `qa-precheck` | **Date**: 2026-09-16 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/003-bundle-promotions/spec.md`

> **หมายเหตุเรื่อง branch**: `setup-plan.ps1` รายงาน branch เป็น `003-bundle-promotions` ตามชื่อโฟลเดอร์ spec
> แต่โปรเจกต์นี้ไม่ได้เปิดใช้ git extension hook จึงไม่มีการสร้าง branch ใหม่ งานจริงอยู่บน `qa-precheck`
> ชื่อโฟลเดอร์ spec กับชื่อ branch เป็นอิสระต่อกัน

## Summary

เพิ่มโปรโมชั่นชนิดใหม่ที่มี "เงื่อนไขการซื้อ + สิ่งตอบแทน" ครอบคลุมสามรูปแบบที่ผู้ใช้ขอ (ซื้อ a+b แถม c, ซื้อ a+b ลด x%,
ซื้อ a จำนวน y ชิ้น แถม x ชิ้น) โดยไม่แตะโปรโมชั่นเปอร์เซ็นต์เดิมเลย (FR-026)

แนวทางเทคนิค: สร้าง aggregate ใหม่ `ConditionalPromotion` แยกตาราง แยก repository แยก controller จาก `Promotion` เดิม
และย้ายตรรกะการตั้งราคาตะกร้าทั้งหมดไปอยู่ใน **domain service ตัวเดียวชื่อ `CartPricer`** ที่เป็น pure function
(ไม่เรียก repository) แล้วให้ทั้ง checkout (`CompleteSaleUseCase`) และ **endpoint พรีวิวตัวใหม่** (`POST /api/v1/sales/preview`)
เรียกใช้ตัวเดียวกัน — นี่คือวิธีเดียวที่ทำให้ยอดที่แคชเชียร์เห็นก่อนจ่ายตรงกับยอดที่ระบบตัดจริงถึงหลักสตางค์ (FR-012, SC-006)
และทำให้ `web/` ไม่ต้องมีสูตรคำนวณส่วนลดอยู่ในตัวเองแม้แต่บรรทัดเดียว (constitution Principle I)

ของแถมบันทึกเป็น `SaleLineItem` แยกบรรทัดที่ราคาปกติและส่วนลดเท่ากับราคานั้น (ยอดสุทธิ 0) พร้อม flag `IsGift`
ซึ่งจำเป็นเพราะส่วนลดชุด 100% ก็ให้ยอดสุทธิ 0 เหมือนกัน ใบเสร็จจึงแยกสองกรณีนี้จากตัวเลขอย่างเดียวไม่ได้

## Technical Context

**Language/Version**: C# 12 / .NET 8 (LTS) สำหรับ `api/`; TypeScript 5 / Node.js 20 LTS สำหรับ `web/` — สืบทอดจาก 001 ทั้งหมด

**Primary Dependencies**: ไม่มี dependency ใหม่ ใช้ของเดิมทั้งหมด — ASP.NET Core Web API (.NET 8), EF Core 8 + Npgsql,
JWT authentication ฝั่ง `api/`; Next.js 14 App Router + React 18 + Tailwind CSS 3 ฝั่ง `web/`

**Storage**: PostgreSQL 16 — เพิ่มตารางใหม่ 3 ตาราง (`conditional_promotions`, `conditional_promotion_lines`,
`sale_applied_promotions`) และเพิ่มคอลัมน์ `is_gift` ในตาราง `sale_line_items` เดิม **ไม่แตะตาราง `promotions` เดิม**

**Testing**: xUnit + FluentAssertions ตาม constitution Principle III
- `TaladPOS.Domain.Tests` — ทุก acceptance scenario ใน spec.md แปลงเป็นเทสของ `CartPricer` ที่ยืนยันตัวเลขจริง
  บวกเทส validation ของ `ConditionalPromotion`
- `TaladPOS.Application.Tests` — `CompleteSaleUseCase`, `PreviewSaleUseCase` และเทสความหมายของตัวเลขในรายงานเดิมเมื่อบิลมีของแถม (FR-027) ทั้งหมดใช้ fake repository ไม่พึ่ง PostgreSQL
- `TaladPOS.Api.IntegrationTests` — CRUD ของ conditional promotion และ endpoint พรีวิว (รวมสิทธิ์: แคชเชียร์เรียกพรีวิวได้)

**Target Platform**: เหมือน 001 — เว็บบนเบราว์เซอร์คอมพิวเตอร์/แท็บเล็ตหน้าร้าน เรียก API ที่ host แบบ Linux server/container

**Project Type**: Web application แยก frontend + backend (`api/` + `web/`) ตาม constitution Repository Structure

**Performance Goals**: พรีวิวตะกร้าต้องไม่ทำให้การสแกนสะดุด — `web/` เรียก `POST /api/v1/sales/preview` แบบ debounce
ประมาณ 250 มิลลิวินาทีหลังตะกร้าเปลี่ยน และแสดงยอดเดิมค้างไว้ระหว่างรอ ไม่ใช่แสดงค่าว่าง
การตั้งราคาเป็นการคำนวณในหน่วยความจำล้วน ขนาดอินพุตคือจำนวนรายการในตะกร้า (หลักสิบ) คูณจำนวนโปรโมชั่นที่มีผล (หลักสิบ)

**Constraints**:
- `web/` ห้ามมีสูตรคำนวณส่วนลด ต้องรับตัวเลขทุกตัวจาก REST endpoint เท่านั้น (constitution Principle I)
- พรีวิวต้องให้ผลลัพธ์ตรงกับ checkout ทุกกรณี จึงต้องใช้ `CartPricer` ตัวเดียวกัน ห้าม implement ซ้ำสองที่
- โปรโมชั่นเปอร์เซ็นต์เดิมและตาราง `promotions` ห้ามเปลี่ยนพฤติกรรมและห้ามย้ายข้อมูล (FR-026)
- บิลขายยังเป็น append-only ไม่มี void/refund ตามข้อจำกัดเดิมของ 001
- การตัดสต็อกยังต้อง concurrency-safe ข้ามหลายจุดขาย และตอนนี้ต้องครอบคลุมจำนวนของแถมด้วย (FR-017)

**Scale/Scope**: ร้านค้าเดี่ยว 1 สาขา แคตตาล็อกหลักร้อยถึงพันรายการ บิลหลักร้อยถึงหลักพันบิล/เดือน
จำนวนโปรโมชั่นแบบมีเงื่อนไขที่มีผลพร้อมกันคาดว่าอยู่ระดับหลักหน่วยถึงหลักสิบ
ขอบเขตงาน: เพิ่ม endpoint ใหม่ 5 ตัว (CRUD ของ conditional promotion 4 ตัว + พรีวิว 1 ตัว), แก้ endpoint เดิม 3 ตัว
โดยเพิ่มฟิลด์ในผลลัพธ์อย่างเดียว (`POST /api/v1/sales`, `GET /api/v1/sales/{id}`, `GET /api/v1/sales/{id}/receipt`),
migration 1 ไฟล์, หน้าจอ `web/` ที่กระทบ 3 หน้า (โปรโมชั่น, ขาย, ใบเสร็จ)

**ไม่มี NEEDS CLARIFICATION**: stack, storage, testing, scale ถูกกำหนดไว้แล้วโดย `001/plan.md` และ constitution
ส่วนประเด็นเดียวที่ spec ต้องถามผู้ใช้ (วิธีนำของแถมเข้าตะกร้า) ได้คำตอบแล้วและเขียนลง FR-014 เรียบร้อย
Phase 0 ของฟีเจอร์นี้จึงเป็น **การตัดสินใจเชิงออกแบบ** ไม่ใช่การค้นคว้าสิ่งที่ยังไม่รู้

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate (จาก `.specify/memory/constitution.md`) | สถานะ | หมายเหตุ |
|---|---|---|
| I. Separation of API and Frontend — REST-only, ห้ามต่อ DB ตรง **และข้อมูลทุกอย่างที่ frontend ต้องใช้ ต้องเปิดผ่าน endpoint ที่มี version** | PASS | ประโยคที่สองคือประโยคที่กำหนดการออกแบบฟีเจอร์นี้: หน้าขายต้องแสดงส่วนลดชุด ของแถม และข้อความเตือนสิทธิ์ที่ยังไม่ได้ใช้ (FR-013, FR-015) ซึ่งเป็น "ข้อมูลที่ frontend ต้องใช้" จึงต้องมาจาก endpoint ที่มี version ไม่ใช่คำนวณเองใน `web/` — เป็นเหตุผลทั้งหมดที่ออกแบบ `POST /api/v1/sales/preview` (research.md #3) ทุก endpoint ใหม่อยู่ใต้ `api/v1/` และไม่มี DB driver/connection string ใน `web/` |
| II. API Architecture & Technology Stack (DDD) | PASS | `ConditionalPromotion`, `ConditionLine`, `Reward` และ `CartPricer` อยู่ใน `TaladPOS.Domain` ทั้งหมด ไม่มี attribute ของ EF Core หรือ ASP.NET ในนั้น การ map ลงตารางอยู่ใน `TaladPOS.Infrastructure/Configurations/` ตามรูปแบบเดิม ส่วน use case อยู่ใน `TaladPOS.Application` และ controller เป็นแค่ชั้นแปลง DTO |
| III. Test-First for Business Logic (NON-NEGOTIABLE) | PASS | `CartPricer` คือ business logic ก้อนใหญ่ที่สุดของฟีเจอร์นี้และถูกออกแบบให้เป็น pure function โดยเจตนา (ไม่เรียก repository, ไม่แตะ `DateTime.UtcNow` เอง — รับ `DateOnly` เข้ามา) จึงเทสได้ครบทุกกรณีโดยไม่ต้องมี PostgreSQL ทุก acceptance scenario ใน spec.md มีเทสคู่ที่ยืนยันตัวเลขจริง |
| IV. Frontend Technology Stack — Next.js + Tailwind CSS, REST-only | PASS | ไม่เพิ่ม framework ใหม่ หน้าจอที่แก้ทั้งหมดใช้ component เดิมของโปรเจกต์ (`Modal`, `DataTable`, `Cart`, `Receipt`) และเรียกข้อมูลผ่าน `lib/api/` ตามเดิม |
| Repository Structure — แยก `api/` และ `web/` ไม่แชร์โค้ด/build output | PASS | ไม่มีโฟลเดอร์ระดับบนสุดใหม่ จุดเชื่อมต่อเดียวยังเป็น REST contract ที่เอกสารอยู่ใน `specs/003-bundle-promotions/contracts/` |
| Development Workflow & Quality Gates | PASS (บังคับใช้ตอน PR) | PR ที่แตะ `api/` ต้องมาพร้อมเทสตาม Testing ด้านบน และ PR ที่แตะ `web/` ต้องถูกตรวจว่าไม่มีสูตรคำนวณส่วนลดหลุดเข้าไป ซึ่งเป็นความเสี่ยงเฉพาะของฟีเจอร์นี้เพราะหน้าขายต้องแสดงตัวเลขส่วนลดแบบ real-time |

ไม่มี violation ที่ต้องกรอกใน Complexity Tracking

**บทเรียนจาก 001 ที่นำมาใช้กับ gate I**: รอบก่อนเคยประเมิน gate นี้โดยดูแค่ประโยค "ห้ามต่อ DB ตรง" แล้วขึ้น PASS
ทั้งที่ละเมิดประโยคเรื่อง versioned endpoint อยู่ รอบนี้จึงประเมินทั้งสองประโยคแยกกันอย่างชัดเจน
และประโยคที่สองคือสิ่งที่ตัดตัวเลือก "ให้ `web/` คำนวณส่วนลดเองเพื่อแสดงพรีวิว" ทิ้งไปตั้งแต่ต้น

**Post-Design Re-check** (หลัง Phase 1) — ดูท้ายไฟล์ ส่วน [Post-Design Constitution Re-check](#post-design-constitution-re-check)

## Project Structure

### Documentation (this feature)

```text
specs/003-bundle-promotions/
├── plan.md              # ไฟล์นี้ (/speckit-plan)
├── spec.md              # /speckit-specify
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   ├── conditional-promotions.md
│   └── sales-preview.md
├── checklists/
│   └── requirements.md
└── tasks.md             # Phase 2 (/speckit-tasks — ยังไม่สร้างในคำสั่งนี้)
```

### Source Code (repository root)

```text
api/
├── src/
│   ├── TaladPOS.Domain/
│   │   ├── Promotions/
│   │   │   ├── Promotion.cs                   # เดิม — ห้ามแก้ (FR-026)
│   │   │   ├── DiscountResolver.cs            # เดิม — ห้ามแก้ CartPricer เรียกใช้ต่อ
│   │   │   ├── ConditionalPromotion.cs        # ใหม่ — aggregate root
│   │   │   ├── ConditionLine.cs               # ใหม่ — owned entity (สินค้า + จำนวนขั้นต่ำ)
│   │   │   ├── Reward.cs                      # ใหม่ — value object (ของแถม | ส่วนลด %)
│   │   │   └── CartPricer.cs                  # ใหม่ — domain service ตั้งราคาตะกร้า (pure)
│   │   └── Sales/
│   │       ├── Sale.cs                        # แก้ — รับ SaleAppliedPromotion เพิ่ม
│   │       ├── SaleLineItem.cs                # แก้ — เพิ่ม IsGift
│   │       └── SaleAppliedPromotion.cs        # ใหม่ — บันทึกสิทธิ์ที่ถูกใช้ (FR-024)
│   ├── TaladPOS.Application/
│   │   ├── Promotions/
│   │   │   ├── IConditionalPromotionRepository.cs      # ใหม่
│   │   │   ├── CreateConditionalPromotionUseCase.cs    # ใหม่
│   │   │   ├── UpdateConditionalPromotionUseCase.cs    # ใหม่
│   │   │   ├── DeleteConditionalPromotionUseCase.cs    # ใหม่
│   │   │   └── ListConditionalPromotionsQuery.cs       # ใหม่ — ประกอบข้อความ + isUsable (FR-008/FR-023)
│   │   └── Sales/
│   │       ├── CompleteSaleUseCase.cs         # แก้ — เรียก CartPricer สลับลำดับตัดสต็อก
│   │       └── PreviewSaleUseCase.cs          # ใหม่ — เรียก CartPricer ตัวเดียวกัน ไม่แตะ DB
│   ├── TaladPOS.Infrastructure/
│   │   ├── Configurations/
│   │   │   ├── ConditionalPromotionConfiguration.cs    # ใหม่ — รวม ConditionLine + Reward
│   │   │   ├── SaleLineItemConfiguration.cs            # แก้ — เพิ่มคอลัมน์ is_gift
│   │   │   ├── SaleConfiguration.cs                    # แก้ — owned collection AppliedPromotions
│   │   │   └── PromotionConfiguration.cs               # เดิม — ห้ามแก้
│   │   ├── Repositories/ConditionalPromotionRepository.cs  # ใหม่
│   │   └── Persistence/Migrations/*_AddConditionalPromotions.cs  # ใหม่ 1 ไฟล์
│   └── TaladPOS.Api/Controllers/
│       ├── ConditionalPromotionsController.cs # ใหม่ — Manager-only
│       └── SalesController.cs                 # แก้ — เพิ่ม POST preview + ฟิลด์ผลลัพธ์
├── tests/
│   ├── TaladPOS.Domain.Tests/Promotions/
│   │   ├── CartPricerTests.cs                 # ใหม่ — ทุก acceptance scenario + edge case
│   │   └── ConditionalPromotionValidationTests.cs      # ใหม่
│   ├── TaladPOS.Application.Tests/Sales/       # แก้/เพิ่ม — Complete + Preview use case
│   ├── TaladPOS.Application.Tests/Reports/     # ใหม่ — ความหมายของตัวเลขรายงานเมื่อมีของแถม (FR-027)
│   └── TaladPOS.Api.IntegrationTests/          # เพิ่ม — CRUD + preview + สิทธิ์แคชเชียร์
└── tools/TaladPOS.TestData/
    ├── TestDataSpec.cs                        # แก้ — เพิ่ม conditional promotion ตัวอย่าง 3 แบบ
    └── TestDataBuilder.cs                     # แก้ — seed ตารางใหม่

web/
└── src/
    ├── lib/api/
    │   ├── conditionalPromotions.ts           # ใหม่ — CRUD client
    │   └── sales.ts                           # แก้ — เพิ่ม previewSale()
    ├── components/
    │   ├── PromotionFormDialog.tsx            # แก้ — สลับชนิดโปรโมชั่น + ตัวแก้ไขรายการเงื่อนไข
    │   ├── Cart.tsx                           # แก้ — ยอดจากพรีวิว ป้ายของแถม ข้อความเตือนสิทธิ์
    │   └── Receipt.tsx                        # แก้ — แยกบรรทัดของแถม + ชื่อโปรโมชั่นที่ถูกใช้
    └── app/(protected)/promotions/page.tsx    # แก้ — รวมสองชนิดในตารางเดียว

scripts/reset-test-data.ps1                    # ใช้ตามเดิม ได้ข้อมูลโปรโมชั่นแบบมีเงื่อนไขจาก TestData tool
```

**Structure Decision**: ใช้โครงสร้าง `api/` + `web/` เดิมของโปรเจกต์ตาม constitution Repository Structure
ไม่เพิ่ม project ใหม่ใน solution และไม่เพิ่มโฟลเดอร์ระดับบนสุด งานทั้งหมดเป็นการเพิ่มไฟล์เข้าไปใน layer ที่มีอยู่แล้ว
โดยแยก `ConditionalPromotion` ออกจาก `Promotion` เดิมในระดับไฟล์ ตาราง repository และ controller
เพื่อให้ FR-026 (ของเดิมต้องไม่เปลี่ยนพฤติกรรม) ตรวจสอบได้ด้วยการดู diff ว่าไฟล์เดิมไม่ถูกแตะ

## Post-Design Constitution Re-check

ประเมินอีกครั้งหลังสร้าง `research.md`, `data-model.md`, `contracts/` และ `quickstart.md`:

- **Principle I** — `contracts/sales-preview.md` ทำให้ตัวเลขทุกตัวที่หน้าขายแสดง (ส่วนลดชุด ของแถม สิทธิ์ที่ยังไม่ได้ใช้
  ยอดสุทธิ) มาจาก `POST /api/v1/sales/preview` ซึ่งอยู่ใต้ `api/v1/` ครบตามประโยค versioned endpoint
  `web/` ไม่มีการคำนวณส่วนลดเหลืออยู่เลยแม้แต่จุดเดียว → **PASS**
- **Principle II** — `data-model.md` วาง `ConditionalPromotion`/`ConditionLine`/`Reward` ไว้ใน Domain ทั้งหมด
  ส่วนคอลัมน์ที่แบนราบ (`reward_kind`, `gift_product_id`, ...) เป็นรายละเอียดของ EF configuration ไม่ใช่รูปร่างของ domain
  → **PASS**
- **Principle III** — อัลกอริทึมตั้งราคาถูกเขียนเป็นขั้นตอนที่มีเลขกำกับใน `research.md` #10 และ `CartPricer`
  ถูกออกแบบให้ไม่มี dependency ภายนอกเลย จึงเทสได้ครบโดยไม่พึ่ง PostgreSQL → **PASS**
- **Principle IV** — ไม่มี library ใหม่ฝั่ง `web/` → **PASS**
- **Repository Structure** — ไม่มีโฟลเดอร์ระดับบนสุดใหม่ ไม่มีโค้ดร่วมระหว่าง `api/` กับ `web/` → **PASS**

**สรุป: ผ่านทุก gate ไม่มี violation ใหม่ที่เกิดจากการออกแบบ Complexity Tracking จึงว่างตามเดิม**
