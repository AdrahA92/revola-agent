import { fireEvent, render, screen } from '@testing-library/react';
import { expect, test, vi } from 'vitest';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { CompanyPage } from './CompanyPage';
import { AuditsPage } from './AuditsPage';

function setup(page: 'company' | 'audits', role: string) {
  const fetchMock = vi.spyOn(globalThis, 'fetch').mockImplementation(async input => {
    const path = String(input);
    if (path.includes('/company/profile')) return new Response(JSON.stringify({ profile: null }));
    if (path.includes('/company/knowledge') || path.includes('/demo-audits')) return new Response('[]');
    return new Response(JSON.stringify({ id: 'tenant', name: 'Example', role }));
  });
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(<QueryClientProvider client={client}><MemoryRouter initialEntries={[`/workspace/tenant/${page}`]}><Routes>
    <Route path="/workspace/:tenantId/company" element={<CompanyPage userId="user" />} />
    <Route path="/workspace/:tenantId/audits" element={<AuditsPage userId="user" />} />
  </Routes></MemoryRouter></QueryClientProvider>);
  return fetchMock;
}

test('company profile validates required facts before mutation', async () => {
  const fetchMock = setup('company', 'Owner');
  fireEvent.click(await screen.findByRole('button', { name: 'Profil speichern' }));
  expect((await screen.findAllByText('Pflichtfeld.')).length).toBeGreaterThan(0);
  expect(fetchMock.mock.calls.every(call => call[1]?.method === 'GET')).toBe(true);
});

test('viewer cannot edit company facts', async () => {
  setup('company', 'Viewer');
  expect(await screen.findByLabelText('Firmenname')).toBeDisabled();
  expect(screen.queryByRole('button', { name: 'Profil speichern' })).not.toBeInTheDocument();
  expect(screen.queryByRole('button', { name: 'Fakt speichern' })).not.toBeInTheDocument();
});

test('demo audits communicate limitations and viewers cannot start runs', async () => {
  setup('audits', 'Viewer');
  expect(await screen.findByText('Noch kein Audit gestartet.')).toBeVisible();
  expect(screen.getByRole('alert')).toHaveTextContent('Fiktive Beispieldaten');
  expect(screen.queryByRole('button', { name: 'Demo-Audit starten' })).not.toBeInTheDocument();
});
