"use client";

import type { Sale } from "@/lib/api/sales";

// tasks.md T074 (US6): printable receipt (research.md #5 - browser print via
// window.print(), no receipt-printer integration, no VAT line). Set like a
// till slip rather than an app card, so what's on screen is what comes out
// of the printer.
//
// Pure presentation: no frame, no width cap, no print button. Whoever shows
// it decides the frame - the standalone page draws a bordered slip, the
// post-sale dialog lets the dialog itself be the frame - and supplies its
// own print action. `data-receipt-print` is the hook the print stylesheet in
// globals.css uses to print this slip alone, without the app around it.
export function Receipt({ sale }: { sale: Sale }) {
  return (
    <article data-receipt-print className="bg-white px-6 py-7 text-sm print:px-0 print:py-0">
      <header className="text-center">
        <p className="font-display text-lg font-semibold tracking-tight text-ink">TaladPOS</p>
        <p className="mt-0.5 text-xs text-ink-300">ใบเสร็จรับเงิน</p>
      </header>

      <dl className="mt-5 space-y-1 text-xs text-ink-500">
        <div className="flex justify-between gap-4">
          <dt>วันที่</dt>
          <dd className="money text-ink-700">{new Date(sale.createdAt).toLocaleString("th-TH")}</dd>
        </div>
        <div className="flex justify-between gap-4">
          <dt>พนักงาน</dt>
          <dd className="text-ink-700">{sale.staff?.name ?? "-"}</dd>
        </div>
        {sale.member && (
          <div className="flex justify-between gap-4">
            <dt>สมาชิก</dt>
            <dd className="text-ink-700">{sale.member.name}</dd>
          </div>
        )}
        <div className="flex justify-between gap-4">
          <dt>เลขที่บิล</dt>
          <dd className="money text-ink-300">{sale.id.slice(0, 8)}</dd>
        </div>
      </dl>

      <ul className="my-5 space-y-2 border-y border-dashed border-steel-200 py-5">
        {sale.lineItems.map((li) => (
          <li key={li.productId} className="flex justify-between gap-4">
            <span className="text-ink-700">
              {li.productNameSnapshot}
              <span className="money text-ink-300"> ×{li.quantity}</span>
              {li.discountAmount > 0 && (
                <span className="block text-xs text-mango-600">
                  ส่วนลด <span className="money">{li.discountAmount.toFixed(2)}</span>
                </span>
              )}
            </span>
            <span className="money shrink-0 text-ink">{li.lineTotal.toFixed(2)}</span>
          </li>
        ))}
      </ul>

      <div className="space-y-1 text-xs text-ink-500">
        <div className="flex justify-between gap-4">
          <span>ยอดก่อนลด</span>
          <span className="money">{sale.subtotalAmount.toFixed(2)}</span>
        </div>
        <div className="flex justify-between gap-4">
          <span>ส่วนลด</span>
          <span className="money">{sale.discountAmount.toFixed(2)}</span>
        </div>
      </div>

      <div className="mt-3 flex items-baseline justify-between gap-4 border-t border-steel-200 pt-3">
        <span className="text-sm text-ink-700">ยอดสุทธิ</span>
        <span className="money text-2xl font-semibold text-ink">
          {sale.totalAmount.toFixed(2)}
          <span className="ml-1 text-sm font-normal text-ink-300">บาท</span>
        </span>
      </div>

      <p className="mt-7 text-center text-xs text-ink-300">ขอบคุณที่ใช้บริการ</p>
    </article>
  );
}
