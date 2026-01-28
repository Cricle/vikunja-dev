<template>
  <v-container fluid>
    <v-row>
      <v-col cols="12">
        <v-card>
          <v-card-title class="d-flex justify-space-between align-center">
            <span>{{ t('pushHistory.title') }}</span>
            <div class="d-flex gap-2">
              <v-btn
                color="primary"
                variant="text"
                :loading="loading"
                @click="loadHistory"
              >
                <v-icon start>mdi-refresh</v-icon>
                {{ t('common.refresh') }}
              </v-btn>
              <v-btn
                color="error"
                variant="text"
                :disabled="records.length === 0"
                @click="confirmClear"
              >
                <v-icon start>mdi-delete</v-icon>
                {{ t('pushHistory.clear') }}
              </v-btn>
            </div>
          </v-card-title>

          <v-card-subtitle v-if="totalCount > 0">
            {{ t('pushHistory.totalCount', { count: totalCount }) }}
          </v-card-subtitle>

          <v-card-text>
            <v-alert
              v-if="records.length === 0 && !loading"
              type="info"
              variant="tonal"
            >
              {{ t('pushHistory.noRecords') }}
            </v-alert>

            <v-timeline
              v-else
              side="end"
              align="start"
              density="compact"
            >
              <v-timeline-item
                v-for="record in records"
                :key="record.id"
                :dot-color="getEventColor(record)"
                size="small"
              >
                <template #opposite>
                  <div class="text-caption">
                    {{ formatTime(record.timestamp) }}
                  </div>
                </template>

                <v-card>
                  <v-card-title class="text-subtitle-1">
                    <v-chip
                      :color="getEventColor(record)"
                      size="small"
                      class="mr-2"
                    >
                      {{ record.eventName }}
                    </v-chip>
                    {{ record.eventData.title }}
                  </v-card-title>

                  <v-card-text>
                    <div class="mb-3">
                      <div class="text-caption text-medium-emphasis mb-1">
                        {{ t('pushHistory.content') }}
                      </div>
                      <div class="text-body-2">{{ record.eventData.body }}</div>
                    </div>

                    <v-divider class="my-3" />

                    <div class="text-caption text-medium-emphasis mb-2">
                      {{ t('pushHistory.providers') }}
                    </div>

                    <v-list density="compact" class="pa-0">
                      <v-list-item
                        v-for="(provider, idx) in record.providers"
                        :key="idx"
                        class="px-0"
                      >
                        <template #prepend>
                          <v-icon
                            :color="provider.success ? 'success' : 'error'"
                            size="small"
                          >
                            {{ provider.success ? 'mdi-check-circle' : 'mdi-alert-circle' }}
                          </v-icon>
                        </template>

                        <v-list-item-title class="text-body-2">
                          {{ provider.providerType }}
                        </v-list-item-title>

                        <v-list-item-subtitle class="text-caption">
                          {{ provider.message || (provider.success ? t('pushHistory.success') : t('pushHistory.failed')) }}
                          · {{ formatTime(provider.timestamp) }}
                        </v-list-item-subtitle>
                      </v-list-item>
                    </v-list>
                  </v-card-text>
                </v-card>
              </v-timeline-item>
            </v-timeline>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>

    <v-dialog v-model="clearDialog" max-width="400">
      <v-card>
        <v-card-title>{{ t('pushHistory.confirmClear') }}</v-card-title>
        <v-card-text>
          {{ t('pushHistory.confirmClearMessage') }}
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="clearDialog = false">
            {{ t('common.cancel') }}
          </v-btn>
          <v-btn color="error" variant="text" @click="clearHistory">
            {{ t('common.confirm') }}
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-snackbar v-model="snackbar" :color="snackbarColor" timeout="3000">
      {{ snackbarText }}
    </v-snackbar>
  </v-container>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { pushHistoryService, type PushEventRecord } from '@/services/vikunjaApi'

const { t } = useI18n()

const loading = ref(false)
const records = ref<PushEventRecord[]>([])
const totalCount = ref(0)
const clearDialog = ref(false)
const snackbar = ref(false)
const snackbarText = ref('')
const snackbarColor = ref('success')

const loadHistory = async () => {
  loading.value = true
  try {
    const response = await pushHistoryService.getHistory(50)
    records.value = response.records
    totalCount.value = response.totalCount
  } catch (error) {
    console.error('Failed to load push history:', error)
    showSnackbar(t('pushHistory.loadError'), 'error')
  } finally {
    loading.value = false
  }
}

const confirmClear = () => {
  clearDialog.value = true
}

const clearHistory = async () => {
  try {
    await pushHistoryService.clearHistory()
    records.value = []
    totalCount.value = 0
    clearDialog.value = false
    showSnackbar(t('pushHistory.cleared'), 'success')
  } catch (error) {
    console.error('Failed to clear history:', error)
    showSnackbar(t('pushHistory.clearError'), 'error')
  }
}

const getEventColor = (record: PushEventRecord): string => {
  const allSuccess = record.providers.every(p => p.success)
  const allFailed = record.providers.every(p => !p.success)
  
  if (allSuccess) return 'success'
  if (allFailed) return 'error'
  return 'warning'
}

const formatTime = (timestamp: string): string => {
  const date = new Date(timestamp)
  const now = new Date()
  const diff = now.getTime() - date.getTime()
  
  // Less than 1 minute
  if (diff < 60000) {
    return t('pushHistory.justNow')
  }
  
  // Less than 1 hour
  if (diff < 3600000) {
    const minutes = Math.floor(diff / 60000)
    return t('pushHistory.minutesAgo', { count: minutes })
  }
  
  // Less than 1 day
  if (diff < 86400000) {
    const hours = Math.floor(diff / 3600000)
    return t('pushHistory.hoursAgo', { count: hours })
  }
  
  // Format as date time
  return date.toLocaleString()
}

const showSnackbar = (text: string, color: string = 'success') => {
  snackbarText.value = text
  snackbarColor.value = color
  snackbar.value = true
}

onMounted(() => {
  loadHistory()
  
  // Auto refresh every 30 seconds
  const interval = setInterval(loadHistory, 30000)
  
  // Cleanup on unmount
  return () => clearInterval(interval)
})
</script>

<style scoped>
.gap-2 {
  gap: 8px;
}
</style>
