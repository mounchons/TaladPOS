// Per-browser JWT storage (see specs/001-single-store-pos/contracts/auth.md:
// "logout" is simply the client discarding this token, no server session).
const TOKEN_KEY = "taladpos_token";
const STAFF_KEY = "taladpos_staff";

export function getToken(): string | null {
  if (typeof window === "undefined") return null;
  try {
    return window.localStorage.getItem(TOKEN_KEY);
  } catch {
    return null;
  }
}

export function setToken(token: string): void {
  if (typeof window === "undefined") return;
  try {
    window.localStorage.setItem(TOKEN_KEY, token);
  } catch {
    // ignore storage failures (private browsing, quota, etc.)
  }
}

export function clearToken(): void {
  if (typeof window === "undefined") return;
  try {
    window.localStorage.removeItem(TOKEN_KEY);
  } catch {
    // ignore
  }
}

// The JWT itself isn't decoded client-side - the staff summary from the
// login response ({id, name, role}) is cached alongside it so a page reload
// can restore "who's logged in" without re-authenticating.
export function getStoredStaff<T>(): T | null {
  if (typeof window === "undefined") return null;
  try {
    const raw = window.localStorage.getItem(STAFF_KEY);
    return raw ? (JSON.parse(raw) as T) : null;
  } catch {
    return null;
  }
}

export function setStoredStaff(staff: unknown): void {
  if (typeof window === "undefined") return;
  try {
    window.localStorage.setItem(STAFF_KEY, JSON.stringify(staff));
  } catch {
    // ignore
  }
}

export function clearStoredStaff(): void {
  if (typeof window === "undefined") return;
  try {
    window.localStorage.removeItem(STAFF_KEY);
  } catch {
    // ignore
  }
}
