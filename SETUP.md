# SETUP.md — คู่มือติดตั้ง TaladPOS สำหรับ Claude Code

> **ไฟล์นี้เขียนให้ Claude Code อ่านและทำตาม** นักเรียน clone repo แล้วเปิด Claude Code ที่ root ของ repo
> จากนั้นสั่งว่า: `อ่าน SETUP.md แล้วติดตั้งและเปิดระบบให้`
>
> คู่มือสำหรับคนอ่านเอง (อธิบายละเอียดกว่า): [docs/student-setup.md](docs/student-setup.md)

---

## เป้าหมาย

ทำให้นักเรียนเปิด **http://localhost:3000** แล้วล็อกอินด้วย `manager` / `Manager123!` ได้
และเห็นสินค้า 20 รายการพร้อมรูป โดยทุกอย่างรันบนเครื่องนักเรียนเอง:

| ส่วน | รันด้วย | พอร์ต |
|---|---|---|
| ฐานข้อมูล PostgreSQL 16 + ข้อมูลตัวอย่าง (`db/init/01-taladpos.sql`) | `docker compose` | 5432 |
| API (.NET 8) | `dotnet run` | 5054 |
| เว็บ (Next.js) | `npm run dev` | 3000 |

---

## กติกาที่ Claude Code ต้องทำตาม

1. **คุยกับนักเรียนเป็นภาษาไทย** บอกทุกครั้งก่อนติดตั้งโปรแกรมว่ากำลังจะติดตั้งอะไรและทำไม
2. **ทำทีละขั้น: ตรวจ → ทำ → ยืนยันผล** ถ้าตรวจแล้วผ่านอยู่แล้วให้ข้ามขั้นนั้น คู่มือนี้ต้องรันซ้ำได้โดยไม่มีผลเสีย
3. **เจอคำว่า ⛔ STOP ให้หยุดรอนักเรียนจริง ๆ** อย่าข้ามไปขั้นถัดไปเอง
4. **ห้ามแก้ไฟล์ที่อยู่ใน git** ไฟล์เดียวที่สร้างได้คือ `web/.env.local`
5. **ห้าม** `git commit` / `push` / `pull` / `checkout` / `reset`
6. **ห้ามลบข้อมูลหรือหยุดโปรแกรมอื่นโดยไม่ถามก่อน** ได้แก่ `docker compose down -v`, `docker volume rm`, `docker stop` container อื่น, kill process
7. **ห้ามรัน** `dotnet ef database update` และห้ามแก้ข้อมูลในฐานข้อมูล เพราะไฟล์ dump มีตาราง ประวัติ migration และข้อมูลครบแล้ว
8. **ห้ามเปลี่ยนพอร์ต** 5432 / 5054 / 3000 เพราะ URL รูปสินค้าในข้อมูลตัวอย่าง, CORS และ `web/.env.local` ผูกกับพอร์ตเหล่านี้
9. **คำสั่งที่ใช้เวลานาน** (`winget`, `brew`, `docker compose up` ครั้งแรก, `dotnet build`, `npm ci`) ให้ตั้ง timeout 600000 ms
10. **server ที่ต้องรันค้าง** (`dotnet run`, `npm run dev`) ให้รันแบบ background แล้ว poll ด้วย `curl` ห้ามรันแบบรอจนจบ
11. **คำสั่งที่ต้องใส่รหัสผ่าน `sudo`** Claude Code พิมพ์รหัสให้ไม่ได้ ให้บอกนักเรียนพิมพ์ `! <คำสั่ง>` ในช่องแชทเอง
12. คำสั่งในคู่มือเขียนแบบ bash ใช้ได้กับ macOS, Linux และ Git Bash บน Windows ส่วนที่ต่างกันระบุ OS ไว้แล้ว ทุกคำสั่งรันจาก root ของ repo

---

## ขั้น 0 — ตรวจตำแหน่งและระบบปฏิบัติการ

**ตรวจ:** ต้องอยู่ที่ root ของ repo คือมีไฟล์เหล่านี้ครบ

```bash
ls docker-compose.yml db/init/01-taladpos.sql api/TaladPOS.sln web/package.json
```

- ถ้าไม่ครบ → ถามนักเรียนว่า clone repo ไว้ที่ไหน ถ้ายังไม่ได้ clone ให้ขอ URL แล้ว `git clone <URL>`
  จากนั้นบอกนักเรียนให้เปิด Claude Code ใหม่ในโฟลเดอร์นั้น ⛔ **STOP**

