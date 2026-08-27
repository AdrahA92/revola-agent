import { fireEvent, render, screen } from '@testing-library/react';
import { test, expect, vi } from 'vitest';
import { MemoryRouter } from 'react-router-dom';
import { RecoveryPage } from './RecoveryPage';

test('recovery request uses generic success and no account disclosure', async () => {
  vi.spyOn(globalThis, 'fetch').mockResolvedValueOnce(new Response(JSON.stringify({ token: 'csrf' })))
    .mockResolvedValueOnce(new Response(JSON.stringify({ accepted: true }), { status: 202 }));
  render(<MemoryRouter><RecoveryPage mode="forgot" /></MemoryRouter>);
  fireEvent.change(screen.getByLabelText('E-Mail-Adresse'), { target: { value: 'example@example.test' } });
  fireEvent.click(screen.getByRole('button', { name: 'Passwort vergessen' }));
  expect(await screen.findByRole('alert')).toHaveTextContent('Anfrage angenommen');
  expect(screen.getByRole('link', { name: 'Code eingeben' })).toHaveAttribute('href', '/reset');
});

test('confirmation validates identity and token before any request', async () => {
  const fetchMock = vi.spyOn(globalThis, 'fetch');
  render(<MemoryRouter><RecoveryPage mode="confirm" /></MemoryRouter>);
  fireEvent.click(screen.getByRole('button', { name: 'E-Mail bestätigen' }));
  expect(await screen.findByText('Benutzer-ID aus der Nachricht erforderlich.')).toBeVisible();
  expect(fetchMock).not.toHaveBeenCalled();
});
