import { apiFetch } from "./client";

export interface SalesReport {
  period: "daily" | "monthly";
  rangeStart: string;
  rangeEnd: string;
  totalSalesAmount: number;
  totalDiscountAmount: number;
  billCount: number;
}

export interface BestSellingProductRow {
  productId: string;
  productName: string;
  quantitySold: number;
  totalSalesAmount: number;
}

export interface SalesByStaffRow {
  staffId: string;
  staffName: string;
  billCount: number;
  totalSalesAmount: number;
}

export interface StockReportRow {
  productId: string;
  productName: string;
  stockQuantity: number;
  lowStockThreshold: number;
  isLowStock: boolean;
}

// contracts/reports.md - Manager-only (FR-025-FR-029)
export function getSalesReport(period: "daily" | "monthly", date: string): Promise<SalesReport> {
  return apiFetch<SalesReport>(`/api/v1/reports/sales?period=${period}&date=${date}`);
}

export function getBestSellingProducts(from: string, to: string, limit = 10): Promise<BestSellingProductRow[]> {
  return apiFetch<BestSellingProductRow[]>(
    `/api/v1/reports/best-selling-products?from=${from}&to=${to}&limit=${limit}`,
  );
}

export function getSalesByStaff(from: string, to: string): Promise<SalesByStaffRow[]> {
  return apiFetch<SalesByStaffRow[]>(`/api/v1/reports/sales-by-staff?from=${from}&to=${to}`);
}

export function getStockReport(): Promise<StockReportRow[]> {
  return apiFetch<StockReportRow[]>("/api/v1/reports/stock");
}
