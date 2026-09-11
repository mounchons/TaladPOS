# Phase 1 Data Model: Export รายงานสต็อกคงเหลือและประวัติการขาย

ฟีเจอร์นี้ไม่มี entity ใหม่ระดับฐานข้อมูล (ไม่มีตาราง/คอลัมน์ใหม่ใน PostgreSQL — ดู
[`research.md`](./research.md) ข้อ 2) ข้อมูลทั้งหมดมาจาก entity ที่มีอยู่แล้ว (`Product`, `Sale`,
`SaleLineItem` ผ่าน `StockReportRow`/`SaleDto` ที่ `api/` คืนมาอยู่แล้ว — ดู
`docs/logic/architecture.md`) สิ่งที่ต้องนิยามใหม่คือ **shape ของข้อมูลระหว่างทาง** ในฝั่ง `web/` เท่านั้น
(TypeScript types)

## 1. `ExportColumn<T>` — นิยามคอลัมน์ (ใช้ร่วมกันทั้งสอง export)

```ts
interface ExportColumn<T> {
  header: string;           // หัวคอลัมน์ภาษาไทย ต้องตรงกับ label บนหน้าจอ (FR-002, FR-005)
  value: (row: T) => string | number; // ค่าที่จะใส่ใน cell ของแถวนั้น
}
```

ตั้งใจให้หน้าตาใกล้เคียง `DataTableColumn<T>` ที่มีอยู่แล้วใน `components/DataTable.tsx` (มี `header` +
ฟังก์ชันดึงค่าต่อแถวเหมือนกัน) เพื่อให้แต่ละหน้าจอนิยาม export columns ควบคู่กับ `DataTableColumn` เดิมได้
ง่าย ไม่ต้องคิดโครงสร้างใหม่ — ต่างกันแค่ `DataTableColumn.cell` คืน `ReactNode` (render บนจอ) ส่วน
`ExportColumn.value` คืน `string | number` ดิบ (ใส่ใน cell ของ Excel)

## 2. คอลัมน์ของรายงานสต็อกคงเหลือ (อ้างอิง `StockReportRow` ที่มีอยู่แล้วใน `lib/api/reports.ts`)

| header (ภาษาไทย) | ที่มา |
|---|---|
| ชื่อสินค้า | `row.productName` |
| จำนวนคงเหลือ | `row.stockQuantity` |
| สถานะ | `row.isLowStock ? "ใกล้หมด" : "ปกติ"` |

ตรงกับ 3 คอลัมน์ที่แสดงบนแท็บ "สต็อกคงเหลือ" ของหน้ารายงานทุกประการ (FR-002) — ไม่ export
`lowStockThreshold` แม้จะมีอยู่ใน `StockReportRow` เพราะไม่ได้แสดงบนหน้าจอ

## 3. คอลัมน์ของประวัติการขาย (อ้างอิง `Sale` ที่มีอยู่แล้วใน `lib/api/sales.ts`)

| header (ภาษาไทย) | ที่มา |
|---|---|
| วันที่ | `new Date(sale.createdAt).toLocaleString("th-TH")` |
| พนักงาน | `sale.staff?.name ?? "-"` |
| สมาชิก | `sale.member?.name ?? "-"` |
| จำนวนรายการ | `sale.lineItems.length` |
| ส่วนลด | `sale.discountAmount` |
| ยอดรวม | `sale.totalAmount` |

ตรงกับ 6 คอลัมน์ที่แสดงในตารางประวัติการขายทุกประการ (FR-005) — ไม่รวมคอลัมน์ปุ่ม "ใบเสร็จ" (ไม่มี
ความหมายในไฟล์)

## 4. ฟังก์ชันดึงข้อมูลประวัติการขายทั้งหมดตามตัวกรอง (ใหม่ ใน `lib/api/sales.ts`)

```ts
const SALES_EXPORT_ROW_CAP = 10_000; // FR-006, ยืนยันใน Clarifications รอบที่ 2

type FetchAllSalesResult =
  | { status: "ok"; sales: Sale[] }
  | { status: "cap_exceeded"; totalCount: number };

function fetchAllSalesForExport(filters: {
  from?: string;
  to?: string;
  staffId?: string;
  memberId?: string;
}): Promise<FetchAllSalesResult>;
```

พฤติกรรม:
1. เรียกหน้าแรกด้วย `pageSize=100` (ค่าสูงสุดตาม `contracts/sales.md` ของ `001-single-store-pos`)
2. ถ้า response แรกมี `totalCount > 10_000` → คืน `{ status: "cap_exceeded", totalCount }` ทันที **ไม่** ยิง
   หน้าถัดไปต่อ (`research.md` ข้อ 3)
3. ถ้าไม่เกิน → วนเรียกหน้าถัดไปจนครบ `totalPages` แล้วคืน `{ status: "ok", sales: [...ทุกแถว] }`

## 5. Request body ของ Route Handler `POST /api/export/xlsx`

```ts
interface XlsxExportRequest {
  filenamePrefix: string;     // เช่น "taladpos-stock-report" — Route Handler ต่อ timestamp ให้เอง (FR-010)
  sheetName: string;          // ชื่อ sheet ใน Excel เช่น "สต็อกคงเหลือ"
  columns: { header: string }[];        // หัวคอลัมน์ตามลำดับ
  rows: (string | number)[][];          // ค่าตามลำดับคอลัมน์ ต่อแถว
}
```

`columns`/`rows` แยกจากกันโดยตั้งใจ (ไม่ส่งเป็น array ของ object) เพื่อให้ payload กะทัดรัดที่สุดตอนมี
10,000 แถว — ไม่ต้องส่งชื่อ key ซ้ำทุกแถว

รายละเอียด response/error ดู [`contracts/export-routes.md`](./contracts/export-routes.md)
