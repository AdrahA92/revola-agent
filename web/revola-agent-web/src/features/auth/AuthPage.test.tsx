import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { test, expect, vi } from 'vitest';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AuthPage } from './AuthPage';

function setup(registerAccount = false) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
  client.setQueryData(['private-previous-account'], { value: 'must be cleared' });
  render(<QueryClientProvider client={client}><MemoryRouter><Routes>
    <Route path="/" element={<AuthPage registerAccount={registerAccount} />} />
    <Route path="/workspace" element={<p>Workspace loaded</p>} />
  </Routes></MemoryRouter></QueryClientProvider>);
  return client;
}

test('invalid credentials stay local and show validation messages', async () => {
  const fetchMock = vi.spyOn(globalThis, 'fetch');
  setup();
  fireEvent.change(screen.getByLabelText('E-Mail-Adresse'), { target: { value: 'not-an-email' } });
  fireEvent.click(screen.getByRole('button', { name: 'Anmelden' }));
  expect(await screen.findByText('Bitte geben Sie eine gültige E-Mail-Adresse ein.')).toBeVisible();
  expect(fetchMock).not.toHaveBeenCalled();
});

test('login sends CSRF and clears data from the previous account', async () => {
  const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValueOnce(new Response(JSON.stringify({ token: 'test-csrf' })))
    .mockResolvedValueOnce(new Response(null, { status: 204 }));
  const client = setup();
  fireEvent.change(screen.getByLabelText('E-Mail-Adresse'), { target: { value: 'example@example.test' } });
  fireEvent.change(screen.getByLabelText('Passwort'), { target: { value: 'Only-Test-Password-42!' } });
  fireEvent.click(screen.getByRole('button', { name: 'Anmelden' }));
  expect(await screen.findByText('Workspace loaded')).toBeVisible();
  expect(fetchMock).toHaveBeenCalledTimes(2);
  const request = fetchMock.mock.calls[1][1]!;
  expect((request.headers as Headers).get('X-CSRF-TOKEN')).toBe('test-csrf');
  expect(request.credentials).toBe('same-origin');
  expect(client.getQueryData(['private-previous-account'])).toBeUndefined();
});

test('registration never claims success when backend is unavailable', async () => {
  vi.spyOn(globalThis, 'fetch').mockResolvedValueOnce(new Response(JSON.stringify({ token: 'test-csrf' })))
    .mockResolvedValueOnce(new Response(null, { status: 503 }));
  setup(true);
  fireEvent.change(screen.getByLabelText('E-Mail-Adresse'), { target: { value: 'example@example.test' } });
  fireEvent.change(screen.getByLabelText('Passwort'), { target: { value: 'Only-Test-Password-42!' } });
  fireEvent.click(screen.getByRole('button', { name: 'Konto erstellen' }));
  expect(await screen.findByRole('alert')).toHaveTextContent('Diese Funktion ist derzeit nicht verfügbar.');
  expect(screen.queryByText('Konto erstellt')).not.toBeInTheDocument();
  await waitFor(() => expect(screen.getByRole('button', { name: 'Konto erstellen' })).toBeEnabled());
});

test('MFA challenge does not authenticate and sends the supplied second factor', async () => {
  const fetchMock = vi.spyOn(globalThis, 'fetch')
    .mockResolvedValueOnce(new Response(JSON.stringify({ token: 'csrf' })))
    .mockResolvedValueOnce(new Response(JSON.stringify({ requiresMfa: true }), { status: 202 }))
    .mockResolvedValueOnce(new Response(JSON.stringify({ token: 'csrf' })))
    .mockResolvedValueOnce(new Response(null, { status: 204 }));
  setup();
  fireEvent.change(screen.getByLabelText('E-Mail-Adresse'), { target: { value: 'example@example.test' } });
  fireEvent.change(screen.getByLabelText('Passwort'), { target: { value: 'Only-Test-Password-42!' } });
  fireEvent.click(screen.getByRole('button', { name: 'Anmelden' }));
  const code = await screen.findByLabelText('Authenticator-Code');
  expect(screen.queryByText('Workspace loaded')).not.toBeInTheDocument();
  fireEvent.change(code, { target: { value: '123456' } });
  fireEvent.click(screen.getByRole('button', { name: 'Anmelden' }));
  expect(await screen.findByText('Workspace loaded')).toBeVisible();
  expect(JSON.parse(fetchMock.mock.calls[3][1]!.body as string).code).toBe('123456');
});
