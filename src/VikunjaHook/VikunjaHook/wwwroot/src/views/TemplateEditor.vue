<template>
  <div class="template-editor-container">
    <!-- Header -->
    <div class="editor-header">
      <div class="header-content">
        <div class="header-text">
          <h1 class="editor-title">{{ t('templates.title') }}</h1>
          <p class="editor-subtitle">{{ t('templates.description') }}</p>
        </div>
      </div>
    </div>

    <div class="editor-layout">
      <!-- Event List Sidebar -->
      <div class="event-sidebar">
        <va-card class="sidebar-card">
          <va-card-title>
            <div class="sidebar-title">
              <va-icon name="event" />
              <span>{{ t('templates.eventType') }}</span>
            </div>
          </va-card-title>
          <va-card-content class="sidebar-content">
            <!-- Task Events -->
            <va-collapse 
              v-model="taskExpanded" 
              :header="t('projects.taskEvents')" 
              icon="task"
            >
              <va-list class="event-list">
                <va-list-item
                  v-for="event in allEvents.filter(e => getEventMetadata(e).category === 'task')"
                  :key="event"
                  :active="selectedEvent === event"
                  @click="selectEvent(event)"
                  clickable
                  class="event-item"
                >
                  <va-list-item-section>
                    <va-list-item-label>
                      {{ getEventLabel(event) }}
                    </va-list-item-label>
                  </va-list-item-section>
                  <va-list-item-section icon>
                    <va-icon 
                      v-if="templates[event]" 
                      name="check_circle" 
                      color="success" 
                      size="small" 
                    />
                  </va-list-item-section>
                </va-list-item>
              </va-list>
            </va-collapse>

            <!-- Project Events -->
            <va-collapse 
              v-model="projectExpanded" 
              :header="t('projects.projectEvents')" 
              icon="folder"
            >
              <va-list class="event-list">
                <va-list-item
                  v-for="event in allEvents.filter(e => getEventMetadata(e).category === 'project')"
                  :key="event"
                  :active="selectedEvent === event"
                  @click="selectEvent(event)"
                  clickable
                  class="event-item"
                >
                  <va-list-item-section>
                    <va-list-item-label>
                      {{ getEventLabel(event) }}
                    </va-list-item-label>
                  </va-list-item-section>
                  <va-list-item-section icon>
                    <va-icon 
                      v-if="templates[event]" 
                      name="check_circle" 
                      color="success" 
                      size="small" 
                    />
                  </va-list-item-section>
                </va-list-item>
              </va-list>
            </va-collapse>

            <!-- Label Events -->
            <va-collapse 
              v-model="labelExpanded" 
              :header="t('projects.labelEvents')" 
              icon="label"
            >
              <va-list class="event-list">
                <va-list-item
                  v-for="event in allEvents.filter(e => getEventMetadata(e).category === 'label')"
                  :key="event"
                  :active="selectedEvent === event"
                  @click="selectEvent(event)"
                  clickable
                  class="event-item"
                >
                  <va-list-item-section>
                    <va-list-item-label>
                      {{ getEventLabel(event) }}
                    </va-list-item-label>
                  </va-list-item-section>
                  <va-list-item-section icon>
                    <va-icon 
                      v-if="templates[event]" 
                      name="check_circle" 
                      color="success" 
                      size="small" 
                    />
                  </va-list-item-section>
                </va-list-item>
              </va-list>
            </va-collapse>

            <!-- Team Events -->
            <va-collapse 
              v-model="teamExpanded" 
              :header="t('projects.teamEvents')" 
              icon="group"
            >
              <va-list class="event-list">
                <va-list-item
                  v-for="event in allEvents.filter(e => getEventMetadata(e).category === 'team')"
                  :key="event"
                  :active="selectedEvent === event"
                  @click="selectEvent(event)"
                  clickable
                  class="event-item"
                >
                  <va-list-item-section>
                    <va-list-item-label>
                      {{ getEventLabel(event) }}
                    </va-list-item-label>
                  </va-list-item-section>
                  <va-list-item-section icon>
                    <va-icon 
                      v-if="templates[event]" 
                      name="check_circle" 
                      color="success" 
                      size="small" 
                    />
                  </va-list-item-section>
                </va-list-item>
              </va-list>
            </va-collapse>

            <!-- User Events -->
            <va-collapse 
              v-model="userExpanded" 
              :header="t('projects.userEvents')" 
              icon="person"
            >
              <va-list class="event-list">
                <va-list-item
                  v-for="event in allEvents.filter(e => getEventMetadata(e).category === 'user')"
                  :key="event"
                  :active="selectedEvent === event"
                  @click="selectEvent(event)"
                  clickable
                  class="event-item"
                >
                  <va-list-item-section>
                    <va-list-item-label>
                      {{ getEventLabel(event) }}
                    </va-list-item-label>
                  </va-list-item-section>
                  <va-list-item-section icon>
                    <va-icon 
                      v-if="templates[event]" 
                      name="check_circle" 
                      color="success" 
                      size="small" 
                    />
                  </va-list-item-section>
                </va-list-item>
              </va-list>
            </va-collapse>
          </va-card-content>
        </va-card>
      </div>

      <!-- Template Editor -->
      <div class="editor-main">
        <va-card v-if="selectedEvent" class="editor-card">
          <va-card-title>
            <div class="card-title-content">
              <va-icon name="edit_note" />
              <span>{{ getEventLabel(selectedEvent) }}</span>
            </div>
          </va-card-title>
          <va-card-content>
            <div class="form-section">
              <va-input
                v-model="currentTemplate.title"
                :label="t('templates.titleTemplate')"
                :placeholder="t('templates.titlePlaceholder')"
              >
                <template #prepend>
                  <va-icon name="title" />
                </template>
              </va-input>
            </div>

            <div class="form-section">
              <label class="editor-label">{{ t('templates.bodyTemplate') }}</label>
              <textarea ref="editorTextarea" class="markdown-editor"></textarea>
            </div>

            <div class="form-section">
              <label class="editor-label">
                {{ t('templates.providers') }}
                <va-icon name="info" size="small" class="info-icon" />
              </label>
              <p class="field-hint">{{ t('templates.providersHint') }}</p>
              <va-select
                v-model="currentTemplate.providers"
                :label="t('templates.selectProviders')"
                :options="providerOptions"
                :no-options-text="t('common.noOptions')"
                multiple
                :placeholder="t('templates.useDefaultProviders')"
                clearable
              />
            </div>

            <div v-if="selectedEvent === 'task.updated'" class="form-section">
              <va-checkbox
                v-model="currentTemplate.onlyNotifyWhenCompleted"
                :label="t('templates.onlyNotifyWhenCompleted')"
              >
                <template #label>
                  <span>{{ t('templates.onlyNotifyWhenCompleted') }}</span>
                  <va-icon name="info" size="small" class="info-icon" />
                </template>
              </va-checkbox>
              <p class="field-hint">{{ t('templates.onlyNotifyWhenCompletedHint') }}</p>
            </div>

            <div class="action-buttons">
              <va-button @click="saveTemplate" color="primary" icon="save">
                {{ t('templates.save') }}
              </va-button>
              <va-button @click="copyTemplate" icon="content_copy" preset="secondary">
                {{ t('templates.copyTemplate') }}
              </va-button>
              <va-button @click="resetTemplate" icon="refresh" preset="secondary">
                {{ t('templates.reset') }}
              </va-button>
            </div>
          </va-card-content>
        </va-card>

        <va-card v-else class="empty-state-card">
          <va-card-content>
            <div class="empty-state">
              <va-icon name="edit_note" size="4rem" color="secondary" />
              <h3 class="empty-title">{{ t('templates.selectEvent') }}</h3>
              <p class="empty-description">{{ t('templates.description') }}</p>
            </div>
          </va-card-content>
        </va-card>
      </div>

      <!-- Placeholders Panel -->
      <div class="placeholders-panel">
        <va-card v-if="selectedEvent" class="placeholders-card">
          <va-card-title>
            <div class="sidebar-title">
              <va-icon name="code" />
              <span>{{ t('templates.placeholders') }}</span>
            </div>
          </va-card-title>
          <va-card-content class="placeholders-content">
            <p class="placeholders-hint">{{ t('common.clickToCopy') }}</p>
            <div v-for="(placeholders, group) in groupedPlaceholders" :key="group" class="placeholder-group">
              <h4 class="group-title">{{ t(`templates.placeholderGroups.${group}`) }}</h4>
              <div class="placeholder-list">
                <va-chip
                  v-for="placeholder in placeholders"
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
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, nextTick } from 'vue'
import { useI18n } from 'vue-i18n'
import { useConfigStore } from '@/stores/configStore'
import { useEventStore } from '@/stores/eventStore'
import { getPlaceholdersForEvent, type EventType } from '@/types/events'
import type { NotificationTemplate } from '@/types/config'
import { NotificationFormat } from '@/types/config'
import EasyMDE from 'easymde'
import 'easymde/dist/easymde.min.css'
import { useToast } from 'vuestic-ui'

