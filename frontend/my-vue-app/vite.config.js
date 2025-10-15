import { fileURLToPath, URL } from 'node:url'
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import vueDevTools from 'vite-plugin-vue-devtools'
import mkcert from 'vite-plugin-mkcert'

import fs from 'node:fs'
import path from 'node:path'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    vue(),
    vueDevTools(),
    mkcert()
  ],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url))
    },
  },
  server: {
    port: 8081,
    host: 'localhost',
    https: {
      key: fs.readFileSync(path.resolve('C:/Users/dotan/.vite-plugin-mkcert/dev.pem')),
      cert: fs.readFileSync(path.resolve('C:/Users/dotan/.vite-plugin-mkcert/cert.pem'))
    }
  },
})
