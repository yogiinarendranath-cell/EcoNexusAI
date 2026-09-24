import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

export default defineConfig({
  plugins: [
    react(),
    tailwindcss(),
  ],
  server: {
    port: 5173,
    strictPort: true,
    proxy: {
      // Forward any /api/* call to the .NET backend during dev.
      // This sidesteps browser CORS entirely (single origin from the browser's view)
      // and keeps the backend port out of the frontend source code.
      '/api': {
        target: 'http://localhost:5067',
        changeOrigin: true,
        secure: false,
      },
    },
  },
})
