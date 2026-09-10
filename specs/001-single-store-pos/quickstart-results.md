# ผลการรัน Quickstart Validation (tasks.md T078)

**วันที่รัน**: 2026-09-10 (UTC)
**อ้างอิง**: [quickstart.md](./quickstart.md) — ครบทุก scenario US1–US6 + หัวข้อ 5 (concurrency / append-only / ไม่มี VAT)
**ผลรวม**: **28/28 PASS**

> **ขอบเขตของผลรอบนี้**: การรัน 28/28 นี้เกิดขึ้น **ก่อน** งาน UI หน้าขาย TD02 (ใบเสร็จแบบ modal + ปุ่มพิมพ์)
> จึงยังไม่ครอบคลุม quickstart.md ข้อ US1-6 ที่เพิ่มเข้ามาทีหลัง เส้นทางใบเสร็จ modal/พิมพ์ถูกตรวจแยกต่างหาก
> ตอนทำ TD02 — สั่งพิมพ์เป็น PDF จริงจากทั้ง modal และหน้า `/sales/receipt/{id}` ได้หน้าเดียวทั้งคู่ที่มีเฉพาะสลิป
> — ดูรายละเอียดใน tasks.md TD02 ครั้งถัดไปที่รัน quickstart เต็มชุด ควรได้ 29 scenario

## สภาพแวดล้อมที่ใช้ทดสอบ

| ส่วน | ที่อยู่ | หมายเหตุ |
|---|---|---|
| PostgreSQL 16 | `localhost:5432` db `taladpos` | container `taladpos-postgres-1` ตาม `docker-compose.yml` |
| `api/` | `http://localhost:8080` | `dotnet run` ใน container SDK 8.0, `ASPNETCORE_ENVIRONMENT=Development` |
| `web/` | `http://localhost:3000` | `npm run dev`, `NEXT_PUBLIC_API_BASE_URL=http://localhost:8080` |

> พอร์ต `8080` เป็นค่าของสภาพแวดล้อมเครื่องนี้ (รัน API ใน container) ส่วน `dotnet run` บนเครื่องโดยตรงจะได้
> `5054` ตาม `launchSettings.json` ที่ quickstart.md ระบุ — ต่างกันแค่พอร์ต พฤติกรรมเดียวกัน

ฐานข้อมูลมีบิลสะสมจากการทดสอบก่อนหน้าอยู่แล้ว ทุก scenario จึงคำนวณค่าที่คาดหวังจากข้อมูลจริงที่อ่านกลับมา
ไม่ได้สมมติว่าฐานข้อมูลว่าง

## US1 — พนักงานทำการขายสินค้า

| # | Scenario | คาดหวัง | ผลที่สังเกตได้ | ผล |
|---|---|---|---|---|
| 1 | `POST /api/auth/login` ด้วย Cashier | 200 + JWT | 200, token present, staff=แคชเชียร์/Cashier | PASS |
| 2 | `GET /api/products?search=มะม่วง` | เจอสินค้า | 200, matches=1, found=มะม่วง | PASS |
| 3 | `GET /api/products?barcode=8850000000012` | สินค้าเดียวกัน | 200, matches=1, id=`354a6d6f-…e7b7` ตรงกับข้อ 2 | PASS |
| 4 | `POST /api/sales` มะม่วง ×2 | 201, ยอดถูก, สต็อกลด 2 | 201, totalAmount=90.00, stock 14→12 | PASS |
| 5 | `POST /api/sales` ตะกร้าว่าง | 400 | 400, `{"error":"empty_cart"}` | PASS |

## US2 — พนักงานล็อกอินและถูกบันทึกว่าเป็นผู้ขาย

| # | Scenario | คาดหวัง | ผลที่สังเกตได้ | ผล |
|---|---|---|---|---|
| 1 | `POST /api/sales` ไม่แนบ `Authorization` | 401 | 401 | PASS |
| 2 | `GET /api/sales/{id}` แสดงผู้ขาย | `staff.name` = บัญชีที่ล็อกอิน | 200, staff.name=แคชเชียร์ | PASS |

## US3 — ผู้จัดการดูแลสต็อกสินค้า

| # | Scenario | คาดหวัง | ผลที่สังเกตได้ | ผล |
|---|---|---|---|---|
| 1 | Manager `POST /api/products` | 201 | 201, id=`bc8a89c6-…714d`, name=ทุเรียนทดสอบ-g3x7jc | PASS |
| 2 | Cashier `GET /api/products` เห็นสินค้าใหม่ | พบในรายการ | 200, total=6, contains=true | PASS |
| 3 | `PUT /api/products/{id}` แก้ราคา | GET แสดงราคาใหม่ | 200/200, price=133.50 | PASS |
| 4 | สต็อก 3 ≤ threshold 5 → `?lowStockOnly=true` | เจอสินค้านี้ | 200, lowStockCount=1, contains=true | PASS |
| 5 | Cashier `POST /api/products` | 403 (FR-029) | 403 | PASS |

