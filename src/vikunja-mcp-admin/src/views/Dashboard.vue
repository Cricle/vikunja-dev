<template>
  <div class="dashboard">
    <!-- Server Status Card -->
    <VaCard class="mb-4">
      <VaCardTitle>
        <div class="d-flex justify-space-between align-center">
          <span>Server Status</span>
          <VaButton 
            size="small" 
            @click="refreshData"
            :loading="loading"
          >
            <VaIcon name="refresh" class="mr-1" />
            Refresh
          </VaButton>
        </div>
      </VaCardTitle>
      <VaCardContent>
        <VaInnerLoading :loading="loading">
          <div v-if="serverStore.health" class="row">
            <div class="flex xs12 md6 lg3">
              <div class="status-item">
                <div class="text-secondary">Status</div>
                <VaBadge :text="serverStore.health.status" color="success" />
              </div>
            </div>
            <div class="flex xs12 md6 lg3">
              <div class="status-item">
                <div class="text-secondary">Server</div>
                <div>{{ serverStore.health.server }}</div>
              </div>
            </div>
            <div class="flex xs12 md6 lg3">
              <div class="status-item">
                <div class="text-secondary">Version</div>
                <div>{{ serverStore.health.version }}</div>
              </div>
            </div>
            <div class="flex xs12 md6 lg3">
              <div class="status-item">
                <div class="text-secondary">Last Check</div>
                <div>{{ formatDate(serverStore.health.timestamp) }}</div>
              </div>
            </div>
          </div>
        </VaInnerLoading>
      </VaCardContent>
    </VaCard>

    <!-- Server Statistics -->
    <VaCard class="mb-4" v-if="stats">
      <VaCardTitle>Server Statistics</VaCardTitle>
      <VaCardContent>
        <div class="row">
          <div class="flex xs12 md6 lg3">
            <div class="stat-box">
              <VaIcon name="schedule" size="large" color="primary" class="mb-2" />
              <div class="stat-value">{{ stats.server.uptime }}</div>
              <div class="text-secondary">Uptime</div>
            </div>
          </div>
          <div class="flex xs12 md6 lg3">
            <div class="stat-box">
              <VaIcon name="people" size="large" color="success" class="mb-2" />
              <div class="stat-value">{{ stats.sessions.active }} / {{ stats.sessions.total }}</div>
              <div class="text-secondary">Active Sessions</div>
            </div>
          </div>
          <div class="flex xs12 md6 lg3">
            <div class="stat-box">
              <VaIcon name="build" size="large" color="info" class="mb-2" />
              <div class="stat-value">{{ stats.tools.total }}</div>
              <div class="text-secondary">Tools ({{ stats.tools.subcommands }} subcommands)</div>
            </div>
          </div>
          <div class="flex xs12 md6 lg3">
            <div class="stat-box">
              <VaIcon name="memory" size="large" color="warning" class="mb-2" />
              <div class="stat-value">{{ formatMemory(stats.memory.workingSet) }}</div>
              <div class="text-secondary">Memory Usage</div>
            </div>
          </div>
        </div>
      </VaCardContent>
    </VaCard>

    <!-- Quick Stats Cards -->
    <div class="row">
      <div class="flex xs12 md4">
        <VaCard class="stat-card">
          <VaCardContent class="text-center">
            <VaIcon name="build" size="large" color="primary" class="mb-2" />
            <div class="stat-value">{{ serverStore.tools.length }}</div>
            <div class="text-secondary">Registered Tools</div>
          </VaCardContent>
        </VaCard>
      </div>
      <div class="flex xs12 md4">
        <VaCard class="stat-card">
          <VaCardContent class="text-center">
            <VaIcon name="code" size="large" color="info" class="mb-2" />
            <div class="stat-value">{{ totalSubcommands }}</div>
            <div class="text-secondary">Total Subcommands</div>
          </VaCardContent>
        </VaCard>
      </div>
      <div class="flex xs12 md4">
        <VaCard class="stat-card">
          <VaCardContent class="text-center">
            <VaIcon name="speed" size="large" color="success" class="mb-2" />
            <div class="stat-value">60/min</div>
            <div class="text-secondary">Rate Limit</div>
          </VaCardContent>
        </VaCard>
      </div>
    </div>

    <!-- Quick Actions -->
    <VaCard class="mt-4">
      <VaCardTitle>Quick Actions</VaCardTitle>
      <VaCardContent>
        <div class="actions">
          <VaButton @click="refreshData" :loading="loading">
            <VaIcon name="refresh" class="mr-2" />
            Refresh Status
          </VaButton>
          <VaButton color="primary" to="/config">
            <VaIcon name="settings" class="mr-2" />
            Configure Server
          </VaButton>
          <VaButton to="/tools">
            <VaIcon name="build" class="mr-2" />
            View Tools
          </VaButton>
          <VaButton to="/sessions" color="info">
            <VaIcon name="people" class="mr-2" />
            Manage Sessions
          </VaButton>
          <VaButton 
            color="warning" 
            @click="disconnectAllSessions"
            :loading="disconnecting"
          >
            <VaIcon name="link_off" class="mr-2" />
            Disconnect All Sessions
          </VaButton>
          <VaButton 
            color="danger" 
            @click="clearAllLogs"
            :loading="clearingLogs"
          >
            <VaIcon name="delete" class="mr-2" />
            Clear Logs
          </VaButton>
        </div>
      </VaCardContent>
    </VaCard>

    <!-- Auto Refresh Toggle -->
    <VaCard class="mt-4">
      <VaCardContent>
        <VaSwitch
          v-model="autoRefresh"
          label="Auto Refresh (Every 10 seconds)"
        />
      </VaCardContent>
    </VaCard>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref, watch } from 'vue'
