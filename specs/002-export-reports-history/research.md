# Phase 0 Research: Export รายงานสต็อกคงเหลือและประวัติการขาย

## 1. ไลบรารีสร้างไฟล์ .xlsx

**Decision**: ใช้ [`exceljs`](https://www.npmjs.com/package/exceljs) รันใน Next.js Route Handler (Node.js
runtime) — ไม่ใช่รันในเบราว์เซอร์โดยตรง

**Rationale**:
- Clarifications ของ spec ระบุชัดว่าไฟล์ต้อง "Excel (.xlsx) พร้อมการจัดรูปแบบหัวตาราง/คอลัมน์" ไม่ใช่ CSV ดิบ
  — ต้องการ bold header, กำหนดความกว้างคอลัมน์ เป็นอย่างน้อย
- เปรียบเทียบไลบรารียอดนิยม 2 ตัว:
  - **SheetJS (`xlsx`)** — เบา เร็ว อ่าน/เขียนได้หลายฟอร์แมต แต่ community edition ไม่รองรับการเขียน style
    (สีพื้นหลัง, ตัวหนา) ในตัว ต้องพึ่ง fork ที่ไม่ได้ดูแลอย่างเป็นทางการ (`xlsx-style`, `xlsx-js-style`)
    ถ้าต้องการ header ที่จัด format
  - **`exceljs`** — รองรับ style/formatting/merged cell ในตัวเต็มรูปแบบ เหมาะกับรายงานที่ต้องการความ
    น่าอ่านระดับ "ใช้งานจริงในบริษัท" ซึ่งตรงกับสิ่งที่ผู้ใช้ขอ
- เลือกรันฝั่ง **Route Handler (Node.js runtime)** แทนที่จะ bundle `exceljs` เข้าไปใน client bundle
  โดยตรง เพราะ `exceljs` พึ่งพา Node API (เช่น `Buffer`) ในบาง code path ซึ่งทำให้เกิดปัญหาการ polyfill ใน
  webpack 5/Next.js เวลารันในเบราว์เซอร์ (ต้อง config เพิ่มและเพิ่มขนาด client bundle โดยไม่จำเป็น) การรันใน
  Route Handler ได้ Node.js runtime เต็มรูปแบบให้ฟรีอยู่แล้ว ไม่มีปัญหานี้เลย และไม่เพิ่มขนาด JS ที่ส่งไปเบราว์เซอร์

**Alternatives considered**:
- `xlsx` (SheetJS) + `xlsx-js-style` fork — ปฏิเสธ เพราะพึ่ง community fork ที่ไม่ใช่ official สำหรับ
  ความสามารถหลักที่ spec ต้องการ (styled header) ความเสี่ยงเรื่อง maintenance สูงกว่า
- สร้าง `.xlsx` ฝั่ง browser ล้วน ๆ ด้วย `exceljs` browser build — ปฏิเสธ เพราะปัญหา Buffer polyfill/bundle
  size ข้างต้น และไม่มีประโยชน์เพิ่มเทียบกับการทำใน Route Handler (ข้อมูลต้องเดินทางผ่าน Route Handler อยู่
  แล้วเพื่อ trigger การดาวน์โหลดแบบมี header `Content-Disposition` ที่ตั้งชื่อไฟล์ได้)
- CSV แทน Excel — ปฏิเสธแล้วตั้งแต่ขั้นตอน `/speckit-clarify` (ผู้ใช้เลือก Excel พร้อม formatting โดยตรง)

## 2. สถาปัตยกรรมของ export flow (ทำไมไม่เพิ่ม endpoint ใหม่ใน `api/`)

**Decision**: Client (browser) ดึงข้อมูลที่ต้องการ export ผ่าน REST endpoint ที่มีอยู่แล้ว
(`GET /api/v1/reports/stock`, `GET /api/v1/sales`) โดยใช้ `apiFetch()` เดิม (แนบ JWT, ผ่านการตรวจ role ตามปกติ)
แล้วส่งผลลัพธ์ที่ได้ (แถวข้อมูล + นิยามคอลัมน์) ไปที่ Route Handler ใหม่ของ `web/` เอง
(`POST /api/export/xlsx`) เพื่อแปลงเป็นไฟล์เท่านั้น

