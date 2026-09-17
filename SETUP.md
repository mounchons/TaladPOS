# SETUP.md — คู่มือติดตั้ง TaladPOS สำหรับ Claude Code

> **ไฟล์นี้เขียนให้ Claude Code อ่านและทำตาม** นักเรียน clone repo แล้วเปิด Claude Code ที่ root ของ repo
> จากนั้นสั่งว่า: `อ่าน SETUP.md แล้วติดตั้งและเปิดระบบให้`
>
> คู่มือสำหรับคนอ่านเอง (อธิบายละเอียดกว่า): [docs/student-setup.md](docs/student-setup.md)

---

## เป้าหมาย

ทำให้นักเรียนเปิด **http://localhost:3000** แล้วล็อกอินด้วย `manager` / `Manager123!` ได้
และเห็นสินค้า 20 รายการพร้อมรูป โดยทุกอย่างรันใน Docker บนเครื่องนักเรียนเอง
(ไม่ต้องติดตั้ง .NET หรือ Node.js):

| ส่วน | รันด้วย | พอร์ต |
|---|---|---|
| ฐานข้อมูล PostgreSQL 16 + ข้อมูลตัวอย่าง (`db/init/01-taladpos.sql`) | `docker compose` service `postgres` | 5432 |
| API (.NET 8) — build จาก `api/Dockerfile` | `docker compose` service `api` | 5054 |
| เว็บ (Next.js) — build จาก `web/Dockerfile` | `docker compose` service `web` | 3000 |

---

## กติกาที่ Claude Code ต้องทำตาม

1. **คุยกับนักเรียนเป็นภาษาไทย** บอกทุกครั้งก่อนติดตั้งโปรแกรมว่ากำลังจะติดตั้งอะไรและทำไม
2. **ทำทีละขั้น: ตรวจ → ทำ → ยืนยันผล** ถ้าตรวจแล้วผ่านอยู่แล้วให้ข้ามขั้นนั้น คู่มือนี้ต้องรันซ้ำได้โดยไม่มีผลเสีย
3. **เจอคำว่า ⛔ STOP ให้หยุดรอนักเรียนจริง ๆ** อย่าข้ามไปขั้นถัดไปเอง
4. **ห้ามสร้างหรือแก้ไฟล์ใด ๆ ในโปรเจกต์**
5. **ห้าม** `git commit` / `push` / `pull` / `checkout` / `reset`
6. **ห้ามลบข้อมูลหรือหยุดโปรแกรมอื่นโดยไม่ถามก่อน** ได้แก่ `docker compose down -v`, `docker volume rm`, `docker system prune`, `docker stop` container อื่น, kill process
7. **ห้ามแก้ข้อมูลในฐานข้อมูล** และไม่ต้องสั่ง migration เอง เพราะไฟล์ dump มีตาราง ประวัติ migration และข้อมูลครบแล้ว
8. **ห้ามเปลี่ยนพอร์ต** 5432 / 5054 / 3000 เพราะ URL รูปสินค้าในข้อมูลตัวอย่าง, CORS ของ API และ URL ของ API ที่ฝังอยู่ใน image เว็บตอน build ผูกกับพอร์ตเหล่านี้
9. **คำสั่งที่ใช้เวลานาน** (`winget`, `brew`) ให้ตั้ง timeout 600000 ms
10. **`docker compose build` ให้รันแบบ background** แล้วรอแจ้งเตือนเมื่อจบ (ครั้งแรกอาจนานเกิน 10 นาที) ถ้าถูกตัดกลางคัน ให้รันคำสั่งเดิมซ้ำ Docker จะทำต่อจาก layer ที่เสร็จแล้ว
    ส่วน `docker compose up` ต้องมี `-d` เสมอ ห้ามรันแบบไม่มี `-d` เพราะจะค้างรอไม่จบ
