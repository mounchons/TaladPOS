/**
 * Shared shape for a column used to export a table to Excel (data-model.md §1).
 * Deliberately close to `DataTableColumn<T>` (components/DataTable.tsx) so a
 * screen that already has `DataTableColumn`s can define its export columns
 * alongside them without inventing a new mental model - the difference is
 * `value` returns a raw string/number for a spreadsheet cell instead of a
 * `ReactNode` to render on screen.
 */
export interface ExportColumn<T> {
  /** Thai column heading. Must match the on-screen label (FR-002, FR-005). */
  header: string;
  /** Cell value for one row. */
  value: (row: T) => string | number;
}

/**
 * Request body for POST /api/export/xlsx (contracts/export-routes.md,
 * data-model.md §5). `columns`/`rows` are sent separately (not one array of
 * objects) so the payload stays compact even at the 10,000-row cap for
 * sales-history exports - no repeated key names per row.
 */
export interface XlsxExportRequest {
  /** e.g. "taladpos-stock-report" - the route handler appends a timestamp. */
  filenamePrefix: string;
  /** Excel sheet name, e.g. "สต็อกคงเหลือ". */
  sheetName: string;
  columns: { header: string }[];
  rows: (string | number)[][];
}

/** Builds the `columns`/`rows` pair of an `XlsxExportRequest` from typed data. */
export function toExportRows<T>(data: T[], columns: ExportColumn<T>[]): Pick<XlsxExportRequest, "columns" | "rows"> {
  return {
    columns: columns.map((c) => ({ header: c.header })),
    rows: data.map((row) => columns.map((c) => c.value(row))),
  };
}
