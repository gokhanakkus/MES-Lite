import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { CssBaseline, ThemeProvider } from '@mui/material'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter } from 'react-router-dom'
import App from './App.tsx'
import { theme } from './theme'
import { SignalRProvider } from './realtime/signalr'

const queryClient = new QueryClient({
  defaultOptions: { queries: { refetchOnWindowFocus: false, staleTime: 5000 } },
})

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <QueryClientProvider client={queryClient}>
        <SignalRProvider>
          <BrowserRouter>
            <App />
          </BrowserRouter>
        </SignalRProvider>
      </QueryClientProvider>
    </ThemeProvider>
  </StrictMode>,
)
