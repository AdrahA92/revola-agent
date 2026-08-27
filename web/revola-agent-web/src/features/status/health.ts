export type ServiceStatus = 'Bereit' | 'Nicht verfügbar';

async function readHealth(path: string, signal: AbortSignal): Promise<ServiceStatus> {
  const response = await fetch(path, { signal: AbortSignal.any([signal, AbortSignal.timeout(8000)]) });
  if (!response.ok) return 'Nicht verfügbar';
  const body: unknown = await response.json();
  return typeof body === 'object' && body !== null && 'status' in body && body.status === 'Healthy'
    ? 'Bereit' : 'Nicht verfügbar';
}

export async function checkHealth(signal: AbortSignal) {
  const results = await Promise.allSettled([
    readHealth('/health/live', signal),
    readHealth('/health/ready', signal),
  ]);
  return results.map((result): ServiceStatus => result.status === 'fulfilled' ? result.value : 'Nicht verfügbar');
}
