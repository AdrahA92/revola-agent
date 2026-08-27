import { Link, Route, Routes } from 'react-router-dom';
import { StatusPage } from './features/status/StatusPage';

export function App() {
  return <>
    <a className="skip-link" href="#main">Zum Inhalt</a>
    <header><Link className="brand" to="/">Revola Agent</Link><nav aria-label="Hauptnavigation"><Link to="/">Systemstatus</Link></nav></header>
    <main id="main"><Routes><Route path="/" element={<StatusPage />} /><Route path="*" element={<><h1>Seite nicht gefunden</h1><Link to="/">Zum Systemstatus</Link></>} /></Routes></main>
  </>;
}
