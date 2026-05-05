import { expect, test } from '@playwright/test'

/**
 * Comparison tests pin the YouTrack reference rig as our IA gold standard.
 * Each test pairs a DoTrack page with its YouTrack counterpart so visual /
 * structural drift is caught early. For now this is a stub: we verify the
 * rig responds. Visual snapshot pairs land as the SPA grows real surfaces
 * to compare.
 */
test.describe('YouTrack reference rig', () => {
  test('rig responds at /', async ({ page }) => {
    const response = await page.goto('/')
    expect(response?.status()).toBe(200)
    // Even after the wizard, the page title is YouTrack-branded.
    await expect(page).toHaveTitle(/YouTrack/i)
  })
})
