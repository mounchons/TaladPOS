import { apiFetch, MAX_PAGE_SIZE, type PagedResult } from "./client";

export interface Product {
  id: string;
  name: string;
  imageUrl: string;
  price: number;
  barcode: string | null;
  stockQuantity: number;
  lowStockThreshold: number;
  isLowStock: boolean;
  isOutOfStock: boolean;
}

function productQuery(params: {
  search?: string;
  barcode?: string;
  lowStockOnly?: boolean;
  page?: number;
  pageSize?: number;
}): string {
  const query = new URLSearchParams();
  if (params.search) query.set("search", params.search);
  if (params.barcode) query.set("barcode", params.barcode);
  if (params.lowStockOnly) query.set("lowStockOnly", "true");
  if (params.page) query.set("page", String(params.page));
  if (params.pageSize) query.set("pageSize", String(params.pageSize));
  const qs = query.toString();
  return `/api/v1/products${qs ? `?${qs}` : ""}`;
}

/**
 * contracts/products.md - the paged form, for the stock datagrid.
 * `totalCount` counts the filtered set, so the pager stays honest as filters
 * change.
 */
export function searchProductsPaged(params: {
  search?: string;
  barcode?: string;
  lowStockOnly?: boolean;
  page: number;
  pageSize: number;
}): Promise<PagedResult<Product>> {
  return apiFetch<PagedResult<Product>>(productQuery(params));
}

/**
 * The unpaged view, for screens that show a shelf rather than a table (the
 * sales register, the promotion product picker).
 *
 * pageSize is sent explicitly at the API's ceiling rather than left to the
 * default of 20: the endpoint pages now, and a caller that meant "everything"
 * would otherwise get the first twenty rows and never know. Above 100 matches
 * this does truncate - those screens are driven by search, so the answer is to
 * narrow the search, not to raise the cap.
 */
export function searchProducts(params: {
  search?: string;
  barcode?: string;
  lowStockOnly?: boolean;
}): Promise<Product[]> {
  return apiFetch<PagedResult<Product>>(
    productQuery({ ...params, pageSize: MAX_PAGE_SIZE }),
  ).then((page) => page.items);
}

export interface ProductInput {
  name: string;
  imageUrl: string;
  price: number;
  barcode: string | null;
  stockQuantity: number;
  lowStockThreshold: number;
}

// contracts/products.md - POST/PUT/DELETE /api/v1/products (FR-015, FR-018, Manager only - FR-029)
export function createProduct(input: ProductInput): Promise<Product> {
  return apiFetch<Product>("/api/v1/products", { method: "POST", body: JSON.stringify(input) });
}

export function updateProduct(id: string, input: ProductInput): Promise<Product> {
  return apiFetch<Product>(`/api/v1/products/${id}`, { method: "PUT", body: JSON.stringify(input) });
}

export function deleteProduct(id: string): Promise<void> {
  return apiFetch<void>(`/api/v1/products/${id}`, { method: "DELETE" });
}
