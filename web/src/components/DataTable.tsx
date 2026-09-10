"use client";

import { useEffect, useRef, type ReactNode } from "react";

export interface DataTableColumn<T> {
  /** Thai column heading. Empty string for an action column - see `stack` below. */
  header: string;
  /** Cell contents for one row. */
  cell: (row: T) => ReactNode;
  /** Extra classes on the cell, e.g. "text-right" for money. */
  className?: string;
}

interface DataTableProps<T> {
  columns: DataTableColumn<T>[];
  rows: T[];
  rowKey: (row: T) => string;
  /** Server paging state. Omit `totalCount` to render without a pager. */
  page?: number;
  pageSize?: number;
  totalCount?: number;
  onPageChange?: (page: number) => void;
  /**
   * Anything the current filter depends on, serialised. When it changes the
   * table asks for page 1 again.
   */
  filterKey?: string;
  isLoading?: boolean;
  emptyText?: string;
  /** The filter bar. Rendered above the table, inside the same card. */
  children?: ReactNode;
}

/**
 * The one table in the app (research.md #10).
 *
 * daisyUI ships a `table` class, not a datagrid - no paging, no filtering, no
 * state - so the parts that would otherwise be copy-pasted into four pages
 * live here: the pager, the empty and loading states, and the page reset.
 *
 * Filtering and paging both happen server-side, so this component never sorts
 * or slices `rows`; it renders exactly what it was handed.
 */
export function DataTable<T>({
  columns,
  rows,
  rowKey,
  page = 1,
  pageSize = 20,
  totalCount,
  onPageChange,
  filterKey,
  isLoading = false,
  emptyText = "ไม่มีข้อมูล",
  children,
}: DataTableProps<T>) {
  const paged = totalCount !== undefined && onPageChange !== undefined;
  const totalPages = totalCount === undefined ? 0 : Math.ceil(totalCount / pageSize);

  // Changing a filter while deep in the pages is the classic way to land on an
  // empty table: the result set shrinks to two pages and you are still asking
  // for page five. Resetting here rather than in each page means every screen
  // that uses this table gets the behaviour without writing it again.
  const previousFilter = useRef(filterKey);
  useEffect(() => {
    if (previousFilter.current === filterKey) return;
    previousFilter.current = filterKey;
    if (page !== 1) onPageChange?.(1);
  }, [filterKey, page, onPageChange]);

  return (
    <div className="mt-4 overflow-hidden rounded-control border border-steel-200 bg-white">
      {children && <div className="border-b border-steel-200 p-3">{children}</div>}

      {/* The table scrolls inside this box rather than widening the page.
          Below md that is what keeps columns readable instead of crushed, and
          the page itself never scrolls sideways (research.md #12). */}
      <div className="overflow-x-auto">
        <table className="table w-full">
          <thead>
            <tr>
              {columns.map((column, i) => (
                <th key={i} className={`whitespace-nowrap text-ink-500 ${column.className ?? ""}`}>
                  {column.header}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {isLoading ? (
              <tr>
                <td colSpan={columns.length} className="py-10 text-center text-sm text-ink-300">
                  กำลังโหลด…
                </td>
              </tr>
            ) : rows.length === 0 ? (
              <tr>
                <td colSpan={columns.length} className="py-10 text-center text-sm text-ink-300">
                  {emptyText}
                </td>
              </tr>
            ) : (
              rows.map((row) => (
                <tr key={rowKey(row)} className="hover:bg-mango-100/40">
                  {columns.map((column, i) => (
                    <td key={i} className={column.className}>
                      {column.cell(row)}
                    </td>
                  ))}
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {paged && totalCount > 0 && (
        <Pager
          page={page}
          totalPages={totalPages}
          totalCount={totalCount}
          onPageChange={onPageChange}
        />
      )}
    </div>
  );
}

function Pager({
  page,
  totalPages,
  totalCount,
  onPageChange,
}: {
  page: number;
  totalPages: number;
  totalCount: number;
  onPageChange: (page: number) => void;
}) {
  return (
    <div className="flex flex-wrap items-center justify-between gap-3 border-t border-steel-200 px-3 py-2.5">
      {/* The count is the point of the envelope - without it a cashier cannot
          tell an empty filter from the end of the list. */}
      <span className="text-xs text-ink-500">
        หน้า {page} จาก {totalPages} · ทั้งหมด <span className="money">{totalCount}</span> รายการ
      </span>

      <div className="join">
        <button
          type="button"
          className="btn join-item btn-sm"
          disabled={page <= 1}
          onClick={() => onPageChange(page - 1)}
        >
          ก่อนหน้า
        </button>
        {pageNumbers(page, totalPages).map((n, i) =>
          n === null ? (
            <button key={`gap-${i}`} type="button" className="btn join-item btn-sm btn-disabled">
              …
            </button>
          ) : (
            <button
              key={n}
              type="button"
              aria-current={n === page ? "page" : undefined}
              className={`btn join-item btn-sm ${n === page ? "btn-primary" : ""}`}
              onClick={() => onPageChange(n)}
            >
              {n}
            </button>
          ),
        )}
        <button
          type="button"
          className="btn join-item btn-sm"
          disabled={page >= totalPages}
          onClick={() => onPageChange(page + 1)}
        >
          ถัดไป
        </button>
      </div>
    </div>
  );
}

/**
 * At most seven slots: first, last, the current page and its neighbours, with
 * nulls standing in for the gaps. A shop with a year of sales has hundreds of
 * pages and rendering every number would wrap the pager across the screen.
 */
function pageNumbers(page: number, totalPages: number): (number | null)[] {
  if (totalPages <= 7) return Array.from({ length: totalPages }, (_, i) => i + 1);

  const candidates = [1, totalPages, page, page - 1, page + 1];
  const sorted = candidates
    .filter((n, i) => n >= 1 && n <= totalPages && candidates.indexOf(n) === i)
    .sort((a, b) => a - b);

  const out: (number | null)[] = [];
  sorted.forEach((n, i) => {
    if (i > 0 && n - sorted[i - 1] > 1) out.push(null);
    out.push(n);
  });
  return out;
}
