"use client";

import { useEffect, useId, useRef, useState } from "react";
import { searchMembers, type Member } from "@/lib/api/members";

interface MemberSearchProps {
  selected: Member | null;
  onSelect: (member: Member | null) => void;
  /** "dark" for the register panel on the sales screen. */
  tone?: "light" | "dark";
}

// tasks.md T053 (US4): member search used during checkout - typing a
// phone number or name suggests matches (contracts/members.md GET /api/v1/members).
//
// Written out rather than pulled from a library: the whole behaviour is
// "debounce, fetch, pick one", and the accessibility a combobox needs
// (role/aria-activedescendant/arrow keys) is a dozen lines. Carrying a
// component library for this one widget was the last thing keeping PrimeReact
// in the bundle (research.md #9).
export function MemberSearch({ selected, onSelect, tone = "light" }: MemberSearchProps) {
  const [query, setQuery] = useState(selected?.name ?? "");
  const [suggestions, setSuggestions] = useState<Member[]>([]);
  const [isOpen, setIsOpen] = useState(false);
  const [activeIndex, setActiveIndex] = useState(-1);
  const containerRef = useRef<HTMLDivElement>(null);
  const listId = useId();

  // Debounced so a fast typist does not fire a request per keystroke; the
  // sales screen uses the same 250ms for the product scanner.
  useEffect(() => {
    if (!query.trim()) {
      setSuggestions([]);
      return;
    }
    let cancelled = false;
    const timeout = setTimeout(() => {
      searchMembers(query)
        .then((results) => {
          if (cancelled) return;
          setSuggestions(results);
          setActiveIndex(-1);
        })
        .catch(() => !cancelled && setSuggestions([]));
    }, 250);
    return () => {
      cancelled = true;
      clearTimeout(timeout);
    };
  }, [query]);

  // Clicking away closes the list. Without this the suggestions stay up over
  // the shelf after the cashier moves on to tapping products.
  useEffect(() => {
    function onPointerDown(event: MouseEvent) {
      if (!containerRef.current?.contains(event.target as Node)) setIsOpen(false);
    }
    document.addEventListener("mousedown", onPointerDown);
    return () => document.removeEventListener("mousedown", onPointerDown);
  }, []);

  function choose(member: Member) {
    setQuery(member.name);
    onSelect(member);
    setIsOpen(false);
    setActiveIndex(-1);
  }

  function handleKeyDown(event: React.KeyboardEvent<HTMLInputElement>) {
    if (event.key === "Escape") {
      setIsOpen(false);
      return;
    }
    if (!isOpen || suggestions.length === 0) return;

    if (event.key === "ArrowDown" || event.key === "ArrowUp") {
      event.preventDefault();
      const step = event.key === "ArrowDown" ? 1 : -1;
      setActiveIndex((i) => (i + step + suggestions.length) % suggestions.length);
      return;
    }
    if (event.key === "Enter" && activeIndex >= 0) {
      // Only swallow Enter when a suggestion is highlighted - otherwise the
      // key belongs to whatever form the search sits in.
      event.preventDefault();
      choose(suggestions[activeIndex]);
    }
  }

  const dark = tone === "dark";
  const fieldClass = dark
    ? "input w-full rounded-control border-ink-700 bg-ink-700 text-white placeholder:text-ink-300"
    : "input w-full rounded-control border-steel-200 bg-white";

  return (
    <div ref={containerRef} className="relative">
      <input
        type="text"
        role="combobox"
        aria-expanded={isOpen && suggestions.length > 0}
        aria-controls={listId}
        aria-autocomplete="list"
        aria-activedescendant={activeIndex >= 0 ? `${listId}-${activeIndex}` : undefined}
        value={query}
        placeholder="ค้นหาสมาชิก ชื่อหรือเบอร์โทร"
        className={fieldClass}
        onChange={(e) => {
          setQuery(e.target.value);
          setIsOpen(true);
          if (e.target.value === "") onSelect(null);
        }}
        onFocus={() => setIsOpen(true)}
        onKeyDown={handleKeyDown}
      />

      {isOpen && suggestions.length > 0 && (
        <ul
          id={listId}
          role="listbox"
          className={`absolute z-50 mt-1 max-h-60 w-full overflow-y-auto rounded-control border py-1 shadow-lg ${
            dark ? "border-ink-700 bg-ink" : "border-steel-200 bg-white"
          }`}
        >
          {suggestions.map((member, i) => (
            <li
              key={member.id}
              id={`${listId}-${i}`}
              role="option"
              aria-selected={i === activeIndex}
              // onMouseDown, not onClick: the input's blur would otherwise
              // close the list before the click landed.
              onMouseDown={(e) => {
                e.preventDefault();
                choose(member);
              }}
              onMouseEnter={() => setActiveIndex(i)}
              className={`flex cursor-pointer items-baseline justify-between gap-3 px-3 py-2 text-sm ${
                i === activeIndex
                  ? dark
                    ? "bg-ink-700 text-white"
                    : "bg-mango-100"
                  : dark
                    ? "text-white"
                    : "text-ink"
              }`}
            >
              <span>{member.name}</span>
              <span className="money text-xs text-ink-300">{member.phoneNumber}</span>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
