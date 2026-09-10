import { apiFetch, fetchAllPages, type PagedResult } from "./client";

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
 * Every page is read, not just the first: the promotion dialog lists products
 * in a <select> with no search box of its own, so a truncated list means a
 * manager simply cannot pick product 101 and gets no hint that it exists.
 * FR-001 says the catalogue is what the shop sells, not the first hundred of
 * it.
 *
 * `pageSize` exists for tests that need to prove the walk really assembles
 * several pages; leave it alone in app code.
 */
export function searchProducts(
  params: {
    search?: string;
    barcode?: string;
    lowStockOnly?: boolean;
  },
  pageSize?: number,
): Promise<Product[]> {
  return fetchAllPages<Product>(
    (page, size) => productQuery({ ...params, page, pageSize: size }),
    pageSize,
  );
}

/**
 * The register search box: one field that has to answer both a scanner and a
 * cashier typing a name (FR-002).
 *
 * `search` and `barcode` are separate filters on the endpoint and the server
 * ANDs them, so sending the same term as both matched only products whose name
 * equalled their barcode - in practice, nothing. They are asked separately and
 * merged here. Barcode hits lead: if something was scanned, that is the
 * product the cashier is reaching for.
 */
export async function searchProductsByNameOrBarcode(term: string): Promise<Product[]> {
  const trimmed = term.trim();
  if (!trimmed) return searchProducts({});

  const [byBarcode, byName] = await Promise.all([
    searchProducts({ barcode: trimmed }),
    searchProducts({ search: trimmed }),
  ]);

  const merged = new Map(byBarcode.map((product) => [product.id, product]));
  for (const product of byName) {
    if (!merged.has(product.id)) merged.set(product.id, product);
  }
  return Array.from(merged.values());
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
