"use client";

import { AuthProvider } from "@/lib/auth/AuthContext";

// Styling comes entirely from Tailwind 4 + daisyUI (globals.css,
// specs/001-single-store-pos/research.md #8), so there is no UI-library
// provider to wrap the tree in any more - only auth state.
export function AppProviders({ children }: { children: React.ReactNode }) {
  return <AuthProvider>{children}</AuthProvider>;
}
