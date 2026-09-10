"use client";

import { useEffect, useState } from "react";
import { ProductCard } from "@/components/ProductCard";
import { Cart, type CartLine } from "@/components/Cart";
import { MemberFormDialog } from "@/components/MemberFormDialog";
import { ReceiptDialog } from "@/components/ReceiptDialog";
import { searchProductsByNameOrBarcode, type Product } from "@/lib/api/products";
import { createSale, type Sale } from "@/lib/api/sales";
import type { Member } from "@/lib/api/members";
import { ApiError } from "@/lib/api/client";

export default function SalesPage() {
  const [query, setQuery] = useState("");
  const [products, setProducts] = useState<Product[]>([]);
  const [cartLines, setCartLines] = useState<CartLine[]>([]);
  const [isCheckingOut, setIsCheckingOut] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [selectedMember, setSelectedMember] = useState<Member | null>(null);
  const [memberDialogVisible, setMemberDialogVisible] = useState(false);
  const [lastCompletedSale, setLastCompletedSale] = useState<Sale | null>(null);
  const [receiptVisible, setReceiptVisible] = useState(false);

  useEffect(() => {
    const timeout = setTimeout(() => {
      // FR-002: one box answers both the scanner and a typed name. The merge
      // of the two lives in the api module - the endpoint treats search and
      // barcode as AND, so they cannot be sent together.
      searchProductsByNameOrBarcode(query)
        .then(setProducts)
        .catch(() => setProducts([]));
    }, 250);
    return () => clearTimeout(timeout);
  }, [query]);

  function addToCart(product: Product) {
    setLastCompletedSale(null);
    setMessage(null);
    setCartLines((prev) => {
      const existing = prev.find((line) => line.product.id === product.id);
      if (existing) {
        const nextQuantity = Math.min(existing.quantity + 1, product.stockQuantity);
        return prev.map((line) =>
          line.product.id === product.id ? { ...line, quantity: nextQuantity } : line,
        );
      }
      return [...prev, { product, quantity: 1 }];
    });
  }

  function changeQuantity(productId: string, quantity: number) {
    setCartLines((prev) =>
      prev.map((line) => (line.product.id === productId ? { ...line, quantity } : line)),
    );
  }

  function removeFromCart(productId: string) {
    setCartLines((prev) => prev.filter((line) => line.product.id !== productId));
  }

  async function checkout() {
    setMessage(null);
    setIsCheckingOut(true);
    try {
      const sale = await createSale({
        memberId: selectedMember?.id ?? null,
        lineItems: cartLines.map((line) => ({
          productId: line.product.id,
          quantity: line.quantity,
        })),
      });
      setLastCompletedSale(sale);
      setReceiptVisible(true);
      setCartLines([]);
      setSelectedMember(null);
      searchProductsByNameOrBarcode(query)
        .then(setProducts)
        .catch(() => {});
    } catch (err) {
      if (err instanceof ApiError && err.status === 409) {
        setMessage("สินค้าบางรายการหมดสต็อกแล้ว ลดจำนวนในตะกร้าแล้วลองอีกครั้ง");
      } else {
        setMessage("ชำระเงินไม่สำเร็จ ตรวจการเชื่อมต่อแล้วกดชำระเงินอีกครั้ง");
      }
    } finally {
      setIsCheckingOut(false);
    }
  }

  return (
    <main className="flex flex-col md:h-[calc(100dvh-var(--appbar-h))] md:flex-row">
      {/* The bottom padding clears the register sheet, which is fixed to the
          bottom of the screen on phones and would otherwise cover the last
          row of products. */}
      <div className="flex-1 overflow-y-auto px-5 pb-[var(--register-peek-h)] pt-5 md:pb-5">
        {/* The scanner target: full width, tall, first thing focused.
            h-auto/py-3.5 overrides daisyUI's fixed control height - a barcode
            scanner is aimed at by feel, so this stays taller than a form field. */}
        <input
          type="text"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder="สแกนบาร์โค้ด หรือพิมพ์ชื่อสินค้า"
          className="input h-auto w-full rounded-control border-steel-200 bg-white py-3.5 text-base"
          autoFocus
        />

        {message && (
          <p className="mt-4 rounded-control border border-chili/30 bg-chili/5 px-4 py-3 text-sm text-chili">
            {message}
          </p>
        )}

        {products.length === 0 ? (
          <p className="mt-16 text-center text-sm text-ink-300">
            {query ? `ไม่พบสินค้าที่ตรงกับ "${query}"` : "ยังไม่มีสินค้าในร้าน"}
          </p>
        ) : (
          <div className="mt-4 grid grid-cols-[repeat(auto-fill,minmax(7rem,1fr))] gap-2.5">
            {/* Auto-fitting tracks rather than fixed column counts: the shelf
                fills whatever width is left beside the register at any screen
                size, and a card never stretches past the size where its photo
                stops earning the space. */}
            {products.map((product) => (
              <ProductCard key={product.id} product={product} onSelect={addToCart} />
            ))}
          </div>
        )}
      </div>

      <Cart
        lines={cartLines}
        onChangeQuantity={changeQuantity}
        onRemove={removeFromCart}
        onCheckout={checkout}
        isCheckingOut={isCheckingOut}
        selectedMember={selectedMember}
        onSelectMember={setSelectedMember}
        onOpenRegisterMember={() => setMemberDialogVisible(true)}
        lastCompletedSale={lastCompletedSale}
        onShowReceipt={() => setReceiptVisible(true)}
      />

      <ReceiptDialog
        sale={lastCompletedSale}
        visible={receiptVisible}
        onHide={() => setReceiptVisible(false)}
      />

      <MemberFormDialog
        visible={memberDialogVisible}
        onHide={() => setMemberDialogVisible(false)}
        onRegistered={(member) => {
          setSelectedMember(member);
          setMemberDialogVisible(false);
        }}
      />
    </main>
  );
}
