import { useQuery } from '@tanstack/react-query';
import { checkHealth } from './health';

export function StatusPage() {
  const health = useQuery({ queryKey: ['health'], queryFn: ({ signal }) => checkHealth(signal), enabled: false });
  const status = (index: number) => health.isFetching ? 'Wird geprüft …' : health.data?.[index] ?? 'Noch nicht geprüft';
  return <section aria-labelledby="page-heading">
    <h1 id="page-heading">Die Grundlage steht.</h1>
    <p className="subtitle">Phase 1 · Technisches Projektgrundgerüst</p>
    <dl aria-live="polite" aria-busy={health.isFetching}>
      <div><dt>Weboberfläche</dt><dd>Bereit</dd></div>
      <div><dt>Backend</dt><dd>{status(0)}</dd></div>
      <div><dt>Datenbank</dt><dd>{status(1)}</dd></div>
    </dl>
    <button type="button" disabled={health.isFetching} onClick={() => void health.refetch()}>Verbindung prüfen</button>
    <p className="note">Anmeldung, Unternehmensprofile und Agentenfunktionen folgen in den nächsten Phasen.</p>
  </section>;
}
