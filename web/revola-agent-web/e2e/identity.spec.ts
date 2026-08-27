import { test, expect } from '@playwright/test';

test('login, organization creation and member view use the API', async ({ page }) => {
  const userId = '10000000-0000-4000-8000-000000000001';
  let tenant: { id: string; name: string; role: string } | undefined;
  const errors: string[] = [];
  page.on('pageerror', error => errors.push(error.message));
  // Deliberately mocked browser contract test. Backend identity/CSRF/isolation have separate real HTTP tests.
  await page.route('**/api/**', async route => {
    const request = route.request();
    const path = new URL(request.url()).pathname;
    if (path === '/api/identity/csrf') return route.fulfill({ json: { token: 'test-csrf' } });
    if (path === '/api/identity/login') {
      expect(request.headers()['x-csrf-token']).toBe('test-csrf');
      return route.fulfill({ status: 204 });
    }
    if (path === '/api/identity/me') return route.fulfill({ json: { id: userId } });
    if (path === '/api/invitations') return route.fulfill({ json: [] });
    if (path === '/api/tenants') return route.fulfill({ json: tenant ? [tenant] : [] });
    if (request.method() === 'PUT' && /^\/api\/tenants\/[^/]+$/.test(path)) {
      tenant = { id: path.split('/').at(-1)!, name: request.postDataJSON().name, role: 'Owner' };
      return route.fulfill({ json: tenant });
    }
    if (tenant && path === `/api/tenants/${tenant.id}`) return route.fulfill({ json: tenant });
    if (tenant && path === `/api/tenants/${tenant.id}/members`)
      return route.fulfill({ json: [{ userId, role: 'Owner', active: true, version: '20000000-0000-4000-8000-000000000001' }] });
    return route.fulfill({ status: 404 });
  });
  await page.goto('/login');
  await expect(page).toHaveTitle('Revola Agent – Anmelden');
  await page.getByLabel('E-Mail-Adresse').fill('example@example.test');
  await page.getByLabel('Passwort', { exact: true }).fill('Only-Test-Password-42!');
  await page.getByRole('button', { name: 'Anmelden', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Ihre Organisationen' })).toBeVisible();
  await expect(page.getByText('Noch keine Organisation vorhanden.')).toBeVisible();
  await page.getByLabel('Name der Organisation').fill('Example Company');
  await page.getByRole('button', { name: 'Anlegen', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Example Company' })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Mitglieder', exact: true })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Entfernen', exact: true })).toHaveCount(0);
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  expect(errors).toEqual([]);
});

test('login validates input and registration reports unavailable backend', async ({ page }) => {
  await page.route('**/api/identity/csrf', route => route.fulfill({ json: { token: 'test-csrf' } }));
  await page.route('**/api/identity/register', route => route.fulfill({ status: 503 }));
  await page.goto('/login');
  await page.getByRole('button', { name: 'Anmelden', exact: true }).click();
  await expect(page.getByText('Bitte geben Sie Ihr Passwort ein.')).toBeVisible();
  await page.getByRole('link', { name: 'Registrieren', exact: true }).click();
  await page.getByLabel('E-Mail-Adresse').fill('example@example.test');
  await page.getByLabel('Passwort', { exact: true }).fill('Only-Test-Password-42!');
  await page.getByRole('button', { name: 'Konto erstellen' }).click();
  await expect(page.getByRole('alert')).toHaveText('Diese Funktion ist derzeit nicht verfügbar.');
  await expect(page.getByText('Konto erstellt', { exact: true })).toHaveCount(0);
});
