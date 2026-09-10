"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { Receipt } from "@/components/Receipt";
import { getReceipt, type Sale } from "@/lib/api/sales";

export default function ReceiptPage() {
  const params = useParams<{ id: string }>();
  const [sale, setSale] = useState<Sale | null>(null);
  const [error, setError] = useState(false);

  useEffect(() => {
    getReceipt(params.id)
      .then(setSale)
      .catch(() => setError(true));
  }, [params.id]);

  if (error) {
    return (
      <main className="px-5 py-6">
        <p className="text-sm text-chili">ไม่พบบิลขายนี้</p>
      </main>
    );
  }

  if (!sale) {
    return (
      <main className="px-5 py-6">
        <p className="text-sm text-ink-300">กำลังโหลด...</p>
      </main>
    );
  }

  return (
    <main className="px-5 py-6">
      <div className="mx-auto max-w-[22rem]">
        <div className="mb-4 flex justify-end">
          <button
            type="button"
            onClick={() => window.print()}
            className="btn rounded-control border-steel-200 bg-white font-display font-medium text-ink-700 hover:border-ink hover:bg-white"
          >
            พิมพ์ใบเสร็จ
          </button>
        </div>
        <div className="border border-steel-200 print:border-0">
          <Receipt sale={sale} />
        </div>
      </div>
    </main>
  );
}
