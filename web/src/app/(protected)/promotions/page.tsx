"use client";

import { useCallback, useEffect, useState } from "react";
import { DataTable, type DataTableColumn } from "@/components/DataTable";
import { deletePromotion, listPromotions, type Promotion } from "@/lib/api/promotions";
import { searchProducts, type Product } from "@/lib/api/products";
import { PromotionFormDialog } from "@/components/PromotionFormDialog";
import { ConditionalPromotionFormDialog } from "@/components/ConditionalPromotionFormDialog";
import {
  deleteConditionalPromotion,
  listConditionalPromotions,
  type ConditionalPromotion,
} from "@/lib/api/conditionalPromotions";
import { ManagerOnly } from "@/components/ManagerOnly";
import { useAuth } from "@/lib/auth/AuthContext";
import { ApiError } from "@/lib/api/client";

/**
 * 003/FR-008: both promotion kinds live in one table, tagged so edit and delete
 * reach the right endpoint. They are not merged into one shape - the percentage
 * ones keep theirs untouched (003/FR-026) - so the row carries a discriminator
 * instead.
 */
type PromotionRow =
  | { kind: "percentage"; id: string; promotion: Promotion }
  | { kind: "conditional"; id: string; promotion: ConditionalPromotion };

export default function PromotionsPage() {
  const { staff } = useAuth();
  const [promotions, setPromotions] = useState<Promotion[]>([]);
  const [products, setProducts] = useState<Product[]>([]);
  const [activeOnly, setActiveOnly] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [dialogVisible, setDialogVisible] = useState(false);
  const [editingPromotion, setEditingPromotion] = useState<Promotion | null>(null);
  const [conditionalPromotions, setConditionalPromotions] = useState<ConditionalPromotion[]>([]);
  const [conditionalDialogVisible, setConditionalDialogVisible] = useState(false);
  const [editingConditional, setEditingConditional] = useState<ConditionalPromotion | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  // GET /api/v1/promotions is deliberately not paged (research.md #11): a
  // single shop runs promotions in the tens, so the filter goes to the API and
  // the whole filtered set comes back at once.
  const refresh = useCallback(() => {
    setIsLoading(true);
    Promise.all([
      listPromotions(activeOnly).catch(() => [] as Promotion[]),
      listConditionalPromotions(activeOnly).catch(() => [] as ConditionalPromotion[]),
    ])
      .then(([percentage, conditional]) => {
        setPromotions(percentage);
        setConditionalPromotions(conditional);
      })
      .finally(() => setIsLoading(false));
  }, [activeOnly]);

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

  function handleSaved() {
    setDialogVisible(false);
    refresh();
  }

  function openCreateConditionalDialog() {
    setEditingConditional(null);
    setConditionalDialogVisible(true);
  }

  function handleConditionalSaved() {
    setConditionalDialogVisible(false);
    refresh();
  }

  async function handleRowDelete(row: PromotionRow) {
    setMessage(null);
    try {
      if (row.kind === "percentage") {
        await deletePromotion(row.id);
      } else {
        await deleteConditionalPromotion(row.id);
      }
      refresh();
    } catch (err) {
      setMessage(err instanceof ApiError ? "ไม่สามารถลบโปรโมชั่นได้" : "เกิดข้อผิดพลาด");
    }
  }

  function openRowEditDialog(row: PromotionRow) {
    if (row.kind === "percentage") {
      setEditingPromotion(row.promotion);
      setDialogVisible(true);
    } else {
      setEditingConditional(row.promotion);
      setConditionalDialogVisible(true);
    }
  }

  const rows: PromotionRow[] = [
    ...promotions.map((promotion): PromotionRow => ({
      kind: "percentage",
      id: promotion.id,
      promotion,
    })),
    ...conditionalPromotions.map((promotion): PromotionRow => ({
      kind: "conditional",
      id: promotion.id,
      promotion,
    })),
  ];

  function productName(productId: string | null): string {
    if (!productId) return "-";
    return products.find((p) => p.id === productId)?.name ?? productId;
  }

  const columns: DataTableColumn<PromotionRow>[] = [
    {
      header: "โปรโมชั่น",
      cell: (row) =>
        row.kind === "percentage" ? (
          <span className="money text-base font-medium">{row.promotion.discountPercentage}%</span>
        ) : (
          <div className="leading-tight">
            <p className="text-sm font-medium text-ink">{row.promotion.name}</p>
            {/* FR-008: the summary is composed by the server so this screen and
                the receipt can never word the same promotion differently. */}
            <p className="text-xs text-ink-500">{row.promotion.description}</p>
          </div>
        ),
    },
    {
      header: "ใช้กับ",
      cell: (row) =>
        row.kind === "percentage"
          ? row.promotion.scope === "Item"
            ? productName(row.promotion.productId)
            : "ทั้งบิล"
          : "ชุดสินค้าตามเงื่อนไข",
    },
    {
      header: "เงื่อนไข",
      cell: (row) => (row.promotion.appliesToMembersOnly ? "เฉพาะสมาชิก" : "ลูกค้าทุกคน"),
    },
    {
      header: "ช่วงวันที่",
      cell: (row) => (
        <span className="money whitespace-nowrap text-ink-500">
          {row.promotion.startDate} – {row.promotion.endDate}
        </span>
      ),
    },
    {
      header: "สถานะ",
      cell: (row) => {
        // FR-023: a promotion pointing at a deleted product can never fire
        // again, so it is called out here rather than failing silently at the
        // register.
        if (row.kind === "conditional" && !row.promotion.isUsable) {
          return (
            <span className="whitespace-nowrap text-sm text-chili">
              ใช้ไม่ได้ · {row.promotion.unusableReason}
            </span>
          );
        }

        return row.promotion.isActive ? (
          <span className="inline-flex items-center gap-1.5 whitespace-nowrap text-sm text-leaf">
            <span className="h-1.5 w-1.5 rounded-full bg-leaf" />
            ใช้อยู่
          </span>
        ) : (
          <span className="whitespace-nowrap text-sm text-ink-300">ยังไม่เริ่ม/หมดอายุ</span>
        );
      },
    },
    {
      header: "",
      className: "text-right",
      cell: (row) => (
        <div className="flex justify-end gap-2">
          <button
            type="button"
            onClick={() => openRowEditDialog(row)}
            className="btn btn-sm rounded-control border-steel-200 bg-white font-display font-medium text-ink-700 hover:border-ink hover:bg-white"
          >
            แก้ไข
          </button>
          <button
            type="button"
            onClick={() => handleRowDelete(row)}
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
        <h1 className="text-xl font-semibold">โปรโมชั่น</h1>
        <div className="flex flex-wrap gap-2">
          <button
            type="button"
            onClick={openCreateDialog}
            className="btn rounded-control border-steel-200 bg-white font-display font-medium text-ink-700 hover:border-ink hover:bg-white"
          >
            + ส่วนลดเปอร์เซ็นต์
          </button>
          <button
            type="button"
            onClick={openCreateConditionalDialog}
            className="btn rounded-control border-ink bg-ink font-display font-medium text-white hover:border-mango hover:bg-mango hover:text-ink"
          >
            + โปรโมชั่นแบบมีเงื่อนไข
          </button>
        </div>
      </div>

      {message && <p className="mb-4 text-sm text-chili">{message}</p>}

      <DataTable
        columns={columns}
        rows={rows}
        rowKey={(row) => `${row.kind}-${row.id}`}
        isLoading={isLoading}
        filterKey={String(activeOnly)}
        emptyText={
          activeOnly
            ? "ไม่มีโปรโมชั่นที่ใช้ได้ตอนนี้"
            : "ยังไม่มีโปรโมชั่น กดสร้างโปรโมชั่นเพื่อเริ่ม"
        }
      >
        <label className="flex w-fit cursor-pointer items-center gap-2 text-sm text-ink-700">
          <input
            type="checkbox"
            checked={activeOnly}
            onChange={(e) => setActiveOnly(e.target.checked)}
            className="checkbox checkbox-sm"
          />
          เฉพาะที่ใช้ได้ตอนนี้
        </label>
      </DataTable>

      <PromotionFormDialog
        visible={dialogVisible}
        promotion={editingPromotion}
        products={products}
        onHide={() => setDialogVisible(false)}
        onSaved={handleSaved}
      />

      <ConditionalPromotionFormDialog
        visible={conditionalDialogVisible}
        promotion={editingConditional}
        products={products}
        onHide={() => setConditionalDialogVisible(false)}
        onSaved={handleConditionalSaved}
      />
    </main>
  );
}
