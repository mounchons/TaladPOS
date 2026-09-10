"use client";

import { useState } from "react";
import { AutoComplete, type AutoCompleteCompleteEvent, type AutoCompleteChangeEvent } from "primereact/autocomplete";
import { autoCompletePT, darkAutoCompletePT } from "@/styles/primereact-passthrough";
import { searchMembers, type Member } from "@/lib/api/members";

interface MemberSearchProps {
  selected: Member | null;
  onSelect: (member: Member | null) => void;
  /** "dark" for the register panel on the sales screen. */
  tone?: "light" | "dark";
}

// tasks.md T053 (US4): member search used during checkout - typing a
// phone number or name suggests matches (contracts/members.md GET /api/v1/members).
export function MemberSearch({ selected, onSelect, tone = "light" }: MemberSearchProps) {
  const [query, setQuery] = useState(selected?.name ?? "");
  const [suggestions, setSuggestions] = useState<Member[]>([]);

  async function search(e: AutoCompleteCompleteEvent) {
    if (!e.query.trim()) {
      setSuggestions([]);
      return;
    }
    try {
      setSuggestions(await searchMembers(e.query));
    } catch {
      setSuggestions([]);
    }
  }

  function handleChange(e: AutoCompleteChangeEvent) {
    if (typeof e.value === "string") {
      setQuery(e.value);
      if (e.value === "") onSelect(null);
      return;
    }
    const member = e.value as Member;
    setQuery(member.name);
    onSelect(member);
  }

  return (
    <AutoComplete
      value={query}
      suggestions={suggestions}
      completeMethod={search}
      onChange={handleChange}
      field="name"
      itemTemplate={(member: Member) => (
        <span className="flex items-baseline justify-between gap-3">
          <span>{member.name}</span>
          <span className="money text-xs text-ink-300">{member.phoneNumber}</span>
        </span>
      )}
      placeholder="ค้นหาสมาชิก ชื่อหรือเบอร์โทร"
      pt={tone === "dark" ? darkAutoCompletePT : autoCompletePT}
    />
  );
}