const { t } = useI18n()
const { init: notify } = useToast()
const configStore = useConfigStore()
const eventStore = useEventStore()

const allEvents = computed(() => eventStore.allEvents)
const templates = computed(() => configStore.templates)
const providers = computed(() => configStore.providers)

const selectedEvent = ref<string | null>(null)
const taskExpanded = ref(true)
const projectExpanded = ref(false)
const labelExpanded = ref(false)
const teamExpanded = ref(false)
const userExpanded = ref(false)
const editorTextarea = ref<HTMLTextAreaElement | null>(null)
let mdeInstance: EasyMDE | null = null

const currentTemplate = ref<NotificationTemplate>({
  eventType: '',
  title: '',
  body: '',
  format: NotificationFormat.Markdown,
  providers: [],
  onlyNotifyWhenCompleted: false
})

const providerOptions = computed(() => {
  return providers.value.map(p => p.providerType)
})

const availablePlaceholders = computed(() => {
  if (!selectedEvent.value) return []
  
  return getPlaceholdersForEvent(selectedEvent.value as EventType)
})

const groupedPlaceholders = computed(() => {
  const placeholders = availablePlaceholders.value
  const groups: Record<string, string[]> = {}
  
  placeholders.forEach(placeholder => {
    const category = placeholder.split('.')[0]
    if (!groups[category]) {
      groups[category] = []
    }
    groups[category].push(placeholder)
  })
  
  return groups
})

