import { defineConfig, devices } from "@playwright/test";

export default defineConfig({
  testDir: "./tests/e2e",
  fullyParallel: true,
  reporter: "list",
  use: {
    baseURL: "http://localhost:3001",
    trace: "retain-on-failure",
    screenshot: "only-on-failure",
    // รันด้วย `SLOWMO=500 npx playwright test --headed` เพื่อดูการกรอกฟอร์มบนจอจริง
    launchOptions: {
      slowMo: process.env.SLOWMO ? Number(process.env.SLOWMO) : undefined,
    },
  },
  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] },
    },
  ],
});
