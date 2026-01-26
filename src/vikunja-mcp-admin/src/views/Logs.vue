<template>
  <div class="logs-page">
    <!-- Controls Card -->
    <VaCard class="mb-4">
      <VaCardContent>
        <div class="row align-end">
          <div class="flex xs12 md3">
            <VaSelect
              v-model="logLevel"
              label="Log Level"
              :options="logLevels"
              @update:model-value="refreshLogs"
            />
          </div>

          <div class="flex xs12 md3">
            <VaSelect
              v-model="logCount"
              label="Number of Logs"
              :options="logCountOptions"
              @update:model-value="refreshLogs"
            />
          </div>

          <div class="flex xs12 md3">
            <VaSwitch
              v-model="autoRefresh"
              label="Auto Refresh (5s)"
              class="mt-4"
            />
          </div>

          <div class="flex xs12 md3">
            <div class="d-flex" style="gap: 0.5rem;">
              <VaButton 
                @click="refreshLogs"
                :loading="loading"
              >
                <VaIcon name="mdi-refresh" class="mr-2" />
                Refresh
              </VaButton>
              <VaButton 
                color="danger" 
                @click="clearLogs"
                :loading="clearing"
              >
                <VaIcon name="mdi-delete" class="mr-2" />
                Clear
              </VaButton>
            </div>
          </div>
        </div>
      </VaCardContent>
    </VaCard>

    <!-- Logs Display Card -->
    <VaCard>
      <VaCardTitle>
        <div class="d-flex justify-space-between align-center">
          <span>Server Logs</span>
          <VaBadge 
            :text="`${filteredLogs.length} entries`" 
            color="info" 
          />
        </div>
      </VaCardTitle>
      <VaCardContent class="log-container">
        <VaInnerLoading :loading="loading">
          <div v-if="filteredLogs.length > 0">
            <div
              v-for="(log, index) in filteredLogs"
              :key="index"
              class="log-entry"
              :class="`log-level-${log.level.toLowerCase()}`"
            >
              <span class="log-timestamp">{{ formatTimestamp(log.timestamp) }}</span>
              <VaBadge 
                :text="log.level" 
                :color="getLogColor(log.level)" 
                class="mx-2 log-badge"
              />
              <span class="log-message">{{ log.message }}</span>
            </div>
          </div>

          <div v-else class="text-center py-6">
            <VaIcon name="mdi-information" size="large" class="mb-3" color="secondary" />
            <h3 class="va-h3 mb-2">No Logs Available</h3>
            <p class="text-secondary">
              {{ logLevel === 'All' ? 'No logs have been recorded yet.' : `No ${logLevel} logs found.` }}
            </p>
          </div>
        </VaInnerLoading>
      </VaCardContent>
    </VaCard>

    <!-- Log Statistics -->
    <VaCard class="mt-4" v-if="logs.length > 0">
      <VaCardTitle>Log Statistics</VaCardTitle>
      <VaCardContent>
        <div class="row">
          <div class="flex xs12 md3">
            <div class="stat-box">
              <VaIcon name="mdi-text-box" size="large" color="primary" class="mb-2" />
              <div class="stat-value">{{ logs.length }}</div>
              <div class="text-secondary">Total Logs</div>
            </div>
          </div>
          <div class="flex xs12 md3">
            <div class="stat-box">
              <VaIcon name="mdi-alert-circle" size="large" color="danger" class="mb-2" />
              <div class="stat-value">{{ countByLevel('Error') }}</div>
              <div class="text-secondary">Errors</div>
            </div>
          </div>
          <div class="flex xs12 md3">
            <div class="stat-box">
              <VaIcon name="mdi-alert" size="large" color="warning" class="mb-2" />
              <div class="stat-value">{{ countByLevel('Warning') }}</div>
              <div class="text-secondary">Warnings</div>
            </div>
          </div>
          <div class="flex xs12 md3">
            <div class="stat-box">
              <VaIcon name="mdi-information" size="large" color="info" class="mb-2" />
              <div class="stat-value">{{ countByLevel('Info') }}</div>
              <div class="text-secondary">Info</div>
            </div>
          </div>
        </div>
      </VaCardContent>
    </VaCard>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onUnmounted, watch, onMounted } from 'vue'
