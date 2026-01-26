<template>
  <div class="sessions-page">
    <!-- Header Actions -->
    <VaCard class="mb-4">
      <VaCardContent>
        <div class="d-flex justify-space-between align-center flex-wrap" style="gap: 1rem;">
          <div>
            <h2 class="va-h2 mb-1">Session Management</h2>
            <p class="text-secondary">Manage active Vikunja API sessions</p>
          </div>
          <div class="d-flex" style="gap: 0.5rem;">
            <VaButton 
              @click="refreshSessions"
              :loading="loading"
            >
              <VaIcon name="refresh" class="mr-2" />
              Refresh
            </VaButton>
            <VaButton 
              color="danger" 
              @click="disconnectAll"
              :loading="disconnectingAll"
              :disabled="sessions.length === 0"
            >
              <VaIcon name="link_off" class="mr-2" />
              Disconnect All
            </VaButton>
          </div>
        </div>
      </VaCardContent>
    </VaCard>

    <!-- Sessions Table -->
    <VaCard>
      <VaCardContent>
        <VaInnerLoading :loading="loading">
          <VaDataTable
            v-if="sessions.length > 0"
            :items="sessions"
            :columns="columns"
            striped
            hoverable
          >
            <template #cell(sessionId)="{ value }">
              <code class="session-id">{{ value }}</code>
            </template>

            <template #cell(authType)="{ value }">
              <VaBadge 
                :text="value" 
                :color="value === 'Jwt' ? 'success' : 'info'" 
              />
            </template>

            <template #cell(isExpired)="{ value }">
              <VaBadge 
                :text="value ? 'Expired' : 'Active'" 
                :color="value ? 'danger' : 'success'" 
              />
            </template>

            <template #cell(created)="{ value }">
              <div>
                <div>{{ formatDate(value) }}</div>
                <div class="text-secondary" style="font-size: 0.75rem;">
                  {{ formatRelativeTime(value) }}
                </div>
              </div>
            </template>

            <template #cell(lastAccessed)="{ value }">
              <div>
                <div>{{ formatDate(value) }}</div>
                <div class="text-secondary" style="font-size: 0.75rem;">
                  {{ formatRelativeTime(value) }}
                </div>
              </div>
            </template>

            <template #cell(actions)="{ row }">
              <VaButton
                size="small"
                color="danger"
                @click="disconnectSession(row.sessionId)"
                :loading="disconnectingIds.has(row.sessionId)"
              >
                <VaIcon name="link_off" size="small" class="mr-1" />
                Disconnect
              </VaButton>
            </template>
          </VaDataTable>

          <div v-else class="text-center py-6">
            <VaIcon name="info" size="large" class="mb-3" color="secondary" />
            <h3 class="va-h3 mb-2">No Active Sessions</h3>
            <p class="text-secondary">There are currently no active Vikunja API sessions.</p>
          </div>
        </VaInnerLoading>
      </VaCardContent>
    </VaCard>

    <!-- Auto Refresh -->
    <VaCard class="mt-4">
      <VaCardContent>
        <VaSwitch
          v-model="autoRefresh"
          label="Auto Refresh (Every 5 seconds)"
        />
      </VaCardContent>
    </VaCard>

    <!-- Session Statistics -->
    <VaCard class="mt-4" v-if="sessions.length > 0">
      <VaCardTitle>Session Statistics</VaCardTitle>
      <VaCardContent>
        <div class="row">
          <div class="flex xs12 md3">
            <div class="stat-box">
              <VaIcon name="people" size="large" color="primary" class="mb-2" />
              <div class="stat-value">{{ sessions.length }}</div>
              <div class="text-secondary">Total Sessions</div>
            </div>
          </div>
          <div class="flex xs12 md3">
            <div class="stat-box">
              <VaIcon name="check_circle" size="large" color="success" class="mb-2" />
              <div class="stat-value">{{ activeSessions }}</div>
              <div class="text-secondary">Active</div>
            </div>
          </div>
          <div class="flex xs12 md3">
            <div class="stat-box">
              <VaIcon name="error" size="large" color="danger" class="mb-2" />
              <div class="stat-value">{{ expiredSessions }}</div>
              <div class="text-secondary">Expired</div>
            </div>
          </div>
          <div class="flex xs12 md3">
            <div class="stat-box">
              <VaIcon name="vpn_key" size="large" color="info" class="mb-2" />
              <div class="stat-value">{{ jwtSessions }}</div>
              <div class="text-secondary">JWT Auth</div>
            </div>
          </div>
        </div>
      </VaCardContent>
    </VaCard>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, watch } from 'vue'
import { adminApi } from '../services/api'
import type { Session } from '../types'
import { useToast } from 'vuestic-ui'

const { init: notify } = useToast()

