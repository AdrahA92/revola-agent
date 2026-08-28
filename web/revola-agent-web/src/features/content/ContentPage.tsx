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
import { api, errorMessage } from '../../lib/api';
import { Pagination } from '../tenancy/WorkspacePage';
import type { Tenant } from '../tenancy/WorkspacePage';

type Draft = { title: string; text: string; imageBrief: string; altText: string };
type ContentData = Draft & { target: string; scheduledAt: string; timeZone: string };
type Content = { id: string; version: string; authorId: string; status: string; data: ContentData; hash: string; approvedBy: string | null; approvalExpiresAt: string | null };
type Run = { id: string; goal: string; platform: string; status: string; model: string; result: Draft | null; cost: number; errorCode: string | null; steps: { tool: string; risk: string; status: string }[] };
const emptyVersion = '00000000-0000-0000-0000-000000000000';
const required = (max: number) => z.string().trim().min(1, 'Pflichtfeld.').max(max);
const schema = z.object({ title: required(160), text: required(5000), imageBrief: required(2000), altText: required(500),
  target: z.enum(['demo-facebook', 'demo-linkedin']), scheduledAt: required(30).refine(value => !Number.isNaN(Date.parse(value + 'Z')), 'Gültigen UTC-Zeitpunkt eingeben.'), timeZone: z.enum(['Europe/Berlin', 'UTC']) });
type Fields = z.infer<typeof schema>;
const labels: Record<keyof Fields, string> = { title: 'Titel', text: 'Beitragstext', imageBrief: 'Bildbriefing (noch kein Bild)', altText: 'Geplanter Alternativtext', target: 'Demo-Ziel', scheduledAt: 'Planungszeit (UTC)', timeZone: 'Anzeige-Zeitzone' };

export function ContentPage({ userId }: { userId: string }) {
  const { tenantId = '' } = useParams();
  const root = `/tenants/${tenantId}`;
  const cache = useQueryClient();
  const [page, setPage] = useState(1);
  const [runPage, setRunPage] = useState(1);
  const [edit, setEdit] = useState<Content | null>(null);
  const [seed, setSeed] = useState<Draft | null>(null);
  const [formKey, setFormKey] = useState(0);
  const [historyId, setHistoryId] = useState('');
  const [historyPage, setHistoryPage] = useState(1);
  const tenant = useQuery({ queryKey: ['tenant', userId, tenantId], queryFn: ({ signal }) => api<Tenant>(root, { signal }) });
  const content = useQuery({ queryKey: ['content', userId, tenantId, page], queryFn: ({ signal }) => api<Content[]>(`${root}/content?page=${page}`, { signal }) });
  const runs = useQuery({ queryKey: ['runs', userId, tenantId, runPage], queryFn: ({ signal }) => api<Run[]>(`${root}/agent-runs?page=${runPage}`, { signal }) });
  const history = useQuery({ queryKey: ['content-history', userId, tenantId, historyId, historyPage], enabled: !!historyId,
    queryFn: ({ signal }) => api<{ version: string; data: ContentData; hash: string }[]>(`${root}/content/${historyId}/history?page=${historyPage}`, { signal }) });
  const role = tenant.data?.role ?? 'Viewer';
  const canEdit = ['Owner', 'Admin', 'Manager', 'Editor'].includes(role);
  async function refresh() { await Promise.all([cache.invalidateQueries({ queryKey: ['content', userId, tenantId] }), cache.invalidateQueries({ queryKey: ['content-history', userId, tenantId] })]); }
  function clearEditor() { setEdit(null); setSeed(null); setFormKey(key => key + 1); }
  return <section className="workspace-page"><h1>Agent und Content-Planung</h1><p><Link to={`/workspace/${tenantId}`}>Zur Organisation</Link></p>
    <Alert severity="info">Testmodus: Vorlagen statt echter KI. Keine OpenAI-Kosten, kein Bild-Upload und keine Veröffentlichung. „Geplant“ bedeutet ausschließlich einen internen Kalendereintrag.</Alert>
    <h2>Test-Agent</h2><p>Maximal 20 Läufe je Organisation und UTC-Tag, zwei gleichzeitig. Zulässig: Unternehmensprofil lesen und internen Entwurf erstellen.</p>
    {canEdit ? <AgentForm root={root} completed={async () => { setRunPage(1); await cache.invalidateQueries({ queryKey: ['runs', userId, tenantId] }); }} /> : <p>Für Agentenläufe benötigen Sie Bearbeitungsrechte.</p>}
    {runs.isPending ? <p role="status">Agentenläufe werden geladen …</p> : runs.isError ? <Alert severity="error">{errorMessage(runs.error)}</Alert> : <>
      <ul className="resource-list">{runs.data.map(run => <li key={run.id}><div><h3>{run.goal}</h3><p>{run.status} · {run.model} · Kosten: {run.cost} (Testmodus)</p>
        <p>{run.steps.map(step => `${step.tool}: ${step.risk} / ${step.status}`).join(' · ')}</p>
        {run.errorCode ? <Alert severity="warning">Lauf nicht erfolgreich. Ein neuer Lauf benötigt eine neue Anfrage.</Alert> : null}
        {run.result ? <><p style={{ whiteSpace: 'pre-wrap' }}>{run.result.text}</p>{canEdit ? <Button onClick={() => { setEdit(null); setSeed(run.result); setFormKey(key => key + 1); }}>Als bearbeitbaren Entwurf übernehmen</Button> : null}</> : null}</div></li>)}</ul>
      <Pagination page={runPage} setPage={setRunPage} hasNext={runs.data.length === 50} /></>}
    {canEdit ? <><h2>{edit ? 'Entwurf bearbeiten' : 'Entwurf anlegen'}</h2><p>Jede Änderung erzeugt eine neue Version und verwirft bisherige Freigaben. Eine andere berechtigte Person muss freigeben.</p>
      <ContentForm key={`${formKey}:${edit?.version ?? 'new'}`} root={root} record={edit} seed={seed} saved={async () => { clearEditor(); await refresh(); }} />
      {edit || seed ? <Button onClick={clearEditor}>Bearbeitung abbrechen</Button> : null}</> : null}
    <h2>Redaktionsplan und Freigaben</h2>
    {content.isPending ? <p role="status">Inhalte werden geladen …</p> : content.isError ? <Alert severity="error">{errorMessage(content.error)}</Alert> : <>
      {content.data.length === 0 ? <p>Noch keine Inhalte gespeichert.</p> : content.data.map(item => <ContentCard key={`${item.id}:${item.version}:${item.status}`} item={item} root={root} role={role} userId={userId}
        refresh={refresh} edit={() => { setEdit(item); setSeed(null); }} history={() => { setHistoryId(item.id); setHistoryPage(1); }} />)}
      <Pagination page={page} setPage={setPage} hasNext={content.data.length === 50} /></>}
    {historyId ? <><h2>Inhaltsversionen</h2>{history.isPending ? <p role="status">Versionen werden geladen …</p> : history.isError ? <Alert severity="error">{errorMessage(history.error)}</Alert> : <>
      {history.data.map(item => <details key={item.version}><summary>{item.data.title} · Version {item.version}</summary><p style={{ whiteSpace: 'pre-wrap' }}>{item.data.text}</p><p>Ziel: {item.data.target} · Zeitpunkt: {item.data.scheduledAt}</p><p>Hash: <code>{item.hash}</code></p></details>)}
      <Pagination page={historyPage} setPage={setHistoryPage} hasNext={history.data.length === 50} /></>}</> : null}
  </section>;
}

