<template>
  <VaLayout>
    <template #top>
      <VaNavbar color="background-secondary" class="modern-navbar">
        <template #left>
          <VaNavbarItem class="logo-section">
            <div class="logo-container">
              <VaIcon name="mdi-layers" color="primary" size="large" />
              <span class="app-title ml-2">Vikunja MCP</span>
            </div>
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
              class="nav-item"
            >
              <VaIcon :name="item.icon" size="small" class="mr-2" />
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
      <div class="content-wrapper fade-in">
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
  { title: 'Dashboard', to: '/', icon: 'mdi-view-dashboard' },
  { title: 'Configuration', to: '/config', icon: 'mdi-cog' },
  { title: 'Tools', to: '/tools', icon: 'mdi-tools' },
  { title: 'Sessions', to: '/sessions', icon: 'mdi-account-multiple' },
  { title: 'Logs', to: '/logs', icon: 'mdi-text-box' }
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

<style scoped>
.modern-navbar {
  backdrop-filter: blur(10px);
  border-bottom: 1px solid rgba(0, 0, 0, 0.05);
}

.logo-container {
  display: flex;
  align-items: center;
  padding: 0.5rem 1rem;
  border-radius: 12px;
  background: linear-gradient(135deg, rgba(102, 126, 234, 0.1) 0%, rgba(118, 75, 162, 0.1) 100%);
  transition: all 0.3s ease;
}

.logo-container:hover {
  transform: scale(1.05);
  box-shadow: 0 4px 12px rgba(102, 126, 234, 0.2);
}

.app-title {
  font-size: 1.25rem;
  font-weight: 700;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  letter-spacing: -0.5px;
}

.nav-item {
  display: flex;
  align-items: center;
  font-weight: 500;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}

.content-wrapper {
  max-width: 1400px;
  margin: 0 auto;
  padding: 2rem;
  min-height: calc(100vh - 64px);
}

.status-badge {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 1rem;
  border-radius: 20px;
  background: rgba(255, 255, 255, 0.9);
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.status-dot {
  width: 10px;
  height: 10px;
  border-radius: 50%;
  animation: pulse 2s infinite;
}

.status-dot-success {
  background: linear-gradient(135deg, #11998e 0%, #38ef7d 100%);
  box-shadow: 0 0 10px rgba(56, 239, 125, 0.5);
}

.status-dot-danger {
  background: linear-gradient(135deg, #eb3349 0%, #f45c43 100%);
  animation: none;
  box-shadow: 0 0 10px rgba(235, 51, 73, 0.5);
}

.status-dot-warning {
  background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);
  box-shadow: 0 0 10px rgba(240, 147, 251, 0.5);
}

@keyframes pulse {
  0%, 100% {
    opacity: 1;
    transform: scale(1);
  }
  50% {
    opacity: 0.7;
    transform: scale(1.1);
  }
}

@media (max-width: 768px) {
  .content-wrapper {
    padding: 1rem;
  }
  
  .app-title {
    font-size: 1.125rem;
  }
  
  .nav-item {
    font-size: 0.875rem;
  }
}
</style>
