"use client";

import { Dialog } from "primereact/dialog";
import { Button } from "primereact/button";
import { buttonPT, secondaryButtonPT, receiptDialogPT } from "@/styles/primereact-passthrough";
import { Receipt } from "@/components/Receipt";
import type { Sale } from "@/lib/api/sales";

// The receipt a cashier hands over, shown where the sale just happened
// instead of on a separate page - leaving the register loaded and one tap
// from the next customer. The footer buttons carry no `print:hidden`: the
// print stylesheet in globals.css already prints the slip alone.
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
    <Dialog
      visible={visible && sale !== null}
      onHide={onHide}
      header="ขายสำเร็จ"
      pt={receiptDialogPT}
      modal
      // Deliberately not dismissableMask: at a register the next tap is
      // usually a product, and a stray tap outside must not throw away a
      // receipt the customer is still waiting for. Closing is the explicit
      // button or Escape - same as the member dialog next to it.
      footer={
        <>
          <Button label="ขายรายการต่อไป" onClick={onHide} pt={secondaryButtonPT} />
          <Button
            label="พิมพ์ใบเสร็จ"
            icon="pi pi-print"
            onClick={() => window.print()}
            pt={buttonPT}
          />
        </>
      }
    >
      {sale && <Receipt sale={sale} />}
    </Dialog>
  );
}