11. **คำสั่งที่ต้องใส่รหัสผ่าน `sudo`** Claude Code พิมพ์รหัสให้ไม่ได้ ให้บอกนักเรียนพิมพ์ `! <คำสั่ง>` ในช่องแชทเอง
12. คำสั่งในคู่มือเขียนแบบ bash ใช้ได้กับ macOS, Linux และ Git Bash บน Windows ส่วนที่ต่างกันระบุ OS ไว้แล้ว ทุกคำสั่งรันจาก root ของ repo

---

## ขั้น 0 — ตรวจตำแหน่งและระบบปฏิบัติการ

**ตรวจ:** ต้องอยู่ที่ root ของ repo คือมีไฟล์เหล่านี้ครบ

```bash
ls docker-compose.yml db/init/01-taladpos.sql api/Dockerfile web/Dockerfile web/package.json
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

ไม่ต้องมี .NET หรือ Node.js บนเครื่อง เพราะ API และเว็บ build และรันใน Docker

ถ้าทุกข้อผ่าน → ข้ามไป **ขั้น 3**

---

## ขั้น 2 — ติดตั้งโปรแกรมที่ขาด

ถามนักเรียนก่อนติดตั้ง และบอกล่วงหน้าว่าอาจมีหน้าต่างขอสิทธิ์ผู้ดูแลระบบ (Windows UAC / รหัสผ่าน macOS) ให้กดยืนยันเอง
ติดตั้ง**เฉพาะตัวที่ขาด**

### Windows (winget)

```bash
winget install --id Git.Git -e --accept-package-agreements --accept-source-agreements
winget install --id Docker.DockerDesktop -e --accept-package-agreements --accept-source-agreements
```

### macOS (Homebrew)

```bash
brew install --cask docker-desktop
```

(macOS มี Git มากับ Xcode Command Line Tools ถ้า `git --version` ขอให้ติดตั้ง ให้นักเรียนกดติดตั้งตามหน้าต่างที่ขึ้นมา)

### Linux

ติดตั้งผ่าน package manager ของ distro ต้องใช้ `sudo` ให้นักเรียนพิมพ์คำสั่งเองด้วย `! ...`
ตามคู่มือทางการ (เลือกหัวข้อให้ตรงกับ distro จากขั้น 0):

- Git: `! sudo apt install git` (Debian/Ubuntu) หรือคำสั่งเทียบเท่าของ distro
- Docker Engine + compose plugin: https://docs.docker.com/engine/install/

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

**4.1 ตรวจว่าระบบของโปรเจกต์นี้รันอยู่แล้วหรือยัง:**

```bash
docker compose ps --format '{{.Service}}\t{{.Status}}\t{{.Ports}}'
```

service ไหนสถานะ `Up` → พอร์ตของ service นั้นเป็นของโปรเจกต์นี้ ไม่ต้องตรวจพอร์ตนั้น
(`postgres` = 5432, `api` = 5054, `web` = 3000)

**4.2 ดูว่าพอร์ตไหนถูกใช้อยู่:**

```bash
# Windows (Git Bash)
netstat -ano | grep -E ":(5432|5054|3000) .*LISTENING"

