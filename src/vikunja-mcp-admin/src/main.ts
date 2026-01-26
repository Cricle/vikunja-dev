import { createApp } from 'vue'
import { createPinia } from 'pinia'
import { createVuestic } from 'vuestic-ui'
import 'vuestic-ui/css'
import '@mdi/font/css/materialdesignicons.css'

import App from './App.vue'
import router from './router'

const app = createApp(App)

app.use(createPinia())
app.use(router)
app.use(createVuestic({
  config: {
    icons: {
      aliases: [
        {
          name: '{icon}',
          to: 'mdi-{icon}',
        },
      ],
    },
  }
}))

app.mount('#app')
