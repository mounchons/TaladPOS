import { apiFetch, MAX_PAGE_SIZE, type PagedResult } from "./client";

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
function saleQuery(params: {
  from?: string;
  to?: string;
  staffId?: string;
  memberId?: string;
  page?: number;
  pageSize?: number;
}): string {
  const query = new URLSearchParams();
  if (params.from) query.set("from", params.from);
  if (params.to) query.set("to", params.to);
  if (params.staffId) query.set("staffId", params.staffId);
  if (params.memberId) query.set("memberId", params.memberId);
  if (params.page) query.set("page", String(params.page));
  if (params.pageSize) query.set("pageSize", String(params.pageSize));
  const qs = query.toString();
  return `/api/v1/sales${qs ? `?${qs}` : ""}`;
}

/**
 * contracts/sales.md - the paged form, for the history datagrid. Bills come
 * back newest first with a stable tiebreak, so paging never shows one bill
 * twice or skips another.
 */
export function searchSalesPaged(params: {
  from?: string;
  to?: string;
  staffId?: string;
  memberId?: string;
  page: number;
  pageSize: number;
}): Promise<PagedResult<Sale>> {
  return apiFetch<PagedResult<Sale>>(saleQuery(params));
}

/** As with searchProducts: pageSize is explicit so "all of them" stays true. */
export function searchSales(params: {
  from?: string;
  to?: string;
  staffId?: string;
  memberId?: string;
}): Promise<Sale[]> {
  return apiFetch<PagedResult<Sale>>(saleQuery({ ...params, pageSize: MAX_PAGE_SIZE })).then(
    (page) => page.items,
  );
}

// contracts/sales.md - GET /api/v1/sales/{id}/receipt (FR-030)
export function getReceipt(saleId: string): Promise<Sale> {
  return apiFetch<Sale>(`/api/v1/sales/${saleId}/receipt`);
}
