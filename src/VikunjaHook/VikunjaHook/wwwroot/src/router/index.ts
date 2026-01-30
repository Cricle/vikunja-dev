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
      path: '/reminder',
      name: 'reminder',
      component: () => import('@/views/TaskReminder.vue')
    },
    {
      path: '/scheduled-push',
      name: 'scheduledPush',
      component: () => import('@/views/ScheduledPushView.vue')
    },
    {
      path: '/history',
      name: 'history',
      component: () => import('@/views/PushHistory.vue')
    },
    {
      path: '/settings',
      name: 'settings',
      component: () => import('@/views/Settings.vue')
    }
  ]
})

export default router
