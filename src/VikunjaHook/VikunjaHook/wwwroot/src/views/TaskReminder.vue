<template>
  <div class="reminder-container">
    <div class="reminder-header">
      <div class="header-content">
        <div class="header-text">
          <h1 class="reminder-title">{{ t('reminder.title') }}</h1>
          <p class="reminder-subtitle">{{ t('reminder.description') }}</p>
        </div>
      </div>
    </div>

    <va-card class="reminder-card">
      <va-card-content>
        <div class="form-section">
          <va-switch
            v-model="reminderConfig.enabled"
            :label="t('reminder.enabled')"
            @update:modelValue="saveConfig"
          />
          <p class="field-hint">{{ t('reminder.enabledHint') }}</p>
        </div>

        <div v-if="reminderConfig.enabled" class="form-section">
          <va-input
            v-model.number="reminderConfig.scanIntervalSeconds"
            type="number"
            :label="t('reminder.scanInterval')"
            :min="5"
            :max="300"
            @update:modelValue="saveConfig"
          >
            <template #prepend>
              <va-icon name="schedule" />
            </template>
            <template #append>
              <span class="input-suffix">{{ t('reminder.seconds') }}</span>
            </template>
          </va-input>
          <p class="field-hint">{{ t('reminder.scanIntervalHint') }}</p>
        </div>

        <div v-if="reminderConfig.enabled" class="form-section">
          <label class="editor-label">{{ t('reminder.providers') }}</label>
          <p class="field-hint">{{ t('reminder.providersHint') }}</p>
          <va-select
            v-model="reminderConfig.providers"
            :options="providerOptions"
            multiple
            :placeholder="t('reminder.useDefaultProviders')"
            :no-options-text="t('common.noOptions')"
            clearable
            @update:modelValue="saveConfig"
          />
        </div>

        <!-- Label Filter Section -->
        <div v-if="reminderConfig.enabled" class="form-section">
          <va-switch
            v-model="reminderConfig.enableLabelFilter"
            :label="t('reminder.enableLabelFilter')"
            @update:modelValue="saveConfig"
          />
          <p class="field-hint">{{ t('reminder.enableLabelFilterHint') }}</p>
        </div>

        <div v-if="reminderConfig.enabled && reminderConfig.enableLabelFilter" class="form-section">
          <label class="editor-label">{{ t('reminder.filterLabels') }}</label>
          <p class="field-hint">{{ t('reminder.filterLabelsHint') }}</p>
          <va-select
            v-model="reminderConfig.filterLabelIds"
            :options="labelOptions"
            multiple
            :placeholder="t('reminder.selectLabels')"
            :no-options-text="t('common.noOptions')"
            clearable
            text-by="text"
            value-by="value"
            @update:modelValue="saveConfig"
          />
        </div>

        <!-- Templates Section -->
        <div v-if="reminderConfig.enabled" class="templates-section">
          <div class="section-header">
            <h3 class="section-title">{{ t('reminder.templates') }}</h3>
            <va-button
              color="primary"
              @click="saveConfig"
              :disabled="saving"
            >
              <va-icon name="save" size="small" />
              {{ saving ? t('common.saving') : t('common.save') }}
            </va-button>
          </div>
          
          <!-- Start Date Template -->
          <div class="template-group">
            <h4 class="template-title">
              <va-icon name="play_arrow" size="small" />
              {{ t('reminder.startDateTemplate') }}
            </h4>
            <va-input
              v-model="reminderConfig.startDateTemplate.titleTemplate"
              :label="t('reminder.titleTemplate')"
            >
              <template #prepend>
                <va-icon name="title" size="small" />
              </template>
            </va-input>
            <div class="markdown-editor-wrapper">
              <label class="editor-label">{{ t('reminder.bodyTemplate') }}</label>
              <textarea ref="startBodyEditor" class="markdown-editor"></textarea>
            </div>
          </div>

          <!-- Due Date Template -->
          <div class="template-group">
            <h4 class="template-title">
              <va-icon name="event" size="small" />
              {{ t('reminder.dueDateTemplate') }}
            </h4>
            <va-input
              v-model="reminderConfig.dueDateTemplate.titleTemplate"
              :label="t('reminder.titleTemplate')"
            >
              <template #prepend>
                <va-icon name="title" size="small" />
              </template>
            </va-input>
            <div class="markdown-editor-wrapper">
              <label class="editor-label">{{ t('reminder.bodyTemplate') }}</label>
              <textarea ref="dueBodyEditor" class="markdown-editor"></textarea>
            </div>
          </div>

          <!-- Reminder Time Template -->
          <div class="template-group">
            <h4 class="template-title">
              <va-icon name="notifications" size="small" />
              {{ t('reminder.reminderTimeTemplate') }}
            </h4>
            <va-input
              v-model="reminderConfig.reminderTimeTemplate.titleTemplate"
              :label="t('reminder.titleTemplate')"
            >
              <template #prepend>
                <va-icon name="title" size="small" />
              </template>
            </va-input>
            <div class="markdown-editor-wrapper">
              <label class="editor-label">{{ t('reminder.bodyTemplate') }}</label>
              <textarea ref="reminderBodyEditor" class="markdown-editor"></textarea>
            </div>
          </div>
        </div>

        <div v-if="reminderConfig.enabled" class="placeholders-section">
          <h3>{{ t('reminder.availablePlaceholders') }}</h3>
          <div class="placeholder-grid">
            <va-chip
              v-for="placeholder in availablePlaceholders"
              :key="placeholder"
              @click="copyPlaceholder(placeholder)"
              class="placeholder-chip"
              color="info"
              outline
            >
              <code>{{ formatPlaceholder(placeholder) }}</code>
              <va-icon name="content_copy" size="small" />
            </va-chip>
          </div>
        </div>
      </va-card-content>
    </va-card>

    <!-- Reminder History -->
    <va-card v-if="reminderConfig.enabled" class="history-card">
      <va-card-title>
        <div class="card-title-content">
          <va-icon name="history" />
          <span>{{ t('reminder.history') }}</span>
          <va-spacer />
          <va-button
            size="small"
            preset="secondary"
            icon="refresh"
            @click="loadHistory"
          >
            {{ t('common.refresh') }}
          </va-button>
        </div>
      </va-card-title>
      <va-card-content>
        <div v-if="historyRecords.length === 0" class="empty-state">
          <va-icon name="notifications_none" size="3rem" color="secondary" />
          <p>{{ t('reminder.noHistory') }}</p>
        </div>
        <div v-else class="history-list">
          <div
            v-for="record in historyRecords"
            :key="record.id"
            class="history-item"
            :class="{ 'history-item-failed': !record.success }"
          >
            <div class="history-item-header">
              <div class="history-item-info">
                <va-icon
                  :name="record.success ? 'check_circle' : 'error'"
                  :color="record.success ? 'success' : 'danger'"
                  size="small"
                />
                <span class="history-item-task">{{ record.taskTitle }}</span>
                <va-chip size="small" :color="getReminderTypeColor(record.reminderType)">
                  {{ getReminderTypeLabel(record.reminderType) }}
                </va-chip>
              </div>
              <span class="history-item-time">{{ formatTime(record.timestamp) }}</span>
            </div>
            <div class="history-item-content">
              <div class="history-item-title">{{ record.title }}</div>
              <div class="history-item-body">{{ record.body }}</div>
              <div class="history-item-meta">
                <span>{{ t('reminder.project') }}: {{ record.projectTitle }}</span>
                <span>{{ t('reminder.providers') }}: {{ record.providers.join(', ') }}</span>
                <span v-if="!record.success && record.errorMessage" class="error-message">
                  {{ t('common.error') }}: {{ record.errorMessage }}
                </span>
              </div>
            </div>
          </div>
        </div>
      </va-card-content>
    </va-card>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, nextTick } from 'vue'
