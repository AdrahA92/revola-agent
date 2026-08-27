import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import Button from '@mui/material/Button';
import Alert from '@mui/material/Alert';
import TextField from '@mui/material/TextField';
import { api, errorMessage } from '../../lib/api';
import { Pagination } from '../tenancy/WorkspacePage';
import type { Tenant } from '../tenancy/WorkspacePage';

const required = (max: number) => z.string().trim().min(1, 'Pflichtfeld.').max(max);
const profileSchema = z.object({ name: required(160), industry: required(160), description: required(4000), email: z.email().max(254),
  website: z.url().max(2048).refine(value => { try { const url = new URL(value); return url.protocol === 'https:' && !url.username && !url.password; } catch { return false; } }, 'HTTPS-Website ohne Zugangsdaten erforderlich.'),
  services: required(4000), audience: required(2000), regions: required(1000), brandColor: z.string().regex(/^#[0-9a-fA-F]{6}$/, 'Farbe als #RRGGBB eingeben.'),
  tone: z.string().max(1000), allowedClaims: z.string().max(4000), forbiddenClaims: z.string().max(4000), goals: z.string().max(2000), source: required(2000) });
type ProfileFields = z.infer<typeof profileSchema>;
type Profile = Omit<ProfileFields, 'source'>;
type RecordView<T> = { id: string; version: string; data: T; source: string; updatedAt: string; expiresAt: string | null };
type Knowledge = { title: string; content: string };
type Revision = { version: string; source: string; createdAt: string; dataJson: string };
const emptyVersion = '00000000-0000-0000-0000-000000000000';
const labels: Record<keyof ProfileFields, string> = { name: 'Firmenname', industry: 'Branche', description: 'Beschreibung', email: 'Kontakt-E-Mail',
  website: 'Website (HTTPS)', services: 'Leistungen und Produkte', audience: 'Zielgruppen', regions: 'Zielregionen', brandColor: 'Markenfarbe', tone: 'Schreibstil',
  allowedClaims: 'Erlaubte Aussagen', forbiddenClaims: 'Verbotene Aussagen', goals: 'Geschäftsziele', source: 'Quelle dieser Angaben' };
const defaults: ProfileFields = { name: '', industry: '', description: '', email: '', website: '', services: '', audience: '', regions: '', brandColor: '#006666', tone: '', allowedClaims: '', forbiddenClaims: '', goals: '', source: '' };

export function CompanyPage({ userId }: { userId: string }) {
  const { tenantId = '' } = useParams();
  const root = `/tenants/${tenantId}/company`;
  const cache = useQueryClient();
  const [page, setPage] = useState(1);
  const [historyId, setHistoryId] = useState('');
  const [historyPage, setHistoryPage] = useState(1);
  const [edit, setEdit] = useState<RecordView<Knowledge> | null>(null);
  const [loadedAt] = useState(Date.now);
  const tenant = useQuery({ queryKey: ['tenant', userId, tenantId], queryFn: ({ signal }) => api<Tenant>(`/tenants/${tenantId}`, { signal }) });
  const profile = useQuery({ queryKey: ['company', userId, tenantId, 'profile'], queryFn: ({ signal }) => api<{ profile: RecordView<Profile> | null }>(`${root}/profile`, { signal }), refetchOnWindowFocus: false });
  const knowledge = useQuery({ queryKey: ['company', userId, tenantId, 'knowledge', page], queryFn: ({ signal }) => api<RecordView<Knowledge>[]>(`${root}/knowledge?page=${page}`, { signal }), refetchOnWindowFocus: false });
  const history = useQuery({ queryKey: ['company', userId, tenantId, 'history', historyId, historyPage], queryFn: ({ signal }) => api<Revision[]>(`${root}/history/${historyId}?page=${historyPage}`, { signal }), enabled: !!historyId });
  const editable = !!tenant.data && ['Owner', 'Admin', 'Manager'].includes(tenant.data.role);
  const refresh = async () => { await cache.invalidateQueries({ queryKey: ['company', userId, tenantId] }); };
  const showHistory = (id: string) => { setHistoryId(id); setHistoryPage(1); };
  return <section className="workspace-page"><h1>Unternehmenswissen</h1><p><Link to={`/workspace/${tenantId}`}>Zur Organisation</Link></p>
    <p>Nur geprüfte Unternehmensangaben speichern. Alle Änderungen erhalten eine Quelle und eine eigene Version. Dies ändert keine öffentlichen Profile.</p>
    {tenant.isError ? <Alert severity="error">{errorMessage(tenant.error)}</Alert> : null}
    {profile.isPending ? <p role="status">Unternehmensprofil wird geladen …</p> : profile.isError ? <Alert severity="error">{errorMessage(profile.error)} <Button onClick={() => void profile.refetch()}>Erneut versuchen</Button></Alert> :
      <ProfileForm key={profile.data.profile?.version ?? 'new'} record={profile.data.profile} editable={editable} root={root} refresh={refresh} />}
    {profile.data?.profile ? <Button onClick={() => showHistory(tenantId)}>Profilhistorie ansehen</Button> : null}
    <h2>Wissenseinträge</h2>
    {knowledge.isPending ? <p role="status">Wissen wird geladen …</p> : knowledge.isError ? <Alert severity="error">{errorMessage(knowledge.error)} <Button onClick={() => void knowledge.refetch()}>Erneut versuchen</Button></Alert> : <>
      <ul className="resource-list">{knowledge.data.length === 0 ? <li>Noch keine zusätzlichen Fakten.</li> : knowledge.data.map(item => <li key={item.id}><div><h3>{item.data.title}</h3><p>{item.data.content}</p><p>Quelle: {item.source}</p>
        {item.expiresAt ? <p>Gültig bis {new Date(item.expiresAt).toLocaleString('de-DE')}{Date.parse(item.expiresAt) <= loadedAt ? ' · Abgelaufen – neu prüfen' : ''}</p> : null}
        {editable ? <Button onClick={() => setEdit(item)}>Bearbeiten</Button> : null}<Button onClick={() => showHistory(item.id)}>Historie</Button></div></li>)}</ul>
      <Pagination page={page} setPage={setPage} hasNext={knowledge.data.length === 50} /></>}
    {editable ? <><h3>{edit ? 'Fakt bearbeiten' : 'Fakt hinzufügen'}</h3><KnowledgeForm key={edit?.version ?? 'new'} record={edit} root={root} refresh={async () => { setEdit(null); await refresh(); }} />
      {edit ? <Button onClick={() => setEdit(null)}>Bearbeitung abbrechen</Button> : null}</> : null}
    {historyId ? <><h2>Versionshistorie</h2>{history.isPending ? <p role="status">Historie wird geladen …</p> : history.isError ? <Alert severity="error">{errorMessage(history.error)}</Alert> : <>
      <ul>{history.data.map(item => <li key={item.version}><p>{new Date(item.createdAt).toLocaleString('de-DE')} · Quelle: {item.source}</p><details><summary>Gespeicherte Angaben anzeigen</summary><pre style={{ whiteSpace: 'pre-wrap', overflowWrap: 'anywhere' }}>{JSON.stringify(JSON.parse(item.dataJson), null, 2)}</pre></details></li>)}</ul>
      <Pagination page={historyPage} setPage={setHistoryPage} hasNext={history.data.length === 50} /></>}</> : null}
  </section>;
}

function ProfileForm({ record, editable, root, refresh }: { record: RecordView<Profile> | null; editable: boolean; root: string; refresh: () => Promise<void> }) {
  const [error, setError] = useState('');
  const [newVersion] = useState(() => crypto.randomUUID());
  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<ProfileFields>({ resolver: zodResolver(profileSchema), defaultValues: record ? { ...record.data, source: record.source } : defaults });
  const submit = handleSubmit(async ({ source, ...data }) => {
    setError('');
    try { await api(`${root}/profile`, { method: 'PUT', body: { version: record?.version ?? emptyVersion, newVersion, data, source, expiresAt: null } }); await refresh(); }
    catch (failure) { setError(errorMessage(failure)); }
  });
  return <><h2>Unternehmensprofil</h2>{error ? <Alert severity="error">{error} <Button onClick={() => void refresh()}>Aktuellen Stand laden (verwirft Änderungen)</Button></Alert> : null}
    <form noValidate onSubmit={submit} className="auth-form">{(Object.keys(labels) as (keyof ProfileFields)[]).map(name =>
      <TextField key={name} label={labels[name]} {...register(name)} disabled={!editable} error={!!errors[name]} helperText={errors[name]?.message}
        multiline={!['name', 'industry', 'email', 'website', 'brandColor'].includes(name)} fullWidth sx={{ mt: 2 }} />)}
      {editable ? <Button type="submit" variant="contained" disabled={isSubmitting} sx={{ mt: 2 }}>Profil speichern</Button> : <p>Lesemodus. Änderungen benötigen Owner-, Admin- oder Manager-Rechte.</p>}
    </form></>;
}

const knowledgeSchema = z.object({ title: required(160), content: required(8000), source: required(2000), expiresAt: z.string().refine(value => !value || !Number.isNaN(Date.parse(value)), 'Gültigen Zeitpunkt eingeben.') });
function KnowledgeForm({ record, root, refresh }: { record: RecordView<Knowledge> | null; root: string; refresh: () => Promise<void> }) {
  const [error, setError] = useState('');
  const [id, setId] = useState(() => record?.id ?? crypto.randomUUID());
  const [newVersion, setNewVersion] = useState(() => crypto.randomUUID());
  const { register, handleSubmit, reset, formState: { errors, isSubmitting } } = useForm<z.infer<typeof knowledgeSchema>>({ resolver: zodResolver(knowledgeSchema), defaultValues: { title: record?.data.title ?? '', content: record?.data.content ?? '', source: record?.source ?? '', expiresAt: record?.expiresAt?.slice(0, 16) ?? '' } });
  const submit = handleSubmit(async ({ title, content, source, expiresAt }) => {
    setError('');
    try { await api(`${root}/knowledge/${id}`, { method: 'PUT', body: { version: record?.version ?? emptyVersion, newVersion, data: { title, content }, source, expiresAt: expiresAt ? new Date(expiresAt + 'Z').toISOString() : null } }); reset(); setId(crypto.randomUUID()); setNewVersion(crypto.randomUUID()); await refresh(); }
    catch (failure) { setError(errorMessage(failure)); }
  });
  return <form noValidate onSubmit={submit} className="auth-form">{error ? <Alert severity="error">{error}</Alert> : null}
    {(['title', 'content', 'source', 'expiresAt'] as const).map(name => <TextField key={name} label={{ title: 'Titel', content: 'Fakt', source: 'Quelle', expiresAt: 'Gültig bis (UTC, optional)' }[name]}
      {...register(name)} error={!!errors[name]} helperText={errors[name]?.message} type={name === 'expiresAt' ? 'datetime-local' : 'text'} multiline={name === 'content'} slotProps={{ inputLabel: { shrink: true } }} sx={{ mt: 2 }} />)}
    <Button type="submit" disabled={isSubmitting} variant="contained" sx={{ mt: 2 }}>Fakt speichern</Button></form>;
}
