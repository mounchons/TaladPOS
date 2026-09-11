# เอกสารอธิบาย Logic ระบบ TaladPOS

เอกสารชุดนี้อธิบายการทำงานของ **หน้าขายสินค้า** (`/sales`) และ **การคำนวณโปรโมชั่น/ส่วนลด**
ทั้งฝั่ง `web/` (Next.js) และ `api/` (ASP.NET Core) โดยอ้างอิงจากโค้ดจริงในโปรเจกต์ ณ วันที่เขียน
(commit ล่าสุดที่เกี่ยวข้อง: migration `AddPromotions`)

| ไฟล์ | เนื้อหา |
|---|---|
| [`architecture.md`](./architecture.md) | High-level diagram ของทั้งระบบ (web / api / database), layer ภายใน `api/`, ตาราง DB ทั้งหมด |
| [`sales-screen.md`](./sales-screen.md) | หน้าจอ "ขายสินค้า" ทำงานอย่างไร — component, state, API ที่เรียก, flow diagram ทั้งหน้า |
| [`promotion-discount.md`](./promotion-discount.md) | กลไกคำนวณส่วนลด/โปรโมชั่นแบบละเอียด — sequence diagram ของ checkout, กฎการเลือกโปรโมชั่น, ตัวอย่างตัวเลขจริงจาก unit test |
| [`authorization-roles.md`](./authorization-roles.md) | สิทธิ์การเข้าถึง/role (Manager vs Cashier) — ตารางสิทธิ์ต่อ endpoint, login/JWT flow, จุดที่บังคับสิทธิ์จริง (server) vs แค่ UX (client) |

## สรุปสั้น ๆ (TL;DR)

- ราคา/ส่วนลดที่เห็นระหว่างหยิบสินค้าในตะกร้า (`Cart.tsx`) เป็นแค่ **ผลรวมราคาสินค้า ไม่รวมส่วนลด** —
  ระบบ **ไม่คำนวณโปรโมชั่นฝั่ง client เลย**
- ส่วนลดทั้งหมด (ทั้งระดับสินค้า/Item และระดับบิล/Bill) คำนวณที่ฝั่ง **API เท่านั้น** ตอนกดปุ่ม "ชำระเงิน"
  (`POST /api/v1/sales`) ภายใน database transaction เดียวกับการตัดสต็อกและบันทึกบิล
- กติกาโปรโมชั่น: ต่อ 1 scope (Item หรือ Bill) ถ้ามีหลายโปรโมชั่นเข้าเงื่อนไขพร้อมกัน **เลือกเอาตัวที่ให้ส่วนลด
  มากที่สุดตัวเดียว ไม่บวกสะสมกัน** แต่ส่วนลดระดับ Item และระดับ Bill **รวมกันได้** (เป็นคนละ scope)
- ส่วนลดระดับบิล (Bill) ถูก "กระจาย" (prorate) ลงในแต่ละ line ตามสัดส่วนยอดของ line นั้น เพื่อให้
  `SubtotalAmount - DiscountAmount == TotalAmount` เป๊ะเสมอ แม้จะมีเศษสตางค์จากการปัดเศษ