import { useI18n } from 'vue-i18n'
import { useConfigStore } from '@/stores/configStore'
import type { TaskReminderConfig } from '@/types/config'
import { NotificationFormat } from '@/types/config'
import { useToast } from 'vuestic-ui'
import axios from 'axios'
import EasyMDE from 'easymde'
import 'easymde/dist/easymde.min.css'

const { t } = useI18n()
const { init: notify } = useToast()
const configStore = useConfigStore()

const providers = computed(() => configStore.providers)

const reminderConfig = ref<TaskReminderConfig>({
  enabled: false,
  scanIntervalSeconds: 10,
  format: NotificationFormat.Text,
  providers: [],
  enableLabelFilter: false,
  filterLabelIds: [],
  startDateTemplate: {
    titleTemplate: '🚀 Task Starting: {{task.title}}',
    bodyTemplate: 'Task: {{task.title}}\nProject: {{project.title}}\nStart Time: {{task.startDate}}'
  },
  dueDateTemplate: {
    titleTemplate: '⏰ Task Due Soon: {{task.title}}',
    bodyTemplate: 'Task: {{task.title}}\nProject: {{project.title}}\nDue Time: {{task.dueDate}}'
  },
  reminderTimeTemplate: {
    titleTemplate: '🔔 Task Reminder: {{task.title}}',
    bodyTemplate: 'Task: {{task.title}}\nProject: {{project.title}}\nReminder: {{task.reminders}}'
  }
})

