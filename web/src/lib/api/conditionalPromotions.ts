import { apiFetch } from "./client";

export type RewardKind = "Gift" | "Percentage";

export interface ConditionLine {
  productId: string;
  /** null when the product has since been deleted (003/FR-023). */
  productName: string | null;
  minimumQuantity: number;
}

export interface Reward {
  kind: RewardKind;
  giftProductId: string | null;
  giftProductName: string | null;
  giftQuantity: number | null;
  discountPercentage: number | null;
}

export interface ConditionalPromotion {
  id: string;
  name: string;
  conditionLines: ConditionLine[];
  reward: Reward;
  appliesToMembersOnly: boolean;
  startDate: string; // "YYYY-MM-DD"
  endDate: string;
  isActive: boolean;
  /**
   * The one-line summary shown in the table and on the receipt. Composed by the
   * server so the promotions screen and the receipt can never word it
   * differently (003/FR-008).
   */
  description: string;
  /** false when a product this promotion points at has been deleted (003/FR-023). */
  isUsable: boolean;
  unusableReason: string | null;
}

export interface ConditionLineInput {
  productId: string;
  minimumQuantity: number;
}

export interface RewardInput {
  kind: RewardKind;
  giftProductId: string | null;
  giftQuantity: number | null;
  discountPercentage: number | null;
}

export interface ConditionalPromotionInput {
  name: string;
  conditionLines: ConditionLineInput[];
  reward: RewardInput;
  appliesToMembersOnly: boolean;
  startDate: string;
  endDate: string;
}

// 003/contracts/conditional-promotions.md - Manager-only CRUD (FR-001-FR-009, 001/FR-029)
export function listConditionalPromotions(activeOnly = false): Promise<ConditionalPromotion[]> {
  const query = activeOnly ? "?activeOnly=true" : "";
  return apiFetch<ConditionalPromotion[]>(`/api/v1/conditional-promotions${query}`);
}

export function createConditionalPromotion(
  input: ConditionalPromotionInput,
): Promise<ConditionalPromotion> {
  return apiFetch<ConditionalPromotion>("/api/v1/conditional-promotions", {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export function updateConditionalPromotion(
  id: string,
  input: ConditionalPromotionInput,
): Promise<ConditionalPromotion> {
  return apiFetch<ConditionalPromotion>(`/api/v1/conditional-promotions/${id}`, {
    method: "PUT",
    body: JSON.stringify(input),
  });
}

export function deleteConditionalPromotion(id: string): Promise<void> {
  return apiFetch<void>(`/api/v1/conditional-promotions/${id}`, { method: "DELETE" });
}
