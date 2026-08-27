export class ApiError extends Error {
  constructor(public readonly status: number) {
    super(({ 400: 'Bitte prüfen Sie Ihre Eingaben und versuchen Sie es erneut.', 401: 'Bitte melden Sie sich erneut an.',
      403: 'Für diese Aktion fehlt die Berechtigung.', 404: 'Der Eintrag ist nicht verfügbar.',
      409: 'Der Eintrag wurde geändert. Bitte laden Sie die aktuellen Daten.',
      429: 'Zu viele Anfragen. Bitte warten Sie kurz.', 503: 'Diese Funktion ist derzeit nicht verfügbar.' } as Record<number, string>)[status]
      ?? 'Die Anfrage konnte nicht abgeschlossen werden.');
  }
}

export async function api<T>(path: string, options: { method?: string; body?: unknown; signal?: AbortSignal } = {}): Promise<T> {
  const method = options.method ?? 'GET';
  const headers = new Headers();
  const signal = options.signal ? AbortSignal.any([options.signal, AbortSignal.timeout(15000)]) : AbortSignal.timeout(15000);
  if (method !== 'GET') {
    const response = await fetch('/api/identity/csrf', { credentials: 'same-origin', signal });
    if (!response.ok) throw new ApiError(response.status);
    const csrf = await response.json() as { token: string };
    headers.set('X-CSRF-TOKEN', csrf.token);
  }
  if (options.body !== undefined) headers.set('Content-Type', 'application/json');
  const response = await fetch(`/api${path}`, { method, headers, credentials: 'same-origin', signal,
    body: options.body === undefined ? undefined : JSON.stringify(options.body) });
  if (!response.ok) {
    if (response.status === 401 && path !== '/identity/login' && path !== '/identity/me')
      window.dispatchEvent(new Event('revola-session-expired'));
    throw new ApiError(response.status);
  }
  return response.status === 204 || response.status === 201 ? undefined as T : await response.json() as T;
}

export function errorMessage(error: unknown) {
  return error instanceof ApiError ? error.message : 'Verbindung fehlgeschlagen. Bitte erneut versuchen.';
}
