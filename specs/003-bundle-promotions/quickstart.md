# Quickstart Validation: Bundle & Gift Promotions

**Feature**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md)
**Contracts**: [conditional-promotions.md](./contracts/conditional-promotions.md) · [sales-preview.md](./contracts/sales-preview.md)

เอกสารนี้คือ **วิธีพิสูจน์ว่าฟีเจอร์ทำงานจริงตั้งแต่ต้นจนจบ** ไม่ใช่คู่มือการเขียนโค้ด
รายละเอียดรูปร่างข้อมูลอยู่ใน `contracts/` และ `data-model.md` เอกสารนี้ไม่ทำซ้ำ

---

## 0. ข้อควรรู้ก่อนเริ่ม (เฉพาะเครื่องนี้)

| เรื่อง | รายละเอียด |
|---|---|
| พอร์ต PostgreSQL ชนกัน | ถ้าขึ้น `auth failed for taladpos` ให้ตรวจ `docker ps` ก่อน — คอนเทนเนอร์ `pg16-test` ก็ผูกพอร์ต 5432 เหมือนกัน อาการนี้คือพอร์ตชน ไม่ใช่รหัสผ่านผิด ต้องหยุดคอนเทนเนอร์นั้นก่อน |
| `dotnet` ผิดเวอร์ชัน | หน้าต่าง pwsh ใหม่จะหยิบ `~/.dotnet` ซึ่งมีแต่ .NET 10 ต้องเรียก `"C:\Program Files\dotnet\dotnet.exe"` แบบเต็ม path เมื่อรัน API |
| พอร์ตของบริการ | API: `http://localhost:5054` · Web: `http://localhost:3000` |
| บัญชีทดสอบ | `manager` / `Manager123!` (ผู้จัดการ) และ `cashier` / `Cashier123!` (แคชเชียร์) — script รีเซ็ตข้อมูลไม่ล้างตาราง `staff` |
| migration ทำงานเอง | `Program.cs` เรียก `dbContext.Database.MigrateAsync()` ตอนสตาร์ท **ไม่ต้องรัน `dotnet ef database update` เอง** แค่เปิด API ก็ได้สคีมาล่าสุด |
| `dotnet-ef` เป็น global tool | ติดตั้งไว้เวอร์ชัน 10 ใช้กับคำสั่ง `migrations add` / `migrations remove` ได้ แต่ต้องรันผ่าน SDK ใน Program Files เช่นเดียวกับข้างบน |

---

## 1. เตรียมสภาพแวดล้อม

```powershell
# 1. เปิด API — migration ถูกใช้อัตโนมัติตอนสตาร์ท จึงต้องรันขั้นนี้ก่อนรีเซ็ตข้อมูล
#    เพื่อให้ตารางใหม่ของฟีเจอร์นี้มีอยู่แล้วเมื่อ seeding tool ทำงาน
& "C:\Program Files\dotnet\dotnet.exe" run --project api/src/TaladPOS.Api

# 2. รีเซ็ตฐานข้อมูลเป็นชุดข้อมูลทดสอบ (สินค้า 20 รายการ + โปรโมชั่น + สมาชิก + บิลย้อนหลัง)
#    หลังทำฟีเจอร์นี้เสร็จ script เดียวกันจะสร้างโปรโมชั่นแบบมีเงื่อนไขตัวอย่างครบ 3 รูปแบบด้วย (research.md #12)
#    script นี้เรียก `dotnet run` แบบไม่ระบุ path ถ้าล้มเหลวเรื่อง framework resolution
#    ให้ใช้เหตุผลเดียวกับข้อ ".NET ผิดเวอร์ชัน" ข้างบน แล้วรัน TestData tool ด้วย dotnet.exe แบบเต็ม path แทน
./scripts/reset-test-data.ps1

# 3. เปิด web (อีกหน้าต่างหนึ่ง)
cd web; npm run dev
```

ขอ token ไว้ใช้กับ curl ทุกข้อด้านล่าง:

```powershell
$mgr  = (Invoke-RestMethod -Method Post http://localhost:5054/api/v1/auth/login -ContentType application/json `
         -Body '{"username":"manager","password":"Manager123!"}').token
$cash = (Invoke-RestMethod -Method Post http://localhost:5054/api/v1/auth/login -ContentType application/json `
         -Body '{"username":"cashier","password":"Cashier123!"}').token
