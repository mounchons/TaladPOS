# Contract: Authentication

**Base path**: `/api/auth`
**เกี่ยวข้องกับ**: FR-007, FR-009

REST API เท่านั้น — `web/` ต้องเรียกผ่าน endpoint เหล่านี้ ห้ามเข้าถึงตาราง Staff โดยตรง (constitution Principle I)

## `POST /api/auth/login`

ล็อกอินด้วย username/password (FR-007)

**Auth required**: ไม่ต้อง

**Request body**:
```json
{
  "username": "string",
  "password": "string"
}
```

**Response 200 OK**:
```json
{
  "token": "string (JWT)",
  "expiresAt": "2026-09-10T18:00:00Z",
  "staff": {
    "id": "guid",
    "name": "string",
    "role": "Manager | Cashier"
  }
}
```

**Response 401 Unauthorized**: username หรือ password ไม่ถูกต้อง
```json
{ "error": "invalid_credentials" }
```

## ล็อกเอาต์ (FR-009)

ไม่มี endpoint แยก — JWT เป็น stateless token ที่มีอายุจำกัด (`expiresAt`) การ "ล็อกเอาต์" คือ `web/` ทิ้ง token
ที่เก็บไว้ฝั่ง client (เช่นลบออกจาก memory/storage) แล้วนำผู้ใช้กลับไปหน้าล็อกอิน

## การใช้ token กับ endpoint อื่น

ทุก endpoint ที่ต้องล็อกอิน (ระบุไว้ในไฟล์ contract อื่น) ต้องแนบ header:
```
Authorization: Bearer <token>
```
token มี claim `role` (`Manager` หรือ `Cashier`) ให้ API ใช้ตรวจสิทธิ์ตาม FR-029 และ claim `staffId` ที่ API ใช้ระบุ
ผู้ขายในบิล (FR-008) โดยไม่รับ `staffId` จาก request body ของฝั่ง client เพื่อป้องกันการปลอมแปลงผู้ขาย
