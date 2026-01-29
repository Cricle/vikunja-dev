<template>
  <div class="push-history-container">
    <va-inner-loading :loading="loading">
      <!-- Header Section -->
      <div class="history-header">
        <h1>{{ t('pushHistory.title') }}</h1>
        <div class="header-actions">
          <va-button
            color="primary"
            :loading="loading"
            @click="loadHistory"
          >
            <va-icon name="refresh" class="mr-2" />
            {{ t('common.refresh') }}
          </va-button>
          <va-button
            color="danger"
            :disabled="records.length === 0"
            @click="confirmClear"
          >
            <va-icon name="delete" class="mr-2" />
            {{ t('pushHistory.clear') }}
          </va-button>
        </div>
      </div>

      <p v-if="totalCount > 0" class="total-count">
        {{ t('pushHistory.totalCount', { count: totalCount }) }}
      </p>

      <va-alert
        v-if="records.length === 0 && !loading"
        color="info"
        class="no-records-alert"
      >
        {{ t('pushHistory.noRecords') }}
      </va-alert>

      <!-- Timeline -->
      <div v-else class="timeline-container">
        <div
          v-for="record in records"
          :key="record.id"
          class="timeline-item"
        >
          <div class="timeline-marker">
            <va-badge
              :color="getEventColor(record)"
              class="timeline-badge"
            />
            <div class="timeline-line" />
          </div>

          <div class="timeline-content">
            <va-card class="event-card">
              <va-card-title>
                <div class="event-title">
                  <va-chip
                    :color="getEventColor(record)"
                    size="small"
                    class="event-chip"
                  >
                    {{ getEventDisplayName(record.eventName) }}
                  </va-chip>
                  <span class="event-name">{{ record.eventData.title }}</span>
                </div>
                <span class="event-time">{{ formatTime(record.timestamp) }}</span>
              </va-card-title>

              <va-card-content>
                <div class="event-body">
                  <div class="content-section">
                    <div class="section-label">{{ t('pushHistory.content') }}</div>
                    <div class="section-value">{{ record.eventData.body }}</div>
                  </div>

                  <va-divider class="my-3" />

                  <div class="providers-section">
                    <div class="section-label">{{ t('pushHistory.providers') }}</div>
                    <div class="providers-list">
                      <div
                        v-for="(provider, idx) in record.providers"
                        :key="idx"
                        class="provider-item"
                      >
                        <va-icon
                          :name="provider.success ? 'check_circle' : 'error'"
                          :color="provider.success ? 'success' : 'danger'"
                          size="small"
                        />
                        <div class="provider-info">
                          <div class="provider-name">{{ provider.providerType }}</div>
                          <div class="provider-details">
                            {{ provider.message || (provider.success ? t('pushHistory.success') : t('pushHistory.failed')) }}
                            · {{ formatTime(provider.timestamp) }}
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </va-card-content>
            </va-card>
          </div>
        </div>
      </div>
    </va-inner-loading>

    <!-- Clear Confirmation Modal -->
    <va-modal
      v-model="clearDialog"
      size="small"
      :message="t('pushHistory.confirmClearMessage')"
      :title="t('pushHistory.confirmClear')"
      :ok-text="t('common.confirm')"
      :cancel-text="t('common.cancel')"
      @ok="clearHistory"
    />

    <!-- Snackbar -->
    <div v-if="snackbar" class="snackbar-container">
      <va-alert
        :color="snackbarColor"
        class="snackbar"
        closeable
        @close="snackbar = false"
      >
        {{ snackbarText }}
      </va-alert>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed } from 'vue'
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
let refreshInterval: number | undefined

// Event name mapping for display
const eventNameMap: Record<string, string> = {
  'task.created': 'taskCreated',
  'task.updated': 'taskUpdated',
  'task.deleted': 'taskDeleted',
  'project.created': 'projectCreated',
  'project.updated': 'projectUpdated',
  'project.deleted': 'projectDeleted',
  'task.assignee.created': 'taskAssigneeCreated',
  'task.assignee.deleted': 'taskAssigneeDeleted',
  'task.comment.created': 'taskCommentCreated',
  'task.comment.updated': 'taskCommentUpdated',
  'task.comment.deleted': 'taskCommentDeleted',
  'task.attachment.created': 'taskAttachmentCreated',
  'task.attachment.deleted': 'taskAttachmentDeleted',
  'task.relation.created': 'taskRelationCreated',
  'task.relation.deleted': 'taskRelationDeleted',
  'label.created': 'labelCreated',
  'label.updated': 'labelUpdated',
  'label.deleted': 'labelDeleted',
  'task.label.created': 'taskLabelCreated',
  'task.label.deleted': 'taskLabelDeleted',
  'user.created': 'userCreated',
  'team.created': 'teamCreated',
  'team.updated': 'teamUpdated',
  'team.deleted': 'teamDeleted',
  'team.member.added': 'teamMemberAdded',
  'team.member.removed': 'teamMemberRemoved'
}

