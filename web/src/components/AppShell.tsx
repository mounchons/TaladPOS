"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { Button } from "primereact/button";
import { secondaryButtonPT } from "@/styles/primereact-passthrough";
import { useAuth } from "@/lib/auth/AuthContext";

const ROLE_LABEL_TH: Record<string, string> = {
  Manager: "ผู้จัดการ",
  Cashier: "แคชเชียร์",
};

const CASHIER_LINKS = [
  { href: "/sales", label: "ขายสินค้า" },
  { href: "/sales/history", label: "ประวัติการขาย" },
];

const MANAGER_LINKS = [
  { href: "/stock", label: "จัดการสต็อก" },
  { href: "/promotions", label: "โปรโมชั่น" },
  { href: "/reports", label: "รายงาน" },
];

// Shared header for every route inside app/(protected) - shows who's logged in
// (T037) and lets them log out (T036). Wraps children in a plain div, not
// <main>, since each protected page already renders its own <main>.
export function AppShell({ children }: { children: React.ReactNode }) {
  const { staff, logout } = useAuth();
  const router = useRouter();
  const pathname = usePathname();

  const links = staff?.role === "Manager" ? [...CASHIER_LINKS, ...MANAGER_LINKS] : CASHIER_LINKS;

  function handleLogout() {
    logout();
    router.push("/login");
  }

  return (
    <div className="min-h-screen bg-steel-50">
      {/* md and up the bar is exactly --appbar-h tall (border-box, so the border
          is included) - the sales register rail subtracts that from the
          viewport, and a guessed number left it a few px too tall. */}
      <header className="border-b border-steel-200 bg-white md:h-[var(--appbar-h)]">
        <div className="flex flex-col md:h-full md:flex-row md:items-stretch md:justify-between md:gap-6 md:px-5">
          {/* Wordmark and account sit on their own row until there is room for
              one line; the links scroll sideways rather than wrapping, since
              Thai has no word spaces and breaks in the wrong places. */}
          <div className="flex items-center justify-between gap-3 px-5 py-2.5 md:hidden">
            <span className="shrink-0 font-display text-lg font-semibold tracking-tight text-ink">
              TaladPOS
            </span>
            <div className="flex min-w-0 items-center gap-3">
              {/* Truncate rather than wrap: a Thai name has no word spaces, so
                  wrapping it breaks mid-word and makes the bar three lines tall
                  on a 320px screen. */}
              {staff && <span className="truncate text-sm text-ink-700">{staff.name}</span>}
              <Button label="ออกจากระบบ" onClick={handleLogout} pt={secondaryButtonPT} />
            </div>
          </div>

          <nav className="flex items-stretch gap-1 overflow-x-auto px-5 md:px-0 [&::-webkit-scrollbar]:hidden">
            <span className="mr-4 hidden self-center font-display text-lg font-semibold tracking-tight text-ink md:block">
              TaladPOS
            </span>
            {links.map((link) => {
              // Exact match, except /sales must not stay lit while you are in
              // one of its child routes (history, receipt).
              const active =
                link.href === "/sales" ? pathname === "/sales" : pathname.startsWith(link.href);
              return (
                <Link
                  key={link.href}
                  href={link.href}
                  aria-current={active ? "page" : undefined}
                  className={`flex shrink-0 items-center whitespace-nowrap border-b-2 px-3 py-3 text-sm transition-colors md:py-0 ${
                    active
                      ? "border-mango font-medium text-ink"
                      : "border-transparent text-ink-500 hover:text-ink"
                  }`}
                >
                  {link.label}
                </Link>
              );
            })}
          </nav>

          <div className="hidden items-center gap-4 py-2.5 md:flex">
            {staff && (
              <span className="whitespace-nowrap text-sm leading-tight text-ink-700">
                {staff.name}
                <span className="block text-xs text-ink-300">
                  {ROLE_LABEL_TH[staff.role] ?? staff.role}
                </span>
              </span>
            )}
            <Button label="ออกจากระบบ" onClick={handleLogout} pt={secondaryButtonPT} />
          </div>
        </div>
      </header>
      <div>{children}</div>
    </div>
  );
}
