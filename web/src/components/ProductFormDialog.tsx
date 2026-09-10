"use client";

import { useEffect, useState } from "react";
import { Dialog } from "primereact/dialog";
import { InputText } from "primereact/inputtext";
import { InputNumber } from "primereact/inputnumber";
import { Button } from "primereact/button";
import { dialogPT, inputTextPT, inputNumberPT, buttonPT, secondaryButtonPT } from "@/styles/primereact-passthrough";
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
      const input: ProductInput = { ...form, barcode: form.barcode?.trim() ? form.barcode.trim() : null };
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
    <Dialog
      visible={visible}
      onHide={onHide}
      header={product ? "แก้ไขสินค้า" : "เพิ่มสินค้าใหม่"}
      pt={dialogPT}
      modal
    >
      <form onSubmit={handleSubmit}>
        <label className="mb-1.5 block text-sm text-ink-700" htmlFor="product-name">
          ชื่อสินค้า
        </label>
        <InputText
          id="product-name"
          value={form.name}
          onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
          pt={inputTextPT}
          className="mb-3"
          required
        />

        <label className="mb-1.5 block text-sm text-ink-700" htmlFor="product-image">
          URL รูปภาพ
        </label>
        <InputText
          id="product-image"
          value={form.imageUrl}
          onChange={(e) => setForm((f) => ({ ...f, imageUrl: e.target.value }))}
          pt={inputTextPT}
          className="mb-3"
          required
        />

        <label className="mb-1.5 block text-sm text-ink-700" htmlFor="product-price">
          ราคา (บาท)
        </label>
        <InputNumber
          inputId="product-price"
          value={form.price}
          onValueChange={(e) => setForm((f) => ({ ...f, price: e.value ?? 0 }))}
          mode="decimal"
          minFractionDigits={2}
          maxFractionDigits={2}
          min={0.01}
          pt={inputNumberPT}
          className="mb-3"
        />

        <label className="mb-1.5 block text-sm text-ink-700" htmlFor="product-barcode">
          บาร์โค้ด (ไม่บังคับ)
        </label>
        <InputText
          id="product-barcode"
          value={form.barcode ?? ""}
          onChange={(e) => setForm((f) => ({ ...f, barcode: e.target.value }))}
          pt={inputTextPT}
          className="mb-3"
        />

        <div className="mb-3 grid grid-cols-2 gap-3">
          <div>
            <label className="mb-1.5 block text-sm text-ink-700" htmlFor="product-stock">
              จำนวนคงเหลือ
            </label>
            <InputNumber
              inputId="product-stock"
              value={form.stockQuantity}
              onValueChange={(e) => setForm((f) => ({ ...f, stockQuantity: e.value ?? 0 }))}
              min={0}
              pt={inputNumberPT}
            />
          </div>
          <div>
            <label className="mb-1.5 block text-sm text-ink-700" htmlFor="product-threshold">
              เกณฑ์ใกล้หมด
            </label>
            <InputNumber
              inputId="product-threshold"
              value={form.lowStockThreshold}
              onValueChange={(e) => setForm((f) => ({ ...f, lowStockThreshold: e.value ?? 0 }))}
              min={0}
              pt={inputNumberPT}
            />
          </div>
        </div>

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
