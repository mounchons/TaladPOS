# TaladPOS Web

Frontend ของระบบ POS ร้านค้าเดี่ยว — Next.js 14 (App Router) + Tailwind CSS + PrimeReact

`web/` คุยกับ backend ผ่าน **REST API เท่านั้น** ไม่มีการเชื่อมต่อฐานข้อมูลโดยตรง
(constitution Principle I — ดูหัวข้อ [ขอบเขตของ frontend](#ขอบเขตของ-frontend))

สเปกอยู่ที่ [`specs/001-single-store-pos/`](../specs/001-single-store-pos/) —
ขั้นตอนตรวจรับฟีเจอร์ดู [`quickstart.md`](../specs/001-single-store-pos/quickstart.md)

## Prerequisites

- Node.js 20 LTS + npm
- `api/` ที่รันอยู่แล้ว (ดู [`api/README.md`](../api/README.md)) — หน้าเว็บทุกหน้าต้องล็อกอินก่อนใช้งาน

## รันครั้งแรก

```bash
cd web
cp .env.local.example .env.local   # แล้วแก้ URL ให้ตรงกับ api/ ที่รันอยู่
npm install
npm run dev
```

เปิด **http://localhost:3000** แล้วล็อกอินด้วยบัญชี seed จาก `api/` (`manager` / `Manager123!`
หรือ `cashier` / `Cashier123!`)

## Environment variables

| ตัวแปร | ค่าเริ่มต้นใน `.env.local.example` | ใช้ทำอะไร |
|---|---|---|
| `NEXT_PUBLIC_API_BASE_URL` | `http://localhost:5054` | base URL ของ `api/` — ตัวแปรเดียวที่ `web/` ต้องใช้ |

ต้องตั้งให้ตรงกับพอร์ตที่ `api/` รันจริง (`dotnet run` โปรไฟล์ `http` → `5054`) และพอร์ตนี้ต้องตรงกับค่า
`Cors:WebAppOrigin` ฝั่ง API ด้วย ไม่งั้นเบราว์เซอร์จะบล็อกด้วย CORS

## คำสั่งที่ใช้บ่อย

| คำสั่ง | ทำอะไร |
|---|---|
| `npm run dev` | dev server ที่ `http://localhost:3000` |
| `npm run build` | production build |
| `npm start` | รัน production build (ต้อง `npm run build` ก่อน) |
| `npm run lint` | ESLint |
| `npx tsc --noEmit` | ตรวจ type ทั้งโปรเจกต์ |

## โครงสร้าง

```text
src/
├── app/                Next.js App Router — /login, /sales, /stock, /members, /promotions, /reports
├── components/         UI component ที่ใช้ร่วมกัน (Cart, AppShell, ฟอร์ม dialog ต่าง ๆ)
├── lib/api/            REST client — จุดเดียวที่ประกอบ URL/header ไปหา api/
└── styles/             Tailwind design token + PrimeReact passthrough preset
```

### Styling

Tailwind CSS เป็นแหล่ง styling เดียว PrimeReact ใช้แบบ unstyled ผ่าน passthrough preset ใน
`src/styles/primereact-passthrough.ts` ไม่ได้โหลดธีม CSS ของ PrimeReact เข้ามา

> `tailwind.config.ts` ต้อง scan `src/styles/` ด้วย ไม่งั้นคลาสที่ใช้เฉพาะใน passthrough preset
> จะถูก purge ทิ้งแบบเงียบ ๆ

## ขอบเขตของ frontend

ตาม constitution Principle I — `web/` **ห้าม** เข้าถึง PostgreSQL โดยตรง สิ่งที่ตามมา:

- ไม่มี database driver หรือ ORM ใน `package.json` (`pg`, Prisma, Drizzle, TypeORM ฯลฯ)
- ไม่มี connection string หรือ `DATABASE_URL` ที่ไหนใน `web/` — env var ตัวเดียวคือ `NEXT_PUBLIC_API_BASE_URL`
- ไม่มี route handler / server action ที่ดึงข้อมูลเอง ทุกการเรียกข้อมูลผ่าน `src/lib/api/`
- JWT เก็บฝั่ง client และแนบเป็น `Authorization: Bearer` โดย `src/lib/api/client.ts`

ตรวจซ้ำได้ด้วย:

```bash
grep -rniE 'npgsql|postgres|prisma|drizzle|typeorm|knex|sequelize|DATABASE_URL|:5432' \
  --include='*.ts' --include='*.tsx' --include='*.json' src/ package.json
```

ต้องไม่มีผลลัพธ์
