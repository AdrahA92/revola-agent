import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import Alert from '@mui/material/Alert';
import Button from '@mui/material/Button';
import TextField from '@mui/material/TextField';
import { api, errorMessage } from '../../lib/api';

const schema = z.object({
  email: z.string(), userId: z.string(), token: z.string(), newPassword: z.string(),
});
type Fields = z.infer<typeof schema>;

export function RecoveryPage({ mode }: { mode: 'confirm' | 'reset' | 'forgot' | 'resend' }) {
  const [error, setError] = useState('');
  const [done, setDone] = useState(false);
  const validation = schema.superRefine((value, ctx) => {
    if (mode === 'forgot' || mode === 'resend') {
      if (!z.email().max(254).safeParse(value.email).success)
        ctx.addIssue({ code: 'custom', path: ['email'], message: 'Gültige E-Mail-Adresse erforderlich.' });
    } else {
      if (!z.uuid().safeParse(value.userId).success)
        ctx.addIssue({ code: 'custom', path: ['userId'], message: 'Benutzer-ID aus der Nachricht erforderlich.' });
      if (!value.token || value.token.length > 4096)
        ctx.addIssue({ code: 'custom', path: ['token'], message: 'Code aus der Nachricht erforderlich.' });
      if (mode === 'reset' && !/^(?=.*[a-z])(?=.*[A-Z])(?=.*[0-9])(?=.*[^a-zA-Z0-9]).{12,128}$/.test(value.newPassword))
        ctx.addIssue({ code: 'custom', path: ['newPassword'], message: '12–128 Zeichen mit Groß-/Kleinbuchstaben, Zahl und Sonderzeichen.' });
    }
  });
  const { register, handleSubmit, reset, formState: { errors, isSubmitting } } = useForm<Fields>({
    resolver: zodResolver(validation), defaultValues: { email: '', userId: '', token: '', newPassword: '' },
  });
  const title = mode === 'confirm' ? 'E-Mail bestätigen' : mode === 'forgot' ? 'Passwort vergessen' : mode === 'resend' ? 'Bestätigung erneut anfordern' : 'Passwort zurücksetzen';
  const submit = handleSubmit(async values => {
    setError('');
    try {
      await api<void>(`/identity/${mode === 'confirm' ? 'confirm-email' : mode === 'forgot' ? 'request-reset' : mode === 'resend' ? 'request-confirmation' : 'reset-password'}`,
        { method: 'POST', body: mode === 'forgot' || mode === 'resend' ? { email: values.email } : mode === 'confirm'
          ? { userId: values.userId, token: values.token } : { userId: values.userId, token: values.token, newPassword: values.newPassword } });
      reset(); setDone(true);
    } catch (failure) { setError(errorMessage(failure)); }
  });
  return <section className="auth-page"><h1>{title}</h1>
    <p>Entwicklungsmodus: Nachrichten finden Sie im lokalen Test-Posteingang (Mailpit, Port 8025). Es erfolgt kein externer Versand.</p>
    {error ? <Alert severity="error">{error}</Alert> : null}
    {done ? <Alert severity="success">{mode === 'forgot' || mode === 'resend' ? 'Anfrage angenommen. Für ein passendes Konto wird bei erreichbarem Test-Posteingang eine Nachricht hinterlegt.' : 'Erfolgreich. Sie können sich jetzt anmelden.'}</Alert> :
      <form onSubmit={submit} noValidate className="auth-form">
        {(mode === 'forgot' || mode === 'resend' ? ['email'] as const : mode === 'reset' ? ['userId', 'token', 'newPassword'] as const : ['userId', 'token'] as const).map(name =>
          <TextField key={name} label={{ email: 'E-Mail-Adresse', userId: 'Benutzer-ID', token: 'Code', newPassword: 'Neues Passwort' }[name]}
            {...register(name)} type={name === 'newPassword' || name === 'token' ? 'password' : name === 'email' ? 'email' : 'text'}
            autoComplete={name === 'newPassword' ? 'new-password' : 'off'} error={!!errors[name]} helperText={errors[name]?.message}
            fullWidth sx={{ mt: 2 }} slotProps={{ htmlInput: { maxLength: name === 'token' ? 4096 : name === 'userId' ? 36 : name === 'email' ? 254 : 128 } }} />)}
        <Button type="submit" variant="contained" disabled={isSubmitting} sx={{ mt: 3 }}>{isSubmitting ? 'Wird verarbeitet …' : title}</Button>
      </form>}
    <p><Link to="/login">Zur Anmeldung</Link>{mode === 'forgot' ? <> · <Link to="/reset">Code eingeben</Link></> : null}</p>
    {mode === 'confirm' ? <p><Link to="/resend">Bestätigung erneut anfordern</Link></p> : mode === 'resend' ? <p><Link to="/confirm">Code eingeben</Link></p> : null}
  </section>;
}
