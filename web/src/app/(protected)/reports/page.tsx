"use client";

import { useEffect, useState } from "react";
import { DataTable, type DataTableColumn } from "@/components/DataTable";
import {
  getSalesReport,
  getBestSellingProducts,
  getSalesByStaff,
  getStockReport,
  type SalesReport,
  type BestSellingProductRow,
  type SalesByStaffRow,
  type StockReportRow,
} from "@/lib/api/reports";
import { ManagerOnly } from "@/components/ManagerOnly";
import { useAuth } from "@/lib/auth/AuthContext";

function toDateOnly(date: Date): string {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, "0");
  const d = String(date.getDate()).padStart(2, "0");
  return `${y}-${m}-${d}`;
}

function Stat({ label, value, unit }: { label: string; value: string; unit: string }) {
  return (
    <div className="rounded-control border border-steel-200 bg-white px-5 py-4">
      <p className="text-sm text-ink-500">{label}</p>
      <p className="money mt-1 text-3xl font-semibold leading-none text-ink">
        {value}
        <span className="ml-1.5 text-sm font-normal text-ink-300">{unit}</span>
      </p>
    </div>
  );
}

const TABS = ["ยอดขายวันนี้", "สินค้าขายดี", "ยอดขายตามพนักงาน", "สต็อกคงเหลือ"] as const;

// tasks.md T073 (US6): 4 report types in tabs (contracts/reports.md, Manager-only).
export default function ReportsPage() {
  const { staff } = useAuth();
  const today = toDateOnly(new Date());
  const monthStart = toDateOnly(new Date(new Date().getFullYear(), new Date().getMonth(), 1));

  const [activeTab, setActiveTab] = useState(0);
  // The range used to be hard-coded to this month. It is a filter now, so a
  // manager can ask about last week without waiting for the month to roll over.
  const [from, setFrom] = useState(monthStart);
  const [to, setTo] = useState(today);

  const [dailyReport, setDailyReport] = useState<SalesReport | null>(null);
  const [bestSelling, setBestSelling] = useState<BestSellingProductRow[]>([]);
  const [byStaff, setByStaff] = useState<SalesByStaffRow[]>([]);
  const [stock, setStock] = useState<StockReportRow[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    if (staff?.role !== "Manager") return;
    setIsLoading(true);
    // The report endpoints return bounded aggregates (top-N, one row per
    // member of staff), so they are deliberately not paged - research.md #11.
    Promise.allSettled([
      getSalesReport("daily", today).then(setDailyReport),
      getBestSellingProducts(from, to, 10).then(setBestSelling),
      getSalesByStaff(from, to).then(setByStaff),
      getStockReport().then(setStock),
    ]).finally(() => setIsLoading(false));
  }, [staff, today, from, to]);

  // FR-029: /reports is a Manager-only screen, same guard shape as /stock and /promotions.
  if (staff?.role !== "Manager") {
    return <ManagerOnly />;
  }

  const bestSellingColumns: DataTableColumn<BestSellingProductRow>[] = [
    { header: "สินค้า", cell: (r) => r.productName },
    {
      header: "ขายได้",
      className: "text-right",
      cell: (r) => (
        <span className="money whitespace-nowrap">
          {r.quantitySold}
          <span className="ml-1 text-ink-300">ชิ้น</span>
        </span>
      ),
    },
    {
      header: "ยอดขาย",
      className: "text-right",
      cell: (r) => <span className="money">{r.totalSalesAmount.toFixed(2)}</span>,
    },
  ];

  const byStaffColumns: DataTableColumn<SalesByStaffRow>[] = [
    { header: "พนักงาน", cell: (r) => r.staffName },
    {
      header: "จำนวนบิล",
      className: "text-right",
      cell: (r) => <span className="money">{r.billCount}</span>,
    },
    {
      header: "ยอดขาย",
      className: "text-right",
      cell: (r) => <span className="money">{r.totalSalesAmount.toFixed(2)}</span>,
    },
  ];

  const stockColumns: DataTableColumn<StockReportRow>[] = [
    { header: "สินค้า", cell: (r) => r.productName },
    {
      header: "คงเหลือ",
      className: "text-right",
      cell: (r) => <span className="money">{r.stockQuantity}</span>,
    },
    {
      header: "สถานะ",
      cell: (r) =>
        r.isLowStock ? (
          <span className="rounded-[3px] bg-mango-100 px-1.5 py-0.5 text-xs font-medium text-mango-600">
            ใกล้หมด
          </span>
        ) : (
          <span className="text-ink-300">—</span>
        ),
    },
  ];

  const dateField = "input money w-full rounded-control border-steel-200 bg-white";

  // Shared by the three range-driven tabs; the stock tab is a snapshot and has
  // no range to pick.
  const rangeFilter = (
    <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 sm:max-w-md">
      <div>
        <label className="mb-1.5 block text-sm text-ink-700" htmlFor="report-from">
          ตั้งแต่วันที่
        </label>
        <input
          id="report-from"
          type="date"
          value={from}
          onChange={(e) => setFrom(e.target.value)}
          className={dateField}
        />
      </div>
      <div>
        <label className="mb-1.5 block text-sm text-ink-700" htmlFor="report-to">
          ถึงวันที่
        </label>
        <input
          id="report-to"
          type="date"
          value={to}
          min={from || undefined}
          onChange={(e) => setTo(e.target.value)}
          className={dateField}
        />
      </div>
    </div>
  );

  return (
    <main className="px-5 py-5">
      <h1 className="mb-5 text-xl font-semibold">รายงาน</h1>

      {/* role="tablist" by hand: daisyUI's `tabs` is styling only, and these
          are buttons rather than radio inputs so the panel below can be one
          conditional render instead of four always-mounted ones. */}
      <div role="tablist" className="tabs tabs-border overflow-x-auto">
        {TABS.map((label, i) => (
          <button
            key={label}
            role="tab"
            aria-selected={activeTab === i}
            onClick={() => setActiveTab(i)}
            className={`tab whitespace-nowrap ${activeTab === i ? "tab-active font-medium text-ink" : "text-ink-500"}`}
          >
            {label}
          </button>
        ))}
      </div>

      <div className="pt-4">
        {activeTab === 0 && dailyReport && (
          <div className="grid max-w-3xl grid-cols-1 gap-3 sm:grid-cols-3">
            <Stat label="ยอดขายรวม" value={dailyReport.totalSalesAmount.toFixed(2)} unit="บาท" />
            <Stat label="ส่วนลดรวม" value={dailyReport.totalDiscountAmount.toFixed(2)} unit="บาท" />
            <Stat label="จำนวนบิล" value={String(dailyReport.billCount)} unit="บิล" />
          </div>
        )}

        {activeTab === 1 && (
          <DataTable
            columns={bestSellingColumns}
            rows={bestSelling}
            rowKey={(r) => r.productId}
            isLoading={isLoading}
            emptyText="ยังไม่มียอดขายในช่วงนี้"
          >
            {rangeFilter}
          </DataTable>
        )}

        {activeTab === 2 && (
          <DataTable
            columns={byStaffColumns}
            rows={byStaff}
            rowKey={(r) => r.staffId}
            isLoading={isLoading}
            emptyText="ยังไม่มียอดขายในช่วงนี้"
          >
            {rangeFilter}
          </DataTable>
        )}

        {activeTab === 3 && (
          <DataTable
            columns={stockColumns}
            rows={stock}
            rowKey={(r) => r.productId}
            isLoading={isLoading}
            emptyText="ยังไม่มีสินค้าในร้าน"
          />
        )}
      </div>
    </main>
  );
}
