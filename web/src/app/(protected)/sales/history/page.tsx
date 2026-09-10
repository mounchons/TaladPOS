"use client";

import { useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { DataTable } from "primereact/datatable";
import { Column } from "primereact/column";
import { Calendar } from "primereact/calendar";
import { Checkbox } from "primereact/checkbox";
import { Button } from "primereact/button";
import {
  dataTablePT,
  calendarPT,
  checkboxPT,
  secondaryButtonPT,
} from "@/styles/primereact-passthrough";
import { searchSales, type Sale } from "@/lib/api/sales";
import { MemberSearch } from "@/components/MemberSearch";
import type { Member } from "@/lib/api/members";
import { useAuth } from "@/lib/auth/AuthContext";

function toDateOnly(date: Date): string {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, "0");
  const d = String(date.getDate()).padStart(2, "0");
  return `${y}-${m}-${d}`;
}

// tasks.md T072 (US6): sales history with date/staff/member filters (contracts/sales.md GET /api/sales).
export default function SalesHistoryPage() {
  const { staff } = useAuth();
  const router = useRouter();
  const [from, setFrom] = useState<Date | null>(null);
  const [to, setTo] = useState<Date | null>(null);
  const [onlyMine, setOnlyMine] = useState(false);
  const [member, setMember] = useState<Member | null>(null);
  const [sales, setSales] = useState<Sale[]>([]);
  const [isLoading, setIsLoading] = useState(false);

  const refresh = useCallback(() => {
    setIsLoading(true);
    searchSales({
      from: from ? toDateOnly(from) : undefined,
      to: to ? toDateOnly(to) : undefined,
      staffId: onlyMine ? staff?.id : undefined,
      memberId: member?.id,
    })
      .then(setSales)
      .catch(() => setSales([]))
      .finally(() => setIsLoading(false));
  }, [from, to, onlyMine, member, staff]);

  useEffect(() => {
    refresh();
    // eslint-disable-next-line react-hooks/exhaustive-deps -- only run on mount; refresh() is triggered explicitly below
  }, []);

  return (
    <main className="px-5 py-5">
      <h1 className="mb-5 text-xl font-semibold">ประวัติการขาย</h1>

      <div className="mb-4 rounded-control border border-steel-200 bg-white p-4">
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <div>
            <label className="mb-1.5 block text-sm text-ink-700">จากวันที่</label>
            <Calendar
              value={from}
              onChange={(e) => setFrom((e.value as Date) ?? null)}
              dateFormat="dd/mm/yy"
              pt={calendarPT}
              showButtonBar
            />
          </div>
          <div>
            <label className="mb-1.5 block text-sm text-ink-700">ถึงวันที่</label>
            <Calendar
              value={to}
              onChange={(e) => setTo((e.value as Date) ?? null)}
              dateFormat="dd/mm/yy"
              pt={calendarPT}
              showButtonBar
            />
          </div>
          <div className="sm:col-span-2 lg:col-span-1">
            <label className="mb-1.5 block text-sm text-ink-700">สมาชิก</label>
            <MemberSearch selected={member} onSelect={setMember} />
          </div>
          {/* Bottom-aligned so the checkbox and the button sit on the field
              baseline on one row, and stack sanely once the grid collapses. */}
          <div className="flex flex-wrap items-center gap-4 lg:self-end lg:pb-1">
            <label className="flex items-center gap-2 text-sm text-ink-700">
              <Checkbox
                checked={onlyMine}
                onChange={(e) => setOnlyMine(e.checked ?? false)}
                pt={checkboxPT}
              />
              เฉพาะบิลของฉัน
            </label>
            <Button label="ค้นหา" icon="pi pi-search" onClick={refresh} pt={secondaryButtonPT} />
          </div>
        </div>
      </div>

      <DataTable
        value={sales}
        pt={dataTablePT}
        dataKey="id"
        loading={isLoading}
        responsiveLayout="stack"
        emptyMessage="ไม่พบบิลขายในเงื่อนไขนี้ ลองขยายช่วงวันที่"
      >
        <Column
          header="วันที่"
          body={(s: Sale) => (
            <span className="money text-ink-500">
              {new Date(s.createdAt).toLocaleString("th-TH")}
            </span>
          )}
        />
        <Column header="พนักงาน" body={(s: Sale) => s.staff?.name ?? "—"} />
        <Column header="สมาชิก" body={(s: Sale) => s.member?.name ?? "—"} />
        <Column
          header="รายการ"
          body={(s: Sale) => <span className="money">{s.lineItems.length}</span>}
        />
        <Column
          header="ส่วนลด"
          body={(s: Sale) =>
            s.discountAmount > 0 ? (
              <span className="money text-mango-600">{s.discountAmount.toFixed(2)}</span>
            ) : (
              <span className="text-ink-300">—</span>
            )
          }
        />
        <Column
          header="ยอดรวม"
          body={(s: Sale) => <span className="money font-medium">{s.totalAmount.toFixed(2)}</span>}
        />
        <Column
          header=""
          body={(s: Sale) => (
            <Button
              label="ใบเสร็จ"
              onClick={() => router.push(`/sales/receipt/${s.id}`)}
              pt={secondaryButtonPT}
            />
          )}
        />
      </DataTable>
    </main>
  );
}
