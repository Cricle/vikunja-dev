<template>
  <div class="project-rules-container">
    <!-- Header -->
    <div class="rules-header">
      <div class="header-content">
        <div class="header-text">
          <h1 class="rules-title">{{ t('projects.title') }}</h1>
          <p class="rules-subtitle">{{ t('projects.description') }}</p>
        </div>
        <div class="header-actions">
          <va-button @click="showAddModal = true" icon="add" color="primary" size="large">
            {{ t('projects.add') }}
          </va-button>
        </div>
      </div>
    </div>

    <!-- Rules List -->
    <div class="rules-content">
      <div v-if="projectRules.length > 0" class="rules-list">
        <va-collapse
          v-for="rule in projectRules"
          :key="rule.projectId"
          :header="getProjectName(rule.projectId)"
          class="rule-collapse"
        >
          <template #header>
            <div class="collapse-header">
              <div class="collapse-header-left">
                <va-icon name="folder" size="large" color="primary" />
                <div class="collapse-header-info">
                  <h3 class="collapse-title">{{ getProjectName(rule.projectId) }}</h3>
                  <p class="collapse-subtitle">{{ rule.enabledEvents.length }} {{ t('projects.enabledEvents') }}</p>
                </div>
              </div>
              <div class="collapse-header-right">
                <va-chip 
                  :color="rule.enabledEvents.length > 0 ? 'success' : 'secondary'" 
                  size="small"
                >
                  {{ rule.enabledEvents.length > 0 ? t('projects.active') : t('projects.inactive') }}
                </va-chip>
              </div>
            </div>
          </template>

          <div class="rule-content">
            <div class="events-section">
              <div class="events-header">
                <h4 class="events-title">{{ t('projects.eventTypes') }}</h4>
                <div class="events-actions">
                  <va-button 
                    @click="selectAllEvents(rule.projectId)" 
                    size="small" 
                    preset="secondary"
                    icon="check_box"
                  >
                    {{ t('common.selectAll') }}
                  </va-button>
                  <va-button 
                    @click="deselectAllEvents(rule.projectId)" 
                    size="small" 
                    preset="secondary"
                    icon="check_box_outline_blank"
                  >
                    {{ t('common.deselectAll') }}
                  </va-button>
                </div>
              </div>
              
              <!-- Task Events -->
              <div class="event-category-section">
                <h5 class="category-subtitle">
                  <va-icon name="task" size="small" />
                  {{ t('projects.taskEvents') }}
                </h5>
                <div class="events-list">
                  <va-checkbox
                    v-for="event in allEvents.filter(e => getEventMetadata(e).category === 'task')"
                    :key="event"
                    :model-value="rule.enabledEvents.includes(event)"
                    @update:model-value="toggleEvent(rule.projectId, event, $event)"
                    :label="getEventLabel(event)"
                  />
                </div>
              </div>

              <!-- Project Events -->
              <div class="event-category-section">
                <h5 class="category-subtitle">
                  <va-icon name="folder" size="small" />
                  {{ t('projects.projectEvents') }}
                </h5>
                <div class="events-list">
                  <va-checkbox
                    v-for="event in allEvents.filter(e => getEventMetadata(e).category === 'project')"
                    :key="event"
                    :model-value="rule.enabledEvents.includes(event)"
                    @update:model-value="toggleEvent(rule.projectId, event, $event)"
                    :label="getEventLabel(event)"
                  />
                </div>
              </div>

              <!-- Label Events -->
              <div class="event-category-section">
                <h5 class="category-subtitle">
                  <va-icon name="label" size="small" />
                  {{ t('projects.labelEvents') }}
                </h5>
                <div class="events-list">
                  <va-checkbox
                    v-for="event in allEvents.filter(e => getEventMetadata(e).category === 'label')"
                    :key="event"
                    :model-value="rule.enabledEvents.includes(event)"
                    @update:model-value="toggleEvent(rule.projectId, event, $event)"
                    :label="getEventLabel(event)"
                  />
                </div>
              </div>

              <!-- Team Events -->
              <div class="event-category-section">
                <h5 class="category-subtitle">
                  <va-icon name="group" size="small" />
                  {{ t('projects.teamEvents') }}
                </h5>
                <div class="events-list">
                  <va-checkbox
                    v-for="event in allEvents.filter(e => getEventMetadata(e).category === 'team')"
                    :key="event"
                    :model-value="rule.enabledEvents.includes(event)"
                    @update:model-value="toggleEvent(rule.projectId, event, $event)"
                    :label="getEventLabel(event)"
                  />
                </div>
              </div>

              <!-- User Events -->
              <div class="event-category-section">
                <h5 class="category-subtitle">
                  <va-icon name="person" size="small" />
                  {{ t('projects.userEvents') }}
                </h5>
                <div class="events-list">
                  <va-checkbox
                    v-for="event in allEvents.filter(e => getEventMetadata(e).category === 'user')"
                    :key="event"
                    :model-value="rule.enabledEvents.includes(event)"
                    @update:model-value="toggleEvent(rule.projectId, event, $event)"
                    :label="getEventLabel(event)"
                  />
                </div>
              </div>
            </div>
            
            <va-divider class="rule-divider" />
            
            <div class="rule-actions">
              <va-button
                @click="goToTemplates(rule.projectId)"
                color="primary"
                icon="edit_note"
                preset="secondary"
                size="small"
              >
                {{ t('templates.title') }}
              </va-button>
              <va-button
                v-if="rule.projectId !== '*'"
                @click="removeRule(rule.projectId)"
                color="danger"
                icon="delete"
                preset="secondary"
                size="small"
              >
                {{ t('common.delete') }}
              </va-button>
            </div>
          </div>
        </va-collapse>
      </div>

      <!-- Empty State -->
      <va-card v-else class="empty-state-card">
        <va-card-content>
          <div class="empty-state">
            <va-icon name="folder_off" size="4rem" color="secondary" />
            <h3 class="empty-title">{{ t('projects.noRules') }}</h3>
            <p class="empty-description">{{ t('projects.noRulesDescription') }}</p>
            <va-button @click="showAddModal = true" icon="add" color="primary" size="large">
              {{ t('projects.add') }}
            </va-button>
          </div>
        </va-card-content>
      </va-card>
    </div>

    <!-- Add Rule Modal -->
    <va-modal
      v-model="showAddModal"
      :title="t('projects.add')"
      size="medium"
      @ok="addRule"
      @cancel="cancelAdd"
    >
      <div class="modal-form">
        <va-select
          v-model="newProjectId"
          :label="t('projects.projectId')"
          :options="availableProjects"
          text-by="title"
          value-by="id"
          searchable
          class="form-field"
        >
          <template #prepend>
            <va-icon name="folder" />
          </template>
        </va-select>
      </div>
    </va-modal>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { useConfigStore } from '@/stores/configStore'
