<template>
  <div>
    <VaCard>
      <VaCardContent>
        <VaDataTable
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

          <template #cell(createdAt)="{ value }">
            {{ formatDate(value) }}
          </template>

          <template #cell(actions)="{ row }">
            <VaButton
              size="small"
              color="danger"
              @click="disconnectSession(row.sessionId)"
            >
              Disconnect
            </VaButton>
          </template>
        </VaDataTable>

        <div v-if="sessions.length === 0" class="text-center py-4">
          <VaIcon name="info" size="large" class="mb-2" />
          <p>No active sessions</p>
        </div>
      </VaCardContent>
    </VaCard>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import type { AuthSession } from '../types'

const columns = [
  { key: 'sessionId', label: 'Session ID', sortable: true },
  { key: 'apiUrl', label: 'API URL', sortable: true },
  { key: 'authType', label: 'Auth Type', sortable: true },
  { key: 'createdAt', label: 'Created At', sortable: true },
  { key: 'actions', label: 'Actions' }
]

const sessions = ref<AuthSession[]>([
  {
    sessionId: '123e4567-e89b-12d3-a456-426614174000',
    apiUrl: 'https://vikunja.example.com/api/v1',
    authType: 'Jwt',
    createdAt: new Date().toISOString()
  }
])

function formatDate(dateString: string) {
  return new Date(dateString).toLocaleString()
}

function disconnectSession(sessionId: string) {
  if (confirm('Are you sure you want to disconnect this session?')) {
    sessions.value = sessions.value.filter(s => s.sessionId !== sessionId)
  }
}
</script>

<style scoped>
.session-id {
  font-family: 'Courier New', monospace;
  font-size: 0.875rem;
  background: rgba(0, 0, 0, 0.1);
  padding: 0.125rem 0.375rem;
  border-radius: 3px;
}

.text-center {
  text-align: center;
}

.py-4 {
  padding-top: 1rem;
  padding-bottom: 1rem;
}
</style>