function getEventLabel(eventType: string): string {
  // Convert event type like "task.created" to camelCase "taskCreated"
  const key = eventType.replace(/[.-](\w)/g, (_, c) => c.toUpperCase())
  return t(`events.${key}`)
}

function getEventMetadata(eventType: string) {
  return eventStore.getEventMetadata(eventType as EventType)
}

function initMarkdownEditor() {
  if (editorTextarea.value && !mdeInstance) {
    mdeInstance = new EasyMDE({
      element: editorTextarea.value,
      spellChecker: false,
      placeholder: t('templates.bodyPlaceholder'),
      toolbar: [
        'bold', 'italic', 'heading', '|',
        'quote', 'unordered-list', 'ordered-list', '|',
        'link', 'image', '|',
        'preview', 'side-by-side', 'fullscreen', '|',
        'guide'
      ],
      status: false,
      autosave: {
        enabled: false
      }
    })
    
    mdeInstance.codemirror.on('change', () => {
      currentTemplate.value.body = mdeInstance?.value() || ''
    })
  }
}

function destroyMarkdownEditor() {
  if (mdeInstance) {
    mdeInstance.toTextArea()
    mdeInstance = null
  }
}

function selectEvent(eventType: string) {
  selectedEvent.value = eventType
  
  const existing = templates.value[eventType]
  if (existing) {
    currentTemplate.value = { ...existing }
  } else {
    currentTemplate.value = {
      eventType,
      title: `{event.type}`,
      body: `Event occurred at {event.timestamp}`,
      format: NotificationFormat.Markdown,
      providers: [],
      onlyNotifyWhenCompleted: false
    }
  }
  
  // Update editor content
  nextTick(() => {
    destroyMarkdownEditor()
    initMarkdownEditor()
    if (mdeInstance) {
      mdeInstance.value(currentTemplate.value.body)
    }
  })
}

