"use client";

import { Modal, modalButton } from "@/components/Modal";
import { Receipt } from "@/components/Receipt";
import type { Sale } from "@/lib/api/sales";

// The receipt a cashier hands over, shown where the sale just happened
// instead of on a separate page - leaving the register loaded and one tap
// from the next customer. The footer buttons carry no `print:hidden`: the
// print stylesheet in globals.css already prints the slip alone.
//
// Modal is deliberately not dismissable by an outside click (see its comment):
// a stray tap must not throw away a receipt the customer is still waiting for.
export function ReceiptDialog({
  sale,
  visible,
  onHide,
}: {
  sale: Sale | null;
  visible: boolean;
  onHide: () => void;
}) {
  return (
    <Modal
      visible={visible && sale !== null}
      onHide={onHide}
      title="ขายสำเร็จ"
      className="max-w-md"
      // Receipt carries its own px-6 py-7; the modal must not add a second
      // layer on top of it.
      bodyClassName=""
      footer={
        <>
          <button type="button" onClick={onHide} className={modalButton.secondary}>
            ขายรายการต่อไป
          </button>
          <button type="button" onClick={() => window.print()} className={modalButton.primary}>
            พิมพ์ใบเสร็จ
          </button>
        </>
      }
    >
      {sale && <Receipt sale={sale} />}
    </Modal>
  );
}
