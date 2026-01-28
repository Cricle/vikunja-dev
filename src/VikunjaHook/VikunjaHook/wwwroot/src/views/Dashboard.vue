<template>
  <div class="dashboard-container">
    <va-inner-loading :loading="loading">
      <!-- Header Section -->
      <div class="dashboard-header">
        <h1>{{ t('dashboard.title') }}</h1>
        <p>{{ t('dashboard.welcome') }}</p>
      </div>

      <va-alert v-if="error" color="danger" class="error-alert">
        {{ error }}
      </va-alert>

      <!-- Stats Cards -->
      <div class="stats-grid">
        <va-card class="stats-card">
          <va-card-content>
            <div class="stats-content">
              <va-icon name="folder" size="2rem" color="primary" />
              <div class="stats-info">
                <div class="stats-number">{{ vikunjaProjects.length }}</div>
                <div class="stats-label">{{ t('dashboard.stats.projects') }}</div>
              </div>
            </div>
          </va-card-content>
        </va-card>

        <va-card class="stats-card">
          <va-card-content>
            <div class="stats-content">
              <va-icon name="task" size="2rem" color="success" />
              <div class="stats-info">
                <div class="stats-number">{{ vikunjaTasks.length }}</div>
                <div class="stats-label">{{ t('dashboard.stats.tasks') }}</div>
              </div>
            </div>
          </va-card-content>
        </va-card>

        <va-card class="stats-card">
          <va-card-content>
            <div class="stats-content">
              <va-icon name="label" size="2rem" color="warning" />
              <div class="stats-info">
                <div class="stats-number">{{ vikunjaLabels.length }}</div>
                <div class="stats-label">{{ t('dashboard.stats.labels') }}</div>
              </div>
            </div>
          </va-card-content>
        </va-card>

        <va-card class="stats-card">
          <va-card-content>
            <div class="stats-content">
              <va-icon name="notifications" size="2rem" color="info" />
              <div class="stats-info">
                <div class="stats-number">{{ providers.length }}</div>
                <div class="stats-label">{{ t('dashboard.stats.providers') }}</div>
              </div>
            </div>
          </va-card-content>
        </va-card>
      </div>

      <!-- Recent Tasks -->
      <va-card class="content-card">
        <va-card-title>
          <div class="card-title-content">
            <va-icon name="task" />
            <span>{{ t('dashboard.recentTasks') }}</span>
          </div>
        </va-card-title>
        <va-card-content>
          <va-inner-loading :loading="loadingTasks">
            <va-alert v-if="vikunjaTasks.length === 0" color="info" border="left">
              {{ t('dashboard.noTasks') }}
            </va-alert>
            <va-data-table
              v-else
              :items="vikunjaTasks.slice(0, 10)"
              :columns="taskColumns"
              striped
              hoverable
            >
              <template #cell(title)="{ rowData }">
                <div class="table-cell-content">
                  <va-icon 
                    :name="rowData.done ? 'check_circle' : 'radio_button_unchecked'" 
                    :color="rowData.done ? 'success' : 'secondary'"
                    size="small"
                  />
                  <span>{{ rowData.title }}</span>
                </div>
              </template>
              <template #cell(priority)="{ rowData }">
                <va-chip size="small" :color="getPriorityColor(rowData.priority)">
                  {{ getPriorityLabel(rowData.priority) }}
                </va-chip>
              </template>
              <template #cell(dueDate)="{ rowData }">
                <span v-if="rowData.dueDate">
                  {{ formatDate(rowData.dueDate) }}
                </span>
                <span v-else>-</span>
              </template>
            </va-data-table>
          </va-inner-loading>
        </va-card-content>
      </va-card>
    </va-inner-loading>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useConfigStore } from '@/stores/configStore'
import { vikunjaService, type VikunjaTask } from '@/services/vikunjaApi'

const { t } = useI18n()
const configStore = useConfigStore()
const vikunjaTasks = ref<VikunjaTask[]>([])
const loadingTasks = ref(false)

const loading = computed(() => configStore.loading)
const error = computed(() => configStore.error)
const providers = computed(() => configStore.providers)
const vikunjaProjects = computed(() => configStore.config?.vikunjaProjects || [])
const vikunjaLabels = computed(() => configStore.config?.vikunjaLabels || [])

const taskColumns = computed(() => [
  { key: 'title', label: t('dashboard.taskTitle'), sortable: true },
  { key: 'priority', label: t('dashboard.priority'), sortable: true },
  { key: 'dueDate', label: t('dashboard.dueDate'), sortable: true }
])

function getPriorityColor(priority: number): string {
  if (priority >= 4) return 'danger'
  if (priority >= 3) return 'warning'
  if (priority >= 2) return 'info'
  return 'secondary'
}

function getPriorityLabel(priority: number): string {
  if (priority >= 4) return t('dashboard.priorityHigh')
  if (priority >= 3) return t('dashboard.priorityMedium')
  if (priority >= 2) return t('dashboard.priorityLow')
  return t('dashboard.priorityNone')
}

function formatDate(dateString: string): string {
  const date = new Date(dateString)
  return date.toLocaleDateString()
}

async function loadVikunjaData() {
  loadingTasks.value = true
  try {
    const allTasks: VikunjaTask[] = []
    const projects = await vikunjaService.getProjects()
    
    for (const project of projects) {
      try {
        const projectTasks = await vikunjaService.getTasks(project.id)
        allTasks.push(...projectTasks)
      } catch (err) {
        console.error(`Failed to load tasks for project ${project.id}:`, err)
      }
    }
    vikunjaTasks.value = allTasks
  } catch (err) {
    console.error('Failed to load Vikunja tasks:', err)
  } finally {
    loadingTasks.value = false
  }
}

onMounted(async () => {
  await configStore.loadConfig('default')
  await loadVikunjaData()
})
</script>

<style scoped>
.dashboard-container {
  padding: 1.5rem;
  max-width: 1400px;
  margin: 0 auto;
}

.dashboard-header {
  margin-bottom: 2rem;
}

.dashboard-header h1 {
  margin: 0 0 0.5rem 0;
}

.dashboard-header p {
  margin: 0;
  opacity: 0.7;
}

.error-alert {
  margin-bottom: 1.5rem;
}

/* Stats Grid */
.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 1rem;
  margin-bottom: 2rem;
}

.stats-card {
  transition: transform 0.2s;
}

.stats-card:hover {
  transform: translateY(-2px);
}

.stats-content {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.stats-info {
  flex: 1;
}

.stats-number {
  font-size: 2rem;
  font-weight: 600;
  line-height: 1;
  margin-bottom: 0.25rem;
}

.stats-label {
  opacity: 0.7;
}

/* Content Cards */
.content-card {
  margin-bottom: 1.5rem;
}

.card-title-content {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.table-cell-content {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

/* Responsive */
@media (max-width: 768px) {
  .dashboard-container {
    padding: 1rem;
  }

  .stats-grid {
    grid-template-columns: repeat(2, 1fr);
  }

  .stats-number {
    font-size: 1.5rem;
  }
}
</style>
