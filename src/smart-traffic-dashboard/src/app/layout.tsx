import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import Link from "next/link";
import "./globals.css";

const geistSans = Geist({ variable: "--font-geist-sans", subsets: ["latin"] });
const geistMono = Geist_Mono({ variable: "--font-geist-mono", subsets: ["latin"] });

export const metadata: Metadata = {
  title: "Smart Traffic AI Dashboard",
  description: "IoT Smart Traffic Light – Real-time monitoring & AI timing control",
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="vi" className={`${geistSans.variable} ${geistMono.variable} h-full antialiased`}>
      <body
        className="min-h-full flex flex-col"
        style={{ background: "linear-gradient(135deg, #0c1222 0%, #0f172a 40%, #111827 100%)" }}
      >
        {/* ── Global Navigation ── */}
        <nav
          className="border-b border-white/5 px-6 py-0 sticky top-0 z-50"
          style={{ background: "rgba(15,23,42,0.85)", backdropFilter: "blur(12px)" }}
        >
          <div className="max-w-[1400px] mx-auto flex items-center gap-1 h-11">
            <Link
              href="/"
              className="px-4 py-2 text-xs font-bold tracking-widest rounded-md transition-all hover:bg-white/5"
              style={{ color: "#94a3b8" }}
            >
              🚦 LIVE DASHBOARD
            </Link>
            <Link
              href="/history"
              className="px-4 py-2 text-xs font-bold tracking-widest rounded-md transition-all hover:bg-white/5"
              style={{ color: "#94a3b8" }}
            >
              📋 DETECTION HISTORY
            </Link>
          </div>
        </nav>

        {children}
      </body>
    </html>
  );
}
