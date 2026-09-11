"use client";

import { useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { DataTable, type DataTableColumn } from "@/components/DataTable";
import { fetchAllSalesForExport, searchSalesPaged, SALES_EXPORT_ROW_CAP, type Sale } from "@/lib/api/sales";
import { MemberSearch } from "@/components/MemberSearch";
import type { Member } from "@/lib/api/members";
import { useAuth } from "@/lib/auth/AuthContext";
import { ExportButton } from "@/components/ExportButton";
import { triggerXlsxExport } from "@/lib/export/xlsxClient";
import { toExportRows, type ExportColumn } from "@/lib/export/types";

const PAGE_SIZE = 20;

// tasks.md T072 (US6): sales history with date/staff/member filters (contracts/sales.md GET /api/v1/sales).
export default function SalesHistoryPage() {
  const { staff } = useAuth();
  const router = useRouter();
  // Dates are held as the ISO strings <input type="date"> speaks natively,
  // which is also what the API wants - no Date round trip in between.
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const [onlyMine, setOnlyMine] = useState(false);
  const [member, setMember] = useState<Member | null>(null);
  const [page, setPage] = useState(1);
  const [sales, setSales] = useState<Sale[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [isLoading, setIsLoading] = useState(true);

  // Sales history is the fastest-growing table in the shop, so this is real
  // server paging: the API returns one page plus the filtered count, ordered
  // newest first with a stable tiebreak so no bill appears twice across pages.
  const refresh = useCallback(() => {
    setIsLoading(true);
    searchSalesPaged({
      from: from || undefined,
      to: to || undefined,
      staffId: onlyMine ? staff?.id : undefined,
      memberId: member?.id,
      page,
      pageSize: PAGE_SIZE,
    })
      .then((result) => {
        setSales(result.items);
        setTotalCount(result.totalCount);
      })
      .catch(() => {
        setSales([]);
        setTotalCount(0);
      })
      .finally(() => setIsLoading(false));
  }, [from, to, onlyMine, member, staff, page]);

  useEffect(() => {
    refresh();
  }, [refresh]);

  const columns: DataTableColumn<Sale>[] = [
    {
      header: "วันที่",
      cell: (s) => (
        <span className="money whitespace-nowrap text-ink-500">
          {new Date(s.createdAt).toLocaleString("th-TH")}
        </span>
      ),
    },
    { header: "พนักงาน", cell: (s) => s.staff?.name ?? "—" },
    { header: "สมาชิก", cell: (s) => s.member?.name ?? "—" },
    {
      header: "รายการ",
      className: "text-right",
      cell: (s) => <span className="money">{s.lineItems.length}</span>,
    },
    {
      header: "ส่วนลด",
      className: "text-right",
      cell: (s) =>
        s.discountAmount > 0 ? (
          <span className="money text-mango-600">{s.discountAmount.toFixed(2)}</span>
        ) : (
          <span className="text-ink-300">—</span>
        ),
    },
    {
      header: "ยอดรวม",
      className: "text-right",
      cell: (s) => <span className="money font-medium">{s.totalAmount.toFixed(2)}</span>,
    },
    {
      header: "",
      className: "text-right",
      cell: (s) => (
        <button
          type="button"
          onClick={() => router.push(`/sales/receipt/${s.id}`)}
          className="btn btn-sm rounded-control border-steel-200 bg-white font-display font-medium text-ink-700 hover:border-ink hover:bg-white"
        >
          ใบเสร็จ
        </button>
      ),
    },
  ];

  // data-model.md §3 - same 6 columns as `columns` above, minus the receipt
  // button (no meaning in a spreadsheet), as raw cell values instead of nodes.
  const salesExportColumns: ExportColumn<Sale>[] = [
    { header: "วันที่", value: (s) => new Date(s.createdAt).toLocaleString("th-TH") },
    { header: "พนักงาน", value: (s) => s.staff?.name ?? "-" },
    { header: "สมาชิก", value: (s) => s.member?.name ?? "-" },
    { header: "จำนวนรายการ", value: (s) => s.lineItems.length },
    { header: "ส่วนลด", value: (s) => s.discountAmount },
    { header: "ยอดรวม", value: (s) => s.totalAmount },
  ];

  // tasks.md T009 (US2, FR-003-FR-006) - pulls every bill matching the
  // filters on screen (not just the current page), same filters as `refresh`
  // above. Throwing here (instead of calling triggerXlsxExport) is how the
  // cap-exceeded message reaches <ExportButton>'s error slot.
  async function exportSalesHistory() {
    const result = await fetchAllSalesForExport({
      from: from || undefined,
      to: to || undefined,
      staffId: onlyMine ? staff?.id : undefined,
      memberId: member?.id,
    });

    if (result.status === "cap_exceeded") {
      throw new Error(
        `ตัวกรองนี้ตรงกับ ${result.totalCount.toLocaleString("th-TH")} บิล เกิน ` +
          `${SALES_EXPORT_ROW_CAP.toLocaleString("th-TH")} บิล กรุณาแคบช่วงวันที่หรือตัวกรองลงก่อน`,
      );
    }

    await triggerXlsxExport({
      filenamePrefix: "taladpos-sales-history",
      sheetName: "ประวัติการขาย",
      ...toExportRows(result.sales, salesExportColumns),
    });
  }

  const fieldClass = "input w-full rounded-control border-steel-200 bg-white";

  return (
    <main className="px-5 py-5">
      <div className="mb-5 flex items-center justify-between gap-3">
        <h1 className="text-xl font-semibold">ประวัติการขาย</h1>
        <ExportButton onExport={exportSalesHistory} />
      </div>

      <DataTable
        columns={columns}
        rows={sales}
        rowKey={(s) => s.id}
        page={page}
        pageSize={PAGE_SIZE}
        totalCount={totalCount}
        onPageChange={setPage}
        // Same mechanism as /stock (T111): narrowing the range while on a later
        // page sends the table back to page 1 instead of showing nothing.
        filterKey={`${from}|${to}|${onlyMine}|${member?.id ?? ""}`}
        isLoading={isLoading}
        emptyText="ไม่พบบิลขายในเงื่อนไขนี้ ลองขยายช่วงวันที่"
      >
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <div>
            <label className="mb-1.5 block text-sm text-ink-700" htmlFor="history-from">
              จากวันที่
            </label>
            <input
              id="history-from"
              type="date"
              value={from}
              onChange={(e) => setFrom(e.target.value)}
              className={`${fieldClass} money`}
            />
          </div>
          <div>
            <label className="mb-1.5 block text-sm text-ink-700" htmlFor="history-to">
              ถึงวันที่
            </label>
            <input
              id="history-to"
              type="date"
              value={to}
              // The API answers 400 on an impossible range; min stops it here.
              min={from || undefined}
              onChange={(e) => setTo(e.target.value)}
              className={`${fieldClass} money`}
            />
          </div>
          <div className="sm:col-span-2 lg:col-span-1">
            <label className="mb-1.5 block text-sm text-ink-700">สมาชิก</label>
            <MemberSearch selected={member} onSelect={setMember} />
          </div>
          {/* Bottom-aligned so the checkbox sits on the field baseline on one
              row, and stacks sanely once the grid collapses. No search button:
              changing a filter refetches, which is one less thing to press at
              a counter. */}
          <div className="flex flex-wrap items-center gap-4 lg:self-end lg:pb-1">
            <label className="flex cursor-pointer items-center gap-2 text-sm text-ink-700">
              <input
                type="checkbox"
                checked={onlyMine}
                onChange={(e) => setOnlyMine(e.target.checked)}
                className="checkbox checkbox-sm"
              />
              เฉพาะบิลของฉัน
            </label>
          </div>
        </div>
      </DataTable>
    </main>
  );
}
