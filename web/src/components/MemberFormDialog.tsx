"use client";

import { useEffect, useState } from "react";
import { Dialog } from "primereact/dialog";
import { InputText } from "primereact/inputtext";
import { Button } from "primereact/button";
import { dialogPT, inputTextPT, buttonPT, secondaryButtonPT } from "@/styles/primereact-passthrough";
import { registerMember, type Member } from "@/lib/api/members";
import { ApiError } from "@/lib/api/client";

interface MemberFormDialogProps {
  visible: boolean;
  onHide: () => void;
  onRegistered: (member: Member) => void;
}

// tasks.md T054 (US4): new-member registration form, opened from the sales
// page when a phone number search finds no existing member.
export function MemberFormDialog({ visible, onHide, onRegistered }: MemberFormDialogProps) {
  const [name, setName] = useState("");
  const [phoneNumber, setPhoneNumber] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (visible) {
      setName("");
      setPhoneNumber("");
      setError(null);
    }
  }, [visible]);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setIsSubmitting(true);
    try {
      const member = await registerMember({ name, phoneNumber });
      onRegistered(member);
    } catch (err) {
      if (err instanceof ApiError && err.status === 409) {
        setError("เบอร์โทรศัพท์นี้ถูกใช้สมัครสมาชิกไปแล้ว");
      } else {
        setError("เกิดข้อผิดพลาด ไม่สามารถสมัครสมาชิกได้");
      }
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <Dialog visible={visible} onHide={onHide} header="สมัครสมาชิกใหม่" pt={dialogPT} modal>
      <form onSubmit={handleSubmit}>
        <label className="mb-1.5 block text-sm text-ink-700" htmlFor="member-name">
          ชื่อ
        </label>
        <InputText
          id="member-name"
          value={name}
          onChange={(e) => setName(e.target.value)}
          pt={inputTextPT}
          className="mb-3"
          required
        />

        <label className="mb-1.5 block text-sm text-ink-700" htmlFor="member-phone">
          เบอร์โทรศัพท์
        </label>
        <InputText
          id="member-phone"
          value={phoneNumber}
          onChange={(e) => setPhoneNumber(e.target.value)}
          pt={inputTextPT}
          className="mb-3"
          required
        />

        {error && <p className="mb-3 rounded-control border border-chili/30 bg-chili/5 px-3 py-2.5 text-sm text-chili">{error}</p>}

        <div className="mt-4 flex justify-end gap-2">
          <Button type="button" label="ยกเลิก" onClick={onHide} pt={secondaryButtonPT} />
          <Button
            type="submit"
            label={isSubmitting ? "กำลังบันทึก..." : "สมัครสมาชิก"}
            disabled={isSubmitting}
            pt={buttonPT}
          />
        </div>
      </form>
    </Dialog>
  );
}