async function saveTemplate() {
  if (!selectedEvent.value) return
  
  try {
    // Get latest value from editor
    if (mdeInstance) {
      currentTemplate.value.body = mdeInstance.value()
    }
    
    // Ensure config is loaded
    if (!configStore.config) {
      notify({
        message: t('templates.errorNoConfig'),
        color: 'danger'
      })
      return
    }
    
    configStore.setTemplate(selectedEvent.value, { ...currentTemplate.value })
    await configStore.saveConfig()
    
    notify({
      message: t('templates.saved'),
      color: 'success'
    })
  } catch (error: unknown) {
    console.error('Failed to save template:', error)
    notify({
      message: t('common.error') + ': ' + (error instanceof Error ? error.message : 'Unknown error'),
      color: 'danger'
    })
  }
}

async function resetTemplate() {
  if (!selectedEvent.value) return
  
  try {
    configStore.removeTemplate(selectedEvent.value)
    await configStore.saveConfig()
    selectEvent(selectedEvent.value)
    
    notify({
      message: t('templates.saved'),
      color: 'success'
    })
  } catch (error: unknown) {
    console.error('Failed to reset template:', error)
    notify({
      message: t('common.error') + ': ' + (error instanceof Error ? error.message : 'Unknown error'),
      color: 'danger'
    })
  }
}

function copyPlaceholder(placeholder: string) {
  const text = `{${placeholder}}`
  navigator.clipboard.writeText(text)
  notify({
    message: t('templates.placeholderCopied'),
    color: 'info',
    duration: 1500
  })
}

function formatPlaceholder(placeholder: string): string {
  return `{${placeholder}}`
}

async function copyTemplate() {
  if (!selectedEvent.value || !currentTemplate.value) return
  
  try {
    // Get latest value from editor
    if (mdeInstance) {
      currentTemplate.value.body = mdeInstance.value()
    }
    
    const templateJson = JSON.stringify(currentTemplate.value, null, 2)
    await navigator.clipboard.writeText(templateJson)
    
    notify({
      message: t('templates.templateCopied'),
      color: 'success'
    })
  } catch (error) {
    console.error('Failed to copy template:', error)
    notify({
      message: t('templates.copyFailed'),
      color: 'danger'
    })
  }
}

onMounted(async () => {
  try {
    await configStore.loadConfig('default')
  } catch (error) {
    console.error('Failed to load config:', error)
    // Create a default config if loading fails
    configStore.setDefaultConfig('default')
  }
  
  // Editor will be initialized when an event is selected
})

onUnmounted(() => {
  destroyMarkdownEditor()
})
</script>

<style scoped>
.template-editor-container {
  padding: 1.5rem;
  width: 100%;
}