**Rationale**:
- Endpoint ที่มีอยู่แล้วให้ข้อมูลครบพอสำหรับทั้งสอง export อยู่แล้ว (`GET /api/v1/reports/stock` คืน array
  ไม่แบ่งหน้าอยู่แล้ว, `GET /api/v1/sales` รองรับตัวกรองและ pagination ที่ต้องใช้อยู่แล้ว) — ไม่มีข้อมูลอะไร
  ที่ขาดหายจนต้องเปิด endpoint ใหม่ใน `api/`
- สอดคล้องกับ constitution Principle I/II ตรง ๆ: `api/` ไม่ต้องรู้จัก "export" เป็นแนวคิดเลยด้วยซ้ำ มันแค่
  ให้ข้อมูล ส่วนการจัดรูปแบบเป็นไฟล์เป็นเรื่องของ presentation layer ล้วน ๆ ซึ่งอยู่ฝั่ง `web/` อยู่แล้ว
- สิทธิ์การเข้าถึง (FR-007) ได้มาแบบ "ฟรี" — เพราะขั้นตอนดึงข้อมูลจริงต้องผ่าน `apiFetch()` ซึ่งจะโดน
  `api/` ตอบ 401/403 ทันทีถ้าไม่มีสิทธิ์ ก่อนจะเดินทางไปถึงขั้นตอนสร้างไฟล์ด้วยซ้ำ — ไม่ต้อง implement
  authorization logic ซ้ำใน Route Handler เลย
- Route Handler เองไม่จำเป็นต้องรู้จัก JWT/role อะไรทั้งสิ้น เป็นแค่ "ตัวแปลง JSON → xlsx" ล้วน ๆ ทำให้ผิว
  สัมผัส (attack surface) เล็กและ logic ง่ายต่อการตรวจทาน

**Alternatives considered**:
- เพิ่ม `GET /api/v1/reports/stock/export` และ `GET /api/v1/sales/export` ใน `api/` ที่คืนไฟล์ .xlsx ตรง ๆ
  — ปฏิเสธ เพราะเพิ่ม dependency ใหม่ (`exceljs` หรือ .NET เทียบเท่าอย่าง `ClosedXML`) เข้าไปใน backend DDD
  ที่ปัจจุบันไม่มี presentation-format concern แบบนี้เลย, ต้องเขียน + รีวิว business/infrastructure code
  เพิ่มใน `api/` (ผิด principle YAGNI ของ constitution ที่เน้นให้ business logic อยู่ domain layer ไม่ใช่
  format ไฟล์), และทำให้ `api/` ต้องรับรู้เรื่อง UI-specific concern (ชื่อคอลัมน์ภาษาไทยที่ตรงกับหน้าจอ)
  ซึ่งควรเป็นเรื่องของ `web/` ตาม Principle I
- สร้างไฟล์ทั้งหมดฝั่ง client (ไม่มี Route Handler เลย) — ปฏิเสธเพราะปัญหา `exceljs` ใน browser bundle
  ตามข้อ 1 ด้านบน

## 3. การดึงข้อมูลประวัติการขายให้ครบตามตัวกรอง (เกิน 1 หน้าจอ) พร้อมเพดาน 10,000

**Decision**: เพิ่มฟังก์ชันใน `web/src/lib/api/sales.ts` ที่วนเรียก `GET /api/v1/sales` ด้วย `pageSize=100`
(ค่าสูงสุดที่ endpoint อนุญาตตาม `contracts/sales.md` ของ `001-single-store-pos`) ไปเรื่อย ๆ จนกว่าจะครบ
`totalCount` — แต่เช็ค `totalCount` จาก response ของหน้าแรกก่อนเสมอ ถ้า `totalCount > 10000` ให้หยุดทันที
และคืนสัญญาณ "เกินเพดาน" แทนที่จะวนเก็บข้อมูลต่อ (กันการยิง request จำนวนมากโดยเปล่าประโยชน์)

**Rationale**:
- รูปแบบเดียวกับ `fetchAllPages()` ที่มีอยู่แล้วใน `web/src/lib/api/products.ts` (ใช้กับหน้าโปรโมชั่น/สต็อก
  ที่ต้องการรายการสินค้าทั้งหมดแบบไม่แบ่งหน้า) — ใช้แพทเทิร์นเดิมของโปรเจกต์แทนที่จะคิดใหม่
