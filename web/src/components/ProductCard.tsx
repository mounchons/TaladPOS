"use client";

import type { Product } from "@/lib/api/products";

export function ProductCard({
  product,
  onSelect,
}: {
  product: Product;
  onSelect: (product: Product) => void;
}) {
  const disabled = product.isOutOfStock;

  return (
    <button
      type="button"
      onClick={() => onSelect(product)}
      disabled={disabled}
      className={`flex flex-col items-center rounded-lg border border-gray-200 bg-white p-3 text-center shadow-sm transition hover:shadow-md ${
        disabled ? "cursor-not-allowed opacity-50" : "cursor-pointer"
      }`}
    >
      <div className="relative mb-2 h-24 w-24 overflow-hidden rounded-md bg-gray-100">
        {/* eslint-disable-next-line @next/next/no-img-element -- product image URLs are arbitrary (manager-entered, FR-015), so next/image's fixed remote-domain allowlist doesn't fit */}
        <img src={product.imageUrl} alt={product.name} className="h-full w-full object-cover" />
      </div>
      <span className="text-sm font-medium text-gray-900">{product.name}</span>
      <span className="text-sm text-emerald-700">{product.price.toFixed(2)} บาท</span>
      {disabled && <span className="mt-1 text-xs font-semibold text-red-600">สินค้าหมด</span>}
      {!disabled && product.isLowStock && (
        <span className="mt-1 text-xs font-semibold text-amber-600">ใกล้หมด ({product.stockQuantity})</span>
      )}
    </button>
  );
}
