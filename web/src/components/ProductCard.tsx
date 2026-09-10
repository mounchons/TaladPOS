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
      className={`group flex flex-col overflow-hidden rounded-control border bg-white text-left transition-colors ${
        disabled
          ? "cursor-not-allowed border-steel-100 opacity-50"
          : "cursor-pointer border-steel-200 hover:border-ink active:border-mango"
      }`}
    >
      <div className="relative aspect-square w-full overflow-hidden bg-steel-100">
        {/* eslint-disable-next-line @next/next/no-img-element -- product image URLs are arbitrary (manager-entered, FR-015), so next/image's fixed remote-domain allowlist doesn't fit */}
        <img src={product.imageUrl} alt="" className="h-full w-full object-cover" />
        {disabled && (
          <span className="absolute inset-x-0 bottom-0 bg-chili px-2 py-1 text-center text-xs font-medium text-white">
            สินค้าหมด
          </span>
        )}
        {!disabled && product.isLowStock && (
          <span className="absolute right-2 top-2 rounded-[3px] bg-white/95 px-1.5 py-0.5 text-xs font-medium text-chili">
            เหลือ {product.stockQuantity}
          </span>
        )}
      </div>

      <div className="flex flex-1 flex-col justify-between gap-1 px-3 py-2.5">
        <span className="text-sm leading-snug text-ink-700">{product.name}</span>
        <span className="money text-xl font-semibold text-ink">{product.price.toFixed(2)}</span>
      </div>
    </button>
  );
}
