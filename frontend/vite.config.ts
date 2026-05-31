import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// Proxy API + SignalR hub to the backend during development.
const API_TARGET = process.env.VITE_API_TARGET ?? 'http://localhost:5080'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': { target: API_TARGET, changeOrigin: true },
      '/hubs': { target: API_TARGET, changeOrigin: true, ws: true },
    },
  },
})
