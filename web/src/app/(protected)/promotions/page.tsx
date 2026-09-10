"use client";

import { useCallback, useEffect, useState } from "react";
import { DataTable } from "primereact/datatable";
import { Column } from "primereact/column";
import { Button } from "primereact/button";
import { dataTablePT, buttonPT, secondaryButtonPT } from "@/styles/primereact-passthrough";
import { deletePromotion, listPromotions, type Promotion } from "@/lib/api/promotions";
import { searchProducts, type Product } from "@/lib/api/products";
import { PromotionFormDialog } from "@/components/PromotionFormDialog";
import { ManagerOnly } from "@/components/ManagerOnly";
import { useAuth } from "@/lib/auth/AuthContext";
import { ApiError } from "@/lib/api/client";

export default function PromotionsPage() {
  const { staff } = useAuth();
  const [promotions, setPromotions] = useState<Promotion[]>([]);
  const [products, setProducts] = useState<Product[]>([]);
  const [dialogVisible, setDialogVisible] = useState(false);
  const [editingPromotion, setEditingPromotion] = useState<Promotion | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const refresh = useCallback(() => {
    listPromotions()
      .then(setPromotions)
      .catch(() => setPromotions([]));
  }, []);

  useEffect(() => {
    refresh();
    searchProducts({})
      .then(setProducts)
      .catch(() => setProducts([]));
  }, [refresh]);

  // FR-029: /promotions is a Manager-only screen, same guard shape as /stock.
  if (staff?.role !== "Manager") {
    return <ManagerOnly />;
  }

  function openCreateDialog() {
    setEditingPromotion(null);
    setDialogVisible(true);
  }

  function openEditDialog(promotion: Promotion) {
    setEditingPromotion(promotion);
    setDialogVisible(true);
  }

  function handleSaved() {
    setDialogVisible(false);
    refresh();
  }

  async function handleDelete(promotion: Promotion) {
    setMessage(null);
    try {
      await deletePromotion(promotion.id);
      refresh();
    } catch (err) {
      setMessage(err instanceof ApiError ? "ไม่สามารถลบโปรโมชั่นได้" : "เกิดข้อผิดพลาด");
    }
  }

  function productName(productId: string | null): string {
    if (!productId) return "-";
    return products.find((p) => p.id === productId)?.name ?? productId;
  }

  return (
    <main className="px-5 py-5">
      <div className="mb-5 flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-xl font-semibold">โปรโมชั่น</h1>
        <Button label="สร้างโปรโมชั่น" icon="pi pi-plus" onClick={openCreateDialog} pt={buttonPT} />
      </div>

      {message && <p className="mb-4 text-sm text-chili">{message}</p>}

      <DataTable
        value={promotions}
        pt={dataTablePT}
        dataKey="id"
        responsiveLayout="stack"
        emptyMessage="ยังไม่มีโปรโมชั่น กดสร้างโปรโมชั่นเพื่อเริ่ม"
      >
        <Column
          header="ส่วนลด"
          body={(p: Promotion) => (
            <span className="money text-base font-medium">{p.discountPercentage}%</span>
          )}
        />
        <Column
          header="ใช้กับ"
          body={(p: Promotion) => (p.scope === "Item" ? productName(p.productId) : "ทั้งบิล")}
        />
        <Column
          header="เงื่อนไข"
          body={(p: Promotion) => (p.appliesToMembersOnly ? "เฉพาะสมาชิก" : "ลูกค้าทุกคน")}
        />
        <Column
          header="ช่วงวันที่"
          body={(p: Promotion) => (
            <span className="money text-ink-500">
              {p.startDate} – {p.endDate}
            </span>
          )}
        />
        <Column
          header="สถานะ"
          body={(p: Promotion) =>
            p.isActive ? (
              <span className="inline-flex items-center gap-1.5 text-sm text-leaf">
                <span className="h-1.5 w-1.5 rounded-full bg-leaf" />
                ใช้อยู่
              </span>
            ) : (
              <span className="text-sm text-ink-300">ยังไม่เริ่ม/หมดอายุ</span>
            )
          }
        />
        <Column
          header=""
          body={(promotion: Promotion) => (
            <div className="flex gap-2">
              <Button
                label="แก้ไข"
                onClick={() => openEditDialog(promotion)}
                pt={secondaryButtonPT}
              />
              <Button label="ลบ" onClick={() => handleDelete(promotion)} pt={secondaryButtonPT} />
            </div>
          )}
        />
      </DataTable>

      <PromotionFormDialog
        visible={dialogVisible}
        promotion={editingPromotion}
        products={products}
        onHide={() => setDialogVisible(false)}
        onSaved={handleSaved}
      />
    </main>
  );
}
