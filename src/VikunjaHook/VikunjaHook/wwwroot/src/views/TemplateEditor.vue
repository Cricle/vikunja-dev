<template>
  <div class="template-editor-container">
    <!-- Header -->
    <div class="editor-header">
      <div class="header-content">
        <div class="header-text">
          <h1 class="editor-title">{{ t('templates.title') }}</h1>
          <p class="editor-subtitle">{{ t('templates.description') }}</p>
        </div>
        <div class="header-actions">
          <va-select
            v-model="selectedProjectId"
            :label="t('projects.selectProject')"
            :options="projectOptions"
            text-by="label"
            value-by="value"
            class="project-selector"
          />
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
                placeholder="e.g., New Task: {{task.title}}"
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
            <div class="placeholder-list">
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
          </va-card-content>
        </va-card>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, nextTick, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute } from 'vue-router'
import { useConfigStore } from '@/stores/configStore'
import { useEventStore } from '@/stores/eventStore'
import { vikunjaService } from '@/services/vikunjaApi'
import type { VikunjaProject } from '@/services/vikunjaApi'
import { PlaceholdersByEventType } from '@/types/events'
import type { NotificationTemplate } from '@/types/config'
import { NotificationFormat } from '@/types/config'
import EasyMDE from 'easymde'
import 'easymde/dist/easymde.min.css'
import { useToast } from 'vuestic-ui'

const { t } = useI18n()
const { init: notify } = useToast()
const route = useRoute()
const configStore = useConfigStore()
const eventStore = useEventStore()

const allEvents = computed(() => eventStore.allEvents)
const templates = computed(() => configStore.templates)

const selectedEvent = ref<string | null>(null)
const selectedProjectId = ref<string>('all')
const projects = ref<VikunjaProject[]>([])
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
  format: NotificationFormat.Markdown
})

const projectOptions = computed(() => {
  return [
    { label: t('projects.allProjects'), value: 'all' },
    ...projects.value.map(p => ({
      label: p.title,
      value: p.id.toString()
    }))
  ]
})

const availablePlaceholders = computed(() => {
  if (!selectedEvent.value) return []
  
  const metadata = eventStore.getEventMetadata(selectedEvent.value as any)
  return PlaceholdersByEventType[metadata.category] || []
})

function getEventLabel(eventType: string): string {
  // Convert event type like "task.created" to camelCase "taskCreated"
  const key = eventType.replace(/[.-](\w)/g, (_, c) => c.toUpperCase())
  return t(`events.${key}`)
}

function getEventMetadata(eventType: string) {
  return eventStore.getEventMetadata(eventType as any)
}

function initMarkdownEditor() {
  if (editorTextarea.value && !mdeInstance) {
    mdeInstance = new EasyMDE({
      element: editorTextarea.value,
      spellChecker: false,
      placeholder: 'e.g., A new task **{{task.title}}** has been created in {{project.title}}',
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
      title: `{{event.type}}`,
      body: `Event occurred at {{event.timestamp}}`,
      format: NotificationFormat.Markdown
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
  } catch (error: any) {
    console.error('Failed to save template:', error)
    notify({
      message: t('common.error') + ': ' + (error.message || 'Unknown error'),
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
  } catch (error: any) {
    console.error('Failed to reset template:', error)
    notify({
      message: t('common.error') + ': ' + (error.message || 'Unknown error'),
      color: 'danger'
    })
  }
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

function formatPlaceholder(placeholder: string): string {
  return `{{${placeholder}}}`
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

async function loadProjects() {
  try {
    projects.value = await vikunjaService.getProjects()
  } catch (error) {
    console.error('Failed to load projects:', error)
  }
}

// Watch for project changes and reload config for that project
watch(selectedProjectId, async (newProjectId) => {
  if (!newProjectId) return
  
  try {
    // Load config for the selected project (use project ID as userId)
    await configStore.loadConfig(newProjectId === 'all' ? 'default' : newProjectId)
    
    // If an event is currently selected, refresh its template
    if (selectedEvent.value) {
      const eventType = selectedEvent.value
      selectedEvent.value = null
      nextTick(() => {
        selectEvent(eventType)
      })
    }
  } catch (error) {
    console.error('Failed to load config for project:', error)
  }
})

onMounted(async () => {
  // Load projects for selector first
  await loadProjects()
  
  // Check if project ID is in URL query
  const projectId = route.query.project as string
  if (projectId) {
    selectedProjectId.value = projectId
  }
  
  // Load config for the selected project
  try {
    const configId = selectedProjectId.value === 'all' ? 'default' : selectedProjectId.value
    await configStore.loadConfig(configId)
  } catch (error) {
    console.error('Failed to load config:', error)
    notify({
      message: t('templates.errorNoConfig'),
      color: 'danger'
    })
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
}

.form-section {
  margin-bottom: 1.5rem;
}

.editor-label {
  display: block;
  margin-bottom: 0.5rem;
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
  margin-top: 2rem;
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

.placeholder-list {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.placeholder-chip {
  cursor: pointer;
  transition: all 0.2s;
  justify-content: space-between;
  width: 100%;
}

.placeholder-chip:hover {
  transform: translateX(4px);
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.12);
}

.placeholder-chip code {
  font-family: 'Courier New', monospace;
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

  .editor-title {
  }

  .action-buttons {
    flex-direction: column;
  }

  .action-buttons button {
    width: 100%;
  }
}
</style>
