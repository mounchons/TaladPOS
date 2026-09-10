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
