<template>
  <div class="dashboard">
    <VaCard class="mb-4">
      <VaCardTitle>Server Status</VaCardTitle>
      <VaCardContent>
        <VaInnerLoading :loading="serverStore.loading">
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

    <div class="row">
      <div class="flex xs12 md4">
        <VaCard class="stat-card">
          <VaCardContent class="text-center">
            <div class="stat-value">{{ serverStore.tools.length }}</div>
            <div class="text-secondary">Registered Tools</div>
          </VaCardContent>
        </VaCard>
      </div>
      <div class="flex xs12 md4">
        <VaCard class="stat-card">
          <VaCardContent class="text-center">
            <div class="stat-value">{{ totalSubcommands }}</div>
            <div class="text-secondary">Total Subcommands</div>
          </VaCardContent>
        </VaCard>
      </div>
      <div class="flex xs12 md4">
        <VaCard class="stat-card">
          <VaCardContent class="text-center">
            <div class="stat-value">60/min</div>
            <div class="text-secondary">Rate Limit</div>
          </VaCardContent>
        </VaCard>
      </div>
    </div>

    <VaCard class="mt-4">
      <VaCardTitle>Quick Actions</VaCardTitle>
      <VaCardContent>
        <div class="actions">
          <VaButton @click="refreshData">
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
        </div>
      </VaCardContent>
    </VaCard>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useServerStore } from '../stores/server'

const serverStore = useServerStore()

const totalSubcommands = computed(() => {
  return serverStore.tools.reduce((sum, tool) => sum + tool.subcommands.length, 0)
})

function formatDate(dateString: string) {
  return new Date(dateString).toLocaleString()
}

async function refreshData() {
  await Promise.all([
    serverStore.checkHealth(),
    serverStore.fetchInfo(),
    serverStore.fetchTools()
  ])
}

onMounted(() => {
  refreshData()
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

@media (max-width: 768px) {
  .md4, .md6, .lg3 {
    flex: 0 0 100%;
    max-width: 100%;
  }
}
</style>
