import { createApp } from 'vue'
import { createPinia } from 'pinia'
import { createVuestic } from 'vuestic-ui'
import 'vuestic-ui/css'
import './assets/global.css'
import App from './App.vue'
import router from './router'
import i18n from './i18n'

const app = createApp(App)

app.use(createPinia())
app.use(router)
app.use(i18n)
app.use(createVuestic({
  config: {
    colors: {
      presets: {
        light: {
          primary: '#3b82f6',
          secondary: '#8b5cf6',
          success: '#10b981',
          info: '#06b6d4',
          danger: '#ef4444',
          warning: '#f59e0b',
          backgroundPrimary: '#ffffff',
          backgroundSecondary: '#f8fafc',
          backgroundElement: '#ffffff',
          backgroundBorder: '#e2e8f0',
          textPrimary: '#1e293b',
          textInverted: '#ffffff'
        },
        dark: {
          primary: '#3b82f6',
          secondary: '#8b5cf6',
          success: '#10b981',
          info: '#06b6d4',
          danger: '#ef4444',
          warning: '#f59e0b',
          backgroundPrimary: '#0f172a',
          backgroundSecondary: '#1e293b',
          backgroundElement: '#1e293b',
          backgroundBorder: '#334155',
          textPrimary: '#e2e8f0',
          textInverted: '#ffffff'
        }
      }
    }
  }
}))

app.mount('#app')
