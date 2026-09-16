# Specification Quality Checklist: Bundle & Gift Promotions (โปรโมชั่นแบบมีเงื่อนไข)

**Purpose**: Validate specification completeness and quality before proceeding to planning
**Created**: 2026-09-16
**Feature**: [spec.md](../spec.md)

## Content Quality

- [x] No implementation details (languages, frameworks, APIs)
- [x] Focused on user value and business needs
- [x] Written for non-technical stakeholders
- [x] All mandatory sections completed

## Requirement Completeness

- [x] No [NEEDS CLARIFICATION] markers remain
- [x] Requirements are testable and unambiguous
- [x] Success criteria are measurable
- [x] Success criteria are technology-agnostic (no implementation details)
- [x] All acceptance scenarios are defined
- [x] Edge cases are identified
- [x] Scope is clearly bounded
- [x] Dependencies and assumptions identified

## Feature Readiness

- [x] All functional requirements have clear acceptance criteria
- [x] User scenarios cover primary flows
- [x] Feature meets measurable outcomes defined in Success Criteria
- [x] No implementation details leak into specification

## Notes

### รอบตรวจที่ 1 (2026-09-16)

ผ่าน 15 จาก 16 ข้อ เหลือ 1 ข้อที่ยังไม่ผ่าน:

- **No [NEEDS CLARIFICATION] markers remain** — ยังเหลือ 1 จุดที่ **FR-014** เรื่องวิธีนำของแถมเข้าตะกร้า
  (พนักงานสแกนเองแล้วระบบปรับยอดเป็น 0 vs ระบบเพิ่มของแถมให้อัตโนมัติ)
  ประเด็นนี้เปลี่ยนขอบเขตงานและ UX ของหน้าขายอย่างมีนัยสำคัญ จึงไม่เดาเองและต้องรอคำตอบจากผู้ใช้

### รอบตรวจที่ 2 (2026-09-16) — ผ่านครบ 16 จาก 16 ข้อ

ผู้ใช้เลือก **"พนักงานสแกนของแถมเข้าตะกร้าเอง แล้วระบบปรับยอดสุทธิให้เป็น 0"** จึงแก้ spec ดังนี้:

- **FR-014** เขียนใหม่เป็นข้อกำหนดชัดเจนว่าระบบต้องไม่เพิ่มหรือลบสินค้าในตะกร้าด้วยตัวเอง
- **FR-015** ขยายให้ครอบคลุมทั้งกรณีพนักงานยังไม่ได้สแกนและกรณีของแถมหมดสต็อก พร้อมระบุว่าต้องบอกว่าขาดอีกกี่ชิ้น
- เพิ่ม **Acceptance Scenario ข้อ 7 ของ User Story 1** ครอบคลุมกรณีเข้าเงื่อนไขแล้วแต่ยังไม่ได้สแกนของแถม
- เพิ่ม Assumption **"ตะกร้าเป็นของพนักงานเสมอ"** พร้อมเหตุผลว่าทำให้ไม่ต้องมีกติกาสต็อกเฉพาะสำหรับของแถม

ไม่มี [NEEDS CLARIFICATION] เหลือในไฟล์ spec แล้ว (ตรวจด้วยการนับ = 0)

### รอบตรวจที่ 3 (2026-09-16) — แก้ข้อกำหนดที่ยังคำนวณตามมือไม่ได้ในบางกรณี

ตรวจย้อนพบว่า FR-003 และ FR-004 อนุญาตให้ตั้งโปรโมชั่นแบบ "ซื้อ A 1 + B 1 แถม A 1" ได้
(เงื่อนไขหลายรายการ และของแถมเป็นสินค้าเดียวกับรายการหนึ่งในเงื่อนไข)
แต่สูตรใน FR-011 เดิมเขียนไว้แบบสินค้ารายการเดียว จึงไม่ครอบคลุมกรณีนี้และผู้ทดสอบคำนวณเองไม่ได้ แก้ดังนี้:

- **FR-011** เขียนใหม่ให้เป็น *การปรับจำนวนที่ใช้ต่อชุดของรายการนั้น* แทนที่จะเป็นสูตรแทน FR-010
  จำนวนชุดยังคงคิดด้วยกติกา "ค่าน้อยที่สุดข้ามทุกรายการ" ของ FR-010 เสมอ
  (ตรวจแล้วว่าทั้ง 5 scenario ของ User Story 2 ยังได้ผลลัพธ์เท่าเดิม เพราะเงื่อนไขรายการเดียวจะยุบกลับเป็นสูตรเดิมพอดี)