## US4 — สมัครสมาชิกและรับส่วนลดสมาชิก

| # | Scenario | คาดหวัง | ผลที่สังเกตได้ | ผล |
|---|---|---|---|---|
| 1 | `POST /api/members` สมาชิกใหม่ | 201, ยอดสะสม 0 | 201, id=`5ec559c1-…4252`, accumulated=0.00 | PASS |
| 2 | `POST /api/members` เบอร์ซ้ำ | 409 (FR-011) | 409, `phone_number_already_registered` | PASS |
| 3 | `GET /api/members?search=<เบอร์>` | เจอสมาชิก | 200, matches=1, name=สมาชิกทดสอบ-g3x7jc | PASS |
| 4 | ขายให้สมาชิก → ยอดสะสมเพิ่ม | 0.00 + 45.00 | 201, totalAmount=45.00, accumulated=45.00 | PASS |

## US5 — ผู้จัดการตั้งโปรโมชั่นส่วนลด

| # | Scenario | คาดหวัง | ผลที่สังเกตได้ | ผล |
|---|---|---|---|---|
| 1 | `POST /api/promotions` รายสินค้า 10% ครอบคลุมวันนี้ | 201 | 201, scope=Item, 10%, id=`c0bbe719-…40c1` | PASS |
| 2 | ขายสินค้าที่มีโปรโมชั่น | discount = 10% ของ 133.50 = 13.35 | 201, subtotal=133.50, discount=13.35, total=120.15 | PASS |
| 3 | โปรโมชั่นหมดอายุ (2026-01-01…2026-01-31, 50%) | discount = 0 | 201, discount=0.00, total=80.00 | PASS |
| 4 | โปรฯ ทั่วไป 10% + สมาชิก 20% บนเป้าหมายเดียวกัน | ใช้ค่ามากกว่าค่าเดียว = 26.70 (ไม่ใช่ 40.05) | 201, subtotal=133.50, discount=26.70, total=106.80 | PASS |

**หมายเหตุ US5-4 (FR-022)**: กฎ "เลือกส่วนลดสูงสุดค่าเดียว" ใช้ **ภายในขอบเขตเดียวกัน** ตามที่ research.md #3 และ
`DiscountResolver` ระบุไว้ ("multiple active promotions could apply to the same target") การทดสอบข้อนี้จึงตั้ง
โปรโมชั่นสองตัวที่เป็น `Item` scope บนสินค้าเดียวกันทั้งคู่ — ตัวหนึ่ง 10% ทั่วไป อีกตัว 20% เฉพาะสมาชิก — แล้วยืนยันว่า
ได้ 26.70 ไม่ใช่ 40.05

ส่วน `Item` scope กับ `Bill` scope ถือเป็นคนละเป้าหมาย ส่วนลดจากสองขอบเขตนี้ใช้ร่วมกันได้ตามการออกแบบ ไม่ใช่การละเมิด
FR-022

## US6 — ดูประวัติการขายและรายงานสรุป

| # | Scenario | คาดหวัง | ผลที่สังเกตได้ | ผล |
|---|---|---|---|---|
| 0 | Cashier เรียก `GET /api/reports/*` | 403 (รายงานเป็นของผู้จัดการ, FR-029) | 403 | PASS |
| 1 | `GET /api/sales?from=วันนี้&to=วันนี้` | เห็นบิลจาก US1–US5 ครบ | 200, billsToday=22, พบครบทั้ง 5 บิลที่สร้างในรอบนี้ | PASS |
| 2 | `GET /api/reports/sales?period=daily` | ตรงกับผลรวมบิลจริง | 200, totalSalesAmount=1675.40, billCount=22 — ตรงกับผลรวมที่คำนวณจาก `GET /api/sales` | PASS |
| 3 | `GET /api/reports/sales-by-staff` | ยอดต่อคนตรงกับบิลของคนนั้น | 200, แคชเชียร์=1223.90, ผู้จัดการร้าน=451.50 — ตรงกับที่จัดกลุ่มเองจากประวัติบิล | PASS |
| 4 | `GET /api/reports/stock` | ตรงกับ `GET /api/products` | 200, rows=7, mismatches=0 | PASS |

## หัวข้อ 5 — Non-Functional จากผลการ Clarify

