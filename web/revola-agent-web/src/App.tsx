import { lazy, Suspense, useEffect } from 'react';
import { Link, Route, Routes, useLocation } from 'react-router-dom';
import { StatusPage } from './features/status/StatusPage';
import { SessionGate } from './features/auth/SessionGate';

const AuthPage = lazy(() => import('./features/auth/AuthPage').then(module => ({ default: module.AuthPage })));
const RecoveryPage = lazy(() => import('./features/auth/RecoveryPage').then(module => ({ default: module.RecoveryPage })));
const SecurityPage = lazy(() => import('./features/auth/SecurityPage').then(module => ({ default: module.SecurityPage })));
const CompanyPage = lazy(() => import('./features/company/CompanyPage').then(module => ({ default: module.CompanyPage })));
const AuditsPage = lazy(() => import('./features/company/AuditsPage').then(module => ({ default: module.AuditsPage })));
const ContentPage = lazy(() => import('./features/content/ContentPage').then(module => ({ default: module.ContentPage })));
const ConnectionsPage = lazy(() => import('./features/content/ConnectionsPage').then(module => ({ default: module.ConnectionsPage })));
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
      <Route path="/confirm" element={<RecoveryPage key="confirm" mode="confirm" />} />
      <Route path="/resend" element={<RecoveryPage key="resend" mode="resend" />} />
      <Route path="/forgot" element={<RecoveryPage key="forgot" mode="forgot" />} />
      <Route path="/reset" element={<RecoveryPage key="reset" mode="reset" />} />
      <Route path="/security" element={<SecurityPage />} />
      <Route path="/workspace" element={<SessionGate>{id => <WorkspacePage key={id} userId={id} />}</SessionGate>} />
      <Route path="/workspace/:tenantId" element={<SessionGate>{id => <MembersPage key={`${id}:${location.pathname}`} userId={id} />}</SessionGate>} />
      <Route path="/workspace/:tenantId/company" element={<SessionGate>{id => <CompanyPage key={`${id}:${location.pathname}`} userId={id} />}</SessionGate>} />
      <Route path="/workspace/:tenantId/audits" element={<SessionGate>{id => <AuditsPage key={`${id}:${location.pathname}`} userId={id} />}</SessionGate>} />
      <Route path="/workspace/:tenantId/content" element={<SessionGate>{id => <ContentPage key={`${id}:${location.pathname}`} userId={id} />}</SessionGate>} />
      <Route path="/workspace/:tenantId/connections" element={<SessionGate>{id => <ConnectionsPage key={`${id}:${location.pathname}`} userId={id} />}</SessionGate>} />
      <Route path="*" element={<><h1>Seite nicht gefunden</h1><Link to="/">Zum Systemstatus</Link></>} />
    </Routes></Suspense></main>
  </>;
}
