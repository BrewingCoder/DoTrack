import { expect, test } from '@playwright/test'

test.describe('DoTrack SPA smoke', () => {
  test('root redirects into a workspace', async ({ page }) => {
    await page.goto('/')
    await expect(page).toHaveURL(/\/workspaces\/[^/]+$/)
  })

  test('projects table renders for dotrack workspace', async ({ page }) => {
    await page.goto('/workspaces/dotrack')
    await expect(page.getByRole('heading', { name: 'Projects' })).toBeVisible()
    await expect(page.getByRole('columnheader', { name: 'Key' })).toBeVisible()
  })

  test('clicking a project key opens its work items page', async ({ page }) => {
    await page.goto('/workspaces/dotrack')
    const dotLink = page.getByRole('link', { name: 'DOT', exact: true }).first()
    await dotLink.click()
    await expect(page).toHaveURL(/\/projects\/DOT\/items$/)
    await expect(page.getByRole('heading', { name: /DOT.*Work items/ })).toBeVisible()
  })

  test('opening an item shows description, comments, history tabs', async ({ page }) => {
    await page.goto('/workspaces/dotrack/projects/DOT/items/3')
    await expect(page.getByRole('heading', { name: 'Login fails on iOS' })).toBeVisible()
    await expect(page.getByRole('tab', { name: /Comments/ })).toBeVisible()
    await expect(page.getByRole('tab', { name: /History/ })).toBeVisible()
  })
})
