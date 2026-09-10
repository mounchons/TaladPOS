"use client";

import { useCallback, useEffect, useState } from "react";
import { DataTable } from "primereact/datatable";
import { Column } from "primereact/column";
import { Button } from "primereact/button";
import { InputText } from "primereact/inputtext";
import { dataTablePT, inputTextPT, buttonPT, secondaryButtonPT } from "@/styles/primereact-passthrough";
import { deleteProduct, searchProducts, type Product } from "@/lib/api/products";
import { ProductFormDialog } from "@/components/ProductFormDialog";
import { ManagerOnly } from "@/components/ManagerOnly";
import { useAuth } from "@/lib/auth/AuthContext";
import { ApiError } from "@/lib/api/client";

export default function StockPage() {
  const { staff } = useAuth();
  const [query, setQuery] = useState("");
  const [products, setProducts] = useState<Product[]>([]);
  const [dialogVisible, setDialogVisible] = useState(false);
  const [editingProduct, setEditingProduct] = useState<Product | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const refresh = useCallback(() => {
    searchProducts({ search: query || undefined })
      .then(setProducts)
      .catch(() => setProducts([]));
  }, [query]);

  useEffect(() => {
    const timeout = setTimeout(refresh, 250);
    return () => clearTimeout(timeout);
  }, [refresh]);

  // FR-029: /stock is a Manager-only screen. T035's (protected) layout
  // already guarantees someone is logged in - this only adds the
  // role check on top of that.
  if (staff?.role !== "Manager") {
    return <ManagerOnly />;
  }

  function openCreateDialog() {
    setEditingProduct(null);
    setDialogVisible(true);
  }

  function openEditDialog(product: Product) {
    setEditingProduct(product);
    setDialogVisible(true);
  }

  function handleSaved() {
    setDialogVisible(false);
    refresh();
  }

  async function handleDelete(product: Product) {
    setMessage(null);
    try {
      await deleteProduct(product.id);
      refresh();
    } catch (err) {
      setMessage(err instanceof ApiError ? "ไม่สามารถลบสินค้าได้" : "เกิดข้อผิดพลาด");
    }
  }

  return (
    <main className="px-5 py-5">
      <div className="mb-5 flex items-center justify-between gap-4">
        <h1 className="text-xl font-semibold">สต็อกสินค้า</h1>
        <Button label="เพิ่มสินค้า" icon="pi pi-plus" onClick={openCreateDialog} pt={buttonPT} />
      </div>

      <InputText
        value={query}
        onChange={(e) => setQuery(e.target.value)}
        placeholder="ค้นหาชื่อสินค้า"
        pt={inputTextPT}
        className="mb-4 max-w-sm"
      />

      {message && <p className="mb-4 text-sm text-chili">{message}</p>}

      <DataTable value={products} pt={dataTablePT} dataKey="id" emptyMessage="ยังไม่มีสินค้า กดเพิ่มสินค้าเพื่อเริ่ม">
        <Column
          header="รูป"
          body={(product: Product) => (
            // eslint-disable-next-line @next/next/no-img-element -- product image URLs are arbitrary (manager-entered, FR-015)
            <img src={product.imageUrl} alt="" className="h-10 w-10 rounded-[3px] object-cover" />
          )}
        />
        <Column field="name" header="ชื่อสินค้า" />
        <Column
          header="บาร์โค้ด"
          body={(p: Product) => <span className="money text-ink-500">{p.barcode ?? "—"}</span>}
        />
        <Column header="ราคา" body={(p: Product) => <span className="money">{p.price.toFixed(2)}</span>} />
        <Column
          header="คงเหลือ"
          body={(p: Product) => (
            <span className="flex items-center gap-2">
              <span className="money">{p.stockQuantity}</span>
              {p.isOutOfStock && (
                <span className="rounded-[3px] bg-chili px-1.5 py-0.5 text-xs font-medium text-white">
                  หมด
                </span>
              )}
              {!p.isOutOfStock && p.isLowStock && (
                <span className="rounded-[3px] bg-mango-100 px-1.5 py-0.5 text-xs font-medium text-mango-600">
                  ใกล้หมด
                </span>
              )}
            </span>
          )}
        />
        <Column
          header=""
          body={(product: Product) => (
            <div className="flex gap-2">
              <Button label="แก้ไข" onClick={() => openEditDialog(product)} pt={secondaryButtonPT} />
              <Button label="ลบ" onClick={() => handleDelete(product)} pt={secondaryButtonPT} />
            </div>
          )}
        />
      </DataTable>

      <ProductFormDialog
        visible={dialogVisible}
        product={editingProduct}
        onHide={() => setDialogVisible(false)}
        onSaved={handleSaved}
      />
    </main>
  );
}