- เช็คเพดานจาก `totalCount` ของหน้าแรกก่อน แทนที่จะดึงมาทั้งหมดแล้วค่อยนับ ลดภาระทั้งฝั่ง client และ `api/`
  เวลาผู้ใช้เผลอเลือกช่วงวันที่กว้างเกินไป (เช่นทั้งปี) — สอดคล้องกับ edge case ที่ spec ระบุไว้ว่าต้อง "ไม่
  export บางส่วนเงียบ ๆ"

**Alternatives considered**:
- ให้ `api/` เพิ่ม endpoint ใหม่ที่คืนข้อมูลไม่จำกัดหน้าในคำขอเดียว — ปฏิเสธ ขัดกับ FR-034/FR-037 ของ
  `001-single-store-pos` ที่ยืนยันแล้วว่าบิลขายเป็นข้อมูลที่โตไม่จำกัด จึงต้องแบ่งหน้าเสมอ (server-side
  paging เป็นการตัดสินใจที่ยืนยันแล้วในฟีเจอร์ก่อนหน้า ไม่ควรเปิดช่องยกเว้นสำหรับ export)

## 4. การดาวน์โหลดไฟล์จาก Route Handler ที่ต้องแนบ JWT

**Decision**: ฝั่ง client เรียก Route Handler ด้วย `fetch()` ธรรมดา (ไม่ใช่ `<a href>` นำทางตรง เพราะไม่
สามารถแนบ custom header ได้) รับ response กลับมาเป็น `Blob` แล้วสร้าง Object URL ชั่วคราวเพื่อ trigger การ
ดาวน์โหลดผ่าน `<a download>` ที่สร้างขึ้นชั่วคราวด้วย JavaScript จากนั้น revoke Object URL ทิ้งทันที

**Rationale**: Route Handler ตัวนี้ไม่ได้ตรวจ JWT เอง (ดูข้อ 2) จึงไม่จำเป็นต้องแนบ Authorization header
ไปด้วยจริง ๆ — แต่ยังต้องใช้ `fetch()` + Blob อยู่ดี เพราะข้อมูลที่จะแปลงเป็นไฟล์ (แถว + นิยามคอลัมน์) ต้อง
ส่งไปเป็น request body (`POST`) ซึ่งการนำทางด้วย `<a href>` ทำไม่ได้ (รองรับแค่ GET แบบไม่มี body)

**Alternatives considered**: ทำ endpoint แบบ `GET` ที่รับ query string ยาว ๆ แทน — ปฏิเสธ เพราะแถวข้อมูล
ของประวัติการขาย (สูงสุด 10,000 แถว) ใหญ่เกินกว่าจะยัดใน URL query string ได้ (ข้อจำกัดความยาว URL ของ
เบราว์เซอร์/เซิร์ฟเวอร์ทั่วไป) `POST` พร้อม JSON body จึงเหมาะกว่า

## 5. ชื่อไฟล์ที่ดาวน์โหลด

**Decision**: ใช้ชื่อไฟล์ภาษาอังกฤษ/ASCII ล้วน ระบุประเภทรายงานและ timestamp เช่น
`taladpos-stock-report-2026-09-11.xlsx` และ `taladpos-sales-history-2026-09-11-1430.xlsx` (FR-010)

**Rationale**: ชื่อไฟล์ภาษาไทยใน HTTP header `Content-Disposition` ต้องเข้ารหัสแบบ RFC 5987
(`filename*=UTF-8''...`) ซึ่งบางเบราว์เซอร์/ระบบปฏิบัติการเก่ายังจัดการไม่สม่ำเสมอ — เนื้อหาภายในไฟล์ (หัว
คอลัมน์, ข้อมูล) เป็นภาษาไทยเต็มรูปแบบอยู่แล้วตาม FR-009 ซึ่งเป็นจุดที่สำคัญจริง ส่วนชื่อไฟล์ใช้ ASCII เพื่อ
ความเข้ากันได้สูงสุดโดยไม่กระทบ requirement ใดของ spec (spec ไม่ได้ระบุว่าชื่อไฟล์ต้องเป็นภาษาไทย)
