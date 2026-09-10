// PrimeReact runs in `unstyled` mode (see specs/001-single-store-pos/research.md #7):
// PrimeReact supplies behavior/accessibility only; all visual styling comes from
// Tailwind utility classes applied here via each component's `pt` (passthrough) prop.
// Add an entry here the first time a given PrimeReact component is used anywhere
// in the app, so styling stays centralized instead of scattered per-usage.
//
// Design system: cool steel ground, ink for anything you press, and mango
// reserved for money and for marking where you are. Flat surfaces with a 1px
// border - no drop shadows, so the one glowing thing on the sales screen is
// the total.

// whitespace-nowrap because Thai has no word spaces: a button label that wraps
// breaks mid-word and doubles the control's height, which on a narrow screen
// drags whatever row it sits in out of shape.
const CONTROL_BASE =
  "inline-flex shrink-0 items-center justify-center gap-2 whitespace-nowrap rounded-control " +
  "px-4 py-2.5 text-sm font-medium transition-colors disabled:cursor-not-allowed disabled:opacity-40";

const FIELD_BASE =
  "w-full rounded-control border border-steel-200 bg-white px-3 py-2.5 text-sm text-ink " +
  "placeholder:text-ink-300 focus:border-mango focus:outline-none focus:ring-1 focus:ring-mango/50";

export const buttonPT = {
  root: { className: `${CONTROL_BASE} bg-ink text-white hover:bg-ink-700` },
  label: { className: "flex-1" },
};

export const secondaryButtonPT = {
  root: {
    className: `${CONTROL_BASE} border border-steel-200 bg-white text-ink-700 hover:border-ink-300 hover:bg-steel-50`,
  },
  label: { className: "flex-1" },
};

export const inputTextPT = {
  root: { className: FIELD_BASE },
};

export const passwordPT = {
  root: { className: "relative inline-flex w-full" },
  input: { className: `${FIELD_BASE} pr-10` },
  // toggleMask renders this outside the field unless it is positioned.
  showIcon: { className: "absolute right-3 top-1/2 -mt-2 text-ink-300 hover:text-ink" },
  hideIcon: { className: "absolute right-3 top-1/2 -mt-2 text-ink-300 hover:text-ink" },
};

export const inputNumberPT = {
  root: { className: "w-full" },
  input: { root: { className: `${FIELD_BASE} money` } },
};

// Quantity stepper inside the dark register panel. Laid out horizontally with
// full-height hit areas: this is the control a cashier taps most, on a touch
// screen, so the stacked micro-arrows PrimeReact defaults to are too small.
const DARK_STEPPER_BUTTON =
  "flex h-10 w-10 shrink-0 items-center justify-center bg-ink-700 text-sm text-ink-300 " +
  "transition-colors hover:bg-ink-500 hover:text-white disabled:opacity-40 disabled:hover:bg-ink-700";

export const darkInputNumberPT = {
  root: { className: "inline-flex overflow-hidden rounded-control border border-ink-700" },
  input: {
    root: {
      className:
        "money h-10 w-11 border-0 bg-ink-700 px-0 text-center text-sm text-white " +
        "focus:outline-none focus:ring-1 focus:ring-inset focus:ring-mango",
    },
  },
  decrementButton: { className: `${DARK_STEPPER_BUTTON} order-first` },
  incrementButton: { className: DARK_STEPPER_BUTTON },
};

// Structure adapted from PrimeReact's own bundled Tailwind PT preset
// (node_modules/primereact/passthrough/tailwind) so the real pt key names are
// correct, re-themed to this app.
// Pair every DataTable using this with `responsiveLayout="stack"`: below md
// that turns each row into a card (see the table block in globals.css), which
// is the only way seven Thai columns fit on a phone. The overflow-x-auto here
// covers md and up, where the table stays a table.
export const dataTablePT = {
  root: { className: "overflow-x-auto rounded-control border border-steel-200 bg-white" },
  table: { className: "w-full border-collapse text-sm md:min-w-[44rem]" },
  thead: { className: "bg-steel-50" },
  tbody: {},
  headerRow: {},
  bodyRow: { className: "border-t border-steel-100 hover:bg-mango-100/40" },
  column: {
    headerCell: {
      className: "border-b border-steel-200 px-4 py-3 text-left text-xs font-medium text-ink-500",
    },
    bodyCell: { className: "px-4 py-3 align-middle text-ink-700" },
  },
};

// Note: in single-select mode AutoComplete renders root > input directly - it
// has no `container` element, so the field styling has to live on input.root.
export const autoCompletePT = {
  root: { className: "relative inline-flex w-full" },
  input: { root: { className: FIELD_BASE } },
  panel: { className: "rounded-control border border-steel-200 bg-white shadow-lg" },
  list: { className: "m-0 list-none p-1" },
  item: { className: "cursor-pointer rounded px-3 py-2 text-sm hover:bg-mango-100" },
};