$H  = @{ Authorization = "Bearer $mgr" }
$HC = @{ Authorization = "Bearer $cash" }
```

---

## 2. เกณฑ์ผ่าน: unit test ต้องเขียวก่อนทดสอบด้วยมือ

```powershell
& "C:\Program Files\dotnet\dotnet.exe" test api/TaladPOS.sln
```

**ต้องผ่านทั้งหมด** และต้องมีเทสเหล่านี้อยู่จริง (constitution Principle III — ดู plan.md ส่วน Testing):

| ชุดเทส | ครอบคลุม |
|---|---|
| `CartPricerTests` | ทุก acceptance scenario ใน spec.md (US1 ข้อ 1–7, US2 ข้อ 1–5, US3 ข้อ 1–4) โดยยืนยัน**ตัวเลขจริง** ไม่ใช่แค่ "มีส่วนลดเกิดขึ้น" |
| `CartPricerTests` (edge) | ทั้ง 10 edge case ใน spec.md โดยเฉพาะกรณี "ซื้อ A1+B1 แถม A1" ที่ของแถมเป็นสินค้าในเงื่อนไขและมีเงื่อนไขหลายรายการ |
| `CartPricerTests` (determinism) | ตะกร้าเดียวกันสลับลำดับ input แล้วได้ผลลัพธ์เท่ากันทุกฟิลด์ (FR-012, SC-006) |
| `ConditionalPromotionValidationTests` | กฎทุกข้อในตาราง Validation ของ `contracts/conditional-promotions.md` |
| `CompleteSaleUseCase` / `PreviewSaleUseCase` tests | การกรองโปรโมชั่นที่อ้างสินค้าที่ถูกลบ และลำดับตั้งราคา→ตัดสต็อกที่สลับใหม่ (research.md #8) |

---

## 3. สถานการณ์ที่ต้องพิสูจน์ด้วยมือ

### S1 — ซื้อครบชุดแล้วได้ของแถม (User Story 1)

1. ล็อกอินเป็น `manager` เปิดหน้าโปรโมชั่น สร้างโปรโมชั่นแบบมีเงื่อนไข "ซื้อ A 1 + B 1 แถม C 1"
   ช่วงวันที่ครอบคลุมวันนี้ ไม่จำกัดเฉพาะสมาชิก
2. ตรวจว่าตารางโปรโมชั่นแสดงข้อความบรรทัดเดียวอ่านรู้เรื่อง เช่น `ซื้อ A 1 + B 1 แถม C 1` (FR-008)
3. ล็อกอินเป็น `cashier` เปิดหน้าขาย เพิ่ม A 1 ชิ้น และ B 1 ชิ้น
   - **คาดหวัง**: ตะกร้าขึ้นข้อความว่ายังไม่ได้ใช้สิทธิ์แถม C อีก 1 ชิ้น และ **ระบบไม่เพิ่ม C ให้เอง** (FR-014, FR-015)
4. เพิ่ม C 1 ชิ้นเข้าตะกร้า
   - **คาดหวัง**: ข้อความเตือนหาย, C แสดงเป็นของแถมยอดสุทธิ 0 บาท, ยอดรวมเท่ากับราคา A + B
5. กดชำระเงิน แล้วดูใบเสร็จ
   - **คาดหวัง**: ใบเสร็จมีบรรทัด C ที่แสดง**ราคาปกติ** พร้อมส่วนลดเท่ากับราคานั้น ยอดสุทธิ 0 (FR-016)
     และแสดงชื่อโปรโมชั่นที่ถูกใช้ (FR-024)
6. เปิดหน้าสต็อก — **คาดหวัง**: จำนวนคงเหลือของ C ลดลง 1 ชิ้น (FR-017)

### S2 — ซื้อ 2 แถม 1 สินค้าเดียวกัน (User Story 2)

1. เป็น `manager` สร้าง "ซื้อ A 2 แถม A 1"
2. เป็น `cashier` เพิ่ม A **5 ชิ้น** ลงตะกร้า
   - **คาดหวัง**: ยอดสุทธิเท่ากับ A 4 ชิ้น และตะกร้าแสดงว่า 1 ชิ้นเป็นของแถม (FR-010, FR-011)
3. กดชำระเงิน — **คาดหวัง**: ใบเสร็จแสดง **สองบรรทัด** คือ A 4 ชิ้นราคาเต็ม และ A 1 ชิ้นของแถมยอดสุทธิ 0
   (FR-016 — ของแถมแยกบรรทัดเสมอแม้เป็นสินค้าเดียวกัน) และสต็อก A ลดลง **5 ชิ้น**

### S3 — ซื้อครบชุดแล้วลดเปอร์เซ็นต์ (User Story 3)

1. เป็น `manager` สร้าง "ซื้อ A 1 + B 1 ลด 15%"
2. เป็น `cashier` เพิ่ม A 1, B 1 และสินค้าอื่นอีก 1 ชิ้น
   - **คาดหวัง**: ส่วนลดเท่ากับ 15% ของ (ราคา A + ราคา B) พอดี สินค้าตัวที่สามไม่ถูกลด (FR-020)

### S4 — พรีวิวต้องตรงกับยอดที่ตัดจริง (FR-012, SC-006)

```powershell
$cart = '{"memberId":null,"lineItems":[{"productId":"<A>","quantity":5}]}'
$preview = Invoke-RestMethod -Method Post http://localhost:5054/api/v1/sales/preview `
           -Headers $HC -ContentType application/json -Body $cart
