"use client";

import { useEffect, useRef, type ReactNode } from "react";

/**
 * The one dialog in the app. daisyUI styles `<dialog>` through `modal` /
 * `modal-box`; the open/close wiring is ours, and it lives here rather than
 * being repeated in every form.
 *
 * Deliberately no `modal-backdrop` close form. daisyUI's usual pattern closes
 * on an outside click, but at a register the next tap is usually a product -
 * a stray tap must not discard a half-filled product form or a receipt the
 * customer is still waiting for. Closing is the explicit button or Escape.
 */
export function Modal({
  visible,
  onHide,
  title,
  children,
  footer,
  className = "max-w-lg",
  bodyClassName = "px-6 py-5",
}: {
  visible: boolean;
  onHide: () => void;
  title: string;
  children: ReactNode;
  footer?: ReactNode;
  className?: string;
  /**
   * Padding for the body. Pass "" for content that brings its own - the
   * receipt slip does, and stacking the two gave it 48px of side padding.
   */
  bodyClassName?: string;
}) {
  const ref = useRef<HTMLDialogElement>(null);

  // showModal() rather than the `open` attribute: only the modal form gets the
  // top layer, the backdrop and the focus trap. Calling it twice throws, hence
  // the guard on dialog.open.
  useEffect(() => {
    const dialog = ref.current;
    if (!dialog) return;
    if (visible && !dialog.open) dialog.showModal();
    if (!visible && dialog.open) dialog.close();
  }, [visible]);

  return (
    // <dialog> fires `close` for Escape as well as for close(); routing both
    // through onHide keeps React's state and the DOM's agreeing.
    <dialog ref={ref} className="modal" onClose={onHide}>
      <div className={`modal-box rounded-control bg-white p-0 ${className}`}>
        <h2 className="border-b border-steel-200 px-6 py-4 font-display text-lg font-semibold text-ink">
          {title}
        </h2>

        <div className={bodyClassName}>{children}</div>

        {footer && (
          <div className="flex justify-end gap-2 border-t border-steel-200 px-6 py-4">{footer}</div>
        )}
      </div>
    </dialog>
  );
}

/** The two button shapes every dialog footer uses. Kept here so they match. */
export const modalButton = {
  secondary:
    "btn rounded-control border-steel-200 bg-white font-display font-medium text-ink-700 hover:border-ink hover:bg-white",
  primary:
    "btn rounded-control border-ink bg-ink font-display font-medium text-white hover:border-mango hover:bg-mango hover:text-ink",
} as const;
