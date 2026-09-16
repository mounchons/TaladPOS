import { apiFetch, MAX_PAGE_SIZE, type PagedResult } from "./client";

export interface SaleLineItemDto {
  productId: string;
  productNameSnapshot: string;
  unitPriceSnapshot: number;
  quantity: number;
  discountAmount: number;
  lineTotal: number;
  /** 003/FR-016 - a promotional gift. Bills saved before 002 come back false. */
  isGift: boolean;
}

/** 003/FR-024 - which conditional promotion produced part of this bill's discount. */
export interface AppliedPromotion {
  promotionId: string;
  description: string;
  setCount: number;
  discountAmount: number;
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
  appliedPromotions: AppliedPromotion[];
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

/** 003/contracts/sales-preview.md - one line of a priced cart. */
export interface PricedLine {
  productId: string;
  productName: string;
  unitPrice: number;
  quantity: number;
  discountAmount: number;
  lineTotal: number;
  isGift: boolean;
}

/** 003/FR-015 - an earned gift that is not in the cart yet. */
export interface UnclaimedGift {
  promotionId: string;
  description: string;
  giftProductId: string;
  giftProductName: string;
  missingQuantity: number;
}

export interface PricedCart {
  lines: PricedLine[];
  appliedPromotions: AppliedPromotion[];
  unclaimedGifts: UnclaimedGift[];
  subtotalAmount: number;
  discountAmount: number;
  totalAmount: number;
}

/**
 * 003/contracts/sales-preview.md - POST /api/v1/sales/preview.
 *
 * Every number the register shows comes from here. The web app deliberately
 * does no discount arithmetic of its own: the server runs the same pricing code
 * for the preview and for the real checkout, which is the only way the total on
 * screen is guaranteed to be the total charged (003/FR-012, constitution
 * Principle I).
 */
export function previewSale(request: {
  memberId?: string | null;
  lineItems: CreateSaleLineItem[];
}): Promise<PricedCart> {
  return apiFetch<PricedCart>("/api/v1/sales/preview", {
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

// contracts/sales.md - GET /api/v1/sales/{id}/receipt (FR-030)
export function getReceipt(saleId: string): Promise<Sale> {
  return apiFetch<Sale>(`/api/v1/sales/${saleId}/receipt`);
}

/** FR-006 - the most bills a single export can cover; above this the user must narrow the filter. */
export const SALES_EXPORT_ROW_CAP = 10_000;

export type FetchAllSalesResult =
  | { status: "ok"; sales: Sale[] }
  | { status: "cap_exceeded"; totalCount: number };

/**
 * data-model.md §4 / research.md #3 - every bill matching the filter, not
 * just the current on-screen page (FR-004). Checks `totalCount` from the
 * first page before paging further: a filter that matches more than
 * SALES_EXPORT_ROW_CAP bills stops immediately instead of pulling all of it
 * only to discard it (FR-006 - no silent partial export either way).
 */
export async function fetchAllSalesForExport(filters: {
  from?: string;
  to?: string;
  staffId?: string;
  memberId?: string;
}): Promise<FetchAllSalesResult> {
  const first = await searchSalesPaged({ ...filters, page: 1, pageSize: MAX_PAGE_SIZE });

  if (first.totalCount > SALES_EXPORT_ROW_CAP) {
    return { status: "cap_exceeded", totalCount: first.totalCount };
  }

  const sales = [...first.items];
  for (let page = 2; page <= first.totalPages; page++) {
    const result = await searchSalesPaged({ ...filters, page, pageSize: MAX_PAGE_SIZE });
    sales.push(...result.items);
  }

  return { status: "ok", sales };
}
