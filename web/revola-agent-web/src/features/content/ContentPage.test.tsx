import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { expect, test, vi } from 'vitest';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ContentPage } from './ContentPage';

function setup(role: string) {
  const fetchMock = vi.spyOn(globalThis, 'fetch').mockImplementation(async input => {
    const url = String(input);
    if (url.includes('/content') || url.includes('/agent-runs')) return new Response('[]');
    return new Response(JSON.stringify({ id: 'tenant', name: 'Example', role }));
  });
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(<QueryClientProvider client={client}><MemoryRouter initialEntries={['/workspace/tenant/content']}><Routes>
    <Route path="/workspace/:tenantId/content" element={<ContentPage userId="user" />} />
  </Routes></MemoryRouter></QueryClientProvider>);
  return fetchMock;
}

test('viewers cannot generate or edit and see the test-mode limitation', async () => {
  setup('Viewer');
  expect(await screen.findByText('Noch keine Inhalte gespeichert.')).toBeVisible();
  expect(screen.getByRole('alert')).toHaveTextContent('Keine OpenAI-Kosten');
  expect(screen.queryByRole('button', { name: 'Test-Agent starten' })).not.toBeInTheDocument();
  expect(screen.queryByRole('button', { name: 'Entwurf speichern' })).not.toBeInTheDocument();
});

test('empty briefings and drafts are rejected before mutation', async () => {
  const fetchMock = setup('Owner');
  fireEvent.click(await screen.findByRole('button', { name: 'Test-Agent starten' }));
  expect(await screen.findByText('Pflichtfeld.')).toBeVisible();
  fireEvent.click(screen.getByRole('button', { name: 'Entwurf speichern' }));
  await waitFor(() => expect(screen.getAllByText('Pflichtfeld.').length).toBeGreaterThan(1));
  expect(fetchMock.mock.calls.every(call => call[1]?.method === 'GET')).toBe(true);
});
