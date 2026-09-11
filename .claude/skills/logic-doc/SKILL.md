---
name: "logic-doc"
description: "Research how a specific screen, feature, or business rule actually works across TaladPOS's web/ (Next.js) and api/ (ASP.NET Core DDD) + PostgreSQL, then write Thai-language logic documentation to docs/logic/*.md with a high-level diagram, a flow diagram, and a sequence diagram (mermaid). Use this whenever the user asks how something works, wants it explained, or wants it documented — phrases like 'อธิบายการทำงานของ...', 'เขียนเอกสาร logic ของ...', 'หน้า X ทำงานยังไง', 'การคำนวณ Y ทำงานอย่างไร', 'explain how X works', 'document the Y logic/flow', or asks for a flow/sequence/architecture diagram of a feature. Trigger this even if the user doesn't say 'docs/logic' explicitly or doesn't name a diagram type — any request to understand or explain a screen, API, or business-rule flow in this repo should go through this skill rather than an ad-hoc read-and-summarize."
compatibility: "Requires the TaladPOS repo (web/ + api/ + specs/). Uses codebase-memory-mcp tools when available (index_repository, search_graph, search_code) as a shortcut; falls back cleanly to Grep/Glob/Read if those tools aren't present."
metadata:
  author: "created via skill-creator, from a conversation that produced docs/logic/{architecture,sales-screen,promotion-discount}.md"
user-invocable: true
disable-model-invocation: false
---

## What this skill produces

Given a topic — a screen (`หน้าขายสินค้า`), a business rule (`การคำนวณโปรโมชั่น`), or a feature area
(`stock management`, `member registration`) — research it end-to-end across both codebases and write
one or more Markdown files under `docs/logic/` that a new engineer (or a non-engineer stakeholder who
can read a diagram) could use to understand the feature without opening an IDE.

Every doc this skill writes follows the shape that already exists in `docs/logic/` (read
`docs/logic/README.md` and `docs/logic/promotion-discount.md` first if they exist — they are the
canonical example of the target quality bar):

1. A short prose overview + a table of the UI components/screens involved
2. A table of every API endpoint the feature calls (method, route, which controller/use case handles it)
3. A table of every DB table/column the feature touches, with the *actual* casing/naming pulled from
   the EF Core configuration + migration files — never assumed
4. One **flow diagram** (`mermaid flowchart`) tracing the user's journey through the screen/feature,
   including error/edge branches (not just the happy path)
5. One **sequence diagram** (`mermaid sequenceDiagram`) tracing the core business logic end-to-end —
   UI → Controller → Application/UseCase → Domain rule → Repository → DB → back to UI
6. Worked numeric examples, preferably lifted straight from existing unit tests rather than invented
7. A "เคสที่มักเข้าใจผิด" / gotchas section for non-obvious behavior (things a reader would get wrong by
   guessing from the UI alone)

Write documentation in **Thai**, matching the rest of `docs/` and `specs/` in this repo, unless the user
asks for English. Code identifiers, table/column names, and file paths stay in their original form.

## Why this workflow, not a quick summary

The value of this skill is that every claim in the output is traceable to a line of code the skill
actually read — not paraphrased from a component name or an educated guess about REST conventions. The
previous run of this workflow (the one this skill was extracted from) found real, non-obvious details
that a surface-level read would have missed or gotten wrong:

- The sales cart shows a **raw subtotal with no discount** while shopping — all discount math happens
  server-side, once, at checkout. A doc written from the UI alone would have assumed the cart preview
  was accurate.
- Promotions don't stack within a scope (best-of-N, not sum), but Item-scope and Bill-scope discounts
  *do* combine on the same line — this is a rule buried in `DiscountResolver.cs` and only provable by
  reading it plus its test suite, not by reading the controller.
- Table names were snake_case but column names were PascalCase in this project — the *opposite* convention
  would have been an equally plausible guess. It only became certain after reading the actual EF Core
  migration file.

So: **read the real implementation before writing anything down.** If something is ambiguous after
reading the code, say so in the doc rather than picking the more plausible-sounding option silently.

## Step 1 — Pin down the topic and scope

If the user's request already names a specific screen, endpoint, or rule, use that. If it's vague
("อธิบายระบบสต็อก" / "explain stock"), do a quick pass to figure out the natural boundary (usually: one
screen + the API/domain logic it drives) before diving deep — better to ask a one-line clarifying
question than research the wrong half of the codebase. Do not over-scope: one topic = one coherent doc,
not a rewrite of the whole `docs/logic/` set unless asked.

## Step 2 — Research the codebase

Prefer the `codebase-memory-mcp` tools when they're available — they're much faster than grep for this
kind of cross-cutting "what calls what" research:

1. Check `mcp__codebase-memory-mcp__index_status` for this project; if not indexed, run
   `mcp__codebase-memory-mcp__index_repository` (mode `moderate` is enough — this repo is small, and
   `full` mode just costs more time for no benefit here) with `repo_path` set to the TaladPOS root.