const agentSchema = z.object({ goal: required(2000), platform: z.enum(['demo-facebook', 'demo-linkedin']) });
function AgentForm({ root, completed }: { root: string; completed: () => Promise<void> }) {
  const [id, setId] = useState(() => crypto.randomUUID());
  const [error, setError] = useState('');
  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<z.infer<typeof agentSchema>>({ resolver: zodResolver(agentSchema), defaultValues: { goal: '', platform: 'demo-facebook' } });
  const submit = handleSubmit(async body => {
    setError('');
    try { await api(`${root}/agent-runs/${id}`, { method: 'PUT', body }); setId(crypto.randomUUID()); await completed(); }
    catch (failure) { setError(errorMessage(failure)); }
  });
  return <form onSubmit={submit} onChange={() => setId(crypto.randomUUID())} noValidate className="auth-form">{error ? <Alert severity="error">{error}</Alert> : null}
    <TextField label="Briefing für den Testlauf" {...register('goal')} error={!!errors.goal} helperText={errors.goal?.message} multiline sx={{ mt: 2 }} />
    <TextField select label="Demo-Plattform" {...register('platform')} defaultValue="demo-facebook" sx={{ mt: 2 }}><MenuItem value="demo-facebook">Facebook-Demo</MenuItem><MenuItem value="demo-linkedin">LinkedIn-Demo</MenuItem></TextField>
    <Button type="submit" variant="contained" disabled={isSubmitting} sx={{ mt: 2 }}>{isSubmitting ? 'Testlauf läuft …' : 'Test-Agent starten'}</Button></form>;
}