**ระบุ OS** (Windows / macOS / Linux) แล้วตรวจตัวติดตั้งแพ็กเกจ:

- Windows: `winget --version`
- macOS: `brew --version` ถ้าไม่มี Homebrew ให้บอกนักเรียนติดตั้งจาก https://brew.sh ก่อน ⛔ **STOP**
- Linux: ดูชื่อ distro จาก `cat /etc/os-release`

---

## ขั้น 1 — ตรวจโปรแกรมที่ต้องมี

รันทุกข้อ แล้วสรุปเป็นตารางให้นักเรียนดูว่ามีอะไรแล้ว อะไรยังขาด

| โปรแกรม | คำสั่งตรวจ | ผ่านเมื่อ |
|---|---|---|
| Git | `git --version` | มีเวอร์ชันแสดง |
| Docker | `docker --version` และ `docker compose version` | มีทั้งสองคำสั่ง (compose ต้องเป็น v2) |
| .NET | `dotnet --list-runtimes` และ `dotnet --list-sdks` | runtimes มีบรรทัดขึ้นต้นด้วย `Microsoft.AspNetCore.App 8.0.` **และ** มี SDK เวอร์ชัน 8 ขึ้นไป |
| Node.js | `node -v` และ `npm -v` | Node เวอร์ชัน 20 ขึ้นไป |

**กรณีพิเศษบน Windows (.NET):** ถ้า `dotnet --list-runtimes` ไม่มี `Microsoft.AspNetCore.App 8.0.`
ให้ลองอีกครั้งด้วย full path:

```bash
"/c/Program Files/dotnet/dotnet.exe" --list-runtimes
```

ถ้า full path มี 8.0 แสดงว่าติดตั้งแล้ว แต่ `dotnet` บน PATH ชี้ไปตัวอื่น (มักเป็น `~/.dotnet` ที่มีแค่ .NET รุ่นใหม่)
ให้ใช้ `"/c/Program Files/dotnet/dotnet.exe"` แทน `dotnet` ในทุกคำสั่งของคู่มือนี้ และถือว่าข้อนี้ผ่าน

ถ้าทุกข้อผ่าน → ข้ามไป **ขั้น 3**

---

## ขั้น 2 — ติดตั้งโปรแกรมที่ขาด

ถามนักเรียนก่อนติดตั้ง และบอกล่วงหน้าว่าอาจมีหน้าต่างขอสิทธิ์ผู้ดูแลระบบ (Windows UAC / รหัสผ่าน macOS) ให้กดยืนยันเอง
ติดตั้ง**เฉพาะตัวที่ขาด**

### Windows (winget)

```bash
winget install --id Git.Git -e --accept-package-agreements --accept-source-agreements
winget install --id Docker.DockerDesktop -e --accept-package-agreements --accept-source-agreements
winget install --id Microsoft.DotNet.SDK.8 -e --accept-package-agreements --accept-source-agreements
winget install --id OpenJS.NodeJS.LTS -e --accept-package-agreements --accept-source-agreements
```

### macOS (Homebrew)

```bash
brew install --cask docker-desktop
brew install --cask dotnet-sdk@8
brew install node@24 && brew link --overwrite --force node@24
```

### Linux

ติดตั้งผ่าน package manager ของ distro ต้องใช้ `sudo` ให้นักเรียนพิมพ์คำสั่งเองด้วย `! ...`
ตามคู่มือทางการ (เลือกหัวข้อให้ตรงกับ distro จากขั้น 0):

- Docker Engine + compose plugin: https://docs.docker.com/engine/install/
- .NET 8 SDK: https://learn.microsoft.com/dotnet/core/install/linux
- Node.js LTS: https://nodejs.org/en/download

### หลังติดตั้งเสร็จ ⛔ STOP

โปรแกรมที่เพิ่งติดตั้งยังไม่อยู่ใน PATH ของ session นี้ ให้บอกนักเรียนว่า:

> ติดตั้งเสร็จแล้ว กรุณาปิด Claude Code และปิดหน้าต่าง terminal นี้
> (ถ้าติดตั้ง Docker Desktop บน Windows แล้วโปรแกรมขอให้ restart ให้ restart เครื่องก่อน)
> จากนั้นเปิด terminal ใหม่ที่โฟลเดอร์โปรเจกต์ เปิด Claude Code แล้วสั่ง `อ่าน SETUP.md แล้วทำต่อ`

