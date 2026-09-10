/**
 * quickstart.md 7.7 - FR-037 and SC-010, measured against the real modules.
 *
 * Compiles src/lib/api/{client,products,tokenStore}.ts to a temp directory and
 * calls them from node, so this exercises the code the app ships rather than a
 * copy that could drift. `searchProducts({}, 3)` forces a small page size: with
 * only a couple of dozen products in the dev database, asking at the ceiling
 * would complete in one request and prove nothing about the walk.
 *
 * Run (api on :8080, no dev server needed):  node scripts/verify-fetch-all.cjs
 */
const { execFileSync } = require("child_process");
const fs = require("fs");
const os = require("os");
const path = require("path");

const API = process.env.TALADPOS_API ?? "http://localhost:8080";
process.env.NEXT_PUBLIC_API_BASE_URL = API;

const WEB = path.resolve(__dirname, "..");
const BUILD = fs.mkdtempSync(path.join(os.tmpdir(), "taladpos-verify-"));

function compile() {
  const sources = ["client.ts", "products.ts", "tokenStore.ts"].map((f) =>
    path.join("src", "lib", "api", f),
  );
  // The tsc binary is invoked through node directly rather than through npx:
  // npx is a .cmd shim on Windows and execFileSync cannot spawn it.
  execFileSync(
    process.execPath,
    [require.resolve("typescript/bin/tsc"), ...sources,
     "--outDir", BUILD, "--module", "commonjs",
     "--target", "es2022", "--moduleResolution", "node", "--skipLibCheck"],
    { cwd: WEB, stdio: "inherit" },
  );
}

let requests = [];
const realFetch = globalThis.fetch;
globalThis.fetch = (url, init) => {
  requests.push(String(url));
  return realFetch(url, init);
};

let pass = 0;
let fail = 0;
function check(label, ok, detail) {
  console.log(`${ok ? "PASS" : "FAIL"}  ${label}${detail ? "  ->  " + detail : ""}`);
  ok ? pass++ : fail++;
}

async function main() {
  compile();

  const token = await realFetch(`${API}/api/v1/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ username: "manager", password: "Manager123!" }),
  })
    .then((r) => r.json())
    .then((r) => r.token);

  // getToken() reads window.localStorage when it is called, so a shim is enough.
  globalThis.window = {
    localStorage: { getItem: (k) => (k === "taladpos_token" ? token : null) },
  };

  const products = require(path.join(BUILD, "products.js"));
  const expected = await realFetch(`${API}/api/v1/products?pageSize=100`, {
    headers: { Authorization: `Bearer ${token}` },
  }).then((r) => r.json());

  // ---- FR-037: the walk assembles every page -----------------------------
  requests = [];
  const all = await products.searchProducts({}, 3);
  const calls = requests.filter((u) => u.includes("/api/v1/products"));
  const ids = all.map((p) => p.id);
  const pagesSeen = calls.map((u) => (u.match(/page=(\d+)/) || [])[1]).join(",");
  const lastPage = Math.ceil(expected.totalCount / 3);

  check("walk issues more than one request", calls.length > 1,
    `${calls.length} requests at pageSize=3`);
  check("walk returns every row", all.length === expected.totalCount,
    `${all.length} assembled vs totalCount ${expected.totalCount}`);
  check("walk returns no duplicate ids", new Set(ids).size === ids.length,
    `${new Set(ids).size} unique of ${ids.length}`);
  check("walk reaches the last page", calls.some((u) => u.includes(`page=${lastPage}`)),
    `pages seen: ${pagesSeen}`);
  check("one page really is a subset (the old cap was a real loss)",
    Math.min(3, all.length) < all.length, `3 < ${all.length}`);

  // ---- SC-010: one box, name or barcode ----------------------------------
  const sample = expected.items.find((p) => p.barcode);
  const byName = await products.searchProductsByNameOrBarcode(sample.name);
  const byBarcode = await products.searchProductsByNameOrBarcode(sample.barcode);

  check("typing a product name finds it", byName.some((p) => p.id === sample.id),
    `${JSON.stringify(sample.name)} -> ${byName.length} hit(s)`);
  check("scanning a barcode finds it", byBarcode.some((p) => p.id === sample.id),
    `${sample.barcode} -> ${byBarcode.length} hit(s)`);
  check("the barcode hit is listed first",
    byBarcode[0] && byBarcode[0].id === sample.id,
    byBarcode[0] ? byBarcode[0].name : "(empty)");

  // The endpoint ANDs search and barcode. Sending one term as both is what the
  // register used to do; keep asserting it returns nothing so nobody "fixes"
  // searchProductsByNameOrBarcode back into a single request.
  const anded = await products.searchProducts({ search: sample.name, barcode: sample.name });
  check("search+barcode with the same term is still empty (they AND)",
    anded.length === 0, `${anded.length} rows`);

  const blank = await products.searchProductsByNameOrBarcode("   ");
  check("a blank query still shows the whole shelf",
    blank.length === expected.totalCount, `${blank.length} products`);

  console.log(`\n=== ${pass} PASS / ${fail} FAIL จาก ${pass + fail} ===`);
  fs.rmSync(BUILD, { recursive: true, force: true });
  process.exit(fail ? 1 : 0);
}

main().catch((e) => {
  console.error(e);
  fs.rmSync(BUILD, { recursive: true, force: true });
  process.exit(1);
});
