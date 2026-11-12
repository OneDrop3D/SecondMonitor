import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// En dev : `npm run dev` (port 5173) relaie l'API et le WebSocket vers le hub (port 8686).
export default defineConfig({
  plugins: [react()],
  build: { outDir: '../src/SecondMonitor.Hub/wwwroot', emptyOutDir: true },
  server: {
    host: true,
    proxy: {
      '/api': 'http://localhost:8686',
      '/ws': { target: 'ws://localhost:8686', ws: true },
    },
  },
});
