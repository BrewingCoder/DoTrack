import path from 'node:path'
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    port: 5273,
    strictPort: true,
    host: true,
    allowedHosts: true,
    proxy: {
      '/api': 'http://localhost:5259',
      '/healthz': 'http://localhost:5259',
      '/openapi': 'http://localhost:5259',
    },
  },
})
