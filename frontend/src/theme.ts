import { createTheme } from '@mui/material/styles'

// Dark "control-room" palette suited to a factory monitoring dashboard.
export const theme = createTheme({
  palette: {
    mode: 'dark',
    primary: { main: '#42a5f5' },
    secondary: { main: '#26c6da' },
    success: { main: '#66bb6a' },
    warning: { main: '#ffa726' },
    error: { main: '#ef5350' },
    background: { default: '#0e1422', paper: '#161d2e' },
    divider: 'rgba(255,255,255,0.08)',
  },
  shape: { borderRadius: 12 },
  typography: {
    fontFamily: 'Inter, Segoe UI, Roboto, system-ui, sans-serif',
    h4: { fontWeight: 700 },
    h6: { fontWeight: 600 },
  },
  components: {
    MuiCard: {
      styleOverrides: {
        root: { backgroundImage: 'none', border: '1px solid rgba(255,255,255,0.06)' },
      },
    },
    MuiPaper: { styleOverrides: { root: { backgroundImage: 'none' } } },
  },
})

// Status -> color mapping reused across pages.
export const statusColor: Record<string, 'success' | 'error' | 'warning' | 'default'> = {
  Running: 'success',
  Stopped: 'error',
  Maintenance: 'warning',
}

export function oeeColor(value: number): string {
  if (value >= 75) return '#66bb6a'
  if (value >= 60) return '#ffa726'
  return '#ef5350'
}

// Machine Health Score thresholds: 100 new · ~60 attention · ~30 failure risk · 0 down.
export function healthColor(value: number): string {
  if (value >= 60) return '#66bb6a'
  if (value >= 30) return '#ffa726'
  return '#ef5350'
}

export function healthLabel(value: number): string {
  if (value >= 80) return 'İyi'
  if (value >= 60) return 'Normal'
  if (value >= 30) return 'Dikkat'
  return 'Arıza Riski'
}
