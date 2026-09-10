"use client";

import { useEffect, useState } from "react";
import { Modal, modalButton } from "@/components/Modal";
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
    <Modal
      visible={visible}
      onHide={onHide}
      title="สมัครสมาชิกใหม่"
      footer={
        <>
          <button type="button" onClick={onHide} className={modalButton.secondary}>
            ยกเลิก
          </button>
          <button
            type="submit"
            form="member-form"
            disabled={isSubmitting}
            className={modalButton.primary}
          >
            {isSubmitting ? "กำลังบันทึก..." : "สมัครสมาชิก"}
          </button>
        </>
      }
    >
      <form id="member-form" onSubmit={handleSubmit}>
        <label className="mb-1.5 block text-sm text-ink-700" htmlFor="member-name">
          ชื่อ
        </label>
        <input
          id="member-name"
          type="text"
          value={name}
          onChange={(e) => setName(e.target.value)}
          className="input mb-3 w-full rounded-control border-steel-200 bg-white"
          required
        />

        <label className="mb-1.5 block text-sm text-ink-700" htmlFor="member-phone">
          เบอร์โทรศัพท์
        </label>
        {/* type="tel" so a phone keypad comes up on a tablet at the counter,
            where members are usually registered. */}
        <input
          id="member-phone"
          type="tel"
          inputMode="tel"
          value={phoneNumber}
          onChange={(e) => setPhoneNumber(e.target.value)}
          className="input money mb-3 w-full rounded-control border-steel-200 bg-white"
          required
        />

        {error && (
          <p className="rounded-control border border-chili/30 bg-chili/5 px-3 py-2.5 text-sm text-chili">
            {error}
          </p>
        )}
      </form>
    </Modal>
  );
}
