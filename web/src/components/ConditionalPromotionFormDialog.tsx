"use client";

import { useEffect, useState } from "react";
import { Modal, modalButton } from "@/components/Modal";
import {
  createConditionalPromotion,
  updateConditionalPromotion,
  type ConditionalPromotion,
  type ConditionalPromotionInput,
  type RewardKind,
} from "@/lib/api/conditionalPromotions";
import type { Product } from "@/lib/api/products";
import { ApiError } from "@/lib/api/client";

interface Props {
  visible: boolean;
  promotion: ConditionalPromotion | null; // null = create mode
  products: Product[];
  onHide: () => void;
  onSaved: () => void;
}

function toDateOnly(date: Date): string {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, "0");
  const d = String(date.getDate()).padStart(2, "0");
  return `${y}-${m}-${d}`;
}

function emptyForm(products: Product[]): ConditionalPromotionInput {
  const today = toDateOnly(new Date());
  return {
    name: "",
    conditionLines: [{ productId: products[0]?.id ?? "", minimumQuantity: 1 }],
    reward: {
      kind: "Gift",
      giftProductId: products[0]?.id ?? null,
      giftQuantity: 1,
      discountPercentage: null,
    },
    appliesToMembersOnly: false,
    startDate: today,
    endDate: today,
  };
}

/**
 * 003/FR-001 - FR-009. A separate dialog from PromotionFormDialog rather than a
 * mode inside it: the two promotion kinds share a date range and a members-only
 * flag and nothing else, so one form holding both would be mostly branches. The
 * promotions screen offers both, which is what FR-008 actually asks for.
 *
 * The product dropdowns use the `products` prop the screen already loads, which
 * walks every page of the catalogue - so every product is selectable (FR-009,
 * 001/FR-037) without this dialog fetching anything itself.
 */
