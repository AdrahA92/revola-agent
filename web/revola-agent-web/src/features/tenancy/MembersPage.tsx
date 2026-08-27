import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import Alert from '@mui/material/Alert';
import Button from '@mui/material/Button';
import TextField from '@mui/material/TextField';
import MenuItem from '@mui/material/MenuItem';
import Dialog from '@mui/material/Dialog';
import DialogTitle from '@mui/material/DialogTitle';
import DialogContent from '@mui/material/DialogContent';
import DialogActions from '@mui/material/DialogActions';
import { api, errorMessage } from '../../lib/api';
import { Pagination } from './WorkspacePage';
import type { Tenant } from './WorkspacePage';

type Member = { userId: string; role: string; active: boolean; version: string };
type Audit = { id: string; action: string; actorId: string; occurredAt: string };
const roles = ['Admin', 'Manager', 'Editor', 'Approver', 'Viewer'];
const schema = z.object({ userId: z.uuid('Eine gültige Benutzer-ID ist erforderlich.'), role: z.enum(['Admin', 'Manager', 'Editor', 'Approver', 'Viewer']) });

export function MembersPage({ userId }: { userId: string }) {
  const { tenantId = '' } = useParams();
  const [page, setPage] = useState(1);
  const [auditPage, setAuditPage] = useState(1);
  const [showAudit, setShowAudit] = useState(false);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const [removing, setRemoving] = useState<Member | null>(null);
  const cache = useQueryClient();
  const tenant = useQuery({ queryKey: ['tenant', userId, tenantId], queryFn: ({ signal }) => api<Tenant>(`/tenants/${tenantId}`, { signal }) });
  const canManage = tenant.data?.role === 'Owner' || tenant.data?.role === 'Admin';
  const members = useQuery({ queryKey: ['members', userId, tenantId, page], enabled: canManage,
    queryFn: ({ signal }) => api<Member[]>(`/tenants/${tenantId}/members?page=${page}`, { signal }) });
  const audit = useQuery({ queryKey: ['audit', userId, tenantId, auditPage], enabled: canManage && showAudit,
    queryFn: ({ signal }) => api<Audit[]>(`/tenants/${tenantId}/audit?page=${auditPage}`, { signal }) });
  const form = useForm<z.infer<typeof schema>>({ resolver: zodResolver(schema), defaultValues: { userId: '', role: 'Viewer' } });
  const availableRoles = roles.filter(role => tenant.data?.role === 'Owner' || role !== 'Admin');
  async function mutate(path: string, method: string, body?: unknown) {
    setBusy(true); setError('');
    try {
      await api(`/tenants/${tenantId}/members/${path}`, { method, body });
      await Promise.all([cache.invalidateQueries({ queryKey: ['members', userId, tenantId] }),
        cache.invalidateQueries({ queryKey: ['audit', userId, tenantId] }), cache.invalidateQueries({ queryKey: ['tenant', userId, tenantId] })]);
      setRemoving(null);
    } catch (failure) {
      setError(errorMessage(failure));
      await cache.invalidateQueries({ queryKey: ['members', userId, tenantId] });
    } finally { setBusy(false); }
  }
  if (tenant.isPending) return <p role="status">Organisation wird geladen …</p>;
  if (tenant.isError) return <Alert severity="error">{errorMessage(tenant.error)} <Button onClick={() => void tenant.refetch()}>Erneut versuchen</Button></Alert>;
  return <section className="workspace-page">
    <Link to="/workspace">Zur Organisationsübersicht</Link>
    <h1>{tenant.data.name}</h1><p>Ihre Rolle: {tenant.data.role}</p>
    <p><Link to={`/workspace/${tenantId}/company`}>Unternehmensprofil und Wissen</Link></p>
    <p><Link to={`/workspace/${tenantId}/audits`}>Demo-Konto-Audit</Link></p>
    {!canManage ? <p>Die Mitgliederverwaltung ist für Owner und Admins verfügbar.</p> : <>
      {error ? <Alert severity="error">{error}</Alert> : null}
      <h2>Mitglieder</h2>
      {members.isPending ? <p role="status">Mitglieder werden geladen …</p> : members.isError ? <Alert severity="error">{errorMessage(members.error)} <Button onClick={() => void members.refetch()}>Erneut versuchen</Button></Alert> :
        <><ul className="member-list">{members.data?.map(member => <MemberRow key={`${member.userId}:${member.version}`} member={member} roles={availableRoles} busy={busy}
          editable={member.role !== 'Owner' && (tenant.data.role === 'Owner' || member.role !== 'Admin')}
          onSave={role => void mutate(`${member.userId}/role`, 'PUT', { role, version: member.version })} onRemove={() => setRemoving(member)} />)}</ul>
          <Pagination page={page} setPage={setPage} hasNext={members.data?.length === 50} /></>}
      <h2>Mitglied einladen</h2><p>Die Person muss bereits registriert sein und die Einladung selbst annehmen. Es wird keine E-Mail versendet.</p>
      <form className="inline-form" noValidate onSubmit={form.handleSubmit(values => mutate(`${values.userId}/invitation`, 'PUT', { role: values.role }))}>
        <TextField fullWidth label="Benutzer-ID" {...form.register('userId')} error={!!form.formState.errors.userId} helperText={form.formState.errors.userId?.message} />
        <TextField select label="Rolle" defaultValue="Viewer" {...form.register('role')} sx={{ minWidth: 150 }}>{availableRoles.map(role => <MenuItem key={role} value={role}>{role}</MenuItem>)}</TextField>
        <Button variant="contained" type="submit" disabled={busy}>Einladen</Button>
      </form>
      <Button onClick={() => setShowAudit(!showAudit)} aria-expanded={showAudit} sx={{ mt: 4 }}>{showAudit ? 'Auditprotokoll ausblenden' : 'Auditprotokoll anzeigen'}</Button>
      {showAudit ? <><h2>Auditprotokoll</h2>{audit.isPending ? <p role="status">Protokoll wird geladen …</p> : audit.isError ? <Alert severity="error">{errorMessage(audit.error)}</Alert> :
        <><ul className="resource-list">{audit.data?.map(item => <li key={item.id}><span>{item.action}<br /><small>{item.actorId}</small></span>
          <time dateTime={item.occurredAt}>{new Date(item.occurredAt).toLocaleString('de-DE')}</time></li>)}</ul>
          <Pagination page={auditPage} setPage={setAuditPage} hasNext={audit.data?.length === 50} /></>}</> : null}
    </>}
    <Dialog open={!!removing} onClose={() => { if (!busy) setRemoving(null); }} aria-labelledby="remove-member-title">
      <DialogTitle id="remove-member-title">Mitgliedschaft entfernen?</DialogTitle>
      <DialogContent>Der Zugriff dieser Person auf die Organisation wird entfernt: <span className="break-id">{removing?.userId}</span></DialogContent>
      <DialogActions><Button disabled={busy} onClick={() => setRemoving(null)}>Abbrechen</Button><Button color="error" disabled={busy} onClick={() => {
        if (removing) void mutate(`${removing.userId}?version=${removing.version}`, 'DELETE');
      }}>Entfernen</Button></DialogActions>
    </Dialog>
  </section>;
}

function MemberRow({ member, roles: allowedRoles, editable, busy, onSave, onRemove }: {
  member: Member; roles: string[]; editable: boolean; busy: boolean; onSave: (role: string) => void; onRemove: () => void;
}) {
  const [role, setRole] = useState(member.role);
  return <li><div className="member-identity"><code>{member.userId}</code><small>{member.active ? 'Aktiv' : 'Einladung offen'}</small></div>
    {editable ? <><TextField select label={`Rolle für ${member.userId}`} value={role} onChange={event => setRole(event.target.value)} size="small" sx={{ minWidth: 140 }}>
      {allowedRoles.map(item => <MenuItem key={item} value={item}>{item}</MenuItem>)}</TextField>
      <Button disabled={busy || role === member.role} onClick={() => onSave(role)}>Speichern</Button><Button color="error" disabled={busy} onClick={onRemove}>Entfernen</Button></>
      : <span>{member.role}</span>}
  </li>;
}
