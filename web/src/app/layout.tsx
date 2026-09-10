import type { Metadata } from "next";
import { Kanit, IBM_Plex_Sans_Thai_Looped } from "next/font/google";
import "./globals.css";
import { AppProviders } from "./providers";

// Loopless geometric Thai - the face of Thai shop signage. Headings and
// every price, so numbers read from across the counter.
const kanit = Kanit({
  subsets: ["thai", "latin"],
  weight: ["400", "500", "600", "700"],
  variable: "--font-kanit",
  display: "swap",
});

// Looped (หัวกลม) Thai - what Thai readers scan fastest at UI sizes.
const plexThai = IBM_Plex_Sans_Thai_Looped({
  subsets: ["thai", "latin"],
  weight: ["400", "500", "600"],
  variable: "--font-plex-thai",
  display: "swap",
});

export const metadata: Metadata = {
  title: "TaladPOS",
  description: "ระบบ POS สำหรับร้านค้าเดี่ยว",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    // The font variables belong on <html>, not <body>. Tailwind 4 emits the
    // @theme tokens onto :root, so --font-display (which is defined as
    // "var(--font-kanit), sans-serif") is computed there - and a custom
    // property containing var() is resolved on the element that declares it.
    // With --font-kanit only on <body>, --font-display resolved to
    // guaranteed-invalid at :root and every `font-display` class silently fell
    // back to the inherited body face. Under Tailwind 3 the utility wrote
    // font-family directly on the element, so <body> was good enough.
    <html lang="th" className={`${kanit.variable} ${plexThai.variable}`}>
      <body>
        <AppProviders>{children}</AppProviders>
      </body>
    </html>
  );
}