| # | Scenario | คาดหวัง | ผลที่สังเกตได้ | ผล |
|---|---|---|---|---|
| NFR-1 | สองคำขอ `POST /api/sales` พร้อมกันซื้อชิ้นสุดท้าย (`stockQuantity == 1`) | 201 หนึ่ง / 409 `insufficient_stock` หนึ่ง / สต็อกไม่ติดลบ | `[201, 409]`, error=`insufficient_stock`, stock=0 | PASS |
| NFR-2 | Append-only: ไม่มี endpoint แก้ไข/ลบบิล | ทุก verb ตอบ 404/405 และบิลยังอยู่ | PUT=405, PATCH=405, DELETE=405, GET หลังจากนั้น=200 | PASS |
| NFR-3 | ไม่มี VAT: `totalAmount == subtotalAmount - discountAmount` | 0 บิลที่ไม่ตรง | ตรวจ 23 บิลของวันนี้, violations=0 | PASS |

NFR-1 ยังถูกครอบด้วย integration test อัตโนมัติที่
`api/tests/TaladPOS.Api.IntegrationTests/ConcurrencyTests.cs` (T075) ซึ่งรันกับ PostgreSQL จริงเช่นกัน

## ตรวจฝั่ง `web/` (quickstart.md ข้อ 2)

ทำผ่านเบราว์เซอร์จริงที่ `http://localhost:3000`:

| รายการ | ผลที่สังเกตได้ | ผล |
|---|---|---|
| หน้าขายโหลดได้ ล็อกอินค้างไว้เป็น "ผู้จัดการร้าน" | แสดงรายการสินค้า 8 รายการพร้อมราคา | PASS |
| สินค้าที่สต็อกหมดถูกปิดการใช้งาน | ปุ่ม "ชิ้นสุดท้าย-…" ขึ้นป้าย "สินค้าหมด" และกดไม่ได้ (ผลจาก NFR-1 ที่ตัดสต็อกเหลือ 0) | PASS |
| ขายจริงผ่าน UI (มะม่วง ×1 → ชำระเงิน) | ใบเสร็จขึ้น "ขายสำเร็จ", มะม่วง ×1, รับไป 45.00 บาท | PASS |
| หน้ารายงานของผู้จัดการ | ยอดขายรวม 1775.40 บาท / ส่วนลดรวม 108.60 บาท / 24 บิล | PASS |
| Console error ของเบราว์เซอร์ | 0 errors, 0 warnings | PASS |

ตัวเลข 1775.40 / 24 บิล สอดคล้องกับผลฝั่ง API พอดี: 1675.40 (ตอนวัดใน US6-2) + 55.00 (บิลที่ชนะใน NFR-1 ซึ่งเกิดหลังจากนั้น)
+ 45.00 (บิลที่ขายผ่าน UI) = 1775.40 และ 22 + 1 + 1 = 24 บิล

ภาพหน้าจอใบเสร็จ: `.playwright-mcp/quickstart-t078-sale-receipt.png` (ไม่ commit — อยู่ใน `.gitignore`)

## สิ่งที่แก้ระหว่างรัน T078

**ไม่มีบั๊กของระบบที่พบจาก scenario เหล่านี้** — 3 ข้อที่ FAIL ในรอบแรก (US6-2/3/4 ได้ 403) เกิดจากสคริปต์ตรวจสอบ
เรียก endpoint รายงานด้วย token ของ Cashier ทั้งที่ `ReportsController` เป็น `[Authorize(Roles = Manager)]`
ตามการออกแบบ (US6 เป็นเรื่องของผู้จัดการ) แก้ที่สคริปต์แล้วเพิ่ม US6-0 เพื่อยืนยัน 403 นั้นเป็นพฤติกรรมที่ตั้งใจ

บั๊กจริงที่เจอในรอบนี้อยู่นอก scenario ของ quickstart คือ `GET /swagger/v1/swagger.json` ตอบ 500
(schemaId ชนกัน) — บันทึกไว้ใน T077

## วิธีรันซ้ำ

Scenario ฝั่ง API ทั้งหมดรันด้วยสคริปต์เดียว (ยิง HTTP จริงตามลำดับใน quickstart.md แล้วเทียบค่าที่อ่านกลับมา)
ตัวสคริปต์เป็นไฟล์ชั่วคราวนอก repo ส่วน scenario ที่เป็นการยืนยันเชิงอัตโนมัติถาวรอยู่ใน
`api/tests/TaladPOS.Api.IntegrationTests/` แล้ว:

```bash
cd api
dotnet test TaladPOS.sln
```

ผลล่าสุด: **Domain 45 passed, Application 21 passed, Api.IntegrationTests 8 passed** (74 tests, 0 failed)