---

## ขั้น 3 — ให้ Docker พร้อมใช้งาน

**ตรวจ:**

```bash
docker info > /dev/null && echo DOCKER_OK
```

- ขึ้น `DOCKER_OK` → ไป **ขั้น 4**
- ไม่ขึ้น:
  - **Windows / macOS:** บอกนักเรียนให้เปิดโปรแกรม **Docker Desktop** กดยอมรับเงื่อนไขการใช้งาน
    แล้วรอจนมุมซ้ายล่างขึ้นว่า Engine running
    ถ้า Windows แจ้งว่าต้องติดตั้ง WSL 2 ให้นักเรียนเปิด PowerShell แบบ Run as administrator
    แล้วรัน `wsl --install` จากนั้น restart เครื่อง ⛔ **STOP** รอนักเรียนบอกว่าเสร็จ แล้วตรวจใหม่
  - **Linux:** ให้นักเรียนรัน `! sudo systemctl start docker`
    ถ้าขึ้น `permission denied` ให้รัน `! sudo usermod -aG docker $USER` แล้ว logout/login ใหม่ ⛔ **STOP**

---

## ขั้น 4 — ตรวจว่าพอร์ตว่าง

**4.1 ตรวจว่าฐานข้อมูลของโปรเจกต์นี้รันอยู่แล้วหรือยัง:**

```bash
docker compose ps
```

ถ้ามี service `postgres` สถานะ `running` / `Up` → พอร์ต 5432 เป็นของโปรเจกต์นี้ ไม่ต้องตรวจ 5432

**4.2 ดูว่าพอร์ตไหนถูกใช้อยู่:**

```bash
# Windows (Git Bash)
netstat -ano | grep -E ":(5432|5054|3000) .*LISTENING"

# macOS / Linux
lsof -nP -iTCP -sTCP:LISTEN | grep -E ":(5432|5054|3000) "
```

ไม่มีผลลัพธ์ = พอร์ตว่างหมด → ไป **ขั้น 5**

**4.3 ถ้ามีพอร์ตถูกใช้** ให้ตรวจทีละพอร์ตว่าเป็นของโปรเจกต์นี้หรือโปรแกรมอื่น

**พอร์ต 5432** ดูว่า container ไหนใช้อยู่:

```bash
docker ps --format '{{.Names}}\t{{.Ports}}' | grep 5432
```

- ไม่ใช่ container ของโปรเจกต์นี้ → ถามนักเรียนว่าหยุด container นั้นได้ไหม ถ้าได้ให้รัน `docker stop <ชื่อ container>`
- ไม่มี container ไหนใช้ แต่พอร์ตยังถูกใช้ → เป็น PostgreSQL ที่ติดตั้งในเครื่อง ให้นักเรียนหยุด service เอง
- นักเรียนไม่อนุญาตให้หยุด ⛔ **STOP** (ระบบรันไม่ได้ถ้าพอร์ตนี้ไม่ว่าง)

**พอร์ต 5054** ตรวจว่าเป็น API ของโปรเจกต์นี้ที่เปิดค้างไว้หรือไม่:

```bash
curl -s http://localhost:5054/swagger/v1/swagger.json | grep -q "TaladPOS API" && echo OURS
```

**พอร์ต 3000** ตรวจว่าเป็นเว็บของโปรเจกต์นี้ที่เปิดค้างไว้หรือไม่ (ได้ `200` = ใช่):

```bash
curl -s -o /dev/null -w '%{http_code}\n' http://localhost:3000/images/products/mango-512.png
```

- เป็นของโปรเจกต์นี้ → ถือว่าส่วนนั้นรันอยู่แล้ว ข้าม "เปิด server" ในขั้น 6 / 7 แต่ยังต้องทำ "ยืนยันผล"
- เป็นโปรแกรมอื่น → ถามนักเรียนก่อนปิดโปรแกรมนั้น ⛔ **STOP** จนกว่าพอร์ตจะว่าง

---

## ขั้น 5 — เปิดฐานข้อมูลและโหลดข้อมูลตัวอย่าง

**ทำ** (ครั้งแรกต้องดาวน์โหลด image `postgres:16` อาจใช้เวลาหลายนาที):

```bash
docker compose up -d
```

