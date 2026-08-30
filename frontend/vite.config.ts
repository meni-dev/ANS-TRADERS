import { readFileSync } from 'node:fs'
import path from 'node:path'
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// package.json is the one place the version lives. Baked in at build time so the running app can
// never disagree with the artefact it was built from.
const { version } = JSON.parse(
  readFileSync(path.resolve(import.meta.dirname, './package.json'), 'utf8'),
) as { version: string }

// https://vite.dev/config/
export default defineConfig({
  define: { __APP_VERSION__: JSON.stringify(version) },
  plugins: [react()],
  server: {
    port: 5175,
    strictPort: true,
  },
  resolve: {
    alias: {
      '@': path.resolve(import.meta.dirname, './src'),
    },
  },
})
