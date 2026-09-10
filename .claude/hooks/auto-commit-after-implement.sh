#!/usr/bin/env bash
# Stop hook: auto-commit changes made during a /speckit-implement run.
#
# Detection is heuristic (Claude Code's Stop hook has no "which skill ran"
# field): we commit only when there are pending changes AND at least one of:
#   - a specs/*/tasks.md file was touched (speckit-implement is the command
#     that checks tasks off as it implements them), or
#   - the tail of this session's transcript mentions "speckit-implement"
#     (the skill/command name appears in the transcript when it was invoked).
# This intentionally stays conservative: no match, no commit.

set +e

INPUT="$(cat)"

REPO_ROOT="$(git rev-parse --show-toplevel 2>/dev/null)"
if [ -z "$REPO_ROOT" ]; then
  exit 0
fi
cd "$REPO_ROOT" || exit 0

CHANGED="$(git status --porcelain -uall)"
if [ -z "$CHANGED" ]; then
  exit 0
fi

TASKS_TOUCHED="$(printf '%s\n' "$CHANGED" | grep -E 'specs/.*/tasks\.md')"

TRANSCRIPT_PATH="$(printf '%s' "$INPUT" | node -e '
let d = "";
process.stdin.on("data", c => d += c);
process.stdin.on("end", () => {
  try {
    const j = JSON.parse(d || "{}");
    process.stdout.write(j.transcript_path || "");
  } catch (e) {
    process.stdout.write("");
  }
});
' 2>/dev/null)"

IMPL_MENTION=""
if [ -n "$TRANSCRIPT_PATH" ] && [ -f "$TRANSCRIPT_PATH" ]; then
  IMPL_MENTION="$(tail -c 20000 "$TRANSCRIPT_PATH" 2>/dev/null | grep -o 'speckit-implement' | head -1)"
fi

if [ -n "$TASKS_TOUCHED" ] || [ -n "$IMPL_MENTION" ]; then
  git add -A
  git commit -m "chore: auto-commit after /speckit-implement run" >/dev/null 2>&1
fi

exit 0
