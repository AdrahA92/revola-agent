import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import Alert from '@mui/material/Alert';
import Button from '@mui/material/Button';
import TextField from '@mui/material/TextField';
import { SessionGate } from './SessionGate';
import { Pagination } from '../tenancy/WorkspacePage';
import { api, errorMessage } from '../../lib/api';

type Session = { id: string; createdAt: string; expiresAt: string; isCurrent: boolean };
const proofSchema = z.object({ password: z.string().min(1, 'Passwort erforderlich.').max(128), code: z.string().max(6), recoveryCode: z.string().max(100) });
type Proof = z.infer<typeof proofSchema>;

export function SecurityPage() {
  const [codes, setCodes] = useState<string[] | null>(null);
  // Keep one-time codes outside the auth gate: enabling MFA deliberately ends all sessions.
  if (codes) return <section className="auth-page"><h1>Zwei-Faktor-Schutz aktiviert</h1>
    <Alert severity="warning">Speichern Sie diese einmalig angezeigten Wiederherstellungscodes sicher. Alle bisherigen Sitzungen sind beendet.</Alert>
    <ul>{codes.map(code => <li key={code}><code>{code}</code></li>)}</ul>
    <p>Für die Anmeldung warten Sie auf den nächsten Authenticator-Code oder verwenden einen Wiederherstellungscode.</p>
    <Button component={Link} to="/login">Codes gesichert – zur Anmeldung</Button></section>;
  return <SessionGate>{id => <SecuritySettings key={id} userId={id} onEnabled={setCodes} />}</SessionGate>;
}

function SecuritySettings({ userId, onEnabled }: { userId: string; onEnabled: (codes: string[]) => void }) {
  const [page, setPage] = useState(1);
  const [sharedKey, setSharedKey] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const cache = useQueryClient();
  const navigate = useNavigate();
  const status = useQuery({ queryKey: ['mfa', userId], queryFn: ({ signal }) => api<{ twoFactorEnabled: boolean; recoveryCodesRemaining: number }>('/identity/mfa/status', { signal }) });
  const sessions = useQuery({ queryKey: ['sessions', userId, page], queryFn: ({ signal }) => api<Session[]>(`/identity/sessions?page=${page}`, { signal }) });
  const { register, handleSubmit, reset, formState: { errors, isSubmitting } } = useForm<Proof>({ resolver: zodResolver(proofSchema), defaultValues: { password: '', code: '', recoveryCode: '' } });
  async function change(action: 'setup' | 'enable' | 'disable', proof: Proof) {
    setError('');
    try {
      if (action === 'setup') {
        const result = await api<{ sharedKey: string }>('/identity/mfa/setup', { method: 'POST', body: proof });
        setSharedKey(result.sharedKey);
      } else if (action === 'enable') {
        const result = await api<{ recoveryCodes: string[] }>('/identity/mfa/enable', { method: 'POST', body: proof });
        reset(); setSharedKey(''); onEnabled(result.recoveryCodes);
        await cache.cancelQueries(); cache.clear();
      } else {
        await api<void>('/identity/mfa/disable', { method: 'POST', body: proof });
        reset(); await cache.cancelQueries(); cache.clear(); navigate('/login', { replace: true });
      }
    } catch (failure) { setError(errorMessage(failure)); }
  }
  async function revoke(session: Session) {
    setBusy(true); setError('');
    try {
      await api<void>(`/identity/sessions/${session.id}`, { method: 'DELETE' });
      if (session.isCurrent) { await cache.cancelQueries(); cache.clear(); navigate('/login', { replace: true }); }
      else await cache.invalidateQueries({ queryKey: ['sessions', userId] });
    } catch (failure) { setError(errorMessage(failure)); } finally { setBusy(false); }
  }
  return <section className="workspace-page"><h1>Kontosicherheit</h1><p><Link to="/workspace">Zurück zu Ihren Organisationen</Link></p>
    {error ? <Alert severity="error">{error}</Alert> : null}
    <h2>Zwei-Faktor-Anmeldung</h2>
    {status.isPending ? <p role="status">Sicherheitsstatus wird geladen …</p> : status.isError ? <Alert severity="error">{errorMessage(status.error)} <Button onClick={() => void status.refetch()}>Erneut versuchen</Button></Alert> : <>
      <p>{status.data.twoFactorEnabled ? `Aktiv · ${status.data.recoveryCodesRemaining} Wiederherstellungscodes übrig.` : 'Noch nicht aktiviert.'}</p>
      {sharedKey ? <Alert severity="warning">Tragen Sie diesen Schlüssel manuell in Ihre Authenticator-App ein (TOTP, 6 Stellen, 30 Sekunden): <code>{sharedKey}</code>. Teilen Sie ihn nicht.</Alert> : null}
      <form noValidate className="auth-form" onSubmit={handleSubmit(values => change(status.data.twoFactorEnabled ? 'disable' : sharedKey ? 'enable' : 'setup', values))}>
        <TextField label="Aktuelles Passwort" type="password" autoComplete="current-password" {...register('password')} error={!!errors.password} helperText={errors.password?.message} sx={{ mt: 2 }} />
        {sharedKey || status.data.twoFactorEnabled ? <TextField label="Authenticator-Code" autoComplete="one-time-code" {...register('code')} sx={{ mt: 2 }} slotProps={{ htmlInput: { maxLength: 6, inputMode: 'numeric' } }} /> : null}
        {status.data.twoFactorEnabled ? <TextField label="Wiederherstellungscode (alternativ)" type="password" autoComplete="off" {...register('recoveryCode')} sx={{ mt: 2 }} /> : null}
        <p>Aktivieren oder Deaktivieren beendet alle Sitzungen und erfordert eine erneute Anmeldung.</p>
        <Button type="submit" variant="contained" disabled={isSubmitting}>{status.data.twoFactorEnabled ? 'Zwei-Faktor-Schutz deaktivieren' : sharedKey ? 'Code prüfen und aktivieren' : 'Authenticator einrichten'}</Button>
      </form></>}
    <h2>Aktive Sitzungen</h2>
    {sessions.isPending ? <p role="status">Sitzungen werden geladen …</p> : sessions.isError ? <Alert severity="error">{errorMessage(sessions.error)} <Button onClick={() => void sessions.refetch()}>Erneut versuchen</Button></Alert> : <>
      <ul className="resource-list">{sessions.data.map(session => <li key={session.id}><span>{session.isCurrent ? 'Diese Sitzung' : 'Weitere Sitzung'} · Beginn {new Date(session.createdAt).toLocaleString('de-DE', { timeZone: 'UTC' })} UTC</span>
        <Button disabled={busy} onClick={() => void revoke(session)}>Sitzung abmelden</Button></li>)}</ul>
      <Pagination page={page} setPage={setPage} hasNext={sessions.data.length === 50} /></>}
  </section>;
}
