import type { Metadata } from "next";
import { Kanit, IBM_Plex_Sans_Thai_Looped } from "next/font/google";
import "primeicons/primeicons.css";
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
    <html lang="th">
      <body className={`${kanit.variable} ${plexThai.variable}`}>
        <AppProviders>{children}</AppProviders>
      </body>
    </html>
  );
}
