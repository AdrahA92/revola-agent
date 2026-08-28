import { render, screen } from '@testing-library/react';
import { expect, test, vi } from 'vitest';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ConnectionsPage } from './ConnectionsPage';

test('manual browser link is never represented as a connected account', async () => {
  vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response(JSON.stringify([{ platform: 'facebook', name: 'Facebook', connected: false,
    canReadAccount: false, canPublish: false, manualPreparationAvailable: true, website: 'https://www.facebook.com/' }])));
  render(<QueryClientProvider client={new QueryClient()}><MemoryRouter initialEntries={['/workspace/tenant/connections']}><Routes>
    <Route path="/workspace/:tenantId/connections" element={<ConnectionsPage userId="user" />} />
  </Routes></MemoryRouter></QueryClientProvider>);
  expect(await screen.findByRole('heading', { name: 'Facebook' })).toBeVisible();
  expect(screen.getByRole('alert')).toHaveTextContent('Eine Anmeldung im separat geöffneten Browser verbindet das Konto nicht');
  const link = screen.getByRole('link', { name: 'Plattform manuell öffnen' });
  expect(link).toHaveAttribute('href', 'https://www.facebook.com/');
  expect(link).toHaveAttribute('rel', 'noopener noreferrer');
  expect(screen.getByText(/Status: Nicht verbunden/)).toBeVisible();
});
