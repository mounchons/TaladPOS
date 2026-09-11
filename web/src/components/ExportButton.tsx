"use client";

import { useRef, useState } from "react";

/**
 * Shared export button for the stock-report tab and sales-history page
 * (tasks.md T005). Owns its own loading/error state so both screens get the
 * same FR-008 guard (disabled + "กำลังเตรียมไฟล์..." while `onExport` is in
 * flight, so a second click can't start a second export) and the same place
 * to surface a message - including US2's "เกินเพดาน" message, which reaches
 * here by `onExport` throwing instead of calling `triggerXlsxExport`.
 *
 * The re-entry guard is a ref, not the `isExporting` state: two clicks fired
 * back-to-back (verified in manual QA) both run before React's first state
 * update commits, so both would read the same stale `isExporting === false`
 * and both call `onExport`. A ref updates synchronously within the same
 * click handler, so the second call sees it immediately - `isExporting`
 * state still drives the `disabled` attribute/label, it just isn't what
 * makes the guard correct.
 */
export function ExportButton({
  onExport,
  className,
}: {
  onExport: () => Promise<void>;
  className?: string;
}) {
  const [isExporting, setIsExporting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const isExportingRef = useRef(false);

  async function handleClick() {
    if (isExportingRef.current) return;
    isExportingRef.current = true;
    setError(null);
    setIsExporting(true);
    try {
      await onExport();
    } catch (err) {
      setError(err instanceof Error ? err.message : "เกิดข้อผิดพลาด ไม่สามารถ export ได้ ลองใหม่อีกครั้ง");
    } finally {
      isExportingRef.current = false;
      setIsExporting(false);
    }
  }

  return (
    <div className="inline-flex flex-col items-start gap-1.5">
      <button
        type="button"
        onClick={handleClick}
        disabled={isExporting}
        className={
          "btn btn-sm rounded-control border-steel-200 bg-white font-display font-medium text-ink-700 " +
          "hover:border-ink hover:bg-white disabled:cursor-not-allowed disabled:opacity-60 " +
          (className ?? "")
        }
      >
        {isExporting ? "กำลังเตรียมไฟล์..." : "Export"}
      </button>
      {error && <p className="max-w-xs text-xs text-chili">{error}</p>}
    </div>
  );
}