const columns = [
  { key: 'sessionId', label: 'Session ID', sortable: true },
  { key: 'apiUrl', label: 'API URL', sortable: true },
  { key: 'authType', label: 'Auth Type', sortable: true },
  { key: 'isExpired', label: 'Status', sortable: true },
  { key: 'created', label: 'Created', sortable: true },
  { key: 'lastAccessed', label: 'Last Accessed', sortable: true },
  { key: 'actions', label: 'Actions', width: 150 }
]

const sessions = ref<Session[]>([])
const loading = ref(false)
const disconnectingAll = ref(false)
const disconnectingIds = ref(new Set<string>())
const autoRefresh = ref(false)
let refreshInterval: number | null = null

const activeSessions = computed(() => {
  return sessions.value.filter(s => !s.isExpired).length
})

const expiredSessions = computed(() => {
  return sessions.value.filter(s => s.isExpired).length
})

const jwtSessions = computed(() => {
  return sessions.value.filter(s => s.authType === 'Jwt').length
})

function formatDate(dateString: string) {
  return new Date(dateString).toLocaleString()
}

function formatRelativeTime(dateString: string): string {
  const date = new Date(dateString)
  const now = new Date()
  const diffMs = now.getTime() - date.getTime()
  const diffMins = Math.floor(diffMs / 60000)
  const diffHours = Math.floor(diffMs / 3600000)
  const diffDays = Math.floor(diffMs / 86400000)

  if (diffMins < 1) return 'just now'
  if (diffMins < 60) return `${diffMins} min ago`
  if (diffHours < 24) return `${diffHours} hour${diffHours > 1 ? 's' : ''} ago`
  return `${diffDays} day${diffDays > 1 ? 's' : ''} ago`
}

async function refreshSessions() {
  loading.value = true
  try {
    sessions.value = await adminApi.getSessions()
    notify({
      message: 'Sessions refreshed successfully',
      color: 'success'
    })
  } catch (error) {
    notify({
      message: 'Failed to fetch sessions',
      color: 'danger'
    })
    console.error('Failed to fetch sessions:', error)
  } finally {
    loading.value = false
  }
}

async function disconnectSession(sessionId: string) {
  if (!confirm('Are you sure you want to disconnect this session?')) {
    return
  }

  disconnectingIds.value.add(sessionId)
  try {
    await adminApi.disconnectSession(sessionId)
    sessions.value = sessions.value.filter(s => s.sessionId !== sessionId)
    notify({
      message: 'Session disconnected successfully',
      color: 'success'
    })
  } catch (error) {
    notify({
      message: 'Failed to disconnect session',
      color: 'danger'
    })
    console.error('Failed to disconnect session:', error)
  } finally {
    disconnectingIds.value.delete(sessionId)
  }
}

async function disconnectAll() {
  if (!confirm('Are you sure you want to disconnect ALL sessions? This will terminate all active connections.')) {
    return
  }

  disconnectingAll.value = true
  try {
    await adminApi.disconnectAllSessions()
    sessions.value = []
    notify({
      message: 'All sessions disconnected successfully',
      color: 'success'
    })
  } catch (error) {
    notify({
      message: 'Failed to disconnect all sessions',
      color: 'danger'
    })
    console.error('Failed to disconnect all sessions:', error)
  } finally {
    disconnectingAll.value = false
  }
}

watch(autoRefresh, (enabled) => {
  if (enabled) {
    refreshInterval = window.setInterval(refreshSessions, 5000)
  } else if (refreshInterval) {
    clearInterval(refreshInterval)
    refreshInterval = null
  }
})

onMounted(() => {
  refreshSessions()
})

onUnmounted(() => {
  if (refreshInterval) {
    clearInterval(refreshInterval)
  }
})
</script>

<style scoped>
.sessions-page {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.session-id {
  font-family: 'Courier New', monospace;
  font-size: 0.875rem;
  background: rgba(0, 0, 0, 0.1);
  padding: 0.125rem 0.375rem;
  border-radius: 3px;
}

.d-flex {
  display: flex;
}

.justify-space-between {
  justify-content: space-between;
}

.align-center {
  align-items: center;
}

.flex-wrap {
  flex-wrap: wrap;
}

.text-center {
  text-align: center;
}

.py-6 {
  padding-top: 2rem;
  padding-bottom: 2rem;
}

.stat-box {
  text-align: center;
  padding: 1rem;
  background: rgba(var(--va-background-element), 0.5);
  border-radius: 8px;
  height: 100%;
}

.stat-value {
  font-size: 2rem;
  font-weight: 600;
  color: var(--va-primary);
  margin-bottom: 0.5rem;
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
.md3 { flex: 0 0 25%; max-width: 25%; }

@media (max-width: 960px) {
  .md3 {
    flex: 0 0 50%;
    max-width: 50%;
  }
}

@media (max-width: 600px) {
  .md3 {
    flex: 0 0 100%;
    max-width: 100%;
  }
}
</style>