const getEventDisplayName = (eventName: string): string => {
  const key = eventNameMap[eventName]
  return key ? t(`events.${key}`) : eventName
}

const loadHistory = async () => {
  loading.value = true
  try {
    const response = await pushHistoryService.getHistory(50)
    records.value = response.records
    totalCount.value = response.totalCount
  } catch (error) {
    console.error('Failed to load push history:', error)
    showSnackbar(t('pushHistory.loadError'), 'danger')
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
    showSnackbar(t('pushHistory.clearError'), 'danger')
  }
}

const getEventColor = (record: PushEventRecord): string => {
  const allSuccess = record.providers.every(p => p.success)
  const allFailed = record.providers.every(p => !p.success)
  
  if (allSuccess) return 'success'
  if (allFailed) return 'danger'
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
  
  setTimeout(() => {
    snackbar.value = false
  }, 3000)
}

onMounted(() => {
  loadHistory()
  
  // Auto refresh every 30 seconds
  refreshInterval = window.setInterval(loadHistory, 30000)
})

onUnmounted(() => {
  if (refreshInterval) {
    clearInterval(refreshInterval)
  }
})
</script>

<style scoped>
.push-history-container {
  padding: 1.5rem;
  max-width: 1200px;
  margin: 0 auto;
}

.history-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
  flex-wrap: wrap;
  gap: 1rem;
}

.history-header h1 {
  margin: 0;
  font-size: 2rem;
  font-weight: 600;
}

.header-actions {
  display: flex;
  gap: 0.5rem;
}

.total-count {
  color: var(--va-text-secondary);
  margin-bottom: 1.5rem;
}

.no-records-alert {
  margin-top: 2rem;
}

.timeline-container {
  margin-top: 2rem;
}

.timeline-item {
  display: flex;
  gap: 1.5rem;
  margin-bottom: 2.5rem;
  position: relative;
}

.timeline-marker {
  display: flex;
  flex-direction: column;
  align-items: center;
  flex-shrink: 0;
}

.timeline-badge {
  width: 14px;
  height: 14px;
  border-radius: 50%;
  z-index: 1;
}

.timeline-line {
  width: 2px;
  flex: 1;
  background: var(--va-background-border);
  margin-top: 0.75rem;
}

.timeline-item:last-child .timeline-line {
  display: none;
}

.timeline-content {
  flex: 1;
  min-width: 0;
}

.event-card {
  width: 100%;
}

.event-title {
  display: flex;
  align-items: center;
  gap: 1rem;
  flex: 1;
  min-width: 0;
  flex-wrap: wrap;
}

.event-chip {
  flex-shrink: 0;
}

.event-name {
  font-size: 1.0625rem;
  font-weight: 500;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.event-time {
  font-size: 0.875rem;
  color: var(--va-text-secondary);
  white-space: nowrap;
  margin-left: auto;
}

.event-body {
  padding-top: 0.75rem;
}

.content-section {
  margin-bottom: 1.25rem;
}

.section-label {
  font-size: 0.8125rem;
  color: var(--va-text-secondary);
  text-transform: uppercase;
  letter-spacing: 0.5px;
  margin-bottom: 0.75rem;
  font-weight: 600;
}

.section-value {
  font-size: 0.9375rem;
  line-height: 1.6;
}

.providers-section {
  margin-top: 1.25rem;
}

.providers-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  margin-top: 1rem;
}

.provider-item {
  display: flex;
  align-items: flex-start;
  gap: 1rem;
  padding: 0.75rem;
  border-radius: 0.5rem;
  background: var(--va-background-element);
}

.provider-info {
  flex: 1;
  min-width: 0;
}

.provider-name {
  font-size: 0.9375rem;
  font-weight: 500;
  margin-bottom: 0.375rem;
}

.provider-details {
  font-size: 0.8125rem;
  color: var(--va-text-secondary);
  line-height: 1.5;
}

.snackbar-container {
  position: fixed;
  bottom: 2rem;
  right: 2rem;
  z-index: 9999;
  max-width: 400px;
}

.snackbar {
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
}

.mr-2 {
  margin-right: 0.5rem;
}

.my-3 {
  margin-top: 1rem;
  margin-bottom: 1rem;
}

/* Responsive Design */
@media (max-width: 768px) {
  .push-history-container {
    padding: 1rem;
  }

  .history-header {
    flex-direction: column;
    align-items: flex-start;
  }

  .history-header h1 {
    font-size: 1.5rem;
  }

  .header-actions {
    width: 100%;
  }

  .header-actions button {
    flex: 1;
  }

  .timeline-item {
    gap: 1rem;
  }

  .event-title {
    flex-direction: column;
    align-items: flex-start;
    gap: 0.5rem;
  }

  .event-time {
    margin-left: 0;
    font-size: 0.75rem;
  }

  .snackbar-container {
    left: 1rem;
    right: 1rem;
    bottom: 1rem;
  }
}
</style>
