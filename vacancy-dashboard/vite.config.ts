import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    allowedHosts: ['vacancy-dashboard'],
    proxy: {
      '/api': process.env.API_PROXY_TARGET ?? 'http://localhost:8081',
    },
  },
})