# macOS / Linux
lsof -nP -iTCP -sTCP:LISTEN | grep -E ":(5432|5054|3000) "
```

ไม่มีผลลัพธ์ หรือมีเฉพาะพอร์ตที่ 4.1 บอกว่าเป็นของโปรเจกต์นี้ → ไป **ขั้น 5**

**4.3 ถ้ามีพอร์ตถูกใช้** ให้ตรวจทีละพอร์ตว่าเป็นของอะไร

**พอร์ต 5432** ดูว่า container ไหนใช้อยู่:

```bash
docker ps --format '{{.Names}}\t{{.Ports}}' | grep ':5432->'
```

- ไม่ใช่ container ของโปรเจกต์นี้ → ถามนักเรียนว่าหยุด container นั้นได้ไหม ถ้าได้ให้รัน `docker stop <ชื่อ container>`
- ไม่มี container ไหนใช้ แต่พอร์ตยังถูกใช้ → เป็น PostgreSQL ที่ติดตั้งในเครื่อง ให้นักเรียนหยุด service เอง
- นักเรียนไม่อนุญาตให้หยุด ⛔ **STOP** (ระบบรันไม่ได้ถ้าพอร์ตนี้ไม่ว่าง)

**พอร์ต 5054 / 3000** ตรวจก่อนว่าเป็น container หรือไม่:

```bash
docker ps --format '{{.Names}}\t{{.Ports}}' | grep -E ':(5054|3000)->'
```

- มี container → ถามนักเรียนว่าหยุดได้ไหม ถ้าได้ให้รัน `docker stop <ชื่อ container>`
  (อาจเป็นระบบนี้ที่ clone ไว้อีกโฟลเดอร์ก็ได้) **ห้าม kill PID** ของพอร์ตนี้ เพราะ PID เป็นของ Docker เอง
  บน Windows ถ้า PID ในข้อ 4.2 เป็นของ `com.docker.backend` หรือ `wslrelay` แปลว่าเป็น container เสมอ

ไม่มี container → ตรวจว่าเป็น API / เว็บของโปรเจกต์นี้ที่เคยเปิดด้วย `dotnet run` / `npm run dev` ค้างไว้หรือไม่:

```bash
curl -s http://localhost:5054/swagger/v1/swagger.json | grep -q "TaladPOS API" && echo API_OURS
curl -s -o /dev/null -w '%{http_code}\n' http://localhost:3000/images/products/mango-512.png
```

(บรรทัดที่สองได้ `200` = เว็บของโปรเจกต์นี้)

- เป็นของโปรเจกต์นี้ที่เปิดนอก Docker → ต้องปิดก่อน ไม่อย่างนั้น Docker จะเปิดพอร์ตเดียวกันไม่ได้
  ถามนักเรียนก่อน ถ้าอนุญาตให้หา PID จากผลลัพธ์ข้อ 4.2 (คอลัมน์สุดท้ายบน Windows / คอลัมน์ที่สองบน macOS, Linux) แล้วปิด:

  ```bash
  # Windows (Git Bash)
  taskkill //PID <PID> //F

  # macOS / Linux
  kill <PID>
  ```

- เป็นโปรแกรมอื่น → ถามนักเรียนก่อนปิดโปรแกรมนั้น ⛔ **STOP** จนกว่าพอร์ตจะว่าง

---

## ขั้น 5 — Build และเปิดระบบ

**5.1 Build image ของ API และเว็บ** — รันแบบ **background** (กติกาข้อ 10) แล้วรอแจ้งเตือนเมื่อจบ:

```bash
docker compose build && echo BUILD_OK
```

ครั้งแรกต้องดาวน์โหลด image ของ .NET SDK, Node.js และ PostgreSQL รวมกว่า 1 GB แล้วติดตั้ง package และ build โค้ด
อาจใช้เวลา 5–20 นาทีตามความเร็วอินเทอร์เน็ต ให้บอกนักเรียนล่วงหน้า
ถ้า build ครบแล้วแต่โค้ดเปลี่ยน จะใช้ cache และ build ใหม่เฉพาะส่วนที่เปลี่ยน (ต้องต่ออินเทอร์เน็ต)

ผ่านเมื่อบรรทัดสุดท้ายเป็น `BUILD_OK`

| ข้อความใน output | สาเหตุ | แก้ |
|---|---|---|
| `ETIMEDOUT`, `Unable to load the service index`, `failed to resolve source metadata`, `TLS handshake timeout` | อินเทอร์เน็ตหลุดหรือช้า | รันคำสั่งเดิมซ้ำ |
| `no space left on device` | พื้นที่ของ Docker เต็ม | แจ้งนักเรียนให้เพิ่มพื้นที่ดิสก์ **ห้าม** `docker system prune` เองโดยไม่ถาม ⛔ **STOP** |
| error อื่นในขั้น `npm ci` หรือ `dotnet restore` / `dotnet publish` | โค้ดหรือไฟล์ใน repo ไม่ครบ | รายงาน error ให้นักเรียนดู ห้ามแก้ไฟล์เอง ⛔ **STOP** |

**5.2 เปิดระบบ:**

```bash
docker compose up -d
```

คำสั่งนี้จะรอจน PostgreSQL พร้อม (healthcheck) แล้วจึงเปิด API และเว็บให้เอง
ถ้าเป็นครั้งแรก PostgreSQL จะโหลดข้อมูลตัวอย่างก่อน

**5.3 ยืนยันผล:**

```bash
docker compose ps --format '{{.Service}}\t{{.Status}}'
```

ต้องได้ `postgres` สถานะ `Up ... (healthy)` และ `api`, `web` สถานะ `Up`

| อาการ | สาเหตุ | แก้ |
|---|---|---|
| `port is already allocated` ตอน `up` | มีโปรแกรมอื่นใช้พอร์ต | กลับไปขั้น 4.3 |
| `dependency failed to start: container ... is unhealthy` | PostgreSQL เปิดไม่ขึ้น | ดู `docker compose logs --tail 50 postgres` แล้วรายงานนักเรียน |
| `api` หรือ `web` ไม่อยู่ในรายการ หรือสถานะ `Exited` | container ล้มหลังเปิด | ดู `docker compose logs --tail 50 api` (หรือ `web`) แล้วเทียบกับตารางในขั้น 6 |

---

## ขั้น 6 — ตรวจฐานข้อมูลและ API

**6.1 ตรวจข้อมูลตัวอย่าง:**

```bash
docker compose exec -T postgres psql -U taladpos -d taladpos -Atc "SELECT (SELECT count(*) FROM products) || ' products, ' || (SELECT count(*) FROM sales) || ' sales'"
```

| ผลลัพธ์ | ความหมาย | ทำอะไรต่อ |
|---|---|---|
| `20 products, 96 sales` | โหลดข้อมูลตัวอย่างครบ | ไป **6.2** |
| `20 products` แต่ sales มากกว่า 96 | นักเรียนเคยใช้ระบบและขายของไปแล้ว | ปกติ ไป **6.2** |
| `relation "products" does not exist` หรือจำนวนสินค้าไม่ใช่ 20 | volume มีข้อมูลเก่าจากก่อนมีไฟล์ dump PostgreSQL จึงไม่โหลดไฟล์ dump | อธิบายให้นักเรียนฟัง แล้ว**ถามก่อน**ว่าลบข้อมูลเดิมได้ไหม ถ้าได้ให้รัน `docker compose down -v` ตามด้วย `docker compose up -d` แล้วทำขั้นนี้ใหม่ |

ดู log เพิ่มได้ที่ `docker compose logs postgres` ถ้าโหลดครั้งแรกจะมีบรรทัด `running /docker-entrypoint-initdb.d/01-taladpos.sql`

**6.2 รอจน API พร้อม:**

```bash
for i in $(seq 1 60); do
  [ "$(curl -s -o /dev/null -w '%{http_code}' http://localhost:5054/swagger/index.html)" = "200" ] && echo API_READY && break
  sleep 2
