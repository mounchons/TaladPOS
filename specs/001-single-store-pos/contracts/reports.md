# Contract: Reports

**Base path**: `/api/reports`
**เกี่ยวข้องกับ**: FR-025–FR-028, FR-029

ทุก endpoint ในไฟล์นี้จำกัดสิทธิ์เฉพาะ **Manager** เท่านั้น (FR-029) — Cashier เรียกแล้วได้ 403 Forbidden

## `GET /api/reports/sales`

รายงานยอดขายรายวัน/รายเดือน (FR-025)

**Query parameters**:
| Param | Type | ความหมาย |
|---|---|---|
| `period` | `daily` \| `monthly` | หน่วยของรายงาน |
| `date` | date (`YYYY-MM-DD`) | วันที่อ้างอิง — `daily` สรุปเฉพาะวันนั้น, `monthly` สรุปทั้งเดือนของวันนั้น |

**Response 200 OK**:
```json
{
  "period": "daily",
  "rangeStart": "2026-09-10",
  "rangeEnd": "2026-09-10",
  "totalSalesAmount": 12500.00,
  "totalDiscountAmount": 800.00,
  "billCount": 42
}
```

## `GET /api/reports/best-selling-products`

รายงานสินค้าขายดี (FR-026)

**Query parameters**: `from`, `to` (ช่วงวันที่, required), `limit` (default 10)

**Response 200 OK**:
```json
[
  { "productId": "guid", "productName": "มะม่วง", "quantitySold": 340, "totalSalesAmount": 15300.00 }
]
```
เรียงจาก `quantitySold` มากไปน้อย

## `GET /api/reports/sales-by-staff`

รายงานยอดขายแยกตามพนักงาน (FR-027)

**Query parameters**: `from`, `to` (ช่วงวันที่, required)

**Response 200 OK**:
```json
[
  { "staffId": "guid", "staffName": "สมหญิง", "billCount": 58, "totalSalesAmount": 22150.00 }
]
```

## `GET /api/reports/stock`

รายงานสต็อกคงเหลือ (FR-028)

**Query parameters**: ไม่มี (คืนสถานะสต็อกล่าสุด ณ เวลาที่เรียก)

**Response 200 OK**:
```json
[
  {
    "productId": "guid",
    "productName": "มะม่วง",
    "stockQuantity": 12,
    "lowStockThreshold": 5,
    "isLowStock": false
  }
]
```