import { useEventStore } from '@/stores/eventStore'
import { vikunjaService, type VikunjaProject } from '@/services/vikunjaApi'

const { t } = useI18n()
const configStore = useConfigStore()
const eventStore = useEventStore()
const router = useRouter()

// Sort project rules by project ID
const projectRules = computed(() => {
  return [...configStore.projectRules].sort((a, b) => {
    // "*" (all projects) should be first
    if (a.projectId === '*') return -1
    if (b.projectId === '*') return 1
    
    // Convert to numbers for proper numeric sorting
    const aNum = parseInt(a.projectId, 10)
    const bNum = parseInt(b.projectId, 10)
    
    // Handle NaN cases (non-numeric IDs)
    if (isNaN(aNum) && isNaN(bNum)) return a.projectId.localeCompare(b.projectId)
    if (isNaN(aNum)) return 1
    if (isNaN(bNum)) return -1
    
    // Numeric sort
    return aNum - bNum
  })
})

const allEvents = computed(() => eventStore.allEvents)

const showAddModal = ref(false)
const newProjectId = ref('')
const expandedRules = ref<string[]>([])
const vikunjaProjects = ref<VikunjaProject[]>([])

const availableProjects = computed(() => {
  const existingIds = projectRules.value.map(r => r.projectId)
  return vikunjaProjects.value.filter(p => !existingIds.includes(String(p.id)))
})

function getProjectName(projectId: string): string {
  if (projectId === '*') return t('projects.allProjects')
  const project = vikunjaProjects.value.find(p => String(p.id) === projectId)
  return project ? project.title : `Project ${projectId}`
}

function getEventLabel(eventType: string): string {
  // Convert event type like "task.created" to camelCase "taskCreated"
  const key = eventType.replace(/[.-](\w)/g, (_, c) => c.toUpperCase())
  return t(`events.${key}`)
}

function getEventMetadata(eventType: string) {
  return eventStore.getEventMetadata(eventType as any)
}

function goToTemplates(projectId: string) {
  router.push({ name: 'templates', query: { project: projectId } })
}

async function toggleEvent(projectId: string, eventType: string, enabled: boolean) {
  const rule = projectRules.value.find(r => r.projectId === projectId)
  if (!rule) return

  if (enabled) {
    if (!rule.enabledEvents.includes(eventType)) {
      rule.enabledEvents.push(eventType)
    }
  } else {
    rule.enabledEvents = rule.enabledEvents.filter(e => e !== eventType)
  }
  
  // Save to backend immediately
  try {
    await configStore.saveConfig()
  } catch (err) {
    console.error('Failed to save event toggle:', err)
  }
}

