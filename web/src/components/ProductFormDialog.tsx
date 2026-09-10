"use client";

import { useEffect, useState } from "react";
import { Modal, modalButton } from "@/components/Modal";
import { createProduct, updateProduct, type Product, type ProductInput } from "@/lib/api/products";
import { ApiError } from "@/lib/api/client";

interface ProductFormDialogProps {
  visible: boolean;
  product: Product | null; // null = create mode, otherwise edit mode
  onHide: () => void;
  onSaved: () => void;
}

const emptyForm: ProductInput = {
  name: "",
  imageUrl: "",
  price: 0,
  barcode: null,
  stockQuantity: 0,
  lowStockThreshold: 5,
};

const FIELD = "input w-full rounded-control border-steel-200 bg-white";
const LABEL = "mb-1.5 block text-sm text-ink-700";

// tasks.md T044 (US3): create/edit form used by web/src/app/stock/page.tsx.
export function ProductFormDialog({ visible, product, onHide, onSaved }: ProductFormDialogProps) {
  const [form, setForm] = useState<ProductInput>(emptyForm);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (visible) {
      setForm(
        product
          ? {
              name: product.name,
              imageUrl: product.imageUrl,
              price: product.price,
              barcode: product.barcode,
              stockQuantity: product.stockQuantity,
              lowStockThreshold: product.lowStockThreshold,
            }
          : emptyForm,
      );
      setError(null);
    }
  }, [visible, product]);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setIsSubmitting(true);
    try {
      const input: ProductInput = {
        ...form,
        barcode: form.barcode?.trim() ? form.barcode.trim() : null,
      };
      if (product) {
        await updateProduct(product.id, input);
      } else {
        await createProduct(input);
      }
      onSaved();
    } catch (err) {
      if (err instanceof ApiError && err.status === 409) {
        setError("บาร์โค้ดนี้ถูกใช้กับสินค้าอื่นแล้ว");
      } else if (err instanceof ApiError && err.status === 400) {
        setError("ข้อมูลไม่ถูกต้อง กรุณาตรวจสอบราคาและจำนวน");
      } else {
        setError("เกิดข้อผิดพลาด ไม่สามารถบันทึกสินค้าได้");
      }
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <Modal
      visible={visible}
      onHide={onHide}
      title={product ? "แก้ไขสินค้า" : "เพิ่มสินค้าใหม่"}
      footer={
        <>
          <button type="button" onClick={onHide} className={modalButton.secondary}>
            ยกเลิก
          </button>
          {/* Outside the <form>, so it is wired back to it by id - the footer
              is a sibling of the form in the dialog's layout. */}
          <button
            type="submit"
            form="product-form"
            disabled={isSubmitting}
            className={modalButton.primary}
          >
            {isSubmitting ? "กำลังบันทึก..." : "บันทึก"}
          </button>
        </>
      }
    >
      <form id="product-form" onSubmit={handleSubmit}>
        <label className={LABEL} htmlFor="product-name">
          ชื่อสินค้า
        </label>
        <input
          id="product-name"
          type="text"
          value={form.name}
          onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
          className={`${FIELD} mb-3`}
          required
        />

        <label className={LABEL} htmlFor="product-image">
          URL รูปภาพ
        </label>
        <input
          id="product-image"
          type="url"
          value={form.imageUrl}
          onChange={(e) => setForm((f) => ({ ...f, imageUrl: e.target.value }))}
          className={`${FIELD} mb-3`}
          required
        />

        <label className={LABEL} htmlFor="product-price">
          ราคา (บาท)
        </label>
        {/* step="0.01" and min="0.01" carry the same rule the API enforces
            (price > 0, two decimals) into the browser's own validation, so a
            bad price is caught before the request rather than by a 400. */}
        <input
          id="product-price"
          type="number"
          inputMode="decimal"
          step="0.01"
          min="0.01"
          value={form.price}
          onChange={(e) => setForm((f) => ({ ...f, price: Number(e.target.value) || 0 }))}
          className={`${FIELD} money mb-3`}
          required
        />

        <label className={LABEL} htmlFor="product-barcode">
          บาร์โค้ด (ไม่บังคับ)
        </label>
        <input
          id="product-barcode"
          type="text"
          value={form.barcode ?? ""}
          onChange={(e) => setForm((f) => ({ ...f, barcode: e.target.value }))}
          className={`${FIELD} money mb-3`}
        />

        <div className="mb-3 grid grid-cols-1 gap-3 sm:grid-cols-2">
          <div>
            <label className={LABEL} htmlFor="product-stock">
              จำนวนคงเหลือ
            </label>
            <input
              id="product-stock"
              type="number"
              inputMode="numeric"
              min="0"
              value={form.stockQuantity}
              onChange={(e) =>
                setForm((f) => ({ ...f, stockQuantity: Number(e.target.value) || 0 }))
              }
              className={`${FIELD} money`}
            />
          </div>
          <div>
            <label className={LABEL} htmlFor="product-threshold">
              เกณฑ์ใกล้หมด
            </label>
            <input
              id="product-threshold"
              type="number"
              inputMode="numeric"
              min="0"
              value={form.lowStockThreshold}
              onChange={(e) =>
                setForm((f) => ({ ...f, lowStockThreshold: Number(e.target.value) || 0 }))
              }
              className={`${FIELD} money`}
            />
          </div>
        </div>

        {error && (
          <p className="rounded-control border border-chili/30 bg-chili/5 px-3 py-2.5 text-sm text-chili">
            {error}
          </p>
        )}
      </form>
    </Modal>
  );
}
