import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import Button from '@mui/material/Button';
import TextField from '@mui/material/TextField';
import Alert from '@mui/material/Alert';
import { api, errorMessage } from '../../lib/api';

export type Tenant = { id: string; name: string; role: string };
type Invitation = { tenantId: string; name: string; role: string; version: string };
const schema = z.object({ name: z.string().trim().min(2, 'Mindestens zwei Zeichen erforderlich.').max(160) });

export function WorkspacePage({ userId }: { userId: string }) {
  const [page, setPage] = useState(1);
  const [invitationPage, setInvitationPage] = useState(1);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const [draftId] = useState(() => crypto.randomUUID());
  const cache = useQueryClient();
  const navigate = useNavigate();
  const tenants = useQuery({ queryKey: ['tenants', userId, page], queryFn: ({ signal }) => api<Tenant[]>(`/tenants?page=${page}`, { signal }) });
  const invitations = useQuery({ queryKey: ['invitations', userId, invitationPage], queryFn: ({ signal }) => api<Invitation[]>(`/invitations?page=${invitationPage}`, { signal }) });
  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<z.infer<typeof schema>>({ resolver: zodResolver(schema), defaultValues: { name: '' } });
  const create = handleSubmit(async body => {
    setError('');
    try {
      await api<Tenant>(`/tenants/${draftId}`, { method: 'PUT', body });
      await cache.invalidateQueries({ queryKey: ['tenants', userId] });
      navigate(`/workspace/${draftId}`);
    } catch (failure) { setError(errorMessage(failure)); }
  });
  async function accept(invitation: Invitation) {
    setBusy(true); setError('');
    try {
      await api<void>(`/invitations/${invitation.tenantId}/accept`, { method: 'PUT', body: { version: invitation.version } });
      await Promise.all([cache.invalidateQueries({ queryKey: ['invitations', userId] }), cache.invalidateQueries({ queryKey: ['tenants', userId] })]);
    } catch (failure) { setError(errorMessage(failure)); } finally { setBusy(false); }
  }
  async function logout() {
    setBusy(true); setError('');
    try {
      await api<void>('/identity/logout', { method: 'POST' });
      await cache.cancelQueries(); cache.clear(); navigate('/login', { replace: true });
    } catch (failure) { setError(errorMessage(failure)); } finally { setBusy(false); }
  }
  return <section className="workspace-page">
    <div className="section-heading"><h1>Ihre Organisationen</h1><Button onClick={() => void logout()} disabled={busy}>Alle Sitzungen abmelden</Button></div>
    <p className="account-id">Ihre Benutzer-ID für Einladungen: <code>{userId}</code></p>
    {error ? <Alert severity="error">{error}</Alert> : null}
    {tenants.isPending ? <p role="status">Organisationen werden geladen …</p> : tenants.isError ? <Alert severity="error">{errorMessage(tenants.error)} <Button onClick={() => void tenants.refetch()}>Erneut versuchen</Button></Alert> :
      <><ul className="resource-list">{tenants.data.length === 0 ? <li>Noch keine Organisation vorhanden.</li> : tenants.data.map(tenant => <li key={tenant.id}>
        <Link to={`/workspace/${tenant.id}`}>{tenant.name}</Link><span>{tenant.role}</span></li>)}</ul>
        <Pagination page={page} setPage={setPage} hasNext={tenants.data.length === 50} /></>}
    <h2>Organisation anlegen</h2>
    <form onSubmit={create} noValidate className="inline-form">
      <TextField label="Name der Organisation" {...register('name')} error={!!errors.name} helperText={errors.name?.message} fullWidth slotProps={{ htmlInput: { maxLength: 160 } }} />
      <Button variant="contained" type="submit" disabled={isSubmitting}>Anlegen</Button>
    </form>
    <h2>Einladungen</h2>
    {invitations.isPending ? <p role="status">Einladungen werden geladen …</p> : invitations.isError ? <Alert severity="error">{errorMessage(invitations.error)} <Button onClick={() => void invitations.refetch()}>Erneut versuchen</Button></Alert> :
      <><ul className="resource-list">{invitations.data.length === 0 ? <li>Keine offenen Einladungen.</li> : invitations.data.map(item => <li key={item.tenantId}>
        <span>{item.name} · {item.role}</span><Button onClick={() => void accept(item)} disabled={busy}>Einladung annehmen</Button></li>)}</ul>
        <Pagination page={invitationPage} setPage={setInvitationPage} hasNext={invitations.data.length === 50} /></>}
  </section>;
}

export function Pagination({ page, setPage, hasNext }: { page: number; setPage: (page: number) => void; hasNext: boolean }) {
  return <div className="pagination"><Button disabled={page === 1} onClick={() => setPage(page - 1)}>Zurück</Button><span>Seite {page}</span>
    <Button disabled={!hasNext || page >= 10000} onClick={() => setPage(page + 1)}>Weiter</Button></div>;
}