$sale    = Invoke-RestMethod -Method Post http://localhost:5054/api/v1/sales `
           -Headers $HC -ContentType application/json -Body $cart

$preview.totalAmount -eq $sale.totalAmount      # ต้องได้ True
```

ทำซ้ำโดยสลับลำดับ `lineItems` ในตะกร้าที่มีหลายสินค้า — `totalAmount` ต้องเท่ากันทุกครั้ง

### S5 — แคชเชียร์ต้องเรียกพรีวิวได้ แต่ห้ามแตะโปรโมชั่น

```powershell
Invoke-RestMethod -Method Post http://localhost:5054/api/v1/sales/preview `
  -Headers $HC -ContentType application/json -Body $cart          # ต้องได้ 200

Invoke-RestMethod http://localhost:5054/api/v1/conditional-promotions -Headers $HC   # ต้องได้ 403
```

### S6 — โปรโมชั่นที่อ้างสินค้าที่ถูกลบ (FR-023)

1. สร้างโปรโมชั่นที่แถมสินค้า C แล้วลบสินค้า C ออกจากระบบ
2. **คาดหวัง**: หน้าจัดการโปรโมชั่นแสดงว่าโปรโมชั่นนี้ใช้ไม่ได้พร้อมเหตุผล (`isUsable = false`)
3. เป็น `cashier` ทำรายการที่เข้าเงื่อนไขของโปรโมชั่นนั้น
   - **คาดหวัง**: ไม่มีสิทธิ์เกิดขึ้น และ **ไม่มีข้อความเตือนให้ไปหยิบสินค้าที่ถูกลบแล้ว** (research.md #7)

### S7 — ของเดิมต้องไม่เปลี่ยน (FR-026, SC-007)

1. ลบหรือปิดโปรโมชั่นแบบมีเงื่อนไขทั้งหมด เหลือแต่โปรโมชั่นเปอร์เซ็นต์เดิม
2. ทำรายการขายที่เคยทดสอบไว้ใน `001/quickstart.md`
   - **คาดหวัง**: ยอดสุทธิ ส่วนลด และใบเสร็จเหมือนเดิมทุกตัวเลข
3. ตรวจ `git diff` — **คาดหวัง**: `Promotion.cs`, `DiscountResolver.cs`, `PromotionConfiguration.cs`,
   `PromotionRepository.cs`, `PromotionsController.cs` **ไม่ถูกแตะเลยแม้แต่บรรทัดเดียว**

### S8 — บิลเก่าต้องอ่านได้เหมือนเดิม

เปิดประวัติการขายและใบเสร็จของบิลที่บันทึกไว้**ก่อน**ทำฟีเจอร์นี้
- **คาดหวัง**: แสดงผลปกติ ทุกบรรทัดมี `isGift = false` และ `appliedPromotions` เป็นอาร์เรย์ว่าง

---

## 4. เช็กลิสต์สุดท้ายก่อนถือว่าเสร็จ

- [ ] `dotnet test` ผ่านทั้ง solution รวมเทสใหม่ทุกชุดในข้อ 2
- [ ] S1–S8 ผ่านครบทุกข้อ
- [ ] หน้าจอทั้งสาม (โปรโมชั่น, ขาย, ใบเสร็จ) ใช้งานได้ที่ความกว้าง 360 / 768 / 1024 / 1440 px
      โดยไม่มีการเลื่อนแนวนอนของทั้งหน้า (`001/FR-031`)
- [ ] ค้นหาในโฟลเดอร์ `web/src` แล้ว **ไม่พบ** การคำนวณเปอร์เซ็นต์ส่วนลดหรือการนับชุดใดๆ
      (constitution Principle I — ตัวเลขทุกตัวต้องมาจาก API)
- [ ] migration ย้อนกลับได้: `dotnet ef migrations remove` แล้วฐานข้อมูลกลับสู่สถานะก่อนหน้าได้สะอาด
