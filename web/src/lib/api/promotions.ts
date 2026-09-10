import { apiFetch } from "./client";

export type PromotionScope = "Item" | "Bill";

export interface Promotion {
  id: string;
  scope: PromotionScope;
  discountPercentage: number;
  productId: string | null;
  appliesToMembersOnly: boolean;
  startDate: string; // "YYYY-MM-DD"
  endDate: string;
  isActive: boolean;
}

export interface PromotionInput {
  scope: PromotionScope;
  discountPercentage: number;
  productId: string | null;
  appliesToMembersOnly: boolean;
  startDate: string;
  endDate: string;
}

// contracts/promotions.md - Manager-only CRUD (FR-019-FR-022, FR-029)
export function listPromotions(activeOnly = false): Promise<Promotion[]> {
  const query = activeOnly ? "?activeOnly=true" : "";
  return apiFetch<Promotion[]>(`/api/v1/promotions${query}`);
}

export function createPromotion(input: PromotionInput): Promise<Promotion> {
  return apiFetch<Promotion>("/api/v1/promotions", { method: "POST", body: JSON.stringify(input) });
}

export function updatePromotion(id: string, input: PromotionInput): Promise<Promotion> {
  return apiFetch<Promotion>(`/api/v1/promotions/${id}`, { method: "PUT", body: JSON.stringify(input) });
}

export function deletePromotion(id: string): Promise<void> {
  return apiFetch<void>(`/api/v1/promotions/${id}`, { method: "DELETE" });
}