2. `search_graph` with a natural-language `query` for the feature's domain terms (Thai or English both
   work reasonably — try the English/technical term first, e.g. "promotion discount", "stock quantity",
   "member accumulate") to find the controller, use case, domain entity, and repository involved.
3. `search_code` for text patterns (route attributes, DTO names, component names) when `search_graph`
   doesn't surface something you expect to exist.
4. If the MCP tools aren't available in this session, fall back to `Glob`/`Grep`/`Read` directly —
   `api/src/TaladPOS.Api/Controllers/*.cs` for routes, `api/src/TaladPOS.Application/**/*.cs` for use
   cases, `api/src/TaladPOS.Domain/**/*.cs` for entities/business rules,
   `api/src/TaladPOS.Infrastructure/{Configurations,Repositories,Persistence/Migrations}/*.cs` for the
   DB shape, and `web/src/app/(protected)/**` + `web/src/components/**` + `web/src/lib/api/*.ts` for the
   frontend.

**Always read, don't infer, these three things** — they're the ones most likely to be wrong if guessed:

- **API routes**: read the `[Route(...)]` / `[Http*]` attributes on the controller, not the frontend's
  guess of what the route "should" be. Read the matching `web/src/lib/api/*.ts` function to confirm the
  frontend actually calls it that way (params, query string shape, request/response body).
- **DB table/column names**: read the `IEntityTypeConfiguration<T>` file in
  `api/src/TaladPOS.Infrastructure/Configurations/` *and* the earliest migration that creates the table
  — the configuration says the intent, the migration file is what's actually in PostgreSQL. Note any
  computed/`Ignore()`'d properties (they exist in the C# model but have no column) and any missing FK
  constraints (this project deliberately omits some, e.g. snapshot references) — both are easy to get
  wrong by assuming a "normal" schema.
- **Business rules with more than one branch** (discount stacking, stock concurrency, role checks): read
  the actual conditional logic, and check for a matching test file (`api/tests/**`) — tests are usually
  the fastest way to get a *confirmed, exact* worked example instead of inventing numbers.

For a frontend screen, also identify: what state lives client-side only vs. what's authoritative only
after a server round-trip (this was the single most important nuance in the sales-cart example above —
always check whether a number shown in the UI mid-flow is really final, or a client-side approximation
that gets replaced by the server's response).

## Step 3 — Write the diagrams

Use `mermaid` fenced code blocks (` ```mermaid `). Conventions that have worked well in this repo's
existing docs (match them for consistency):

- Quote every node label in `flowchart` diagrams (`A["ข้อความ (มีวงเล็บได้)"]`) — this avoids Mermaid
  parse errors from Thai text, parentheses, or punctuation inside unquoted labels.
- In `flowchart TD`/`TD`, model error/alternate paths explicitly with `{"เงื่อนไข?"}` decision nodes —
  a diagram that only shows the happy path is documenting less than the code actually does.
- In `sequenceDiagram`, use `actor` for the human, `participant` for each system component (UI component,
  Controller, UseCase, domain helper, Repository, DB), and `alt`/`opt`/`loop` blocks to show branching,
  optional steps (e.g. "only if a member is attached"), and per-line-item loops — this is what makes the
  diagram match the code's actual control flow instead of a simplified retelling.
- Add a short `Note over X: ...` where the *why* of a step isn't obvious from its name (e.g. why an
  update is an atomic conditional `UPDATE ... WHERE` instead of a read-then-write).
- One sequence diagram should cover one coherent transaction/request, end to end. If the feature has
  multiple independent flows (e.g. "create" and "search"), either give each its own diagram or pick the
  one the user actually asked about — don't cram unrelated flows into one diagram.

## Step 4 — Write the output file(s)

- Default location: `docs/logic/<kebab-case-topic>.md` (e.g. `docs/logic/stock-management.md`). Reuse
  an existing file instead of creating a near-duplicate if the topic clearly overlaps one already there
  — check `docs/logic/README.md` first.
- If `docs/logic/README.md` exists, add a one-line entry to its index table pointing at the new file
  (same pattern as the existing rows). If it doesn't exist yet, create it as a short index the first time
  this skill runs in a repo that has no `docs/logic/` yet.
- Follow the section order from "What this skill produces" above. Keep prose tight — tables and diagrams
  should carry most of the information density, with prose only where a diagram/table can't express the
  nuance (e.g. the "gotchas" section).
- Cite file paths inline (e.g. "`CompleteSaleUseCase.cs` บรรทัด 118") when asserting a specific, easy-to-
  verify-wrong claim like a formula or a magic number — it lets a skeptical reader jump straight to the
  proof instead of trusting the doc blind.

## Step 5 — Report back, don't auto-commit

Summarize what was written (file paths) and any open questions or ambiguities you noticed but couldn't
resolve by reading the code (e.g. "ไม่พบ endpoint สำหรับ X — เดาว่ายังไม่ implement, ควรยืนยันกับทีม").
Do not `git add`/`git commit` the new docs unless the user explicitly asks — documentation is source
the user should get to look over first, same as any other diff.
