"use client";

import { useEffect, useState } from "react";
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
  onShowReceipt,
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
  onShowReceipt: () => void;
}) {
  const subtotal = lines.reduce((sum, line) => sum + line.product.price * line.quantity, 0);
  const itemCount = lines.reduce((sum, line) => sum + line.quantity, 0);

  // On a phone the register cannot sit beside the goods, and stacking it below
  // them would put the total a full scroll away from the products - so it
  // becomes a sheet pinned to the bottom of the screen with only the total and
  // the pay button showing. md and up is untouched: the rail stays a rail.
  const [isSheetOpen, setIsSheetOpen] = useState(false);

  // A finished sale collapses the sheet: the receipt has taken over the
  // screen in its own dialog, and behind it the register should already be
  // reset and out of the way for the next customer.
  const completedSaleId = lastCompletedSale?.id ?? null;
  useEffect(() => {
    if (completedSaleId) setIsSheetOpen(false);
  }, [completedSaleId]);

  const collapsedOnPhone = isSheetOpen ? "" : "hidden";

  return (
    <>
      {isSheetOpen && (
        <div
          className="fixed inset-0 z-30 bg-ink/50 md:hidden"
          onClick={() => setIsSheetOpen(false)}
          aria-hidden
        />
      )}

      <aside
        className={
          "fixed inset-x-0 bottom-0 z-40 flex max-h-[85dvh] w-full flex-col bg-ink text-white " +
          // The rail was a fixed w-96 (384px), which at 900px took 43% of the
        // screen and squeezed the shelf down to three 147px columns. Sizing it
        // as a proportion with a floor and a ceiling keeps it under a third of
        // any screen between tablet and desktop while never shrinking past the
        // width where a line's name, quantity and price stop fitting on one row.
        "md:static md:z-auto md:h-[calc(100dvh-var(--appbar-h))] md:max-h-none md:shrink-0 " +
          "md:w-[clamp(15rem,30%,24rem)]"
        }
      >
        {/* The grab handle, phone only: what is in the cart, and the way in. */}
        <button
          type="button"
          onClick={() => setIsSheetOpen((open) => !open)}
          aria-expanded={isSheetOpen}
          className="flex items-center justify-between gap-3 border-b border-ink-700 px-5 py-3 text-left md:hidden"
        >
          <span className="text-sm text-ink-300">
            ตะกร้า
            {itemCount > 0 && (
              <>
                {" "}
                <span className="money text-white">{itemCount}</span> ชิ้น
              </>
            )}
          </span>
          {/* Inline rather than an icon font: primeicons leaves with
              PrimeReact, and one path is cheaper than a webfont for one glyph. */}
          <svg
            viewBox="0 0 16 16"
            aria-hidden
            className={`h-3 w-3 shrink-0 text-ink-300 transition-transform ${isSheetOpen ? "" : "rotate-180"}`}
          >
            <path d="M3 6l5 5 5-5" fill="none" stroke="currentColor" strokeWidth="2" />
          </svg>
        </button>

        {/* Member. Optional, so it sits above the goods and stays visually quiet. */}
        <div className={`${collapsedOnPhone} border-b border-ink-700 px-5 py-4 md:block`}>
          {selectedMember ? (
            <div className="flex items-start justify-between gap-3">
              <div className="leading-tight">
                <p className="text-sm text-white">{selectedMember.name}</p>
                <p className="money text-xs text-ink-300">{selectedMember.phoneNumber}</p>
                <p className="mt-1 text-xs text-ink-300">
                  ยอดสะสม{" "}
                  <span className="money">
                    {selectedMember.accumulatedPurchaseTotal.toFixed(2)}
                  </span>{" "}
                  บาท
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
        <div
          className={`${collapsedOnPhone} flex-1 overflow-y-auto px-5 py-4 md:block md:flex-1 md:overflow-y-auto`}
        >
          {lines.length === 0 && (
            <p className="pt-8 text-center text-sm text-ink-300">แตะสินค้าเพื่อเริ่มขาย</p>
          )}

          {lines.length > 0 && (
            <ul className="space-y-3">
              {lines.map((line) => (
                <li key={line.product.id} className="flex items-center gap-3">
                  <div className="min-w-0 flex-1 leading-tight">
                    <p className="truncate text-sm text-white">{line.product.name}</p>
                    <p className="money text-xs text-ink-300">
                      {line.product.price.toFixed(2)} / ชิ้น
                    </p>
                  </div>
                  {/* daisyUI has no number stepper, so it is a join of three
                      controls. The clamp to stockQuantity is kept on every path
                      - typing 99 into the field must not sell stock the shop
                      does not have (FR-005 is enforced server-side, but the
                      cashier should not be able to build the bad basket). */}
                  <div className="join shrink-0">
                    <button
                      type="button"
                      className="btn btn-sm join-item border-ink-700 bg-ink-700 text-white hover:bg-ink-500"
                      aria-label={`ลดจำนวน ${line.product.name}`}
                      disabled={line.quantity <= 1}
                      onClick={() => onChangeQuantity(line.product.id, line.quantity - 1)}
                    >
                      −
                    </button>
                    <input
                      type="number"
                      inputMode="numeric"
                      value={line.quantity}
                      min={1}
                      max={line.product.stockQuantity}
                      aria-label={`จำนวน ${line.product.name}`}
                      onChange={(e) => {
                        const next = Number.parseInt(e.target.value, 10);
                        if (Number.isNaN(next)) return;
                        onChangeQuantity(
                          line.product.id,
                          Math.min(Math.max(next, 1), line.product.stockQuantity),
                        );
                      }}
                      className="input input-sm join-item money w-14 border-ink-700 bg-ink text-center text-white"
                    />
                    <button
                      type="button"
                      className="btn btn-sm join-item border-ink-700 bg-ink-700 text-white hover:bg-ink-500"
                      aria-label={`เพิ่มจำนวน ${line.product.name}`}
                      disabled={line.quantity >= line.product.stockQuantity}
                      onClick={() => onChangeQuantity(line.product.id, line.quantity + 1)}
                    >
                      +
                    </button>
                  </div>
                  <button
                    type="button"
                    onClick={() => onRemove(line.product.id)}
                    aria-label={`เอา ${line.product.name} ออกจากตะกร้า`}
                    className="rounded p-2 text-ink-300 transition-colors hover:bg-ink-700 hover:text-chili"
                  >
                    <svg viewBox="0 0 16 16" aria-hidden className="h-3 w-3">
                      <path d="M4 4l8 8M12 4l-8 8" fill="none" stroke="currentColor" strokeWidth="2" />
                    </svg>
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
          {/* Disabled used to be ink-700 on an ink panel - a 1.2:1 difference
              that read as "there is no pay button" until something was in the
              cart. It now keeps a lit outline so the button is always visibly
              there, just clearly not ready yet. */}
          <button
            type="button"
            onClick={onCheckout}
            disabled={lines.length === 0 || isCheckingOut}
            className={
              "btn h-auto w-full rounded-control border-white bg-white px-4 py-4 font-display text-base " +
              "font-semibold text-ink hover:border-mango hover:bg-mango " +
              "disabled:cursor-not-allowed disabled:border-ink-500 disabled:bg-transparent disabled:text-ink-300"
            }
          >
            {isCheckingOut ? "กำลังชำระเงิน" : "ชำระเงิน"}
          </button>

          {lastCompletedSale && (
            <button
              type="button"
              onClick={onShowReceipt}
              className="mt-3 w-full rounded py-1 text-center text-xs text-ink-300 transition-colors hover:text-white"
            >
              ใบเสร็จบิลล่าสุด
            </button>
          )}
        </div>
      </aside>
    </>
  );
}