function ContentForm({ root, record, seed, saved }: { root: string; record: Content | null; seed: Draft | null; saved: () => Promise<void> }) {
  const [id] = useState(() => record?.id ?? crypto.randomUUID());
  const [newVersion] = useState(() => crypto.randomUUID());
  const [error, setError] = useState('');
  const [defaults] = useState(() => ({ title: '', text: '', imageBrief: '', altText: '', target: 'demo-facebook' as const, timeZone: 'Europe/Berlin' as const,
    ...seed, ...record?.data, scheduledAt: record?.data.scheduledAt.slice(0, 16) ?? new Date(Date.now() + 86400000).toISOString().slice(0, 16) }));
  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<Fields>({ resolver: zodResolver(schema), defaultValues: defaults as Fields });
  const submit = handleSubmit(async data => {
    setError('');
    try { await api(`${root}/content/${id}`, { method: 'PUT', body: { version: record?.version ?? emptyVersion, newVersion, data: { ...data, scheduledAt: new Date(data.scheduledAt + 'Z').toISOString() } } }); await saved(); }
    catch (failure) { setError(errorMessage(failure)); }
  });
  return <form noValidate onSubmit={submit} className="auth-form">{error ? <Alert severity="error">{error} Bei Versionskonflikten den aktuellen Entwurf erneut öffnen.</Alert> : null}
    {(Object.keys(labels) as (keyof Fields)[]).map(name => <TextField key={name} label={labels[name]} {...register(name)} error={!!errors[name]} helperText={errors[name]?.message}
      select={name === 'target' || name === 'timeZone'} defaultValue={defaults[name]} type={name === 'scheduledAt' ? 'datetime-local' : 'text'}
      multiline={['text', 'imageBrief', 'altText'].includes(name)} sx={{ mt: 2 }} slotProps={{ inputLabel: { shrink: true } }}>
      {name === 'target' ? [<MenuItem key="fb" value="demo-facebook">Facebook-Demo</MenuItem>, <MenuItem key="li" value="demo-linkedin">LinkedIn-Demo</MenuItem>] : name === 'timeZone' ? [<MenuItem key="berlin" value="Europe/Berlin">Europe/Berlin</MenuItem>, <MenuItem key="utc" value="UTC">UTC</MenuItem>] : null}
    </TextField>)}<Button type="submit" variant="contained" disabled={isSubmitting} sx={{ mt: 2 }}>Entwurf speichern</Button></form>;
}

function ContentCard({ item, root, role, userId, refresh, edit, history }: { item: Content; root: string; role: string; userId: string; refresh: () => Promise<void>; edit: () => void; history: () => void }) {
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const [copied, setCopied] = useState(false);
  const [expires, setExpires] = useState(() => new Date(Date.parse(item.data.scheduledAt) + 3600000).toISOString().slice(0, 16));
  const canEdit = ['Owner', 'Admin', 'Manager', 'Editor'].includes(role);
  const canApprove = ['Owner', 'Admin', 'Approver'].includes(role) && item.authorId !== userId;
  async function decide(decision: string) {
    setBusy(true); setError('');
    try { await api(`${root}/content/${item.id}/decision`, { method: 'POST', body: { version: item.version, decision, expiresAt: decision === 'approve' ? new Date(expires + 'Z').toISOString() : null } }); await refresh(); }
    catch (failure) { setError(errorMessage(failure)); } finally { setBusy(false); }
  }
  return <article><h3>{item.data.title} · {item.status}</h3><p>{item.data.target} · {new Date(item.data.scheduledAt).toLocaleString('de-DE', { timeZone: item.data.timeZone })} ({item.data.timeZone})</p>
    <p style={{ whiteSpace: 'pre-wrap' }}>{item.data.text}</p><p>Bildbriefing: {item.data.imageBrief}</p><p>Alternativtext: {item.data.altText}</p>
    <p>Version: <code>{item.version}</code></p>{item.approvalExpiresAt ? <p>Freigabe gültig bis {new Date(item.approvalExpiresAt).toLocaleString('de-DE')}</p> : null}
    <Button onClick={async () => { try { await navigator.clipboard.writeText(item.data.text); setCopied(true); } catch { setError('Kopieren nicht möglich. Markieren und kopieren Sie den Text manuell.'); } }}>Text für manuelle Bearbeitung kopieren</Button>
    {copied ? <p role="status">Text kopiert. Es wurde nichts veröffentlicht; Prüfung und Freigabe bleiben erforderlich.</p> : null}
    {error ? <Alert severity="error">{error}</Alert> : null}
    {canEdit ? <Button disabled={busy} onClick={edit}>Bearbeiten</Button> : null}<Button onClick={history}>Versionen</Button>
    {canEdit && ['Draft', 'Rejected'].includes(item.status) ? <Button disabled={busy} onClick={() => void decide('submit')}>Zur Prüfung einreichen</Button> : null}
    {canApprove && item.status === 'InReview' ? <div><TextField label="Freigabe gültig bis (UTC, maximal 7 Tage)" type="datetime-local" value={expires} onChange={event => setExpires(event.target.value)} slotProps={{ inputLabel: { shrink: true } }} />
      <Button disabled={busy || !expires} onClick={() => void decide('approve')}>Diese Version freigeben</Button><Button disabled={busy} onClick={() => void decide('reject')}>Ablehnen</Button></div> : null}
    {['Owner', 'Admin', 'Manager'].includes(role) && item.status === 'Approved' ? <Button disabled={busy} onClick={() => void decide('schedule')}>Intern einplanen</Button> : null}
    {canEdit && item.status !== 'Cancelled' ? <Button disabled={busy} onClick={() => void decide('cancel')}>Planung abbrechen</Button> : null}
  </article>;
}
