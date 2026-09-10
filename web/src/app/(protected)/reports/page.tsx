"use client";

import { useEffect, useState } from "react";
import { TabView, TabPanel } from "primereact/tabview";
import { DataTable } from "primereact/datatable";
import { Column } from "primereact/column";
import { dataTablePT, tabViewPT, tabPanelPT } from "@/styles/primereact-passthrough";
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

// tasks.md T073 (US6): 4 report types in tabs (contracts/reports.md, Manager-only).
export default function ReportsPage() {
  const { staff } = useAuth();
  const today = toDateOnly(new Date());
  const monthStart = toDateOnly(new Date(new Date().getFullYear(), new Date().getMonth(), 1));

  const [dailyReport, setDailyReport] = useState<SalesReport | null>(null);
  const [bestSelling, setBestSelling] = useState<BestSellingProductRow[]>([]);
  const [byStaff, setByStaff] = useState<SalesByStaffRow[]>([]);
  const [stock, setStock] = useState<StockReportRow[]>([]);

  useEffect(() => {
    if (staff?.role !== "Manager") return;
    getSalesReport("daily", today).then(setDailyReport).catch(() => setDailyReport(null));
    getBestSellingProducts(monthStart, today, 10).then(setBestSelling).catch(() => setBestSelling([]));
    getSalesByStaff(monthStart, today).then(setByStaff).catch(() => setByStaff([]));
    getStockReport().then(setStock).catch(() => setStock([]));
  }, [staff, today, monthStart]);

  // FR-029: /reports is a Manager-only screen, same guard shape as /stock and /promotions.
  if (staff?.role !== "Manager") {
    return <ManagerOnly />;
  }

  const range = (
    <p className="mb-4 text-sm text-ink-500">
      ตั้งแต่ <span className="money">{monthStart}</span> ถึง <span className="money">{today}</span>
    </p>
  );

  return (
    <main className="px-5 py-5">
      <h1 className="mb-5 text-xl font-semibold">รายงาน</h1>

      <TabView pt={tabViewPT}>
        <TabPanel header="ยอดขายวันนี้" pt={tabPanelPT}>
          {dailyReport && (
            <div className="grid max-w-3xl grid-cols-1 gap-3 sm:grid-cols-3">
              <Stat label="ยอดขายรวม" value={dailyReport.totalSalesAmount.toFixed(2)} unit="บาท" />
              <Stat label="ส่วนลดรวม" value={dailyReport.totalDiscountAmount.toFixed(2)} unit="บาท" />
              <Stat label="จำนวนบิล" value={String(dailyReport.billCount)} unit="บิล" />
            </div>
          )}
        </TabPanel>

        <TabPanel header="สินค้าขายดี" pt={tabPanelPT}>
          {range}
          <DataTable
            value={bestSelling}
            pt={dataTablePT}
            dataKey="productId"
            emptyMessage="ยังไม่มียอดขายในช่วงนี้"
          >
            <Column field="productName" header="สินค้า" />
            <Column
              header="ขายได้"
              body={(r: BestSellingProductRow) => (
                <span className="money">
                  {r.quantitySold}
                  <span className="ml-1 text-ink-300">ชิ้น</span>
                </span>
              )}
            />
            <Column
              header="ยอดขาย"
              body={(r: BestSellingProductRow) => (
                <span className="money">{r.totalSalesAmount.toFixed(2)}</span>
              )}
            />
          </DataTable>
        </TabPanel>

        <TabPanel header="ยอดขายตามพนักงาน" pt={tabPanelPT}>
          {range}
          <DataTable
            value={byStaff}
            pt={dataTablePT}
            dataKey="staffId"
            emptyMessage="ยังไม่มียอดขายในช่วงนี้"
          >
            <Column field="staffName" header="พนักงาน" />
            <Column
              header="จำนวนบิล"
              body={(r: SalesByStaffRow) => <span className="money">{r.billCount}</span>}
            />
            <Column
              header="ยอดขาย"
              body={(r: SalesByStaffRow) => <span className="money">{r.totalSalesAmount.toFixed(2)}</span>}
            />
          </DataTable>
        </TabPanel>

        <TabPanel header="สต็อกคงเหลือ" pt={tabPanelPT}>
          <DataTable value={stock} pt={dataTablePT} dataKey="productId" emptyMessage="ยังไม่มีสินค้าในร้าน">
            <Column field="productName" header="สินค้า" />
            <Column
              header="คงเหลือ"
              body={(r: StockReportRow) => <span className="money">{r.stockQuantity}</span>}
            />
            <Column
              header="สถานะ"
              body={(r: StockReportRow) =>
                r.isLowStock ? (
                  <span className="rounded-[3px] bg-mango-100 px-1.5 py-0.5 text-xs font-medium text-mango-600">
                    ใกล้หมด
                  </span>
                ) : (
                  <span className="text-ink-300">—</span>
                )
              }
            />
          </DataTable>
        </TabPanel>
      </TabView>
    </main>
  );
}
