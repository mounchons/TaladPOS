"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/lib/auth/AuthContext";
import { AppShell } from "@/components/AppShell";

// Redirects to /login when there's no active session (T035). A route group
// (parens don't appear in the URL) so /sales, /stock, etc. stay at their
// plain paths while sharing this guard + the AppShell header.
export default function ProtectedLayout({ children }: { children: React.ReactNode }) {
  const { staff, isLoading } = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (!isLoading && !staff) {
      router.replace("/login");
    }
  }, [isLoading, staff, router]);

  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center text-sm text-ink-300">
        กำลังโหลด...
      </div>
    );
  }

  if (!staff) {
    return null;
  }

  return <AppShell>{children}</AppShell>;
}