const historyRecords = ref<any[]>([])
const allLabels = ref<any[]>([])
const saving = ref(false)

// Markdown editors
const startBodyEditor = ref<HTMLTextAreaElement>()
const dueBodyEditor = ref<HTMLTextAreaElement>()
const reminderBodyEditor = ref<HTMLTextAreaElement>()

let startMDE: EasyMDE | null = null
let dueMDE: EasyMDE | null = null
let reminderMDE: EasyMDE | null = null

const providerOptions = computed(() => {
  return providers.value.map(p => p.providerType)
})

const labelOptions = computed(() => {
  return allLabels.value.map(label => ({
    value: label.id,
    text: label.title
  }))
})

const availablePlaceholders = [
  'task.id',
  'task.title',
  'task.description',
  'task.done',
  'task.priority',
  'task.dueDate',
  'task.startDate',
  'task.endDate',
  'task.reminders',
  'project.id',
  'project.title',
  'project.description',
  'reminder.type',
  'event.url'
]

function initMarkdownEditors() {
  if (startBodyEditor.value && !startMDE) {
    startMDE = new EasyMDE({
      element: startBodyEditor.value,
      spellChecker: false,
      placeholder: t('reminder.bodyPlaceholderStart'),
      toolbar: ['bold', 'italic', 'heading', '|', 'quote', 'unordered-list', 'ordered-list', '|', 'link', '|', 'preview', 'guide'],
      status: false,
      autosave: { enabled: false },
      minHeight: '150px'
    })
    startMDE.codemirror.on('change', () => {
      reminderConfig.value.startDateTemplate.bodyTemplate = startMDE?.value() || ''
    })
  }
  
  if (dueBodyEditor.value && !dueMDE) {
    dueMDE = new EasyMDE({
      element: dueBodyEditor.value,
      spellChecker: false,
      placeholder: t('reminder.bodyPlaceholderDue'),
      toolbar: ['bold', 'italic', 'heading', '|', 'quote', 'unordered-list', 'ordered-list', '|', 'link', '|', 'preview', 'guide'],
      status: false,
      autosave: { enabled: false },
      minHeight: '150px'
    })
    dueMDE.codemirror.on('change', () => {
      reminderConfig.value.dueDateTemplate.bodyTemplate = dueMDE?.value() || ''
    })
  }
  
  if (reminderBodyEditor.value && !reminderMDE) {
    reminderMDE = new EasyMDE({
      element: reminderBodyEditor.value,
      spellChecker: false,
      placeholder: t('reminder.bodyPlaceholderReminder'),
      toolbar: ['bold', 'italic', 'heading', '|', 'quote', 'unordered-list', 'ordered-list', '|', 'link', '|', 'preview', 'guide'],
      status: false,
      autosave: { enabled: false },
      minHeight: '150px'
    })
    reminderMDE.codemirror.on('change', () => {
      reminderConfig.value.reminderTimeTemplate.bodyTemplate = reminderMDE?.value() || ''
    })
  }
}

function destroyMarkdownEditors() {
  if (startMDE) {
    startMDE.toTextArea()
    startMDE = null
  }
  if (dueMDE) {
    dueMDE.toTextArea()
    dueMDE = null
  }
  if (reminderMDE) {
    reminderMDE.toTextArea()
    reminderMDE = null
  }
}

function formatPlaceholder(placeholder: string): string {
  return `{{${placeholder}}}`
}

function copyPlaceholder(placeholder: string) {
  const text = `{{${placeholder}}}`
  navigator.clipboard.writeText(text)
  notify({
    message: t('templates.placeholderCopied'),
    color: 'info',
    duration: 1500
  })
}

async function saveConfig() {
  if (saving.value) return
  
  try {
    saving.value = true
    
    if (!configStore.config) {
      notify({
        message: t('templates.errorNoConfig'),
        color: 'danger'
      })
      return
    }
    
    // Get latest values from markdown editors
    if (startMDE) {
      reminderConfig.value.startDateTemplate.bodyTemplate = startMDE.value()
    }
    if (dueMDE) {
      reminderConfig.value.dueDateTemplate.bodyTemplate = dueMDE.value()
    }
    if (reminderMDE) {
      reminderConfig.value.reminderTimeTemplate.bodyTemplate = reminderMDE.value()
    }
    
    configStore.config.reminderConfig = { ...reminderConfig.value }
    await configStore.saveConfig()
    
    notify({
      message: t('reminder.saved'),
      color: 'success',
      duration: 2000
    })
  } catch (error: unknown) {
    console.error('Failed to save reminder config:', error)
    notify({
      message: t('common.error') + ': ' + (error instanceof Error ? error.message : 'Unknown error'),
      color: 'danger'
    })
  } finally {
    saving.value = false
  }
}

