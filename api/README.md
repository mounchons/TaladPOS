# TaladPOS API

Backend ของระบบ POS ร้านค้าเดี่ยว — ASP.NET Core Web API (.NET 8) + EF Core 8 + PostgreSQL 16
จัดโครงสร้างแบบ DDD ตาม [`.specify/memory/constitution.md`](../.specify/memory/constitution.md)

สเปกและ contract ทั้งหมดอยู่ที่ [`specs/001-single-store-pos/`](../specs/001-single-store-pos/) —
เอกสารนี้เป็นคู่มือติดตั้ง/รันเท่านั้น สำหรับขั้นตอนตรวจรับฟีเจอร์ให้ดู
[`quickstart.md`](../specs/001-single-store-pos/quickstart.md)

## Prerequisites

- .NET 8 SDK
- Docker (สำหรับ PostgreSQL 16 ตาม `docker-compose.yml` ที่ root ของ repo) หรือ PostgreSQL 16 ที่ติดตั้งเอง

## รันครั้งแรก

```bash
docker compose up -d postgres     # จาก root ของ repo
cd api
dotnet restore
dotnet ef database update --project src/TaladPOS.Infrastructure --startup-project src/TaladPOS.Api
dotnet run --project src/TaladPOS.Api
```

API จะขึ้นที่ **http://localhost:5054** (โปรไฟล์ `http` ใน `src/TaladPOS.Api/Properties/launchSettings.json`)
ใช้ `dotnet run --project src/TaladPOS.Api --launch-profile https` ถ้าต้องการ HTTPS ที่ `https://localhost:7096`

> `dotnet ef` ต้องติดตั้ง tool ก่อนถ้ายังไม่มี: `dotnet tool install --global dotnet-ef`
> ตอนรันใน `Development` แอปจะ `Migrate()` + seed ข้อมูลตั้งต้นให้อัตโนมัติ (ดูหัวข้อ Seed ด้านล่าง)
> จึงข้ามคำสั่ง `database update` ได้ถ้ารันในโหมดนี้

## Swagger / OpenAPI

เปิด **http://localhost:5054/swagger** (เอกสาร JSON อยู่ที่ `/swagger/v1/swagger.json`)

ทุก endpoint ต้องล็อกอินก่อน (FR-029) ให้ทำตามนี้:

1. เรียก `POST /api/v1/auth/login` ด้วยบัญชี seed ด้านล่าง → คัดลอกค่า `token`
2. กดปุ่ม **Authorize** มุมขวาบน แล้ววาง token (ไม่ต้องพิมพ์ `Bearer` นำหน้า)
3. endpoint ที่สงวนไว้ให้ผู้จัดการจะตอบ `403` เมื่อเรียกด้วยบัญชี Cashier

Swagger UI เปิดเฉพาะตอน `ASPNETCORE_ENVIRONMENT=Development`

## ค่า Configuration

ค่าเริ่มต้นสำหรับ dev อยู่ใน `src/TaladPOS.Api/appsettings.Development.json` — override ได้ด้วย environment
variable (ใช้ `__` แทนจุดคั่นระดับ เช่น `ConnectionStrings__TaladPOSDb`)

| Key | ค่าเริ่มต้น (dev) | ใช้ทำอะไร |
|---|---|---|
| `ConnectionStrings:TaladPOSDb` | `Host=localhost;Port=5432;Database=taladpos;Username=taladpos;Password=taladpos_dev_password` | เชื่อมต่อ PostgreSQL |
| `Jwt:SigningKey` | คีย์ dev-only (**ต้องเปลี่ยนก่อน deploy จริง**) | เซ็น JWT — ไม่มีค่านี้แอปจะไม่สตาร์ท |
| `Jwt:Issuer` / `Jwt:Audience` | `TaladPOS` | ตรวจสอบ token |
| `Jwt:ExpiryMinutes` | `480` | อายุ token |
| `Cors:WebAppOrigin` | `http://localhost:3000` | origin ของ `web/` ที่อนุญาตให้เรียก API |

## Seed ข้อมูลตั้งต้น

ตอนรันใน `Development` `DevelopmentSeeder` จะเติมข้อมูลให้เมื่อตารางยังว่าง (quickstart.md ข้อ 3):

| ผู้ใช้ | username | password | role |
|---|---|---|---|
| ผู้จัดการร้าน | `manager` | `Manager123!` | Manager |
| แคชเชียร์ | `cashier` | `Cashier123!` | Cashier |