import { adminApi } from '../services/api'
import type { LogEntry } from '../types'
import { useToast } from 'vuestic-ui'

const { init: notify } = useToast()

const logLevel = ref('All')
const logLevels = ['All', 'Debug', 'Info', 'Warning', 'Error']
const logCount = ref(100)
const logCountOptions = [50, 100, 200, 500]
const autoRefresh = ref(false)
const loading = ref(false)
const clearing = ref(false)
let refreshInterval: number | null = null

const logs = ref<LogEntry[]>([])

const filteredLogs = computed(() => {
  if (logLevel.value === 'All') {
    return logs.value
  }
  return logs.value.filter(log => log.level === logLevel.value)
})

function formatTimestamp(timestamp: string) {
  const date = new Date(timestamp)
  return date.toLocaleTimeString('en-US', { 
    hour12: false,
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit'
  })
}

function getLogColor(level: string) {
  const colors: Record<string, string> = {
    Debug: 'info',
    Info: 'success',
    Warning: 'warning',
    Error: 'danger'
  }
  return colors[level] || 'secondary'
}

function countByLevel(level: string): number {
  return logs.value.filter(log => log.level === level).length
}

async function refreshLogs() {
  loading.value = true
  try {
    logs.value = await adminApi.getLogs(logCount.value, logLevel.value)
    notify({
      message: 'Logs refreshed successfully',
      color: 'success'
    })
  } catch (error) {
    notify({
      message: 'Failed to fetch logs',
      color: 'danger'
    })
    console.error('Failed to fetch logs:', error)
  } finally {
    loading.value = false
  }
}

async function clearLogs() {
  if (!confirm('Are you sure you want to clear all logs? This action cannot be undone.')) {
    return
  }

  clearing.value = true
  try {
    await adminApi.clearLogs()
    logs.value = []
    notify({
      message: 'Logs cleared successfully',
      color: 'success'
    })
  } catch (error) {
    notify({
      message: 'Failed to clear logs',
      color: 'danger'
    })
    console.error('Failed to clear logs:', error)
  } finally {
    clearing.value = false
  }
}

watch(autoRefresh, (enabled) => {
  if (enabled) {
    refreshInterval = window.setInterval(refreshLogs, 5000)
  } else if (refreshInterval) {
    clearInterval(refreshInterval)
    refreshInterval = null
  }
})

onMounted(() => {
  refreshLogs()
})

onUnmounted(() => {
  if (refreshInterval) {
    clearInterval(refreshInterval)
  }
})
</script>

<style scoped>
.logs-page {
  display: flex;
  flex-direction: column;
  gap: 1rem;
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

.align-end {
  align-items: flex-end;
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

.log-container {
  font-family: 'Courier New', monospace;
  font-size: 0.875rem;
  max-height: 600px;
  overflow-y: auto;
  background: #1e1e1e;
  color: #d4d4d4;
  padding: 1rem;
  border-radius: 4px;
}

.log-entry {
  padding: 0.5rem;
  border-bottom: 1px solid rgba(255, 255, 255, 0.1);
  display: flex;
  align-items: center;
  gap: 0.5rem;
  transition: background-color 0.2s;
}

.log-entry:hover {
  background: rgba(255, 255, 255, 0.05);
}

.log-entry:last-child {
  border-bottom: none;
}

.log-entry.log-level-error {
  border-left: 3px solid #e34234;
}

.log-entry.log-level-warning {
  border-left: 3px solid #ffc107;
}

.log-entry.log-level-info {
  border-left: 3px solid #2c82e0;
}

.log-entry.log-level-debug {
  border-left: 3px solid #6c757d;
}

.log-timestamp {
  color: #858585;
  font-size: 0.75rem;
  flex-shrink: 0;
  min-width: 80px;
}

.log-badge {
  flex-shrink: 0;
}

.log-message {
  color: #d4d4d4;
  flex: 1;
  word-break: break-word;
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
</style>
