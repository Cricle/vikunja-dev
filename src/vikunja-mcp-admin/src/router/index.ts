import { createRouter, createWebHistory } from 'vue-router'
import Dashboard from '../views/Dashboard.vue'
import Configuration from '../views/Configuration.vue'
import Tools from '../views/Tools.vue'
import Sessions from '../views/Sessions.vue'
import Logs from '../views/Logs.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'dashboard',
      component: Dashboard
    },
    {
      path: '/config',
      name: 'configuration',
      component: Configuration
    },
    {
      path: '/tools',
      name: 'tools',
      component: Tools
    },
    {
      path: '/sessions',
      name: 'sessions',
      component: Sessions
    },
    {
      path: '/logs',
      name: 'logs',
      component: Logs
    }
  ]
})

export default router