// Member lookup inside the dark register panel.
export const darkAutoCompletePT = {
  root: { className: "relative inline-flex w-full" },
  input: {
    root: {
      className:
        "w-full rounded-control border border-ink-700 bg-ink-700 px-3 py-2.5 text-sm text-white " +
        "placeholder:text-ink-300 focus:border-mango focus:outline-none",
    },
  },
  panel: { className: "rounded-control border border-steel-200 bg-white shadow-lg" },
  list: { className: "m-0 list-none p-1" },
  item: { className: "cursor-pointer rounded px-3 py-2 text-sm text-ink hover:bg-mango-100" },
};

export const dropdownPT = {
  // flex, not the default block: the trigger is a sibling of the input, so
  // without it the chevron stacks underneath the field instead of sitting in
  // its right edge, and the control renders ~16px too tall.
  root: {
    className:
      "flex w-full cursor-pointer items-center rounded-control border border-steel-200 bg-white " +
      "focus:border-mango focus:outline-none focus:ring-1 focus:ring-mango/50",
  },
  input: {
    className:
      "min-w-0 flex-1 overflow-hidden text-ellipsis whitespace-nowrap px-3 py-2.5 text-sm text-ink",
  },
  trigger: { className: "flex w-9 shrink-0 items-center justify-center text-ink-300" },
  panel: { className: "rounded-control border border-steel-200 bg-white shadow-lg" },
  list: { className: "m-0 list-none p-1" },
  item: { className: "cursor-pointer rounded px-3 py-2 text-sm hover:bg-mango-100" },
};

export const calendarPT = {
  root: { className: "inline-flex w-full" },
  input: { root: { className: FIELD_BASE } },
  panel: { className: "rounded-control border border-steel-200 bg-white p-2 shadow-lg" },
};

export const checkboxPT = {
  root: { className: "inline-flex h-5 w-5 cursor-pointer select-none align-middle" },
  box: ({ context }: { context: { checked: boolean } }) => ({
    className:
      "flex h-5 w-5 items-center justify-center rounded-[3px] border transition-colors " +
      (context.checked ? "border-ink bg-ink" : "border-steel-200 bg-white"),
  }),
  icon: { className: "h-3 w-3 text-white" },
};

export const tabViewPT = {
  // Four Thai tab labels are wider than a phone. Scroll them sideways rather
  // than wrap, same as AppShell's nav - Thai has no word spaces, so a wrapped
  // label breaks mid-word and the tabs interleave into nonsense.
  nav: {
    className:
      "flex list-none overflow-x-auto border-b border-steel-200 [&::-webkit-scrollbar]:hidden",
  },
};

export const tabPanelPT = {
  headerAction: ({
    parent,
    context,
  }: {
    parent: { state: { activeIndex: number } };
    context: { index: number };
  }) => ({
    className:
      "-mb-px shrink-0 cursor-pointer select-none whitespace-nowrap border-b-2 px-4 py-3 " +
      "text-sm transition-colors " +
      (parent.state.activeIndex === context.index
        ? "border-mango font-medium text-ink"
        : "border-transparent text-ink-500 hover:text-ink"),
  }),
  content: { className: "pt-5" },
};

export const dialogPT = {
  // Capped and scrollable so a long form still fits a short phone, with the
  // scroll on the content rather than the whole dialog - otherwise the title
  // and its close button scroll out of reach.
  root: {
    className:
      "flex max-h-[90dvh] w-[calc(100vw-2rem)] max-w-md flex-col overflow-hidden " +
      "rounded-control border border-steel-200 bg-white",
  },
  header: {
    className: "flex shrink-0 items-center justify-between border-b border-steel-100 px-5 py-4",
  },
  headerTitle: { className: "font-display text-base font-medium text-ink" },
  closeButton: { className: "rounded p-1 text-ink-300 hover:bg-steel-50 hover:text-ink" },
  content: { className: "min-h-0 flex-1 overflow-y-auto px-5 py-5" },
  footer: {
    className: "flex shrink-0 justify-end gap-2 border-t border-steel-100 px-5 py-3",
  },
  mask: { className: "bg-ink/50" },
};

// The receipt dialog frames a till slip, which brings its own padding and
// wants to sit edge to edge inside the dialog - so this drops the content
// padding and narrows the shell to slip width instead of form width.
export const receiptDialogPT = {
  ...dialogPT,
  root: {
    className:
      "flex max-h-[90dvh] w-[calc(100vw-2rem)] max-w-[24rem] flex-col overflow-hidden " +
      "rounded-control border border-steel-200 bg-white",
  },
  content: { className: "min-h-0 flex-1 overflow-y-auto" },
};