**รอจนพร้อม:** ต้องเช็กผ่าน TCP (`-h 127.0.0.1`) เพราะระหว่างโหลดข้อมูลครั้งแรก PostgreSQL ยังไม่เปิด TCP
ถ้ารับ TCP ได้แปลว่าโหลดข้อมูลเสร็จแล้ว

```bash
for i in $(seq 1 90); do
  docker compose exec -T postgres pg_isready -h 127.0.0.1 -U taladpos -d taladpos > /dev/null 2>&1 && echo DB_READY && break
  sleep 2
done
```

**ยืนยันผล:**

```bash
docker compose exec -T postgres psql -U taladpos -d taladpos -Atc "SELECT (SELECT count(*) FROM products) || ' products, ' || (SELECT count(*) FROM sales) || ' sales'"
```

| ผลลัพธ์ | ความหมาย | ทำอะไรต่อ |
|---|---|---|
| `20 products, 96 sales` | โหลดข้อมูลตัวอย่างครบ | ไป **ขั้น 6** |
| `20 products` แต่ sales มากกว่า 96 | นักเรียนเคยใช้ระบบและขายของไปแล้ว | ปกติ ไป **ขั้น 6** |
| `relation "products" does not exist` หรือจำนวนสินค้าไม่ใช่ 20 | volume มีข้อมูลเก่าจากก่อนมีไฟล์ dump PostgreSQL จึงไม่โหลดไฟล์ dump | อธิบายให้นักเรียนฟัง แล้ว**ถามก่อน**ว่าลบข้อมูลเดิมได้ไหม ถ้าได้ให้รัน `docker compose down -v` ตามด้วย `docker compose up -d` แล้วทำขั้นนี้ใหม่ |

ดู log เพิ่มได้ที่ `docker compose logs postgres` ถ้าโหลดครั้งแรกจะมีบรรทัด `running /docker-entrypoint-initdb.d/01-taladpos.sql`

---

## ขั้น 6 — เปิด API

**6.1 Build** (ครั้งแรกต้องดาวน์โหลด NuGet packages):

```bash
dotnet build api/src/TaladPOS.Api --nologo -v q
```

ถ้า build ขึ้น `being used by another process` แปลว่ามี API ตัวเดิมรันค้างอยู่ ให้กลับไปตรวจพอร์ต 5054 ในขั้น 4.3

**6.2 เปิด server แบบ background** (ใช้ `--no-build` เพราะ build แล้ว):

```bash
dotnet run --project api/src/TaladPOS.Api --no-build
```

**6.3 รอจนพร้อม:**

```bash
for i in $(seq 1 60); do
  [ "$(curl -s -o /dev/null -w '%{http_code}' http://localhost:5054/swagger/index.html)" = "200" ] && echo API_READY && break
  sleep 2
done
```

**6.4 ยืนยันผล** ว่าล็อกอินได้และอ่านสินค้าจากฐานข้อมูลได้:

```bash
TOKEN=$(curl -s -X POST http://localhost:5054/api/v1/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"username":"manager","password":"Manager123!"}' | sed -E 's/.*"token":"([^"]+)".*/\1/')
echo "token length: ${#TOKEN}"
curl -s -o /dev/null -w 'products: %{http_code}\n' -H "Authorization: Bearer $TOKEN" http://localhost:5054/api/v1/products
```

ผ่านเมื่อ token ยาวหลายร้อยตัวอักษร และ `products: 200`

**ถ้าไม่ผ่าน** ให้อ่าน output ของ background task แล้วเทียบกับตารางนี้:

| ข้อความใน output | สาเหตุ | แก้ |
|---|---|---|
| `password authentication failed for user "taladpos"` | API ไปต่อ PostgreSQL ตัวอื่นที่ใช้พอร์ต 5432 | กลับไปขั้น 4.3 |
| `You must install or update .NET` / ไม่พบ framework `Microsoft.AspNetCore.App` เวอร์ชัน `8.0.x` | ไม่มี .NET 8 runtime | กลับไปขั้น 1 และดูกรณีพิเศษบน Windows |
| `address already in use` / `Failed to bind to address http://127.0.0.1:5054` | พอร์ต 5054 ถูกใช้ | กลับไปขั้น 4.3 |
| login ได้ `401` | ข้อมูลในฐานข้อมูลไม่ใช่ชุดตัวอย่าง | กลับไปขั้น 5 |

---

## ขั้น 7 — เปิดเว็บ

