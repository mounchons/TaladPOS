// PrimeReact runs in `unstyled` mode (see specs/001-single-store-pos/research.md #7):
// PrimeReact supplies behavior/accessibility only; all visual styling comes from
// Tailwind utility classes applied here via each component's `pt` (passthrough) prop.
// Add an entry here the first time a given PrimeReact component is used anywhere
// in the app, so styling stays centralized instead of scattered per-usage.

export const buttonPT = {
  root: {
    className:
      "inline-flex items-center justify-center gap-2 rounded-md bg-emerald-600 px-4 py-2 " +
      "text-sm font-medium text-white transition-colors hover:bg-emerald-700 " +
      "disabled:cursor-not-allowed disabled:opacity-50 focus-visible:outline " +
      "focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-600",
  },
  label: { className: "flex-1" },
};

export const inputTextPT = {
  root: {
    className:
      "w-full rounded-md border border-gray-300 px-3 py-2 text-sm shadow-sm " +
      "focus:border-emerald-500 focus:outline-none focus:ring-1 focus:ring-emerald-500",
  },
};

export const inputNumberPT = {
  root: { className: "w-full" },
  input: {
    root: {
      className:
        "w-full rounded-md border border-gray-300 px-3 py-2 text-sm shadow-sm " +
        "focus:border-emerald-500 focus:outline-none focus:ring-1 focus:ring-emerald-500",
    },
  },
};