async function loadLabels() {
  try {
    const response = await axios.get('/api/mcp/labels')
    allLabels.value = response.data || []
  } catch (error) {
    console.error('Failed to load labels:', error)
    allLabels.value = []
  }
}

async function loadHistory() {
  try {
    const response = await axios.get('/api/reminder-history?count=50')
    historyRecords.value = response.data.records
  } catch (error) {
    console.error('Failed to load reminder history:', error)
  }
}

async function addTestData() {
  try {
    await axios.post('/api/reminder-history/test')
    await loadHistory()
  } catch (error) {
    console.error('Failed to add test data:', error)
  }
}

function getReminderTypeColor(type: string): string {
  switch (type) {
    case 'due':
      return 'danger'
    case 'start':
      return 'success'
    case 'reminder':
      return 'warning'
    default:
      return 'info'
  }
}

function getReminderTypeLabel(type: string): string {
  return t(`reminder.type.${type}`)
}

function formatTime(timestamp: string): string {
  const date = new Date(timestamp)
  const now = new Date()
  const diff = now.getTime() - date.getTime()
  const minutes = Math.floor(diff / 60000)
  const hours = Math.floor(diff / 3600000)
  
  if (minutes < 1) {
    return t('pushHistory.justNow')
  } else if (minutes < 60) {
    return t('pushHistory.minutesAgo', { count: minutes })
  } else if (hours < 24) {
    return t('pushHistory.hoursAgo', { count: hours })
  } else {
    return date.toLocaleString()
  }
}

onMounted(async () => {
  try {
    await configStore.loadConfig('default')
    
    if (configStore.config?.reminderConfig) {
      reminderConfig.value = { ...configStore.config.reminderConfig }
    }
    
    // Load labels
    await loadLabels()
    
    // Initialize markdown editors
    await nextTick()
    initMarkdownEditors()
    
    // Set initial values
    if (startMDE) startMDE.value(reminderConfig.value.startDateTemplate.bodyTemplate)
    if (dueMDE) dueMDE.value(reminderConfig.value.dueDateTemplate.bodyTemplate)
    if (reminderMDE) reminderMDE.value(reminderConfig.value.reminderTimeTemplate.bodyTemplate)
    
    // Load history if enabled
    if (reminderConfig.value.enabled) {
      await loadHistory()
      
      // Auto-refresh every 10 seconds
      setInterval(loadHistory, 10000)
    }
  } catch (error) {
    console.error('Failed to load config:', error)
    // Create a default config if loading fails
    configStore.setDefaultConfig('default')
  }
})

onUnmounted(() => {
  destroyMarkdownEditors()
})
</script>

<style scoped>
.reminder-container {
  padding: 1.5rem;
  width: 100%;
}

.reminder-header {
  margin-bottom: 2rem;
}

.header-content {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 1rem;
}

.header-text {
  flex: 1;
}

.reminder-title {
  margin: 0 0 0.5rem 0;
}

.reminder-subtitle {
  margin: 0;
}

.reminder-card {
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
}

.form-section {
  margin-bottom: 2rem;
}

.form-section:last-child {
  margin-bottom: 0;
}

.editor-label {
  display: block;
  margin-bottom: 0.75rem;
  font-weight: 500;
  font-size: 0.9375rem;
}

.field-hint {
  margin: 0.75rem 0 0 0;
  font-size: 0.875rem;
  color: var(--va-text-secondary);
  line-height: 1.5;
}

.input-suffix {
  color: var(--va-text-secondary);
  font-size: 0.875rem;
}

.placeholders-section {
  margin-top: 2rem;
  padding-top: 2rem;
  border-top: 1px solid var(--va-background-border);
}

.placeholders-section h3 {
  margin: 0 0 1rem 0;
  font-size: 1rem;
  font-weight: 600;
}

.placeholder-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
  gap: 0.75rem;
}

.placeholder-chip {
  cursor: pointer;
  transition: all 0.2s;
  justify-content: space-between;
  width: 100%;
  padding: 0.5rem 0.75rem;
}

.placeholder-chip:hover {
  transform: translateY(-2px);
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.12);
}

.placeholder-chip code {
  font-family: 'Courier New', monospace;
  font-size: 0.875rem;
  margin-right: 0.5rem;
}