done
```

**6.3 ยืนยันผล** ว่าล็อกอินได้และอ่านสินค้าจากฐานข้อมูลได้:

```bash
TOKEN=$(curl -s -X POST http://localhost:5054/api/v1/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"username":"manager","password":"Manager123!"}' | sed -E 's/.*"token":"([^"]+)".*/\1/')
echo "token length: ${#TOKEN}"
curl -s -o /dev/null -w 'products: %{http_code}\n' -H "Authorization: Bearer $TOKEN" http://localhost:5054/api/v1/products
```

ผ่านเมื่อ token ยาวหลายร้อยตัวอักษร และ `products: 200`

**ถ้าไม่ผ่าน** ให้อ่าน log ของ API แล้วเทียบกับตารางนี้:

```bash
docker compose logs --tail 50 api
```

| ข้อความใน log / อาการ | สาเหตุ | แก้ |
|---|---|---|
| ไม่ขึ้น `API_READY` และ `docker compose ps` ไม่มี `api` สถานะ `Up` | API container ล้ม | อ่าน error ใน log แล้วรายงานนักเรียน |
| `Failed to determine the https port for redirect` | ปกติ (container ใช้แค่ HTTP) | ไม่ต้องแก้ |
| login ได้ `401` | ข้อมูลในฐานข้อมูลไม่ใช่ชุดตัวอย่าง | กลับไปขั้น 6.1 |

---

## ขั้น 7 — ตรวจเว็บ

**7.1 รอจนพร้อม:**

```bash
for i in $(seq 1 30); do
  [ "$(curl -s -o /dev/null -w '%{http_code}' http://localhost:3000/login)" = "200" ] && echo WEB_READY && break
  sleep 2
done
```

**7.2 ยืนยันผล** ว่าเสิร์ฟรูปสินค้าได้:

```bash
curl -s -o /dev/null -w 'image: %{http_code}\n' http://localhost:3000/images/products/mango-512.png
```

ผ่านเมื่อได้ `image: 200`

**ถ้าไม่ผ่าน:**

| อาการ | แก้ |
|---|---|
| ไม่ขึ้น `WEB_READY` | ดู `docker compose logs --tail 50 web` แล้วรายงานนักเรียน |
| `image` ไม่ได้ `200` | ตรวจว่ามีไฟล์ `web/public/images/products/mango-512.png` ถ้าไม่มี แปลว่า clone ไม่ครบ ให้แจ้งนักเรียน (รูปถูกคัดลอกเข้า image ตอน build หลังได้ไฟล์ครบแล้วต้อง `docker compose up -d --build`) |

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
> ระบบทั้งหมดรันอยู่ใน Docker **ปิด Claude Code ได้เลย ระบบยังทำงานต่อ** จนกว่าจะหยุดเอง ปิด Docker Desktop หรือปิดเครื่อง
> ครั้งหน้าให้เปิด Docker Desktop แล้วสั่ง `อ่าน SETUP.md แล้วเปิดระบบให้` หรือรันเองที่โฟลเดอร์โปรเจกต์:
>
> ```bash
> docker compose up -d
> ```
>
> - หยุดระบบ: `docker compose stop` (ข้อมูลยังอยู่)
> - แก้โค้ดหรือ `git pull` แล้ว ให้ build ใหม่: `docker compose up -d --build`
> - ล้างข้อมูลกลับเป็นชุดตัวอย่าง: `docker compose down -v` แล้ว `docker compose up -d`
> - ปัญหาอื่น ๆ: ดู `docs/student-setup.md`

---

## เปิดระบบรอบถัดไป

ถ้านักเรียนสั่งให้ "เปิดระบบ" และเคยติดตั้งครบแล้ว ให้ทำ **ขั้น 3 → 4 → 5.2 → 5.3 → 6 → 7 → 8**
โดยข้ามขั้น 1–2 ยกเว้นคำสั่งในขั้นใดล้มเหลวเพราะหาโปรแกรมไม่เจอ

ข้ามขั้น 5.1 ได้ เพราะ image ที่ build ไว้ยังอยู่ (และ build ต้องต่ออินเทอร์เน็ตทุกครั้ง แม้โค้ดไม่เปลี่ยน)
ให้ทำ 5.1 ก่อน 5.2 เฉพาะเมื่อนักเรียนบอกว่าแก้โค้ดหรือได้โค้ดใหม่มา
หรือเมื่อ 5.3 ไม่มี `api` / `web` ในรายการ
