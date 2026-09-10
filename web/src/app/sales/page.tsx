"use client";

import { useEffect, useState } from "react";
import { InputText } from "primereact/inputtext";
import { inputTextPT } from "@/styles/primereact-passthrough";
import { ProductCard } from "@/components/ProductCard";
import { Cart, type CartLine } from "@/components/Cart";
import { searchProducts, type Product } from "@/lib/api/products";
import { createSale } from "@/lib/api/sales";
import { ApiError } from "@/lib/api/client";

export default function SalesPage() {
  const [query, setQuery] = useState("");
  const [products, setProducts] = useState<Product[]>([]);
  const [cartLines, setCartLines] = useState<CartLine[]>([]);
  const [isCheckingOut, setIsCheckingOut] = useState(false);
  const [message, setMessage] = useState<string | null>(null);

  useEffect(() => {
    const timeout = setTimeout(() => {
      // FR-002: search by name or barcode - a barcode scanner types the code
      // then presses Enter, so trying both here covers scan and manual typing.
      searchProducts({ search: query || undefined, barcode: query || undefined })
        .then((results) => {
          const seen = new Map(results.map((p) => [p.id, p]));
          setProducts(Array.from(seen.values()));
        })
        .catch(() => setProducts([]));
    }, 250);
    return () => clearTimeout(timeout);
  }, [query]);

  function addToCart(product: Product) {
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
        lineItems: cartLines.map((line) => ({ productId: line.product.id, quantity: line.quantity })),
      });
      setMessage(`ชำระเงินสำเร็จ ยอดรวม ${sale.totalAmount.toFixed(2)} บาท`);
      setCartLines([]);
      setQuery((q) => q); // trigger a refresh of stock numbers
      searchProducts({ search: query || undefined }).then(setProducts).catch(() => {});
    } catch (err) {
      if (err instanceof ApiError && err.status === 409) {
        setMessage("สินค้าบางรายการหมดสต็อกแล้ว กรุณาตรวจสอบตะกร้าอีกครั้ง");
      } else {
        setMessage("เกิดข้อผิดพลาด ไม่สามารถชำระเงินได้");
      }
    } finally {
      setIsCheckingOut(false);
    }
  }

  return (
    <main className="flex min-h-screen flex-col gap-4 bg-gray-50 p-4 md:flex-row">
      <div className="flex-1">
        <h1 className="mb-4 text-xl font-semibold text-gray-900">หน้าขายสินค้า</h1>
        <InputText
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder="ค้นหาชื่อสินค้าหรือสแกนบาร์โค้ด"
          pt={inputTextPT}
          className="mb-4"
        />
        {message && <p className="mb-4 rounded-md bg-white p-2 text-sm text-gray-800 shadow-sm">{message}</p>}
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4">
          {products.map((product) => (
            <ProductCard key={product.id} product={product} onSelect={addToCart} />
          ))}
        </div>
      </div>

      <Cart
        lines={cartLines}
        onChangeQuantity={changeQuantity}
        onRemove={removeFromCart}
        onCheckout={checkout}
        isCheckingOut={isCheckingOut}
      />
    </main>
  );
}