.templates-section {
  margin-top: 2rem;
  padding-top: 2rem;
  border-top: 1px solid var(--va-background-border);
}

.section-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 2rem;
  gap: 1.5rem;
  flex-wrap: wrap;
}

.section-title {
  margin: 0;
  font-size: 1.25rem;
  font-weight: 600;
  color: var(--va-text-primary);
}

.template-group {
  margin-bottom: 2rem;
  padding: 2rem;
  border: 1px solid var(--va-background-border);
  border-radius: 12px;
  background: var(--va-background-element);
}

.template-group:last-child {
  margin-bottom: 0;
}

.template-title {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin: 0 0 1.5rem 0;
  font-size: 1.0625rem;
  font-weight: 600;
  color: var(--va-text-primary);
}

.template-group .va-input,
.template-group .va-textarea,
.template-group .markdown-editor-wrapper {
  margin-bottom: 1.5rem;
}

.template-group .va-textarea:last-child,
.template-group .markdown-editor-wrapper:last-child {
  margin-bottom: 0;
}

.markdown-editor-wrapper {
  margin-top: 1rem;
}

.markdown-editor-wrapper .editor-label {
  display: block;
  margin-bottom: 0.5rem;
  font-weight: 500;
  font-size: 0.875rem;
}

.markdown-editor {
  width: 100%;
  min-height: 150px;
  border: 1px solid var(--va-background-border);
  border-radius: 4px;
  font-family: 'Courier New', monospace;
}

/* EasyMDE Dark Mode Support */
:deep(.EasyMDEContainer) {
  background: var(--va-background-element);
}

:deep(.CodeMirror) {
  background: var(--va-background-element);
  color: var(--va-text-primary);
  border: 1px solid var(--va-background-border);
  border-radius: 4px;
}

:deep(.CodeMirror-cursor) {
  border-left-color: var(--va-text-primary);
}

:deep(.editor-toolbar) {
  background: var(--va-background-element);
  border: 1px solid var(--va-background-border);
  border-bottom: none;
  opacity: 1;
}

:deep(.editor-toolbar button) {
  color: var(--va-text-primary) !important;
}

:deep(.editor-toolbar button:hover),
:deep(.editor-toolbar button.active) {
  background: var(--va-background-secondary);
  border-color: var(--va-background-border);
}

:deep(.editor-toolbar i.separator) {
  border-left-color: var(--va-background-border);
  border-right-color: var(--va-background-border);
}

:deep(.editor-preview),
:deep(.editor-preview-side) {
  background: var(--va-background-element);
  color: var(--va-text-primary);
}

@media (max-width: 768px) {
  .reminder-container {
    padding: 1rem;
  }

  .placeholder-grid {
    grid-template-columns: 1fr;
  }
  
  .template-group {
    padding: 1rem;
  }
  
  .history-item-header {
    flex-direction: column;
    align-items: flex-start;
    gap: 0.5rem;
  }
  
  .history-item-time {
    font-size: 0.75rem;
  }
  
  .history-item-meta {
    flex-direction: column;
    gap: 0.5rem;
  }
}

.history-card {
  margin-top: 2rem;
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
}

.card-title-content {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  width: 100%;
}

.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 3rem 1rem;
  text-align: center;
  color: var(--va-text-secondary);
}

.history-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.history-item {
  padding: 1.25rem;
  border: 1px solid var(--va-background-border);
  border-radius: 12px;
  background: var(--va-background-element);
  transition: all 0.2s;
}

.history-item:hover {
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
}

.history-item-failed {
  border-color: var(--va-danger);
  background: rgba(var(--va-danger-rgb), 0.05);
}

.history-item-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
  gap: 1rem;
}

.history-item-info {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  flex: 1;
  flex-wrap: wrap;
}

.history-item-task {
  font-weight: 600;
  color: var(--va-text-primary);
}

.history-item-time {
  font-size: 0.875rem;
  color: var(--va-text-secondary);
  white-space: nowrap;
}

.history-item-content {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.history-item-title {
  font-weight: 500;
  color: var(--va-text-primary);
  font-size: 0.9375rem;
  line-height: 1.5;
}

.history-item-body {
  font-size: 0.875rem;
  color: var(--va-text-secondary);
  white-space: pre-wrap;
  line-height: 1.6;
}

.history-item-meta {
  display: flex;
  flex-wrap: wrap;
  gap: 1.25rem;
  font-size: 0.8125rem;
  color: var(--va-text-secondary);
  margin-top: 0.75rem;
  padding-top: 0.75rem;
  border-top: 1px solid var(--va-background-border);
}

.error-message {
  color: var(--va-danger);
  font-weight: 500;
}
</style>