export function ConditionalPromotionFormDialog({
  visible,
  promotion,
  products,
  onHide,
  onSaved,
}: Props) {
  const [form, setForm] = useState<ConditionalPromotionInput>(emptyForm(products));
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!visible) return;

    setForm(
      promotion
        ? {
            name: promotion.name,
            conditionLines: promotion.conditionLines.map((line) => ({
              productId: line.productId,
              minimumQuantity: line.minimumQuantity,
            })),
            reward: {
              kind: promotion.reward.kind,
              giftProductId: promotion.reward.giftProductId,
              giftQuantity: promotion.reward.giftQuantity,
              discountPercentage: promotion.reward.discountPercentage,
            },
            appliesToMembersOnly: promotion.appliesToMembersOnly,
            startDate: promotion.startDate,
            endDate: promotion.endDate,
          }
        : emptyForm(products),
    );
    setError(null);
  }, [visible, promotion, products]);

  function setRewardKind(kind: RewardKind) {
    setForm((f) => ({
      ...f,
      reward:
        kind === "Gift"
          ? {
              kind,
              giftProductId: f.reward.giftProductId ?? products[0]?.id ?? null,
              giftQuantity: f.reward.giftQuantity ?? 1,
              discountPercentage: null,
            }
          : {
              kind,
              giftProductId: null,
              giftQuantity: null,
              discountPercentage: f.reward.discountPercentage ?? 10,
            },
    }));
  }

  function updateLine(index: number, patch: Partial<{ productId: string; minimumQuantity: number }>) {
    setForm((f) => ({
      ...f,
      conditionLines: f.conditionLines.map((line, i) => (i === index ? { ...line, ...patch } : line)),
    }));
  }

  function addLine() {
    setForm((f) => ({
      ...f,
      conditionLines: [
        ...f.conditionLines,
        // Default to a product not already in the condition, because FR-003
        // forbids the same product twice and the server would reject it.
        {
          productId:
            products.find((p) => !f.conditionLines.some((line) => line.productId === p.id))?.id ?? "",
          minimumQuantity: 1,
        },
      ],
    }));
  }

  function removeLine(index: number) {
    setForm((f) => ({
      ...f,
      conditionLines: f.conditionLines.filter((_, i) => i !== index),
    }));
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setIsSubmitting(true);
    try {
      if (promotion) {
        await updateConditionalPromotion(promotion.id, form);
      } else {
        await createConditionalPromotion(form);
      }
      onSaved();
    } catch (err) {
      setError(messageFor(err));
    } finally {
      setIsSubmitting(false);
    }
  }

  const duplicateProduct =
    new Set(form.conditionLines.map((line) => line.productId)).size !== form.conditionLines.length;

  return (
    <Modal
      visible={visible}
      onHide={onHide}
      title={promotion ? "แก้ไขโปรโมชั่นแบบมีเงื่อนไข" : "สร้างโปรโมชั่นแบบมีเงื่อนไข"}
      footer={
        <>
          <button type="button" onClick={onHide} className={modalButton.secondary}>
            ยกเลิก
          </button>
          <button
            type="submit"
            form="conditional-promotion-form"
            disabled={isSubmitting}
            className={modalButton.primary}
          >
            {isSubmitting ? "กำลังบันทึก..." : "บันทึก"}
          </button>
        </>
      }
    >
      <form id="conditional-promotion-form" onSubmit={handleSubmit}>
        <label className="mb-1.5 block text-sm text-ink-700" htmlFor="cp-name">
          ชื่อโปรโมชั่น
        </label>
        <input
          id="cp-name"
          type="text"
          value={form.name}
          maxLength={100}
          required
          onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
          placeholder="เช่น ซื้อคู่สุดคุ้ม"
          className="input mb-4 w-full rounded-control border-steel-200 bg-white"
        />

        <p className="mb-1.5 text-sm text-ink-700">เงื่อนไขการซื้อ</p>
        <div className="mb-2 space-y-2">
          {form.conditionLines.map((line, index) => (
            <div key={index} className="flex items-center gap-2">
              <select
                value={line.productId}
                onChange={(e) => updateLine(index, { productId: e.target.value })}
                className="select min-w-0 flex-1 rounded-control border-steel-200 bg-white"
                aria-label={`สินค้าในเงื่อนไขรายการที่ ${index + 1}`}
              >
                {products.map((product) => (
                  <option key={product.id} value={product.id}>
                    {product.name}
                  </option>
                ))}
              </select>
              <input
                type="number"
                min={1}
                value={line.minimumQuantity}
                onChange={(e) =>
                  updateLine(index, { minimumQuantity: Math.max(1, Number(e.target.value) || 1) })
                }
                aria-label={`จำนวนขั้นต่ำรายการที่ ${index + 1}`}
                className="input money w-20 rounded-control border-steel-200 bg-white text-center"
              />
              <button
                type="button"
                onClick={() => removeLine(index)}
                disabled={form.conditionLines.length <= 1}
                aria-label={`ลบเงื่อนไขรายการที่ ${index + 1}`}
                className="btn btn-sm border-steel-200 bg-white text-ink-500 disabled:opacity-40"
              >
                ลบ
              </button>
            </div>
          ))}
        </div>
        {/* min-h on phones is spelled out here rather than inherited: the global
            rule in globals.css raises .btn/.input/.select to 44px, and this is a
            bare text button that carries none of those classes, so it came out
            20px tall - well under what a fingertip needs (001/FR-033). */}
        <button
          type="button"
          onClick={addLine}
          className="mb-4 flex min-h-[44px] items-center text-sm text-mango-600 hover:text-mango md:min-h-0"
        >
          + เพิ่มสินค้าในเงื่อนไข
        </button>

        {duplicateProduct && (
          <p className="mb-4 text-sm text-chili">ห้ามระบุสินค้าเดียวกันซ้ำในเงื่อนไขเดียวกัน</p>
        )}

        <p className="mb-1.5 text-sm text-ink-700">สิ่งตอบแทน</p>
        <div className="mb-3 flex gap-2">
          {(["Gift", "Percentage"] as RewardKind[]).map((kind) => (
            <button
              key={kind}
              type="button"
              onClick={() => setRewardKind(kind)}
              className={
                "btn btn-sm rounded-control " +
                (form.reward.kind === kind
                  ? "border-mango bg-mango text-ink"
                  : "border-steel-200 bg-white text-ink-500")
              }
            >
              {kind === "Gift" ? "แถมสินค้า" : "ลดเป็นเปอร์เซ็นต์"}
            </button>
          ))}
        </div>

        {form.reward.kind === "Gift" ? (
          <div className="mb-4 flex items-center gap-2">
            <select
              value={form.reward.giftProductId ?? ""}
              onChange={(e) =>
                setForm((f) => ({ ...f, reward: { ...f.reward, giftProductId: e.target.value } }))
              }
              aria-label="สินค้าที่แถม"
              className="select min-w-0 flex-1 rounded-control border-steel-200 bg-white"
            >
              {products.map((product) => (
                <option key={product.id} value={product.id}>
                  {product.name}
                </option>
              ))}
            </select>
            <input
              type="number"
              min={1}
              value={form.reward.giftQuantity ?? 1}
              onChange={(e) =>
                setForm((f) => ({
                  ...f,
                  reward: { ...f.reward, giftQuantity: Math.max(1, Number(e.target.value) || 1) },
                }))
              }
              aria-label="จำนวนที่แถม"
              className="input money w-20 rounded-control border-steel-200 bg-white text-center"
            />
          </div>
        ) : (
          <div className="mb-4 flex items-center gap-2">
            <input
              type="number"
              min={0.01}
              max={100}
              step={0.01}
              value={form.reward.discountPercentage ?? 10}
              onChange={(e) =>
                setForm((f) => ({
                  ...f,
                  reward: { ...f.reward, discountPercentage: Number(e.target.value) },
                }))
              }
              aria-label="เปอร์เซ็นต์ส่วนลด"
              className="input money w-28 rounded-control border-steel-200 bg-white text-center"
            />
            <span className="text-sm text-ink-500">% ของมูลค่าสินค้าในชุด</span>
          </div>
        )}

        <label className="mb-4 flex items-center gap-2 text-sm text-ink-700">
          <input
            type="checkbox"
            checked={form.appliesToMembersOnly}
            onChange={(e) => setForm((f) => ({ ...f, appliesToMembersOnly: e.target.checked }))}
            className="checkbox checkbox-sm"
          />
          เฉพาะสมาชิกเท่านั้น
        </label>

        <div className="flex gap-3">
          <div className="flex-1">
            <label className="mb-1.5 block text-sm text-ink-700" htmlFor="cp-start">
              วันเริ่มต้น
            </label>
            <input
              id="cp-start"
              type="date"
              value={form.startDate}
              required
              onChange={(e) => setForm((f) => ({ ...f, startDate: e.target.value }))}
              className="input w-full rounded-control border-steel-200 bg-white"
            />
          </div>
          <div className="flex-1">
            <label className="mb-1.5 block text-sm text-ink-700" htmlFor="cp-end">
              วันสิ้นสุด
            </label>
            <input
              id="cp-end"
              type="date"
              value={form.endDate}
              required
              onChange={(e) => setForm((f) => ({ ...f, endDate: e.target.value }))}
              className="input w-full rounded-control border-steel-200 bg-white"
            />
          </div>
        </div>

        {error && <p className="mt-4 text-sm text-chili">{error}</p>}
      </form>
    </Modal>
  );
}

