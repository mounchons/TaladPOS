"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";

// No standalone landing page needed for a single-store POS - send everyone
// straight to /sales, whose (protected) layout redirects to /login if
// there's no active session.
export default function Home() {
  const router = useRouter();

  useEffect(() => {
    router.replace("/sales");
  }, [router]);

  return null;
}
