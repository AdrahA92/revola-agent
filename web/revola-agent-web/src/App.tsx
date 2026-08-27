import { lazy, Suspense, useEffect } from 'react';
import { Link, Route, Routes, useLocation } from 'react-router-dom';
import { StatusPage } from './features/status/StatusPage';
import { SessionGate } from './features/auth/SessionGate';

const AuthPage = lazy(() => import('./features/auth/AuthPage').then(module => ({ default: module.AuthPage })));
const WorkspacePage = lazy(() => import('./features/tenancy/WorkspacePage').then(module => ({ default: module.WorkspacePage })));
const MembersPage = lazy(() => import('./features/tenancy/MembersPage').then(module => ({ default: module.MembersPage })));

export function App() {
  const location = useLocation();
  const status = location.pathname === '/';
  useEffect(() => {
    const title = status ? 'Systemstatus' : location.pathname === '/login' ? 'Anmelden'
      : location.pathname === '/register' ? 'Registrieren' : 'Arbeitsbereich';
    document.title = `Revola Agent – ${title}`;
  }, [location.pathname, status]);
  return <>
    <a className="skip-link" href="#main">Zum Inhalt</a>
    <header className="app-header"><Link className="brand" to="/workspace">Revola Agent</Link><nav aria-label="Hauptnavigation"><Link to={status ? '/login' : '/'}>{status ? 'Anmelden' : 'Systemstatus'}</Link></nav></header>
    <main id="main" className={status ? 'status-main' : 'app-main'}><Suspense fallback={<p role="status">Ansicht wird geladen …</p>}><Routes>
      <Route path="/" element={<StatusPage />} />
      <Route path="/login" element={<AuthPage key="login" />} />
      <Route path="/register" element={<AuthPage key="register" registerAccount />} />
      <Route path="/workspace" element={<SessionGate>{id => <WorkspacePage key={id} userId={id} />}</SessionGate>} />
      <Route path="/workspace/:tenantId" element={<SessionGate>{id => <MembersPage key={`${id}:${location.pathname}`} userId={id} />}</SessionGate>} />
      <Route path="*" element={<><h1>Seite nicht gefunden</h1><Link to="/">Zum Systemstatus</Link></>} />
    </Routes></Suspense></main>
  </>;
}