/**
 * The server answers with the contract's error codes, so the screen can say
 * what is actually wrong instead of "something went wrong".
 */
function messageFor(err: unknown): string {
  if (!(err instanceof ApiError)) return "เกิดข้อผิดพลาด ไม่สามารถบันทึกโปรโมชั่นได้";

  const code = (err.body as { error?: string } | undefined)?.error;
  switch (code) {
    case "name_required":
      return "กรุณาตั้งชื่อโปรโมชั่น";
    case "name_too_long":
      return "ชื่อโปรโมชั่นต้องยาวไม่เกิน 100 ตัวอักษร";
    case "condition_lines_required":
      return "ต้องระบุสินค้าในเงื่อนไขอย่างน้อย 1 รายการ";
    case "duplicate_condition_product":
      return "ห้ามระบุสินค้าเดียวกันซ้ำในเงื่อนไขเดียวกัน";
    case "invalid_minimum_quantity":
      return "จำนวนขั้นต่ำต้องเป็นจำนวนเต็มตั้งแต่ 1 ขึ้นไป";
    case "gift_reward_incomplete":
      return "กรุณาเลือกสินค้าที่แถมและระบุจำนวนตั้งแต่ 1 ขึ้นไป";
    case "invalid_discount_percentage":
      return "ส่วนลดต้องมากกว่า 0 และไม่เกิน 100";
    case "invalid_date_range":
      return "วันสิ้นสุดต้องไม่อยู่ก่อนวันเริ่มต้น";
    case "product_not_found":
      return "สินค้าที่เลือกไม่มีอยู่ในระบบแล้ว";
    default:
      return "ข้อมูลไม่ถูกต้อง กรุณาตรวจสอบอีกครั้ง";
  }
}
