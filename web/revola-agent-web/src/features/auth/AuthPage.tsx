import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import Alert from '@mui/material/Alert';
import Button from '@mui/material/Button';
import TextField from '@mui/material/TextField';
import { api, errorMessage } from '../../lib/api';

const credentials = z.object({ email: z.email('Bitte geben Sie eine gültige E-Mail-Adresse ein.').max(254),
  password: z.string().min(1, 'Bitte geben Sie Ihr Passwort ein.').max(128), code: z.string().max(6).optional(), recoveryCode: z.string().max(100).optional() });
const registration = credentials.extend({ password: z.string().min(12, 'Mindestens 12 Zeichen erforderlich.').max(128)
  .regex(/[a-z]/, 'Ein Kleinbuchstabe fehlt.').regex(/[A-Z]/, 'Ein Großbuchstabe fehlt.')
  .regex(/[0-9]/, 'Eine Zahl fehlt.').regex(/[^a-zA-Z0-9]/, 'Ein Sonderzeichen fehlt.') });
type Credentials = z.infer<typeof credentials>;

export function AuthPage({ registerAccount = false }: { registerAccount?: boolean }) {
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const [error, setError] = useState('');
  const [created, setCreated] = useState(false);
  const [requiresMfa, setRequiresMfa] = useState(false);
  const { register, handleSubmit, formState: { errors, isSubmitting }, resetField } = useForm<Credentials>({
    resolver: zodResolver(registerAccount ? registration : credentials), defaultValues: { email: '', password: '' },
  });
  const submit = handleSubmit(async values => {
    setError('');
    try {
      const result = await api<{ requiresMfa?: boolean } | undefined>(registerAccount ? '/identity/register' : '/identity/login', { method: 'POST', body: values });
      if (result?.requiresMfa) { setRequiresMfa(true); return; }
      resetField('password');
      resetField('code'); resetField('recoveryCode');
      if (registerAccount) { setCreated(true); return; }
      await queryClient.cancelQueries();
      queryClient.clear();
      navigate('/workspace', { replace: true });
    } catch (failure) { setError(errorMessage(failure)); }
  });

  if (created) return <section className="auth-page"><h1>Konto erstellt</h1><p>Bestätigen Sie vor der Anmeldung Ihre E-Mail-Adresse mit dem Code aus dem lokalen Test-Posteingang.</p>
    <Button component={Link} to="/confirm" variant="contained">E-Mail bestätigen</Button></section>;
  return <section className="auth-page">
    <h1>{registerAccount ? 'Konto erstellen' : 'Willkommen zurück'}</h1>
    <p className="auth-subtitle">{registerAccount ? 'Registrierung ist derzeit nur in der Entwicklungsumgebung verfügbar.' : 'Melden Sie sich bei Ihrem Arbeitsbereich an.'}</p>
    {error ? <Alert severity="error" sx={{ mb: 3 }}>{error}</Alert> : null}
    <form onSubmit={submit} noValidate className="auth-form">
      <label htmlFor="auth-email">E-Mail-Adresse</label>
      <TextField id="auth-email" type="email" fullWidth autoComplete="username" {...register('email')}
        error={!!errors.email} helperText={errors.email?.message} slotProps={{ htmlInput: { maxLength: 254 } }} />
      <label htmlFor="auth-password">Passwort</label>
      <TextField id="auth-password" type="password" fullWidth autoComplete={registerAccount ? 'new-password' : 'current-password'}
        {...register('password')} error={!!errors.password} helperText={errors.password?.message ?? (registerAccount ? 'Mindestens 12 Zeichen mit Groß-/Kleinbuchstaben, Zahl und Sonderzeichen.' : undefined)}
        slotProps={{ htmlInput: { maxLength: 128 } }} />
      <Button type="submit" variant="contained" fullWidth disabled={isSubmitting} sx={{ mt: 3, minHeight: 64, fontSize: 18 }}>
        {isSubmitting ? 'Wird verarbeitet …' : registerAccount ? 'Konto erstellen' : 'Anmelden'}</Button>
      {requiresMfa ? <><Alert severity="info" sx={{ mt: 2 }}>Geben Sie einen aktuellen Authenticator-Code oder einen unbenutzten Wiederherstellungscode ein.</Alert>
        <TextField label="Authenticator-Code" {...register('code')} autoComplete="one-time-code" fullWidth sx={{ mt: 2 }} slotProps={{ htmlInput: { maxLength: 6, inputMode: 'numeric' } }} />
        <TextField label="Wiederherstellungscode (alternativ)" {...register('recoveryCode')} type="password" autoComplete="off" fullWidth sx={{ mt: 2 }} slotProps={{ htmlInput: { maxLength: 100 } }} /></> : null}
    </form>
    <p className="auth-switch">{registerAccount ? 'Bereits registriert?' : 'Noch kein Konto?'} <Link to={registerAccount ? '/login' : '/register'}>{registerAccount ? 'Anmelden' : 'Registrieren'}</Link></p>
    {!registerAccount ? <p><Link to="/forgot">Passwort vergessen?</Link> · <Link to="/confirm">E-Mail bestätigen</Link></p> : null}
    <p className="auth-footer">Ihre Organisationen und Daten bleiben getrennt.</p>
  </section>;
}
