import { createRouter, createWebHistory } from 'vue-router'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      name: 'dashboard',
      component: () => import('@/views/Dashboard.vue')
    },
    {
      path: '/providers',
      name: 'providers',
      component: () => import('@/views/ProviderConfig.vue')
    },
    {
      path: '/templates',
      name: 'templates',
      component: () => import('@/views/TemplateEditor.vue')
    },
    {
      path: '/settings',
      name: 'settings',
      component: () => import('@/views/Settings.vue')
    }
  ]
})

export default router
