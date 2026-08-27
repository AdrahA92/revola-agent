import { test, expect } from '@playwright/test';

test('technical status page and health checks', async ({ page }) => {
  const errors: string[] = []; page.on('pageerror', error => errors.push(error.message));
  await page.route('**/health/*', route => route.fulfill({ json: { status: 'Healthy' } }));
  await page.goto('/');
  await expect(page).toHaveTitle('Revola Agent – Systemstatus');
  await expect(page.getByRole('heading', { name: 'Die Grundlage steht.' })).toBeVisible();
  await expect(page.getByText('Noch nicht geprüft')).toHaveCount(2);
  await page.getByRole('button', { name: 'Verbindung prüfen' }).click();
  await expect(page.getByText('Bereit', { exact: true })).toHaveCount(3);
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  expect(errors).toEqual([]);
});

test('unavailable services are reported honestly', async ({page}) => {
  await page.route('**/health/*', route => route.fulfill({ status: 503, json: {status:'Unhealthy'} }));
  await page.goto('/'); await page.getByRole('button').click();
  await expect(page.getByText('Nicht verfügbar')).toHaveCount(2);
});
