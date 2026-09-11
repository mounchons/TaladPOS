import ExcelJS from "exceljs";
import type { XlsxExportRequest } from "@/lib/export/types";

// contracts/export-routes.md - POST /api/export/xlsx
//
// This is a Next.js Route Handler local to web/, not an api/v1/* endpoint -
// it never touches PostgreSQL/EF Core and does not check auth itself. The
// data it formats has already been through apiFetch() (JWT + role checked by
// api/) before it ever reaches here (research.md #2), so this stays a pure
// JSON -> .xlsx transform with no access-control logic of its own.

const MIN_COLUMN_WIDTH = 10;
const MAX_COLUMN_WIDTH = 60;

function isValidRequest(body: unknown): body is XlsxExportRequest {
  if (typeof body !== "object" || body === null) return false;
  const { filenamePrefix, sheetName, columns, rows } = body as Record<string, unknown>;
  if (typeof filenamePrefix !== "string" || !filenamePrefix) return false;
  if (typeof sheetName !== "string" || !sheetName) return false;
  if (!Array.isArray(columns) || !columns.every((c) => typeof c?.header === "string")) return false;
  if (!Array.isArray(rows)) return false;
  return rows.every((row) => Array.isArray(row) && row.length === columns.length);
}

function timestamp(date: Date): string {
  const pad = (n: number) => String(n).padStart(2, "0");
  return (
    `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}` +
    `-${pad(date.getHours())}${pad(date.getMinutes())}`
  );
}

function columnWidths(columns: XlsxExportRequest["columns"], rows: XlsxExportRequest["rows"]): number[] {
  return columns.map((column, i) => {
    const longest = rows.reduce((max, row) => Math.max(max, String(row[i] ?? "").length), column.header.length);
    return Math.min(Math.max(longest + 2, MIN_COLUMN_WIDTH), MAX_COLUMN_WIDTH);
  });
}

export async function POST(request: Request) {
  let body: unknown;
  try {
    body = await request.json();
  } catch {
    return Response.json({ error: "invalid_export_request" }, { status: 400 });
  }

  if (!isValidRequest(body)) {
    return Response.json({ error: "invalid_export_request" }, { status: 400 });
  }

  const { filenamePrefix, sheetName, columns, rows } = body;

  const workbook = new ExcelJS.Workbook();
  const worksheet = workbook.addWorksheet(sheetName);

  worksheet.columns = columnWidths(columns, rows).map((width) => ({ width }));

  const headerRow = worksheet.addRow(columns.map((c) => c.header));
  headerRow.font = { bold: true };

  // rows may be empty (FR-011) - the sheet still has its header row, which is
  // a successful, openable .xlsx, not an error.
  for (const row of rows) {
    worksheet.addRow(row);
  }

  const buffer = await workbook.xlsx.writeBuffer();
  const filename = `${filenamePrefix}-${timestamp(new Date())}.xlsx`;

  return new Response(buffer, {
    status: 200,
    headers: {
      "Content-Type": "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
      "Content-Disposition": `attachment; filename="${filename}"`,
    },
  });
}
