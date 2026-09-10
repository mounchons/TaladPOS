"use client";

import { Button } from "primereact/button";
import { InputNumber } from "primereact/inputnumber";
import { darkInputNumberPT } from "@/styles/primereact-passthrough";
import type { Product } from "@/lib/api/products";
import type { Member } from "@/lib/api/members";
import type { Sale } from "@/lib/api/sales";
import { MemberSearch } from "@/components/MemberSearch";

export interface CartLine {
  product: Product;
  quantity: number;
}

export function Cart({
  lines,
  onChangeQuantity,
  onRemove,
  onCheckout,
  isCheckingOut,
  selectedMember,
  onSelectMember,
  onOpenRegisterMember,
  lastCompletedSale,
}: {
  lines: CartLine[];
  onChangeQuantity: (productId: string, quantity: number) => void;
  onRemove: (productId: string) => void;
  onCheckout: () => void;
  isCheckingOut: boolean;
  selectedMember: Member | null;
  onSelectMember: (member: Member | null) => void;
  onOpenRegisterMember: () => void;
  lastCompletedSale?: Sale | null;
}) {
  const subtotal = lines.reduce((sum, line) => sum + line.product.price * line.quantity, 0);

  return (
    <aside className="flex w-full flex-col bg-ink text-white md:h-[calc(100vh-57px)] md:w-96 md:shrink-0">
      {/* Member. Optional, so it sits above the goods and stays visually quiet. */}
      <div className="border-b border-ink-700 px-5 py-4">
        {selectedMember ? (
          <div className="flex items-start justify-between gap-3">
            <div className="leading-tight">
              <p className="text-sm text-white">{selectedMember.name}</p>
              <p className="money text-xs text-ink-300">{selectedMember.phoneNumber}</p>
              <p className="mt-1 text-xs text-ink-300">
                ยอดสะสม <span className="money">{selectedMember.accumulatedPurchaseTotal.toFixed(2)}</span> บาท
              </p>
            </div>
            <button
              type="button"
              onClick={() => onSelectMember(null)}
              className="rounded px-2 py-1 text-xs text-ink-300 transition-colors hover:bg-ink-700 hover:text-white"
            >
              เอาออก
            </button>
          </div>
        ) : (
          <div className="flex flex-col gap-2">
            <MemberSearch selected={selectedMember} onSelect={onSelectMember} tone="dark" />
            <button
              type="button"
              onClick={onOpenRegisterMember}
              className="self-start rounded px-1 py-1 text-xs text-mango transition-colors hover:text-mango-600"
            >
              สมัครสมาชิกใหม่
            </button>
          </div>
        )}
      </div>

      {/* Goods */}
      <div className="flex-1 overflow-y-auto px-5 py-4">
        {lastCompletedSale && lines.length === 0 && (
          <div className="receipt-settle rounded-control border border-ink-700 bg-ink-700/50 p-4">
            <p className="mb-3 text-xs text-ink-300">ขายสำเร็จ</p>
            <ul className="mb-3 space-y-1.5">
              {lastCompletedSale.lineItems.map((li) => (
                <li key={li.productId} className="flex justify-between gap-3 text-sm">
                  <span className="text-ink-300">
                    {li.productNameSnapshot}
                    <span className="money"> ×{li.quantity}</span>
                    {li.discountAmount > 0 && (
                      <span className="ml-1 text-xs text-mango">
                        ลด <span className="money">{li.discountAmount.toFixed(2)}</span>
                      </span>
                    )}
                  </span>
                  <span className="money text-ink-300">{li.lineTotal.toFixed(2)}</span>
                </li>
              ))}
            </ul>
            <div className="flex justify-between border-t border-ink-700 pt-2 text-sm">
              <span className="text-ink-300">รับไป</span>
              <span className="money font-medium text-white">
                {lastCompletedSale.totalAmount.toFixed(2)} บาท
              </span>
            </div>
          </div>
        )}

        {lines.length === 0 && !lastCompletedSale && (
          <p className="pt-8 text-center text-sm text-ink-300">แตะสินค้าเพื่อเริ่มขาย</p>
        )}

        {lines.length > 0 && (
          <ul className="space-y-3">
            {lines.map((line) => (
              <li key={line.product.id} className="flex items-center gap-3">
                <div className="min-w-0 flex-1 leading-tight">
                  <p className="truncate text-sm text-white">{line.product.name}</p>
                  <p className="money text-xs text-ink-300">{line.product.price.toFixed(2)} / ชิ้น</p>
                </div>
                <InputNumber
                  value={line.quantity}
                  onValueChange={(e) =>
                    onChangeQuantity(line.product.id, Math.min(e.value ?? 1, line.product.stockQuantity))
                  }
                  showButtons
                  buttonLayout="horizontal"
                  incrementButtonIcon="pi pi-plus"
                  decrementButtonIcon="pi pi-minus"
                  min={1}
                  max={line.product.stockQuantity}
                  pt={darkInputNumberPT}
                />
                <button
                  type="button"
                  onClick={() => onRemove(line.product.id)}
                  aria-label={`เอา ${line.product.name} ออกจากตะกร้า`}
                  className="rounded p-2 text-ink-300 transition-colors hover:bg-ink-700 hover:text-chili"
                >
                  <i className="pi pi-times text-xs" />
                </button>
              </li>
            ))}
          </ul>
        )}
      </div>

      {/* The total is the loudest thing in the app on purpose: the cashier
          reads it out, the customer checks it. */}
      <div className="border-t border-ink-700 px-5 pb-5 pt-4">
        <div className="mb-4 flex items-baseline justify-between gap-3">
          <span className="text-sm text-ink-300">ยอดรวม</span>
          <span className="money text-5xl font-semibold leading-none text-mango">
            {subtotal.toFixed(2)}
          </span>
        </div>
        <Button
          label={isCheckingOut ? "กำลังชำระเงิน" : "ชำระเงิน"}
          onClick={onCheckout}
          disabled={lines.length === 0 || isCheckingOut}
          pt={{
            root: {
              className:
                "flex w-full items-center justify-center rounded-control bg-white px-4 py-4 font-display " +
                "text-base font-semibold text-ink transition-colors hover:bg-mango " +
                "disabled:cursor-not-allowed disabled:bg-ink-700 disabled:text-ink-300",
            },
          }}
        />
      </div>
    </aside>
  );
}