สินค้าตัวอย่าง: มะม่วง (บาร์โค้ด `8850000000012`, ราคา 45, สต็อก 10) และ แอปเปิ้ล (ไม่มีบาร์โค้ด, ราคา 60, สต็อก 3)

**บัญชีเหล่านี้เป็น dev-only** อย่าใช้กับฐานข้อมูลจริง

## Reset ข้อมูลทดสอบ (สำหรับ QA)

ล้าง products, promotions, members และ sales แล้วสร้างชุดข้อมูลทดสอบใหม่ (สินค้า 20 รายการพร้อมรูป,
โปรโมชั่น 8, สมาชิก 5, บิลย้อนหลังประมาณ 96 บิล) — `staff` ไม่ถูกล้าง:

```powershell
./scripts/reset-test-data.ps1                      # Windows จาก root ของ repo (start postgres ใน docker ให้ด้วย)
dotnet run --project api/tools/TaladPOS.TestData   # ทุก OS เมื่อ PostgreSQL รันอยู่แล้ว
```

ข้อมูลที่ได้ทั้งหมด สถานการณ์ทดสอบที่เตรียมไว้ การปรับแต่ง และการแก้ปัญหา ดู
[`docs/reset-test-data.md`](../docs/reset-test-data.md)

## Test

```bash
cd api
dotnet test TaladPOS.sln
```

> หยุด `dotnet run` ก่อนรัน `dotnet test` — โปรเซสที่รันอยู่จับไฟล์ `TaladPOS.Api.dll` ไว้
> ทำให้ build ของ test project เขียนทับไม่ได้

| Project | ครอบคลุม | ต้องมี PostgreSQL |
|---|---|---|
| `tests/TaladPOS.Domain.Tests` | business rule ในชั้น Domain (ส่วนลด, ตัดสต็อก, ยอดสะสมสมาชิก, ช่วงวันโปรโมชั่น) | ไม่ |
| `tests/TaladPOS.Application.Tests` | use case orchestration ด้วย fake repository | ไม่ |
| `tests/TaladPOS.Api.IntegrationTests` | REST endpoint จริงผ่าน `WebApplicationFactory` | **ใช่** |

Domain + Application test ต้องผ่าน 100% ก่อน merge งานที่แตะ business logic
(constitution Principle III — NON-NEGOTIABLE)

Integration test รันกับฐานข้อมูลแยกชื่อ `taladpos_test` เพราะสิ่งที่ทดสอบคือ SQL จริงที่ EF Core แปลออกมา
(เช่น conditional `UPDATE ... WHERE stock_quantity >= @qty` ที่กัน stock ติดลบตอนหลายจุดขายขายพร้อมกัน —
research.md #2) ซึ่ง in-memory provider พิสูจน์แทนไม่ได้ สร้าง database ก่อนรันครั้งแรก:

```bash
cd ..     # docker compose ต้องรันจาก root ของ repo ที่มี docker-compose.yml
docker compose exec postgres psql -U taladpos -d postgres -c 'CREATE DATABASE taladpos_test;'
```

ตั้ง `TEST_DB_CONNECTION` ได้ถ้าปลายทางไม่ใช่ค่าเริ่มต้น
(`Host=localhost;Port=5432;Database=taladpos_test;Username=taladpos;Password=taladpos_dev_password`)

## Migration

```bash
cd api
dotnet ef migrations add <ชื่อ> --project src/TaladPOS.Infrastructure --startup-project src/TaladPOS.Api
dotnet ef database update --project src/TaladPOS.Infrastructure --startup-project src/TaladPOS.Api
```

## โครงสร้างโปรเจกต์

```text
src/
├── TaladPOS.Domain/          entity + business rule ล้วน ไม่มี dependency กับ EF Core/ASP.NET
├── TaladPOS.Application/     use case, DTO, interface ของ repository
├── TaladPOS.Infrastructure/  EF Core DbContext, entity configuration, repository, migration
└── TaladPOS.Api/             controller, auth, CORS, Swagger, DI composition root
```

ทิศทาง dependency: `Api → Application → Domain` และ `Infrastructure → Application → Domain`
— ชั้น Domain ต้องไม่อ้างอิงชั้นอื่นเลย

## ขอบเขตของเวอร์ชันนี้

- บิลขายที่ชำระแล้วเป็น **append-only** ไม่มี endpoint แก้ไข/ลบ/คืนเงิน
- **ไม่มีการคำนวณ VAT** — `totalAmount` เท่ากับ `subtotalAmount - discountAmount` พอดี
- ส่วนลดเลือกใช้ค่าที่มากที่สุดเพียงค่าเดียว ไม่สะสมทับกัน (FR-022)
