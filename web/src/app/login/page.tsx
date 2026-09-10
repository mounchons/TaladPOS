"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/lib/auth/AuthContext";
import { ApiError } from "@/lib/api/client";

export default function LoginPage() {
  const router = useRouter();
  const { login } = useAuth();
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setIsSubmitting(true);
    try {
      await login(username, password);
      router.push("/sales");
    } catch (err) {
      if (err instanceof ApiError && err.status === 401) {
        setError("ชื่อผู้ใช้หรือรหัสผ่านไม่ถูกต้อง ลองใหม่อีกครั้ง");
      } else {
        setError("เข้าสู่ระบบไม่สำเร็จ ตรวจการเชื่อมต่อแล้วลองใหม่");
      }
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <main className="flex min-h-screen items-center justify-center bg-ink px-5 py-10">
      <div className="w-full max-w-sm">
        <p className="mb-8 font-display text-2xl font-semibold tracking-tight text-white">
          TaladPOS
        </p>

        <form onSubmit={handleSubmit} className="rounded-control bg-white p-6">
          <h1 className="mb-6 text-lg font-medium text-ink">เปิดร้าน</h1>

          <label className="mb-1.5 block text-sm text-ink-700" htmlFor="username">
            ชื่อผู้ใช้
          </label>
          <input
            id="username"
            type="text"
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            className="input mb-4 w-full rounded-control border-steel-200 bg-white"
            autoFocus
            required
          />

          <label className="mb-1.5 block text-sm text-ink-700" htmlFor="password">
            รหัสผ่าน
          </label>
          {/* A native password field rather than a masked-toggle widget: the
              browser's own reveal control and password manager both work, and
              nothing here needs the strength meter PrimeReact shipped. */}
          <input
            id="password"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            className="input mb-5 w-full rounded-control border-steel-200 bg-white"
            required
          />

          {error && (
            <p className="mb-4 rounded-control border border-chili/30 bg-chili/5 px-3 py-2.5 text-sm text-chili">
              {error}
            </p>
          )}

          <button
            type="submit"
            disabled={isSubmitting}
            className="btn h-auto w-full rounded-control border-ink bg-ink py-3 font-display font-medium text-white hover:border-mango hover:bg-mango hover:text-ink"
          >
            {isSubmitting ? "กำลังเข้าสู่ระบบ" : "เข้าสู่ระบบ"}
          </button>
        </form>
      </div>
    </main>
  );
}
