"use client";

import { Button } from "primereact/button";
import { InputNumber } from "primereact/inputnumber";
import { buttonPT, inputNumberPT } from "@/styles/primereact-passthrough";
import type { Product } from "@/lib/api/products";

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
}: {
  lines: CartLine[];
  onChangeQuantity: (productId: string, quantity: number) => void;
  onRemove: (productId: string) => void;
  onCheckout: () => void;
  isCheckingOut: boolean;
}) {
  const subtotal = lines.reduce((sum, line) => sum + line.product.price * line.quantity, 0);

  return (
    <aside className="flex w-full flex-col rounded-lg border border-gray-200 bg-white p-4 shadow-sm md:w-80">
      <h2 className="mb-3 text-lg font-semibold text-gray-900">ตะกร้าสินค้า</h2>

      {lines.length === 0 ? (
        <p className="text-sm text-gray-500">ยังไม่มีสินค้าในตะกร้า</p>
      ) : (
        <ul className="flex-1 space-y-3 overflow-y-auto">
          {lines.map((line) => (
            <li key={line.product.id} className="flex items-center gap-2">
              <div className="flex-1">
                <p className="text-sm font-medium text-gray-900">{line.product.name}</p>
                <p className="text-xs text-gray-500">{line.product.price.toFixed(2)} บาท/ชิ้น</p>
              </div>
              <InputNumber
                value={line.quantity}
                onValueChange={(e) =>
                  onChangeQuantity(line.product.id, Math.min(e.value ?? 1, line.product.stockQuantity))
                }
                showButtons
                min={1}
                max={line.product.stockQuantity}
                inputClassName="w-14 text-center"
                pt={inputNumberPT}
              />
              <Button
                icon="pi pi-trash"
                onClick={() => onRemove(line.product.id)}
                pt={{ root: { className: "bg-red-600 hover:bg-red-700 px-2 py-2" } }}
                aria-label={`ลบ ${line.product.name} ออกจากตะกร้า`}
              />
            </li>
          ))}
        </ul>
      )}

      <div className="mt-4 flex items-center justify-between border-t border-gray-200 pt-3">
        <span className="text-sm font-medium text-gray-700">ยอดรวม</span>
        <span className="text-lg font-semibold text-emerald-700">{subtotal.toFixed(2)} บาท</span>
      </div>

      <Button
        label={isCheckingOut ? "กำลังชำระเงิน..." : "ชำระเงิน"}
        onClick={onCheckout}
        disabled={lines.length === 0 || isCheckingOut}
        pt={buttonPT}
        className="mt-3"
      />
    </aside>
  );
}