.editor-header {
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

.editor-title {
  margin: 0 0 0.5rem 0;
}

.editor-subtitle {
  margin: 0;
}

/* Layout */
.editor-layout {
  display: grid;
  grid-template-columns: 280px 1fr 300px;
  gap: 1.5rem;
  align-items: start;
}

/* Event Sidebar */
.event-sidebar {
  position: sticky;
  top: 1.5rem;
}

.sidebar-card {
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
}

.sidebar-title {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.sidebar-content {
  padding: 0 !important;
}

.category-header {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  width: 100%;
}

.category-title {
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.event-list {
  max-height: none;
  overflow-y: visible;
}

.event-item {
  padding: 0.75rem 1rem;
  transition: all 0.2s;
}

.event-item:hover {
  background: var(--va-background-element);
}

/* Editor Main */
.editor-main {
  min-height: 500px;
}

.editor-card,
.empty-state-card {
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
}

.card-title-content {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  font-size: 1.125rem;
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
  font-size: 0.9375rem;
}

.field-hint {
  margin: 0 0 1rem 0;
  font-size: 0.875rem;
  color: var(--va-text-secondary);
  line-height: 1.5;
}

.info-icon {
  margin-left: 0.25rem;
  opacity: 0.6;
}

.markdown-editor {
  width: 100%;
  min-height: 300px;
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

.action-buttons {
  display: flex;
  gap: 1rem;
  margin-top: 2.5rem;
  flex-wrap: wrap;
}

.action-buttons .va-button {
  min-width: 120px;
}

/* Empty State */
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  text-align: center;
  padding: 4rem 2rem;
}

.empty-title {
  margin: 1.5rem 0 0.5rem 0;
}

.empty-description {
  margin: 0;
}

/* Placeholders Panel */
.placeholders-panel {
  position: sticky;
  top: 1.5rem;
}

.placeholders-card {
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
}

.placeholders-content {
  max-height: calc(100vh - 250px);
  overflow-y: auto;
}

.placeholders-hint {
  margin: 0 0 1rem 0;
}

.placeholder-group {
  margin-bottom: 2rem;
}

.placeholder-group:last-child {
  margin-bottom: 0;
}

.group-title {
  margin: 0 0 1rem 0;
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--va-text-primary);
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.placeholder-list {
  display: flex;
  flex-direction: column;
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
  transform: translateX(4px);
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.12);
}

.placeholder-chip code {
  font-family: 'Courier New', monospace;
  margin-right: 0.5rem;
}

/* Responsive */
@media (max-width: 1200px) {
  .editor-layout {
    grid-template-columns: 1fr;
    gap: 1.5rem;
  }

  .event-sidebar,
  .placeholders-panel {
    position: static;
  }

  .event-list,
  .placeholders-content {
    max-height: 400px;
  }
}

@media (max-width: 768px) {
  .template-editor-container {
    padding: 1rem;
  }

  .editor-header {
    padding: 1rem;
  }

  .editor-title {
    font-size: 1.5rem;
  }

  .editor-subtitle {
    font-size: 0.875rem;
  }

  .action-buttons {
    flex-direction: column;
    gap: 0.5rem;
  }

  .action-buttons button {
    width: 100%;
  }

  .editor-layout {
    gap: 1rem;
  }

  .event-sidebar,
  .placeholders-panel {
    max-height: 300px;
  }

  /* Make markdown editor more mobile-friendly */
  :deep(.EasyMDEContainer) {
    min-height: 200px;
  }

  :deep(.CodeMirror) {
    min-height: 200px;
    font-size: 14px;
  }

  :deep(.editor-toolbar) {
    padding: 0.25rem;
  }

  :deep(.editor-toolbar button) {
    width: 28px;
    height: 28px;
  }

  /* Make form inputs more mobile-friendly */
  .va-input,
  .va-select {
    font-size: 16px; /* Prevents zoom on iOS */
  }
}

@media (max-width: 480px) {
  .template-editor-container {
    padding: 0.5rem;
  }

  .editor-header {
    padding: 0.75rem;
  }

  .editor-title {
    font-size: 1.25rem;
  }

  .editor-subtitle {
    font-size: 0.8rem;
  }

  /* Stack sidebar items vertically on very small screens */
  .event-list,
  .placeholders-content {
    max-height: 250px;
  }

  /* Reduce markdown editor toolbar */
  :deep(.editor-toolbar) {
    flex-wrap: wrap;
  }

  :deep(.editor-toolbar button) {
    width: 24px;
    height: 24px;
    font-size: 12px;
  }
}
</style>
