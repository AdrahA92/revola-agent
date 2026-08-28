import { Link, useParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import Alert from '@mui/material/Alert';
import Button from '@mui/material/Button';
import { api, errorMessage } from '../../lib/api';

type Mode = { platform: string; name: string; connected: boolean; canReadAccount: boolean; canPublish: boolean; manualPreparationAvailable: boolean; website: string };
export function ConnectionsPage({ userId }: { userId: string }) {
  const { tenantId = '' } = useParams();
  const modes = useQuery({ queryKey: ['connections', userId, tenantId], queryFn: ({ signal }) => api<Mode[]>(`/tenants/${tenantId}/connections`, { signal }) });
  return <section className="workspace-page"><h1>Social-Media-Verbindungen</h1><p><Link to={`/workspace/${tenantId}`}>Zur Organisation</Link></p>
    <Alert severity="warning">Noch keine Konten verbunden. Eine Anmeldung im separat geöffneten Browser verbindet das Konto nicht mit Revola Agent.</Alert>
    <p>Für automatische Kontobetreuung benötigen wir eine freigegebene Plattformintegration. OAuth-Anmeldungen erfolgen im Browser der Plattform; Revola Agent speichert keine Plattformpasswörter.</p>
    {modes.isPending ? <p role="status">Verbindungsoptionen werden geladen …</p> : modes.isError ? <Alert severity="error">{errorMessage(modes.error)} <Button onClick={() => void modes.refetch()}>Erneut versuchen</Button></Alert> : modes.data.map(mode =>
      <article key={mode.platform}><h2>{mode.name}</h2><p>Status: {mode.connected ? 'Verbunden' : 'Nicht verbunden'}. Kontodaten lesen: {mode.canReadAccount ? 'verfügbar' : 'nicht verfügbar'}. Automatisches Veröffentlichen: {mode.canPublish ? 'verfügbar' : 'nicht verfügbar'}.</p>
        {mode.manualPreparationAvailable ? <><p>Manueller Ablauf: Inhalt vorbereiten, prüfen, kopieren und selbst auf der Plattform veröffentlichen. Der tatsächliche Veröffentlichungsstatus wird nicht automatisch erkannt.</p>
          <Button component={Link} to={`/workspace/${tenantId}/content`}>Inhalte vorbereiten</Button>
          <Button component="a" href={mode.website} target="_blank" rel="noopener noreferrer">Plattform manuell öffnen</Button></> : null}
      </article>)}
  </section>;
}
