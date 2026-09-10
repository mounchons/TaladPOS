import Link from "next/link";

// FR-029. Shown to a Cashier who lands on a Manager-only screen. States what
// happened and gives the way back rather than dead-ending.
export function ManagerOnly() {
  return (
    <main className="px-5 py-16 text-center">
      <p className="text-sm text-ink-700">หน้านี้เปิดให้เฉพาะผู้จัดการ</p>
      <Link
        href="/sales"
        className="mt-3 inline-block text-sm text-mango-600 underline underline-offset-4 hover:text-ink"
      >
        กลับไปหน้าขายสินค้า
      </Link>
    </main>
  );
}
