"use client";

import { useCallback, useEffect, useState } from "react";
import { DataTable, type DataTableColumn } from "@/components/DataTable";
import { deleteProduct, searchProductsPaged, type Product } from "@/lib/api/products";
import { ProductFormDialog } from "@/components/ProductFormDialog";
import { ManagerOnly } from "@/components/ManagerOnly";
import { useAuth } from "@/lib/auth/AuthContext";
import { ApiError } from "@/lib/api/client";

const PAGE_SIZE = 20;

export default function StockPage() {
  const { staff } = useAuth();
  const [query, setQuery] = useState("");
  const [lowStockOnly, setLowStockOnly] = useState(false);
  const [page, setPage] = useState(1);
  const [products, setProducts] = useState<Product[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [isLoading, setIsLoading] = useState(true);
  const [dialogVisible, setDialogVisible] = useState(false);
  const [editingProduct, setEditingProduct] = useState<Product | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  // Both filters go to the API rather than being applied to the page in hand:
  // filtering client-side would only ever search the twenty rows already
  // fetched, which looks like a working filter right up until the product you
  // want is on page three.
  const refresh = useCallback(() => {
    setIsLoading(true);
    searchProductsPaged({
      search: query || undefined,
      lowStockOnly: lowStockOnly || undefined,
      page,
      pageSize: PAGE_SIZE,
    })
      .then((result) => {
        setProducts(result.items);
        setTotalCount(result.totalCount);
      })
      .catch(() => {
        setProducts([]);
        setTotalCount(0);
      })
      .finally(() => setIsLoading(false));
  }, [query, lowStockOnly, page]);

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

  const columns: DataTableColumn<Product>[] = [
    {
      header: "รูป",
      cell: (product) => (
        // eslint-disable-next-line @next/next/no-img-element -- product image URLs are arbitrary (manager-entered, FR-015)
        <img src={product.imageUrl} alt="" className="h-10 w-10 rounded-[3px] object-cover" />
      ),
    },
    { header: "ชื่อสินค้า", cell: (p) => p.name },
    {
      header: "บาร์โค้ด",
      cell: (p) => <span className="money text-ink-500">{p.barcode ?? "—"}</span>,
    },
    {
      header: "ราคา",
      className: "text-right",
      cell: (p) => <span className="money">{p.price.toFixed(2)}</span>,
    },
    {
      header: "คงเหลือ",
      cell: (p) => (
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
      ),
    },
    {
      header: "",
      className: "text-right",
      cell: (product) => (
        <div className="flex justify-end gap-2">
          <button
            type="button"
            onClick={() => openEditDialog(product)}
            className="btn btn-sm rounded-control border-steel-200 bg-white font-display font-medium text-ink-700 hover:border-ink hover:bg-white"
          >
            แก้ไข
          </button>
          <button
            type="button"
            onClick={() => handleDelete(product)}
            className="btn btn-sm rounded-control border-steel-200 bg-white font-display font-medium text-ink-700 hover:border-chili hover:bg-white hover:text-chili"
          >
            ลบ
          </button>
        </div>
      ),
    },
  ];

  return (
    <main className="px-5 py-5">
      <div className="mb-5 flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-xl font-semibold">สต็อกสินค้า</h1>
        <button
          type="button"
          onClick={openCreateDialog}
          className="btn rounded-control border-ink bg-ink font-display font-medium text-white hover:border-mango hover:bg-mango hover:text-ink"
        >
          + เพิ่มสินค้า
        </button>
      </div>

      {message && <p className="mb-4 text-sm text-chili">{message}</p>}

      <DataTable
        columns={columns}
        rows={products}
        rowKey={(p) => p.id}
        page={page}
        pageSize={PAGE_SIZE}
        totalCount={totalCount}
        onPageChange={setPage}
        // Everything the server-side filter depends on. DataTable sends the
        // table back to page 1 when this changes, so narrowing the search while
        // on page 5 never leaves the manager staring at an empty table.
        filterKey={`${query}|${lowStockOnly}`}
        isLoading={isLoading}
        emptyText={
          query || lowStockOnly
            ? "ไม่พบสินค้าที่ตรงกับเงื่อนไข"
            : "ยังไม่มีสินค้า กดเพิ่มสินค้าเพื่อเริ่ม"
        }
      >
        <div className="flex flex-wrap items-center gap-3">
          <input
            type="text"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder="ค้นหาชื่อสินค้า"
            className="input w-full max-w-sm rounded-control border-steel-200 bg-white"
          />
          <label className="flex cursor-pointer items-center gap-2 text-sm text-ink-700">
            <input
              type="checkbox"
              checked={lowStockOnly}
              onChange={(e) => setLowStockOnly(e.target.checked)}
              className="checkbox checkbox-sm"
            />
            เฉพาะสินค้าใกล้หมด
          </label>
        </div>
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
