"use client";

import type { Product } from "@/lib/api/products";

// Sized for a register, not a catalogue: the cashier's job is to find one of
// a few hundred products fast, so fitting more of the shelf on screen beats
// showing any single item large. The photo shrinks to a 4:3 identifying
// glance and the price - the only number read aloud - keeps the weight.
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
      <div className="relative aspect-[4/3] w-full overflow-hidden bg-steel-100">
        {/* eslint-disable-next-line @next/next/no-img-element -- product image URLs are arbitrary (manager-entered, FR-015), so next/image's fixed remote-domain allowlist doesn't fit */}
        <img src={product.imageUrl} alt="" className="h-full w-full object-cover" />
        {disabled && (
          <span className="absolute inset-x-0 bottom-0 bg-chili px-1.5 py-0.5 text-center text-[11px] font-medium text-white">
            สินค้าหมด
          </span>
        )}
        {!disabled && product.isLowStock && (
          <span className="absolute right-1.5 top-1.5 rounded-[3px] bg-white/95 px-1 py-0.5 text-[11px] font-medium text-chili">
            เหลือ {product.stockQuantity}
          </span>
        )}
      </div>

      {/* Two lines of name, then stop - a long name must not push the price
          to a different height than its neighbours in the grid. leading is
          set above Tailwind's snug because Thai stacks vowels and tone marks
          above the line and a Latin-tuned default clips them. */}
      <div className="flex flex-1 flex-col justify-between gap-0.5 px-2.5 py-2">
        <span className="line-clamp-2 text-[13px] leading-[1.45] text-ink-700">{product.name}</span>
        <span className="money text-base font-semibold text-ink">{product.price.toFixed(2)}</span>
      </div>
    </button>
  );
}
