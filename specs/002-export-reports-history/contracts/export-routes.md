# Contract: Export Route Handler (`web/` internal — ไม่ใช่ endpoint ของ `api/`)

**Base path**: `/api/export` (เส้นทางของ Next.js Route Handler ภายใน `web/` เอง — คนละอย่างกับ
`/api/v1/*` ของ backend `api/` โดยสิ้นเชิง อย่าสับสน)

**เกี่ยวข้องกับ**: FR-001–FR-002 (สต็อกคงเหลือ), FR-003–FR-006 (ประวัติการขาย), FR-008–FR-011 (ทั้งคู่)

**หมายเหตุสถาปัตยกรรม**: endpoint นี้**ไม่ตรวจสอบสิทธิ์ผู้ใช้เอง** และไม่ใช่ endpoint สาธารณะที่ตั้งใจให้
เรียกตรงจากภายนอก — เป็นแค่ตัวแปลง JSON → ไฟล์ .xlsx ล้วน ๆ (ดูเหตุผลใน `research.md` ข้อ 2) หน้าจอที่เรียก
endpoint นี้ต้องดึงข้อมูลที่จะ export ผ่าน `apiFetch()` ไปยัง `api/v1/...` ก่อนเสมอ ซึ่งเป็นจุดที่การตรวจสิทธิ์
จริง (FR-007) เกิดขึ้น

## `POST /api/export/xlsx`

สร้างไฟล์ Excel (.xlsx) จากข้อมูลตารางที่ส่งมา แล้วคืนเป็นไฟล์ให้ดาวน์โหลดทันที

**Auth required**: ไม่ตรวจสอบในชั้นนี้ (ดูหมายเหตุสถาปัตยกรรมด้านบน)

**Request body**:
```json
{
  "filenamePrefix": "taladpos-stock-report",
  "sheetName": "สต็อกคงเหลือ",
  "columns": [
    { "header": "ชื่อสินค้า" },
    { "header": "จำนวนคงเหลือ" },
    { "header": "สถานะ" }
  ],
  "rows": [
    ["มะม่วง", 10, "ปกติ"],
    ["แอปเปิ้ล", 3, "ใกล้หมด"]
  ]
}
```

**Validation**:
- `rows[i].length` ต้องเท่ากับ `columns.length` ทุกแถว — ไม่ตรงกันตอบ `400`
- `rows` ว่าง (`[]`) เป็นค่าที่ถูกต้อง — ต้องสร้างไฟล์ที่มีแค่หัวคอลัมน์สำเร็จ (FR-011) ไม่ใช่ error
- ไม่มีการจำกัดจำนวน `rows` ในชั้นนี้ — เพดาน 10,000 บิลของประวัติการขาย (FR-006) ถูกบังคับใช้ไปแล้ว **ก่อน**
  ถึงขั้นตอนนี้ ฝั่ง client (ดู `data-model.md` ข้อ 4) ไม่ใช่หน้าที่ของ Route Handler นี้

**Response 200 OK**:
- `Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`
- `Content-Disposition: attachment; filename="<filenamePrefix>-<YYYY-MM-DD-HHmm>.xlsx"`
- Body: ไฟล์ .xlsx แบบ binary — sheet ชื่อ `sheetName`, แถวแรกเป็นหัวคอลัมน์ตัวหนา (bold), ความกว้างคอลัมน์
  ปรับตามความยาวเนื้อหาโดยประมาณ (FR-009 บังคับแค่ว่าอ่านภาษาไทยถูก ไม่ได้บังคับ style เกินนี้)

**Response 400 Bad Request**: `rows` กับ `columns` มีจำนวนคอลัมน์ไม่ตรงกัน หรือ field ที่จำเป็นขาดหาย
```json
{ "error": "invalid_export_request" }
```

## ส่วนที่ไม่อยู่ใน endpoint นี้ — อยู่ฝั่งหน้าจอที่เรียกใช้แทน

สองเรื่องนี้เป็นความรับผิดชอบของ **หน้าจอที่เรียก** endpoint นี้ ไม่ใช่ตัว Route Handler:

1. **การตรวจสิทธิ์ (FR-007)** — เกิดขึ้นตอนหน้าจอเรียก `GET /api/v1/reports/stock` หรือ
   `GET /api/v1/sales` ผ่าน `apiFetch()` (ซึ่งจะได้ `401`/`403` จาก `api/` ทันทีถ้าไม่มีสิทธิ์) **ก่อน** ที่
   จะเรียก endpoint นี้ด้วยซ้ำ
2. **เพดาน 10,000 บิลของประวัติการขาย (FR-006)** — ตรวจตอนดึงข้อมูล (`fetchAllSalesForExport()` ใน
   `data-model.md` ข้อ 4) ถ้าเกินเพดาน หน้าจอต้องแสดงข้อความแจ้งผู้ใช้ทันที **ไม่เรียก** endpoint นี้เลย
