import { fireEvent, render, screen } from '@testing-library/react';
import { test, expect, vi } from 'vitest';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { WorkspacePage } from './WorkspacePage';
import { MembersPage } from './MembersPage';

function setup(element: React.ReactNode, path = '/') {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(<QueryClientProvider client={client}><MemoryRouter initialEntries={[path]}><Routes>
    <Route path={path === '/' ? '/' : '/workspace/:tenantId'} element={element} />
    <Route path="/workspace/:createdId/created" element={<p>Created</p>} />
  </Routes></MemoryRouter></QueryClientProvider>);
}

test('empty organizations and invitations are shown without fake data', async () => {
  vi.spyOn(globalThis, 'fetch').mockImplementation(async () => new Response('[]'));
  setup(<WorkspacePage userId="test-user" />);
  expect(await screen.findByText('Noch keine Organisation vorhanden.')).toBeVisible();
  expect(await screen.findByText('Keine offenen Einladungen.')).toBeVisible();
  fireEvent.click(screen.getByRole('button', { name: 'Anlegen' }));
  expect(await screen.findByText('Mindestens zwei Zeichen erforderlich.')).toBeVisible();
});

test('viewer cannot see or fetch member management', async () => {
  const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response(JSON.stringify({ id: 'org', name: 'Example', role: 'Viewer' })));
  setup(<MembersPage userId="test-user" />, '/workspace/org');
  expect(await screen.findByText('Die Mitgliederverwaltung ist für Owner und Admins verfügbar.')).toBeVisible();
  expect(screen.queryByRole('button', { name: 'Einladen' })).not.toBeInTheDocument();
  expect(fetchMock).toHaveBeenCalledTimes(1);
});
