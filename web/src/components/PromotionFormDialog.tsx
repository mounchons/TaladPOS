"use client";

import { useEffect, useState } from "react";
import { Modal, modalButton } from "@/components/Modal";
import {
  createPromotion,
  updatePromotion,
  type Promotion,
  type PromotionInput,
  type PromotionScope,
} from "@/lib/api/promotions";
import type { Product } from "@/lib/api/products";
import { ApiError } from "@/lib/api/client";

interface PromotionFormDialogProps {
  visible: boolean;
  promotion: Promotion | null; // null = create mode
  products: Product[];
  onHide: () => void;
  onSaved: () => void;
}

const scopeOptions: { label: string; value: PromotionScope }[] = [
  { label: "รายสินค้า", value: "Item" },
  { label: "ทั้งบิล", value: "Bill" },
];

function toDateOnly(date: Date): string {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, "0");
  const d = String(date.getDate()).padStart(2, "0");
  return `${y}-${m}-${d}`;
}

const today = new Date();

function emptyForm(): PromotionInput {
  return {
    scope: "Item",
    discountPercentage: 10,
    productId: null,
    appliesToMembersOnly: false,
    startDate: toDateOnly(today),
    endDate: toDateOnly(today),
  };
}

// tasks.md T065 (US5): create/edit form for web/src/app/promotions/page.tsx.
export function PromotionFormDialog({ visible, promotion, products, onHide, onSaved }: PromotionFormDialogProps) {
  const [form, setForm] = useState<PromotionInput>(emptyForm());
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (visible) {
      setForm(
        promotion
          ? {
              scope: promotion.scope,
              discountPercentage: promotion.discountPercentage,
              productId: promotion.productId,
              appliesToMembersOnly: promotion.appliesToMembersOnly,
              startDate: promotion.startDate,
              endDate: promotion.endDate,
            }
          : emptyForm(),
      );
      setError(null);
    }
  }, [visible, promotion]);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setIsSubmitting(true);
    try {
      const input: PromotionInput = { ...form, productId: form.scope === "Item" ? form.productId : null };
      if (promotion) {
        await updatePromotion(promotion.id, input);
      } else {
        await createPromotion(input);
      }
      onSaved();
    } catch (err) {
      if (err instanceof ApiError && err.status === 400) {
        setError("ข้อมูลไม่ถูกต้อง กรุณาตรวจสอบวันที่และเปอร์เซ็นต์ส่วนลด");
      } else {
        setError("เกิดข้อผิดพลาด ไม่สามารถบันทึกโปรโมชั่นได้");
      }
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <Modal
      visible={visible}
      onHide={onHide}
      title={promotion ? "แก้ไขโปรโมชั่น" : "สร้างโปรโมชั่นใหม่"}
      footer={
        <>
          <button type="button" onClick={onHide} className={modalButton.secondary}>
            ยกเลิก
          </button>
          <button
            type="submit"
            form="promotion-form"
            disabled={isSubmitting}
            className={modalButton.primary}
          >
            {isSubmitting ? "กำลังบันทึก..." : "บันทึก"}
          </button>
        </>
      }
    >
      <form id="promotion-form" onSubmit={handleSubmit}>
        <label className="mb-1.5 block text-sm text-ink-700">ขอบเขต</label>
        <select
          value={form.scope}
          onChange={(e) => setForm((f) => ({ ...f, scope: e.target.value as PromotionInput["scope"] }))}
          className="select mb-3 w-full rounded-control border-steel-200 bg-white"
        >
          {scopeOptions.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </select>

        {form.scope === "Item" && (
          <>
            <label className="mb-1.5 block text-sm text-ink-700">สินค้า</label>
            <select
              value={form.productId ?? ""}
              onChange={(e) => setForm((f) => ({ ...f, productId: e.target.value || null }))}
              className="select mb-3 w-full rounded-control border-steel-200 bg-white"
            >
              <option value="">เลือกสินค้า</option>
              {products.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.name}
                </option>
              ))}
            </select>
          </>
        )}

        <label className="mb-1.5 block text-sm text-ink-700">ส่วนลด (%)</label>
        <input
          type="number"
          inputMode="decimal"
          step="0.01"
          min="0.01"
          max="100"
          value={form.discountPercentage}
          onChange={(e) =>
            setForm((f) => ({ ...f, discountPercentage: Number(e.target.value) || 0 }))
          }
          className="input money mb-3 w-full rounded-control border-steel-200 bg-white"
          required
        />

        <div className="mb-3 grid grid-cols-1 gap-3 sm:grid-cols-2">
          <div>
            <label className="mb-1.5 block text-sm text-ink-700">วันที่เริ่ม</label>
            {/* A native date input: the OS picker is what a manager already
                knows on a tablet, and its value is the ISO yyyy-mm-dd the API
                wants, so no formatting round trip. */}
            <input
              type="date"
              value={form.startDate}
              onChange={(e) => e.target.value && setForm((f) => ({ ...f, startDate: e.target.value }))}
              className="input money w-full rounded-control border-steel-200 bg-white"
              required
            />
          </div>
          <div>
            <label className="mb-1.5 block text-sm text-ink-700">วันที่สิ้นสุด</label>
            <input
              type="date"
              value={form.endDate}
              // The API is the authority on start <= end (it answers 400), but
              // min here stops the impossible range being submitted at all.
              min={form.startDate}
              onChange={(e) => e.target.value && setForm((f) => ({ ...f, endDate: e.target.value }))}
              className="input money w-full rounded-control border-steel-200 bg-white"
              required
            />
          </div>
        </div>

        <label className="mb-4 flex items-center gap-2 text-sm text-ink-700">
          <input
            type="checkbox"
            checked={form.appliesToMembersOnly}
            onChange={(e) => setForm((f) => ({ ...f, appliesToMembersOnly: e.target.checked }))}
            className="checkbox checkbox-sm"
          />
          ส่วนลดสำหรับสมาชิกเท่านั้น
        </label>

        {error && (
          <p className="rounded-control border border-chili/30 bg-chili/5 px-3 py-2.5 text-sm text-chili">
            {error}
          </p>
        )}
      </form>
    </Modal>
  );
}
