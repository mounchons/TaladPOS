import { apiFetch } from "./client";

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

export function searchProducts(params: {
  search?: string;
  barcode?: string;
  lowStockOnly?: boolean;
}): Promise<Product[]> {
  const query = new URLSearchParams();
  if (params.search) query.set("search", params.search);
  if (params.barcode) query.set("barcode", params.barcode);
  if (params.lowStockOnly) query.set("lowStockOnly", "true");
  const qs = query.toString();
  return apiFetch<Product[]>(`/api/products${qs ? `?${qs}` : ""}`);
}

export interface ProductInput {
  name: string;
  imageUrl: string;
  price: number;
  barcode: string | null;
  stockQuantity: number;
  lowStockThreshold: number;
}

// contracts/products.md - POST/PUT/DELETE /api/products (FR-015, FR-018, Manager only - FR-029)
export function createProduct(input: ProductInput): Promise<Product> {
  return apiFetch<Product>("/api/products", { method: "POST", body: JSON.stringify(input) });
}

export function updateProduct(id: string, input: ProductInput): Promise<Product> {
  return apiFetch<Product>(`/api/products/${id}`, { method: "PUT", body: JSON.stringify(input) });
}

export function deleteProduct(id: string): Promise<void> {
  return apiFetch<void>(`/api/products/${id}`, { method: "DELETE" });
}
