import type { XlsxExportRequest } from "./types";

// research.md #4 - fetch()+Blob rather than a plain <a href> navigation,
// because the row data has to travel as a POST body (up to 10,000 rows for
// sales history - far too large for a query string).
export async function triggerXlsxExport(request: XlsxExportRequest): Promise<void> {
  const response = await fetch("/api/export/xlsx", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    throw new Error("สร้างไฟล์ export ไม่สำเร็จ ลองใหม่อีกครั้ง");
  }

  const filename = filenameFromContentDisposition(response.headers.get("Content-Disposition")) ?? "export.xlsx";

  const blob = await response.blob();
  const url = URL.createObjectURL(blob);
  try {
    // A blob: URL carries no HTTP headers of its own, so the server's
    // Content-Disposition filename has to be read from the response here and
    // set explicitly - `download=""` alone would fall back to a generic name.
    const link = document.createElement("a");
    link.href = url;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    link.remove();
  } finally {
    URL.revokeObjectURL(url);
  }
}

function filenameFromContentDisposition(header: string | null): string | null {
  const match = header?.match(/filename="?([^";]+)"?/);
  return match?.[1] ?? null;
}
