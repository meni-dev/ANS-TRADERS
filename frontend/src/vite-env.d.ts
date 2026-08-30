/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_API_BASE_URL: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}

/** Injected by Vite from package.json. See the `define` in vite.config.ts. */
declare const __APP_VERSION__: string
