import { apiFetch } from "./client";

export type StaffRole = "Manager" | "Cashier";

export interface StaffSummary {
  id: string;
  name: string;
  role: StaffRole;
}

export interface LoginResponse {
  token: string;
  expiresAt: string;
  staff: StaffSummary;
}

// contracts/auth.md - POST /api/v1/auth/login (FR-007)
export function login(username: string, password: string): Promise<LoginResponse> {
  return apiFetch<LoginResponse>("/api/v1/auth/login", {
    method: "POST",
    body: JSON.stringify({ username, password }),
  });
}
