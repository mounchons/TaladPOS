"use client";

import { PrimeReactProvider } from "primereact/api";
import { AuthProvider } from "@/lib/auth/AuthContext";

// PrimeReact runs unstyled (see src/styles/primereact-passthrough.ts and
// specs/001-single-store-pos/research.md #7) so Tailwind stays the single
// source of visual styling.
export function AppProviders({ children }: { children: React.ReactNode }) {
  return (
    <PrimeReactProvider value={{ unstyled: true }}>
      <AuthProvider>{children}</AuthProvider>
    </PrimeReactProvider>
  );
}