import { useServerStore } from '../stores/server'
import { adminApi } from '../services/api'
import type { ServerStats } from '../types'
import { useToast } from 'vuestic-ui'

const serverStore = useServerStore()
const { init: notify } = useToast()

const loading = ref(false)
const disconnecting = ref(false)
const clearingLogs = ref(false)
const autoRefresh = ref(false)
const stats = ref<ServerStats | null>(null)
let refreshInterval: number | null = null

const totalSubcommands = computed(() => {
  return serverStore.tools.reduce((sum, tool) => sum + tool.subcommands.length, 0)
})

function formatDate(dateString: string) {
  return new Date(dateString).toLocaleString()
}

function formatMemory(bytes: number): string {
  const mb = bytes / (1024 * 1024)
  return `${mb.toFixed(2)} MB`
}

async function refreshData() {
  loading.value = true
  try {
    await Promise.all([
      serverStore.checkHealth(),
      serverStore.fetchInfo(),
      serverStore.fetchTools(),
      fetchStats()
    ])
    notify({
      message: 'Data refreshed successfully',
      color: 'success'
    })
  } catch (error) {
    notify({
      message: 'Failed to refresh data',
      color: 'danger'
    })
  } finally {
    loading.value = false
  }
}

async function fetchStats() {
  try {
    stats.value = await adminApi.getStats()
  } catch (error) {
    console.error('Failed to fetch stats:', error)
  }
}

async function disconnectAllSessions() {
  if (!confirm('Are you sure you want to disconnect all sessions? This will terminate all active connections.')) {
    return
  }

  disconnecting.value = true
  try {
    await adminApi.disconnectAllSessions()
    notify({
      message: 'All sessions disconnected successfully',
      color: 'success'
    })
    await fetchStats()
  } catch (error) {
    notify({
      message: 'Failed to disconnect sessions',
      color: 'danger'
    })
  } finally {
    disconnecting.value = false
  }
}

async function clearAllLogs() {
  if (!confirm('Are you sure you want to clear all logs? This action cannot be undone.')) {
    return
  }

  clearingLogs.value = true
  try {
    await adminApi.clearLogs()
    notify({
      message: 'Logs cleared successfully',
      color: 'success'
    })
  } catch (error) {
    notify({
      message: 'Failed to clear logs',
      color: 'danger'
    })
  } finally {
    clearingLogs.value = false
  }
}

watch(autoRefresh, (enabled) => {
  if (enabled) {
    refreshInterval = window.setInterval(refreshData, 10000)
  } else if (refreshInterval) {
    clearInterval(refreshInterval)
    refreshInterval = null
  }
})

onMounted(() => {
  refreshData()
})

onUnmounted(() => {
  if (refreshInterval) {
    clearInterval(refreshInterval)
  }
})
</script>

<style scoped>
.dashboard {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.status-item {
  padding: 0.5rem 0;
}

.stat-card {
  height: 100%;
}

.stat-value {
  font-size: 2.5rem;
  font-weight: 600;
  color: var(--va-primary);
  margin-bottom: 0.5rem;
}

.stat-box {
  text-align: center;
  padding: 1rem;
  background: rgba(var(--va-background-element), 0.5);
  border-radius: 8px;
  height: 100%;
}

.stat-box .stat-value {
  font-size: 1.75rem;
}

.actions {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}

.row {
  display: flex;
  flex-wrap: wrap;
  margin: -0.5rem;
}

.flex {
  padding: 0.5rem;
}

.xs12 { flex: 0 0 100%; max-width: 100%; }
.md4 { flex: 0 0 33.333%; max-width: 33.333%; }
.md6 { flex: 0 0 50%; max-width: 50%; }
.lg3 { flex: 0 0 25%; max-width: 25%; }

.d-flex {
  display: flex;
}

.justify-space-between {
  justify-content: space-between;
}

.align-center {
  align-items: center;
}

.text-center {
  text-align: center;
}

@media (max-width: 960px) {
  .md4, .md6, .lg3 {
    flex: 0 0 50%;
    max-width: 50%;
  }
}

@media (max-width: 600px) {
  .md4, .md6, .lg3 {
    flex: 0 0 100%;
    max-width: 100%;
  }
}
</style>
