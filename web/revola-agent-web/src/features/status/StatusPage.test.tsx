import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { StatusPage } from './StatusPage';
import { test, expect, vi } from 'vitest';

function mount() {
  return render(<QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}><StatusPage /></QueryClientProvider>);
}

test('shows unchecked status without triggering external calls', () => {
  const fetchMock = vi.fn(); vi.stubGlobal('fetch', fetchMock); mount();
  expect(screen.getAllByText('Noch nicht geprüft')).toHaveLength(2);
  expect(fetchMock).not.toHaveBeenCalled();
});

test('checks live and ready independently on request', async () => {
  vi.stubGlobal('fetch', vi.fn().mockImplementation((path: string) => Promise.resolve(new Response(
    JSON.stringify({status: path.endsWith('live') ? 'Healthy' : 'Unhealthy'}),
    {status: path.endsWith('live') ? 200 : 503},
  ))));
  mount(); fireEvent.click(screen.getByRole('button'));
  await waitFor(() => expect(screen.getByText('Nicht verfügbar')).toBeInTheDocument());
  expect(screen.getAllByText('Bereit')).toHaveLength(2);
});

test('network failure is visible and can be retried', async () => {
  vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new Error('network'))); mount();
  fireEvent.click(screen.getByRole('button'));
  await waitFor(() => expect(screen.getAllByText('Nicht verfügbar')).toHaveLength(2));
  expect(screen.getByRole('button')).toBeEnabled();
});
