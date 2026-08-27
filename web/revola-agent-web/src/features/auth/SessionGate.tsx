import { useEffect } from 'react';
import type { ReactNode } from 'react';
import { Navigate } from 'react-router-dom';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import Alert from '@mui/material/Alert';
import Button from '@mui/material/Button';
import { api, ApiError, errorMessage } from '../../lib/api';

export function SessionGate({ children }: { children: (userId: string) => ReactNode }) {
  const cache = useQueryClient();
  const session = useQuery({ queryKey: ['session'], queryFn: async ({ signal }) => {
    try { return await api<{ id: string }>('/identity/me', { signal }); }
    catch (error) { if (error instanceof ApiError && error.status === 401) return null; throw error; }
  }, retry: false, refetchOnWindowFocus: true });
  useEffect(() => {
    const expire = () => {
      void cache.cancelQueries();
      cache.clear();
      cache.setQueryData(['session'], null);
    };
    window.addEventListener('revola-session-expired', expire);
    return () => window.removeEventListener('revola-session-expired', expire);
  }, [cache]);
  if (session.isPending) return <p role="status">Sitzung wird geprüft …</p>;
  if (session.isError) return <Alert severity="error">{errorMessage(session.error)} <Button onClick={() => void session.refetch()}>Erneut versuchen</Button></Alert>;
  if (!session.data) return <Navigate to="/login" replace />;
  return children(session.data.id);
}
