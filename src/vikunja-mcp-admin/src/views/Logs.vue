<template>
  <div>
    <VaCard class="mb-4">
      <VaCardContent>
        <div class="row align-end">
          <div class="flex xs12 md4">
            <VaSelect
              v-model="logLevel"
              label="Log Level"
              :options="logLevels"
            />
          </div>

          <div class="flex xs12 md4">
            <VaSwitch
              v-model="autoRefresh"
              label="Auto Refresh (5s)"
            />
          </div>

          <div class="flex xs12 md4">
            <div class="d-flex" style="gap: 0.5rem;">
              <VaButton @click="refreshLogs">
                <VaIcon name="refresh" class="mr-2" />
                Refresh
              </VaButton>
              <VaButton color="danger" @click="clearLogs">
                <VaIcon name="delete" class="mr-2" />
                Clear
              </VaButton>
            </div>
          </div>
        </div>
      </VaCardContent>
    </VaCard>

    <VaCard>
      <VaCardContent class="log-container">
        <div
          v-for="(log, index) in filteredLogs"
          :key="index"
          class="log-entry"
        >
          <span class="log-timestamp">{{ formatTimestamp(log.timestamp) }}</span>
          <VaBadge 
            :text="log.level" 
            :color="getLogColor(log.level)" 
            class="mx-2"
          />
          <span class="log-message">{{ log.message }}</span>
        </div>

        <div v-if="filteredLogs.length === 0" class="text-center py-4">
          <VaIcon name="info" size="large" class="mb-2" />
          <p>No logs available</p>
        </div>
      </VaCardContent>
    </VaCard>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onUnmounted, watch } from 'vue'

interface LogEntry {
  timestamp: string
  level: string
  message: string
}

const logLevel = ref('All')
const logLevels = ['All', 'Debug', 'Info', 'Warning', 'Error']
const autoRefresh = ref(false)
let refreshInterval: number | null = null

const logs = ref<LogEntry[]>([
  {
    timestamp: new Date().toISOString(),
    level: 'Info',
    message: 'VikunjaHook MCP Server started successfully'
  },
  {
    timestamp: new Date().toISOString(),
    level: 'Info',
    message: 'Registered 5 MCP tools with 45 total subcommands'
  },
  {
    timestamp: new Date().toISOString(),
    level: 'Debug',
    message: 'Configuration validated successfully'
  },
  {
    timestamp: new Date().toISOString(),
    level: 'Info',
    message: 'CORS middleware enabled for origins: *'
  },
  {
    timestamp: new Date().toISOString(),
    level: 'Info',
    message: 'Rate limiting configured: 60 requests/minute per token'
  }
])

const filteredLogs = computed(() => {
  if (logLevel.value === 'All') {
    return logs.value
  }
  return logs.value.filter(log => log.level === logLevel.value)
})

function formatTimestamp(timestamp: string) {
  const date = new Date(timestamp)
  return date.toLocaleTimeString('en-US', { hour12: false })
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

function refreshLogs() {
  console.log('Refreshing logs...')
}

function clearLogs() {
  if (confirm('Are you sure you want to clear all logs?')) {
    logs.value = []
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

onUnmounted(() => {
  if (refreshInterval) {
    clearInterval(refreshInterval)
  }
})
</script>

<style scoped>
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

.align-end {
  align-items: flex-end;
}

.d-flex {
  display: flex;
}

@media (max-width: 960px) {
  .md4 {
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
  padding: 0.25rem 0;
  border-bottom: 1px solid rgba(255, 255, 255, 0.1);
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.log-entry:last-child {
  border-bottom: none;
}

.log-timestamp {
  color: #858585;
  font-size: 0.75rem;
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

.py-4 {
  padding-top: 1rem;
  padding-bottom: 1rem;
}
</style>
