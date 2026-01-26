<template>
  <VaLayout>
    <template #top>
      <VaNavbar color="background-secondary">
        <template #left>
          <VaNavbarItem class="logo-section">
            <VaIcon name="layers" color="primary" />
            <span class="app-title ml-2">Vikunja MCP</span>
          </VaNavbarItem>
        </template>

        <template #center>
          <router-link
            v-for="item in menuItems"
            :key="item.to"
            :to="item.to"
            custom
            v-slot="{ navigate, isActive }"
          >
            <VaNavbarItem
              @click="navigate"
              :class="{ 'va-navbar-item--active': isActive }"
              style="cursor: pointer;"
            >
              {{ item.title }}
            </VaNavbarItem>
          </router-link>
        </template>

        <template #right>
          <VaNavbarItem>
            <VaBadge
              :text="serverStatus"
              :color="statusColor"
              class="status-badge"
            >
              <template #prepend>
                <div class="status-dot" :class="`status-dot-${statusColor}`"></div>
              </template>
            </VaBadge>
          </VaNavbarItem>
        </template>
      </VaNavbar>
    </template>

    <template #content>
      <div class="content-wrapper">
        <RouterView />
      </div>
    </template>
  </VaLayout>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useServerStore } from './stores/server'

const serverStore = useServerStore()

const menuItems = [
  { title: 'Dashboard', to: '/' },
  { title: 'Configuration', to: '/config' },
  { title: 'Tools', to: '/tools' },
  { title: 'Sessions', to: '/sessions' },
  { title: 'Logs', to: '/logs' }
]

const serverStatus = ref('Checking...')
const isOnline = ref(false)

const statusColor = computed(() => {
  if (serverStatus.value === 'Checking...') return 'warning'
  return isOnline.value ? 'success' : 'danger'
})

onMounted(async () => {
  try {
    await serverStore.checkHealth()
    serverStatus.value = 'Online'
    isOnline.value = true
  } catch (error) {
    serverStatus.value = 'Offline'
    isOnline.value = false
  }
})
</script>

<style>
.logo-section {
  display: flex;
  align-items: center;
}

.app-title {
  font-size: 1.125rem;
  font-weight: 600;
}

.content-wrapper {
  max-width: 1400px;
  margin: 0 auto;
  padding: 2rem;
}

.status-badge {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.status-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  animation: pulse 2s infinite;
}

.status-dot-success {
  background: var(--va-success);
}

.status-dot-danger {
  background: var(--va-danger);
  animation: none;
}

.status-dot-warning {
  background: var(--va-warning);
}

.va-navbar-item--active {
  background: rgba(var(--va-primary-rgb), 0.15);
  color: var(--va-primary);
}

@keyframes pulse {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.5; }
}

@media (max-width: 768px) {
  .content-wrapper {
    padding: 1rem;
  }
}
</style>
