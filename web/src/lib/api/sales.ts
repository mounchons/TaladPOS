import { apiFetch } from "./client";

export interface SaleLineItemDto {
  productId: string;
  productNameSnapshot: string;
  unitPriceSnapshot: number;
  quantity: number;
  discountAmount: number;
  lineTotal: number;
}

export interface StaffSummary {
  id: string;
  name: string;
}

export interface MemberSummary {
  id: string;
  name: string;
}

export interface Sale {
  id: string;
  createdAt: string;
  staff: StaffSummary | null;
  member: MemberSummary | null;
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
  return apiFetch<Sale>("/api/v1/sales", {
    method: "POST",
    body: JSON.stringify({ memberId: request.memberId ?? null, lineItems: request.lineItems }),
  });
}

// contracts/sales.md - GET /api/v1/sales (FR-024)
export function searchSales(params: {
  from?: string;
  to?: string;
  staffId?: string;
  memberId?: string;
}): Promise<Sale[]> {
  const query = new URLSearchParams();
  if (params.from) query.set("from", params.from);
  if (params.to) query.set("to", params.to);
  if (params.staffId) query.set("staffId", params.staffId);
  if (params.memberId) query.set("memberId", params.memberId);
  const qs = query.toString();
  return apiFetch<Sale[]>(`/api/v1/sales${qs ? `?${qs}` : ""}`);
}

// contracts/sales.md - GET /api/v1/sales/{id}/receipt (FR-030)
export function getReceipt(saleId: string): Promise<Sale> {
  return apiFetch<Sale>(`/api/v1/sales/${saleId}/receipt`);
}
