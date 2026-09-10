"use client";

import { useEffect, useState } from "react";
import { Dialog } from "primereact/dialog";
import { Dropdown } from "primereact/dropdown";
import { InputNumber } from "primereact/inputnumber";
import { Calendar } from "primereact/calendar";
import { Checkbox } from "primereact/checkbox";
import { Button } from "primereact/button";
import {
  dialogPT,
  dropdownPT,
  inputNumberPT,
  calendarPT,
  checkboxPT,
  buttonPT,
  secondaryButtonPT,
} from "@/styles/primereact-passthrough";
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

function fromDateOnly(value: string): Date {
  const [y, m, d] = value.split("-").map(Number);
  return new Date(y, m - 1, d);
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
    <Dialog
      visible={visible}
      onHide={onHide}
      header={promotion ? "แก้ไขโปรโมชั่น" : "สร้างโปรโมชั่นใหม่"}
      pt={dialogPT}
      modal
    >
      <form onSubmit={handleSubmit}>
        <label className="mb-1.5 block text-sm text-ink-700">ขอบเขต</label>
        <Dropdown
          value={form.scope}
          options={scopeOptions}
          onChange={(e) => setForm((f) => ({ ...f, scope: e.value }))}
          pt={dropdownPT}
          className="mb-3"
        />

        {form.scope === "Item" && (
          <>
            <label className="mb-1.5 block text-sm text-ink-700">สินค้า</label>
            <Dropdown
              value={form.productId}
              options={products.map((p) => ({ label: p.name, value: p.id }))}
              onChange={(e) => setForm((f) => ({ ...f, productId: e.value }))}
              placeholder="เลือกสินค้า"
              pt={dropdownPT}
              className="mb-3"
            />
          </>
        )}

        <label className="mb-1.5 block text-sm text-ink-700">ส่วนลด (%)</label>
        <InputNumber
          value={form.discountPercentage}
          onValueChange={(e) => setForm((f) => ({ ...f, discountPercentage: e.value ?? 0 }))}
          min={0.01}
          max={100}
          maxFractionDigits={2}
          pt={inputNumberPT}
          className="mb-3"
        />

        <div className="mb-3 grid grid-cols-1 gap-3 sm:grid-cols-2">
          <div>
            <label className="mb-1.5 block text-sm text-ink-700">วันที่เริ่ม</label>
            <Calendar
              value={fromDateOnly(form.startDate)}
              onChange={(e) => e.value && setForm((f) => ({ ...f, startDate: toDateOnly(e.value as Date) }))}
              dateFormat="dd/mm/yy"
              pt={calendarPT}
            />
          </div>
          <div>
            <label className="mb-1.5 block text-sm text-ink-700">วันที่สิ้นสุด</label>
            <Calendar
              value={fromDateOnly(form.endDate)}
              onChange={(e) => e.value && setForm((f) => ({ ...f, endDate: toDateOnly(e.value as Date) }))}
              dateFormat="dd/mm/yy"
              pt={calendarPT}
            />
          </div>
        </div>

        <label className="mb-4 flex items-center gap-2 text-sm text-ink-700">
          <Checkbox
            checked={form.appliesToMembersOnly}
            onChange={(e) => setForm((f) => ({ ...f, appliesToMembersOnly: e.checked ?? false }))}
            pt={checkboxPT}
          />
          ส่วนลดสำหรับสมาชิกเท่านั้น
        </label>

        {error && <p className="mb-3 rounded-control border border-chili/30 bg-chili/5 px-3 py-2.5 text-sm text-chili">{error}</p>}

        <div className="mt-4 flex justify-end gap-2">
          <Button type="button" label="ยกเลิก" onClick={onHide} pt={secondaryButtonPT} />
          <Button
            type="submit"
            label={isSubmitting ? "กำลังบันทึก..." : "บันทึก"}
            disabled={isSubmitting}
            pt={buttonPT}
          />
        </div>
      </form>
    </Dialog>
  );
}