- เพิ่ม **Edge case** พร้อมตัวเลขจริงสำหรับกรณีนี้ (A 3 ชิ้น + B 1 ชิ้น ได้ 1 ชุด)
- **FR-016** ระบุชัดว่าของแถมแสดงเป็นรายการแยกเสมอแม้เป็นสินค้าเดียวกับที่จ่ายเงิน
  และเปลี่ยนถ้อยคำจากข้อกำหนดเชิงการจัดเก็บ ("ต้องไม่บันทึกเป็นรายการที่ราคาต่อชิ้นเป็น 0")
  เป็นข้อกำหนดเชิงสิ่งที่สังเกตได้ ("ใบเสร็จต้องแสดงราคาปกติ ไม่ใช่ 0 บาท")
- ปรับ **User Story 2 scenario 1** ให้ระบุการแสดงผลสองรายการตาม FR-016

สรุปขนาด spec: FR 26 ข้อ, SC 8 ข้อ, User Story 3 เรื่อง, Edge case 10 ข้อ

### รอบตรวจที่ 4 (2026-09-16) — แก้ตามผล `/speckit-analyze`

`/speckit-analyze` หลังมี `tasks.md` พบ 9 ข้อ (ไม่มีข้อใดระดับ CRITICAL) แก้ครบทุกข้อแล้ว
ส่วนที่กระทบ spec.md โดยตรง:

- **FR-002** เพิ่มขอบเขตความยาวชื่อ 1–100 ตัวอักษร ซึ่งเดิมมีอยู่แต่ใน data-model กับ contract
  ทำให้ข้อจำกัดที่บังคับใช้จริงมีที่มาใน spec
- **FR-019** เพิ่มชั้นที่สามของการเรียงลำดับ (ตัวระบุโปรโมชั่น) พร้อมเหตุผล — เดิม spec เขียนไว้แค่สองชั้น
  ซึ่งยังไม่เป็น total order จึงทำ FR-012 กับ SC-006 ไม่ได้จริง ขณะที่ plan และ tasks ใช้สามชั้นอยู่แล้ว
- **SC-008** จำกัดขอบเขตให้พูดถึงเฉพาะส่วนลดจากโปรโมชั่นแบบมีเงื่อนไข — เดิมเขียนครอบทุกส่วนลด
  ซึ่งเป็นไปไม่ได้เพราะโปรโมชั่นเปอร์เซ็นต์เดิมไม่มีชื่อและ FR-026 ห้ามแก้
- **FR-027 (ใหม่)** กำหนดความหมายของตัวเลขในรายงานเดิมเมื่อบิลมีของแถม ซึ่งเดิมไม่มีข้อกำหนดใดพูดถึง
  ทั้งที่พฤติกรรมเปลี่ยนจริง: ของแถมถูกนับในรายงานสินค้าขายดีและถูกรวมในยอดส่วนลด

สรุปขนาด spec หลังแก้: **FR 27 ข้อ, SC 8 ข้อ**, User Story 3 เรื่อง, Edge case 10 ข้อ
ไม่มี [NEEDS CLARIFICATION] เหลือ และเกณฑ์ทั้ง 16 ข้อด้านบนยังผ่านครบ

### หมายเหตุอื่นที่ตรวจแล้วและถือว่าผ่าน

- **Testable and unambiguous** — กฎที่เสี่ยงคลุมเครือที่สุดคือการนับชุดและการแย่งสินค้าระหว่างโปรโมชั่น
  จึงเขียนเป็นสูตรที่คำนวณด้วยมือได้ที่ FR-010, FR-011 และกำหนดลำดับการจัดสรรแบบ deterministic ที่ FR-019
  พร้อมตัวอย่างตัวเลขจริงใน Acceptance Scenarios (US2 ข้อ 2/5, US3 ข้อ 1/4)
- **No implementation details** — FR-016 อธิบายผลลัพธ์บนใบเสร็จ (ยอดสุทธิรายการเป็น 0)
  ไม่ได้ระบุโครงสร้างตารางหรือฟิลด์ในฐานข้อมูล
- **Scope is clearly bounded** — สิ่งที่ไม่ทำในเวอร์ชันนี้ระบุไว้ครบใน Assumptions
  (เพดานจำนวนชุด, หมวดหมู่สินค้า, เงื่อนไขยอดเงินขั้นต่ำ, ของแถมแบบให้เลือก, การคืนสินค้า)

- Items marked incomplete require spec updates before `/speckit-clarify` or `/speckit-plan`
