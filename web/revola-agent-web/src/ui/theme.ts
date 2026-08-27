import { createTheme } from '@mui/material/styles';

export const theme = createTheme({
  palette: { primary: { main: '#176b65' }, text: { primary: '#142d3b', secondary: '#536875' }, background: { default: '#fff' }, divider: '#dce4e7' },
  typography: { fontFamily: 'Arial, Helvetica, sans-serif', button: { textTransform: 'none', fontSize: '1rem', fontWeight: 600 },
    h1: { fontSize: '2.75rem', fontWeight: 700, letterSpacing: '-1px', lineHeight: 1.15 }, h2: { fontSize: '1.5rem', fontWeight: 700 } },
  shape: { borderRadius: 4 },
  components: { MuiButton: { defaultProps: { disableElevation: true }, styleOverrides: { root: { minHeight: 44 } } },
    MuiOutlinedInput: { styleOverrides: { root: { backgroundColor: '#fff' } } } },
});