async function selectAllEvents(projectId: string) {
  const rule = projectRules.value.find(r => r.projectId === projectId)
  if (!rule) return
  
  rule.enabledEvents = [...allEvents.value]
  
  try {
    await configStore.saveConfig()
  } catch (err) {
    console.error('Failed to select all events:', err)
  }
}

async function deselectAllEvents(projectId: string) {
  const rule = projectRules.value.find(r => r.projectId === projectId)
  if (!rule) return
  
  rule.enabledEvents = []
  
  try {
    await configStore.saveConfig()
  } catch (err) {
    console.error('Failed to deselect all events:', err)
  }
}

async function addRule() {
  if (!newProjectId.value) return
  
  try {
    configStore.addProjectRule({
      projectId: String(newProjectId.value),
      enabledEvents: [],
      providerType: undefined
    })
    
    // Save to backend immediately
    await configStore.saveConfig()
    
    showAddModal.value = false
    newProjectId.value = ''
  } catch (err) {
    console.error('Failed to add rule:', err)
  }
}

function cancelAdd() {
  showAddModal.value = false
  newProjectId.value = ''
}

async function removeRule(projectId: string) {
  if (confirm(t('projects.confirmDelete'))) {
    try {
      configStore.removeProjectRule(projectId)
      await configStore.saveConfig()
    } catch (err) {
      console.error('Failed to remove rule:', err)
    }
  }
}

async function saveConfiguration() {
  try {
    await configStore.saveConfig()
  } catch (err) {
    console.error('Failed to save configuration:', err)
  }
}

onMounted(async () => {
  // Load config first
  try {
    await configStore.loadConfig('default')
  } catch (error) {
    console.error('Failed to load config:', error)
  }
  
  // Load Vikunja projects
  try {
    vikunjaProjects.value = await vikunjaService.getProjects()
  } catch (err) {
    console.error('Failed to load projects:', err)
  }
})
</script>

<style scoped>
.project-rules-container {
  padding: 2rem;
  max-width: 1400px;
  margin: 0 auto;
}

.rules-header {
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

.rules-title {
  margin: 0 0 0.5rem 0;
}

.rules-subtitle {
  margin: 0;
}

.rules-content {
  margin-bottom: 2rem;
}

.rules-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.rule-collapse {
  border-radius: 8px;
}

.collapse-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  width: 100%;
  padding: 0.5rem 0;
}

.collapse-header-left {
  display: flex;
  align-items: center;
  gap: 1rem;
  flex: 1;
}

.collapse-header-info {
  flex: 1;
}

.collapse-title {
  margin: 0 0 0.25rem 0;
}

.collapse-subtitle {
  margin: 0;
}

.collapse-header-right {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.rule-content {
  padding: 1rem 0;
}

.events-section {
  margin-bottom: 1.5rem;
}

.events-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
  flex-wrap: wrap;
  gap: 0.5rem;
}

.events-title {
  margin: 0;
}

.events-actions {
  display: flex;
  gap: 0.5rem;
}

.event-category-section {
  margin-bottom: 1.5rem;
}

.category-subtitle {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin: 0 0 0.75rem 0;
  padding: 0.5rem 0.75rem;
  border-radius: 6px;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.events-list {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
  gap: 0.5rem;
}

.rule-divider {
  margin: 1rem 0;
}

.rule-actions {
  display: flex;
  justify-content: flex-end;
  gap: 0.5rem;
}

.empty-state-card {
  border-radius: 12px;
}

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
  margin: 0 0 2rem 0;
}

.modal-form {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.form-field {
  width: 100%;
}

@media (max-width: 768px) {
  .project-rules-container {
    padding: 1rem;
  }

  .rules-title {
  }

  .events-list {
    grid-template-columns: 1fr;
  }

  .header-content {
    flex-direction: column;
    align-items: stretch;
  }

  .header-actions {
    width: 100%;
  }

  .header-actions button {
    width: 100%;
  }

  .collapse-header {
    flex-direction: column;
    align-items: flex-start;
    gap: 0.5rem;
  }

  .collapse-header-right {
    width: 100%;
    justify-content: flex-start;
  }

  .events-header {
    flex-direction: column;
    align-items: stretch;
  }

  .events-actions {
    width: 100%;
  }

  .events-actions button {
    flex: 1;
  }

  .rule-actions {
    flex-direction: column;
  }

  .rule-actions button {
    width: 100%;
  }
}
</style>
