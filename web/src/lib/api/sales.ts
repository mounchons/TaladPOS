import { apiFetch } from "./client";

export interface SaleLineItemDto {
  productId: string;
  productNameSnapshot: string;
  unitPriceSnapshot: number;
  quantity: number;
  discountAmount: number;
  lineTotal: number;
}

export interface Sale {
  id: string;
  createdAt: string;
  staffId: string;
  memberId: string | null;
  lineItems: SaleLineItemDto[];
  subtotalAmount: number;
  discountAmount: number;
  totalAmount: number;
}

export interface CreateSaleLineItem {
  productId: string;
  quantity: number;
}

export function createSale(request: {
  memberId?: string | null;
  lineItems: CreateSaleLineItem[];
}): Promise<Sale> {
  return apiFetch<Sale>("/api/sales", {
    method: "POST",
    body: JSON.stringify({ memberId: request.memberId ?? null, lineItems: request.lineItems }),
  });
}