**7.1 สร้างไฟล์ตั้งค่า** (สร้างเฉพาะเมื่อยังไม่มี ห้ามเขียนทับ):

```bash
[ -f web/.env.local ] || cp web/.env.local.example web/.env.local
cat web/.env.local
```

ต้องได้ `NEXT_PUBLIC_API_BASE_URL=http://localhost:5054`

**7.2 ติดตั้ง package:**

```bash
npm --prefix web ci
```

ถ้า `npm ci` ล้มเหลว ให้รายงาน error ให้นักเรียนดูก่อน ห้ามเปลี่ยนไปใช้ `npm install` เอง เพราะจะแก้ `package-lock.json` ที่อยู่ใน git

**7.3 เปิด dev server แบบ background:**

```bash
npm --prefix web run dev
```

**7.4 รอจนพร้อม** (หน้าแรกต้อง compile ก่อน อาจใช้เวลา 30–90 วินาที):

```bash
for i in $(seq 1 90); do
  [ "$(curl -s -o /dev/null -w '%{http_code}' http://localhost:3000/login)" = "200" ] && echo WEB_READY && break
  sleep 2
done
```

**7.5 ยืนยันผล** ว่าเสิร์ฟรูปสินค้าได้:

```bash
curl -s -o /dev/null -w 'image: %{http_code}\n' http://localhost:3000/images/products/mango-512.png
```

ผ่านเมื่อได้ `image: 200`

**ถ้าไม่ผ่าน:**

| อาการ | แก้ |
|---|---|
| `/login` ได้ `500` และ output มี `Cannot find module './xxx.js'` | โฟลเดอร์ `.next` เสีย ให้หยุด dev server ลบโฟลเดอร์ `web/.next` แล้วทำ 7.3 ใหม่ |
| dev server ล้มทันทีเพราะ Node.js ใหม่เกินไป | ถามนักเรียนแล้วติดตั้ง Node.js 22 (`winget install --id OpenJS.NodeJS.22 -e` / `brew install node@22 && brew link --overwrite --force node@22`) จากนั้นทำตาม "หลังติดตั้งเสร็จ" ในขั้น 2 |
| `image` ไม่ได้ `200` | ตรวจว่ามีไฟล์ `web/public/images/products/mango-512.png` ถ้าไม่มี แปลว่า clone ไม่ครบ ให้แจ้งนักเรียน |

---

## ขั้น 8 — รายงานนักเรียน

เมื่อผ่านครบทุกขั้น ให้ส่งข้อความนี้ให้นักเรียน (ปรับตามจริงถ้ามีขั้นไหนต่างไป):

> ✅ **ติดตั้งและเปิดระบบ TaladPOS เรียบร้อย**
>
> เปิดเบราว์เซอร์ที่ **http://localhost:3000**
>
> | บัญชี | รหัสผ่าน | เมนูที่ใช้ได้ |
> |---|---|---|
> | `manager` | `Manager123!` | ขายสินค้า, ประวัติการขาย, จัดการสต็อก, โปรโมชั่น, รายงาน |
> | `cashier` | `Cashier123!` | ขายสินค้า, ประวัติการขาย |
>
> ดูรายการ API ได้ที่ http://localhost:5054/swagger
>
> **สำคัญ:** API และเว็บที่ Claude Code เปิดไว้จะหยุดเมื่อปิด Claude Code
> ครั้งหน้าให้สั่ง `อ่าน SETUP.md แล้วเปิดระบบให้` หรือเปิดเองใน terminal 2 หน้าต่าง:
>
> ```bash
> docker compose up -d
> dotnet run --project api/src/TaladPOS.Api
> ```
>
> ```bash
> npm --prefix web run dev
> ```
>
> - หยุดฐานข้อมูล: `docker compose stop` (ข้อมูลยังอยู่)
> - ล้างข้อมูลกลับเป็นชุดตัวอย่าง: `docker compose down -v` แล้ว `docker compose up -d`
> - ปัญหาอื่น ๆ: ดู `docs/student-setup.md`

---

## เปิดระบบรอบถัดไป

ถ้านักเรียนสั่งให้ "เปิดระบบ" และเคยติดตั้งครบแล้ว ให้ทำ **ขั้น 3 → 4 → 5 → 6 → 7 → 8**
โดยข้ามขั้น 1–2 ยกเว้นคำสั่งในขั้นใดล้มเหลวเพราะหาโปรแกรมไม่เจอ
