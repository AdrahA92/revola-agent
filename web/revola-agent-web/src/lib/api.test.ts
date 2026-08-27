import { test, expect, vi } from 'vitest';
import { api, ApiError } from './api';

test('failed CSRF acquisition never submits a mutation', async () => {
  const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response(null, { status: 503 }));
  await expect(api('/tenants/example', { method: 'PUT', body: { name: 'Test' } })).rejects.toBeInstanceOf(ApiError);
  expect(fetchMock).toHaveBeenCalledTimes(1);
});

test('expired protected request notifies the session boundary', async () => {
  vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response(null, { status: 401 }));
  const dispatch = vi.spyOn(window, 'dispatchEvent');
  await expect(api('/tenants')).rejects.toMatchObject({ status: 401 });
  expect(dispatch).toHaveBeenCalledWith(expect.objectContaining({ type: 'revola-session-expired' }));
});
