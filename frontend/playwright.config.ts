import { defineConfig, devices } from '@playwright/test'

/**
 * Playwright config — drives both the DoTrack SPA and the YouTrack reference rig.
 *
 * Two projects let you write tests scoped to either app, or write comparison
 * tests that hit both. Pass `--project=dotrack` or `--project=youtrack` to run
 * one in isolation.
 *
 * Both servers must be running before tests:
 *   docker compose -f .dev/docker-compose.yml up -d   # postgres + youtrack
 *   dotnet run --project src/DoTrack.Api              # API on :5259
 *   pnpm dev                                          # SPA on :5273
 */
export default defineConfig({
  testDir: './tests/e2e',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: 'list',

  use: {
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },

  projects: [
    {
      name: 'dotrack',
      testMatch: /dotrack\..*\.spec\.ts/,
      use: {
        ...devices['Desktop Chrome'],
        baseURL: 'http://localhost:5273',
      },
    },
    {
      name: 'youtrack',
      testMatch: /youtrack\..*\.spec\.ts/,
      use: {
        ...devices['Desktop Chrome'],
        baseURL: 'http://localhost:8888',
        // YouTrack's wizard validation page sometimes 403s tools that look like
        // bots; relax host checks since this is a local rig.
        ignoreHTTPSErrors: true,
      },
    },
  ],
})
