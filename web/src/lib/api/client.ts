import { getToken } from "./tokenStore";

// web/ talks to api/ exclusively over REST (constitution Principle I) - this
// is the only place that builds request URLs / headers for that boundary.
// Fallback matches the `http` launch profile in
// api/src/TaladPOS.Api/Properties/launchSettings.json (quickstart.md step 1),
// so a checkout with no .env.local still points at a port `dotnet run`
// actually listens on.
const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5054";

/**
 * The envelope every paged endpoint returns (contracts/products.md,
 * contracts/sales.md). Declared here rather than in one of the resource
 * modules so products.ts and sales.ts import the same shape instead of
 * declaring it twice and drifting apart.
 */
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

/** The API rejects anything above this with 400 invalid_pagination. */
export const MAX_PAGE_SIZE = 100;

export class ApiError extends Error {
  constructor(
    public status: number,
    public body: unknown,
  ) {
    super(`API request failed with status ${status}`);
  }
}

export async function apiFetch<T>(path: string, init: RequestInit = {}): Promise<T> {
  const token = getToken();
  const headers = new Headers(init.headers);
  if (!headers.has("Content-Type") && init.body) {
    headers.set("Content-Type", "application/json");
  }
  if (token) {
    headers.set("Authorization", `Bearer ${token}`);
  }

  const response = await fetch(`${API_BASE_URL}${path}`, { ...init, headers });

  if (!response.ok) {
    let body: unknown = null;
    try {
      body = await response.json();
    } catch {
      // no JSON body on this error response
    }
    throw new ApiError(response.status, body);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}
