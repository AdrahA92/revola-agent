import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import Button from '@mui/material/Button';
import Alert from '@mui/material/Alert';
import TextField from '@mui/material/TextField';
import MenuItem from '@mui/material/MenuItem';
import { api, errorMessage } from '../../lib/api';
import { Pagination } from '../tenancy/WorkspacePage';
import type { Tenant } from '../tenancy/WorkspacePage';

type Criterion = { name: string; score: number | null; maximum: number; observation: string; action: string | null; priority: string; effort: string; requiresApproval: boolean };
type Audit = { id: string; profileVersion: string; scenario: string; createdAt: string; result: { score: number; ruleVersion: string; assessedCriteria: number; totalCriteria: number; uncertainty: string; criteria: Criterion[] } };

export function AuditsPage({ userId }: { userId: string }) {
  const { tenantId = '' } = useParams();
  const [page, setPage] = useState(1);
  const [scenario, setScenario] = useState('starter');
  const [requestId, setRequestId] = useState(() => crypto.randomUUID());
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const cache = useQueryClient();
  const root = `/tenants/${tenantId}/demo-audits`;
  const tenant = useQuery({ queryKey: ['tenant', userId, tenantId], queryFn: ({ signal }) => api<Tenant>(`/tenants/${tenantId}`, { signal }) });
  const audits = useQuery({ queryKey: ['audits', userId, tenantId, page], queryFn: ({ signal }) => api<Audit[]>(`${root}?page=${page}`, { signal }) });
  async function run() {
    setBusy(true); setError('');
    try { await api(`${root}/${requestId}`, { method: 'PUT', body: { scenario } }); setRequestId(crypto.randomUUID()); setPage(1); await cache.invalidateQueries({ queryKey: ['audits', userId, tenantId] }); }
    catch (failure) { setError(errorMessage(failure)); } finally { setBusy(false); }
  }
  return <section className="workspace-page"><h1>Demo-Konto-Audit</h1><p><Link to={`/workspace/${tenantId}`}>Zur Organisation</Link> · <Link to={`/workspace/${tenantId}/company`}>Unternehmensprofil</Link></p>
    <Alert severity="info">Fiktive Beispieldaten, kein verbundenes Social-Media-Konto. Es werden keine externen Plattformen aufgerufen. Der Score ist kein Nachweis für Reichweite, Kundengewinnung oder rechtliche Konformität.</Alert>
    <p>Voraussetzung ist ein gespeichertes Unternehmensprofil. Der Score bewertet fünf messbare Demo-Kriterien; fünf weitere bleiben ausdrücklich unbewertet.</p>
    {error ? <Alert severity="error">{error}</Alert> : null}
    {tenant.data && ['Owner', 'Admin', 'Manager'].includes(tenant.data.role) ? <div className="inline-form">
      <TextField select label="Demo-Szenario" value={scenario} disabled={busy} onChange={event => { setScenario(event.target.value); setRequestId(crypto.randomUUID()); }}>
        <MenuItem value="starter">Neues Profil mit Lücken</MenuItem><MenuItem value="active">Aktives Beispielprofil</MenuItem></TextField>
      <Button variant="contained" disabled={busy} onClick={() => void run()}>{busy ? 'Audit läuft …' : 'Demo-Audit starten'}</Button></div> : null}
    <h2>Audit-Historie</h2>
    {audits.isPending ? <p role="status">Audits werden geladen …</p> : audits.isError ? <Alert severity="error">{errorMessage(audits.error)} <Button onClick={() => void audits.refetch()}>Erneut versuchen</Button></Alert> : <>
      {audits.data.length === 0 ? <p>Noch kein Audit gestartet.</p> : audits.data.map(audit => <article key={audit.id}>
        <h3>Demo-Score: {audit.result.score}/100</h3><p>{new Date(audit.createdAt).toLocaleString('de-DE')} · Regeln: {audit.result.ruleVersion} · Szenario: {audit.scenario}</p>
        <p>{audit.result.assessedCriteria}/{audit.result.totalCriteria} Kriterien bewertet. {audit.result.uncertainty}</p>
        <p>Unternehmensprofil-Version: <code>{audit.profileVersion}</code></p>
        <ul>{audit.result.criteria.map(criterion => <li key={criterion.name}><h4>{criterion.name}: {criterion.score === null ? 'Nicht bewertet' : `${criterion.score}/${criterion.maximum}`}</h4><p>{criterion.observation}</p>
          {criterion.action ? <p>Vorschlag: {criterion.action} Priorität: {criterion.priority}. Aufwand: {criterion.effort}. {criterion.requiresApproval ? 'Öffentliche Änderung benötigt Freigabe.' : 'Nur interner Entwurf.'}</p> : null}</li>)}</ul>
      </article>)}<Pagination page={page} setPage={setPage} hasNext={audits.data.length === 50} /></>}
  </section>;
}
