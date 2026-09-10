# TaladPOS Web

Frontend ของระบบ POS ร้านค้าเดี่ยว — Next.js 14 (App Router) + Tailwind CSS 4 + daisyUI 5

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

> หยุด `npm run dev` ก่อนรัน `npm run build` — build เขียนทับโฟลเดอร์ `.next/` ที่ dev server กำลังใช้อยู่
> ทำให้ dev server ที่ยังรันค้างเสิร์ฟ chunk เป็น 404 ทั้งหมด (แก้ด้วยการรีสตาร์ท dev server)

## โครงสร้าง

```text
src/
├── app/                Next.js App Router — /login, /sales, /stock, /members, /promotions, /reports
├── components/         UI component ที่ใช้ร่วมกัน (Cart, AppShell, ฟอร์ม dialog ต่าง ๆ)
└── lib/api/            REST client — จุดเดียวที่ประกอบ URL/header ไปหา api/
```

### Styling

**ไม่มี `tailwind.config.ts`** — Tailwind 4 อ่าน config จาก CSS ทั้งหมด ทุกอย่างอยู่ใน
`src/app/globals.css` ที่เดียว แบ่งเป็นสองบล็อกที่ต้องมีคู่กันเสมอ:

| บล็อก | สร้างอะไร | ถ้าขาด |
|---|---|---|
| `@theme` | utility ของโปรเจกต์ (`bg-steel-50`, `text-ink-300`, `font-display`, `rounded-control`) | class เหล่านั้นหายเงียบ ๆ ไม่มี error |
| `@plugin "daisyui/theme"` | สีของ component daisyUI (`btn`, `input`, `table`, `modal`) | component ออกมาเป็นสี default ของ daisyUI ไม่ใช่สีร้าน |

> **กับดักที่เคยเจอจริง**: `@theme` วาง token ไว้ที่ `:root` ส่วน `next/font` ประกาศตัวแปรฟอนต์ไว้ที่
> element ที่ใส่ `className` ให้ custom property ที่มี `var()` ข้างในถูก resolve ณ element ที่ประกาศมัน
> ถ้า `--font-kanit` ไม่ได้อยู่บน `<html>` ตัว `--font-display` จะกลายเป็น invalid ที่ `:root`
> แล้วทุก `font-display` ตกกลับไปใช้ฟอนต์ body **โดยไม่มี error ใด ๆ** — ตัวแปรฟอนต์จึงต้องอยู่บน
> `<html>` ใน `layout.tsx` ไม่ใช่ `<body>`

component ที่เขียนเองแทน library: `DataTable.tsx` (ตาราง + server paging + ช่องใส่ filter),
`Modal.tsx` (`<dialog>` ที่ไม่ปิดเมื่อคลิกนอกกล่อง), `MemberSearch.tsx` (autocomplete พร้อมคีย์บอร์ด)

### สิ่งที่ต้องระวังตอน build

`npm run build` เขียนทับ `.next/` ของ dev server ที่รันอยู่ ทำให้ทุก chunk กลายเป็น 404 ทันที
**ต้องหยุด dev server ก่อน build เสมอ** แล้วค่อยเริ่มใหม่

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
