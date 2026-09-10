import { apiFetch } from "./client";

export interface Member {
  id: string;
  name: string;
  phoneNumber: string;
  accumulatedPurchaseTotal: number;
}

// contracts/members.md - GET/POST /api/members (FR-010-FR-012)
export function searchMembers(search: string): Promise<Member[]> {
  const query = new URLSearchParams({ search });
  return apiFetch<Member[]>(`/api/members?${query.toString()}`);
}

export function registerMember(input: { name: string; phoneNumber: string }): Promise<Member> {
  return apiFetch<Member>("/api/members", { method: "POST", body: JSON.stringify(input) });
}
