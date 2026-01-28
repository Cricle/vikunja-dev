<template>
  <div class="dashboard-container">
    <va-inner-loading :loading="loading">
      <!-- Header Section -->
      <div class="dashboard-header">
        <div class="header-content">
          <div class="header-text">
            <h1 class="dashboard-title">{{ t('dashboard.title') }}</h1>
            <p class="dashboard-subtitle">{{ t('dashboard.welcome') }}</p>
          </div>
          <div class="header-actions">
            <va-button
              v-if="!config"
              @click="loadUserConfig"
              :disabled="!userIdInput"
              color="primary"
              icon="refresh"
            >
              {{ t('common.loading') }}
            </va-button>
          </div>
        </div>
      </div>

      <va-alert v-if="error" color="danger" class="error-alert">
        {{ error }}
      </va-alert>

      <div v-if="config" class="dashboard-content">
        <!-- Stats Cards -->
        <div class="stats-grid">
          <va-card class="stats-card stats-card--primary">
            <va-card-content>
              <div class="stats-content">
                <div class="stats-icon">
                  <va-icon name="notifications" size="2rem" />
                </div>
                <div class="stats-info">
                  <div class="stats-number">{{ providers.length }}</div>
                  <div class="stats-label">{{ t('dashboard.stats.providers') }}</div>
                </div>
              </div>
            </va-card-content>
          </va-card>

          <va-card class="stats-card stats-card--success">
            <va-card-content>
              <div class="stats-content">
                <div class="stats-icon">
                  <va-icon name="folder" size="2rem" />
                </div>
                <div class="stats-info">
                  <div class="stats-number">{{ vikunjaProjects.length }}</div>
                  <div class="stats-label">{{ t('dashboard.stats.projects') }}</div>
                </div>
              </div>
            </va-card-content>
          </va-card>

          <va-card class="stats-card stats-card--info">
            <va-card-content>
              <div class="stats-content">
                <div class="stats-icon">
                  <va-icon name="task" size="2rem" />
                </div>
                <div class="stats-info">
                  <div class="stats-number">{{ vikunjaTasks.length }}</div>
                  <div class="stats-label">{{ t('dashboard.stats.tasks') }}</div>
                </div>
              </div>
            </va-card-content>
          </va-card>

          <va-card class="stats-card stats-card--warning">
            <va-card-content>
              <div class="stats-content">
                <div class="stats-icon">
                  <va-icon name="label" size="2rem" />
                </div>
                <div class="stats-info">
                  <div class="stats-number">{{ vikunjaLabels.length }}</div>
                  <div class="stats-label">{{ t('dashboard.stats.labels') }}</div>
                </div>
              </div>
            </va-card-content>
          </va-card>
        </div>

        <!-- Current User Card -->
        <va-card v-if="currentUser" class="user-card">
          <va-card-title>
            <div class="card-title-content">
              <va-icon name="person" />
              <span>{{ t('dashboard.currentUser') }}</span>
            </div>
          </va-card-title>
          <va-card-content>
            <div class="user-info">
              <va-avatar size="large" color="primary">
                <va-icon name="person" size="large" />
              </va-avatar>
              <div class="user-details">
                <h3 class="user-name">{{ currentUser.name || currentUser.username }}</h3>
                <p class="user-email">{{ currentUser.email }}</p>
                <div class="user-badges">
                  <va-chip color="success" size="small">{{ t('dashboard.userActive') }}</va-chip>
                  <va-chip color="info" size="small">ID: {{ currentUser.id }}</va-chip>
                </div>
              </div>
            </div>
          </va-card-content>
        </va-card>

        <!-- Vikunja Projects Card -->
        <va-card class="content-card">
          <va-card-title>
            <div class="card-title-content">
              <va-icon name="folder" />
              <span>{{ t('dashboard.vikunjaProjects') }}</span>
            </div>
          </va-card-title>
          <va-card-content>
            <va-inner-loading :loading="loadingProjects">
              <va-alert v-if="vikunjaProjects.length === 0" color="info" border="left">
                {{ t('dashboard.noProjects') }}
              </va-alert>
              <va-data-table
                v-else
                :items="vikunjaProjects"
                :columns="projectColumns"
                striped
                hoverable
              >
                <template #cell(title)="{ rowData }">
                  <div class="table-cell-content">
                    <va-icon name="folder" size="small" />
                    <strong>{{ rowData.title }}</strong>
                  </div>
                </template>
                <template #cell(description)="{ rowData }">
                  <span>{{ rowData.description || '-' }}</span>
                </template>
                <template #cell(owner)="{ rowData }">
                  <va-chip v-if="rowData.owner" size="small" color="primary">
                    {{ rowData.owner.name || rowData.owner.username }}
                  </va-chip>
                  <span v-else>-</span>
                </template>
              </va-data-table>
            </va-inner-loading>
          </va-card-content>
        </va-card>

        <!-- Recent Tasks Card -->
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

        <!-- Providers Card -->
        <va-card class="content-card">
          <va-card-title>
            <div class="card-title-content">
              <va-icon name="notifications" />
              <span>{{ t('nav.providers') }}</span>
            </div>
          </va-card-title>
          <va-card-content>
            <va-alert v-if="providers.length === 0" color="info" border="left" class="mb-3">
              {{ t('providers.add') }}
            </va-alert>
            <va-button v-if="providers.length === 0" @click="$router.push('/providers')" icon="add" color="primary">
              {{ t('providers.add') }}
            </va-button>
            <div v-else class="providers-list">
              <va-chip
                v-for="provider in providers"
                :key="provider.providerType"
                color="primary"
                size="medium"
              >
                <va-icon name="notifications" size="small" />
                {{ provider.name || provider.providerType }}
              </va-chip>
            </div>
          </va-card-content>
        </va-card>
      </div>

      <!-- Initial Setup -->
      <div v-else class="setup-container">
        <va-card class="setup-card">
          <va-card-content>
            <div class="setup-content">
              <va-icon name="dashboard" size="4rem" color="primary" class="setup-icon" />
              <h2 class="setup-title">{{ t('dashboard.welcome') }}</h2>
              <p class="setup-description">{{ t('dashboard.setupDescription') }}</p>
              <va-input
                v-model="userIdInput"
                :label="t('dashboard.userIdLabel')"
                :placeholder="t('dashboard.userIdPlaceholder')"
                @keyup.enter="loadUserConfig"
                class="setup-input"
              />
              <va-button @click="loadUserConfig" :disabled="!userIdInput" color="primary" size="large">
                {{ t('common.loading') }}
              </va-button>
            </div>
          </va-card-content>
        </va-card>
      </div>
    </va-inner-loading>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useConfigStore } from '@/stores/configStore'
import { vikunjaService, type VikunjaProject, type VikunjaTask, type VikunjaUser, type VikunjaLabel } from '@/services/vikunjaApi'

const { t } = useI18n()
const configStore = useConfigStore()
const userIdInput = ref('default')
const vikunjaProjects = ref<VikunjaProject[]>([])
const vikunjaTasks = ref<VikunjaTask[]>([])
const vikunjaLabels = ref<VikunjaLabel[]>([])
const currentUser = ref<VikunjaUser | null>(null)
const loadingProjects = ref(false)
const loadingTasks = ref(false)

const config = computed(() => configStore.config)
const loading = computed(() => configStore.loading)
const error = computed(() => configStore.error)
const providers = computed(() => configStore.providers)

const projectColumns = computed(() => [
  { key: 'id', label: 'ID', sortable: true },
  { key: 'title', label: t('projects.projectName'), sortable: true },
  { key: 'description', label: t('providers.description'), sortable: false },
  { key: 'owner', label: t('dashboard.owner'), sortable: false }
])

const taskColumns = computed(() => [
  { key: 'id', label: 'ID', sortable: true },
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

async function loadUserConfig() {
  if (!userIdInput.value) return
  try {
    await configStore.loadConfig(userIdInput.value)
    await loadVikunjaData()
  } catch (err) {
    console.error('Failed to load config:', err)
  }
}

async function loadVikunjaData() {
  loadingProjects.value = true
  try {
    vikunjaProjects.value = await vikunjaService.getProjects()
  } catch (err) {
    console.error('Failed to load Vikunja projects:', err)
  } finally {
    loadingProjects.value = false
  }

  loadingTasks.value = true
  try {
    // Get tasks from all projects
    const allTasks: VikunjaTask[] = []
    for (const project of vikunjaProjects.value) {
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

  try {
    vikunjaLabels.value = await vikunjaService.getLabels()
  } catch (err) {
    console.error('Failed to load Vikunja labels:', err)
  }

  try {
    currentUser.value = await vikunjaService.getCurrentUser()
  } catch (err) {
    console.error('Failed to load current user:', err)
  }
}

onMounted(() => {
  loadUserConfig()
})
</script>

<style scoped>
.dashboard-container {
  padding: 1.5rem;
  width: 100%;
}

.dashboard-header {
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

.dashboard-title {
  margin: 0 0 0.5rem 0;
}

.dashboard-subtitle {
  margin: 0;
}

.error-alert {
  margin-bottom: 1.5rem;
}

.dashboard-content {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

/* Stats Grid */
.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
  gap: 1.5rem;
  margin-bottom: 1rem;
}

.stats-card {
  border-radius: 12px;
  transition: transform 0.2s, box-shadow 0.2s;
}

.stats-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.12);
}

.stats-card--primary {
}

.stats-card--success {
}

.stats-card--info {
}

.stats-card--warning {
}

.stats-content {
  display: flex;
  align-items: center;
  gap: 1.5rem;
}

.stats-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 60px;
  height: 60px;
  border-radius: 12px;
}

.stats-info {
  flex: 1;
}

.stats-number {
  margin-bottom: 0.25rem;
}

.stats-label {
}

/* Content Cards */
.content-card {
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
}

.card-title-content {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

/* User Card */
.user-card {
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
}

.user-info {
  display: flex;
  align-items: center;
  gap: 1.5rem;
}

.user-details {
  flex: 1;
}

.user-name {
  margin: 0 0 0.25rem 0;
}

.user-email {
  margin: 0 0 0.75rem 0;
}

.user-badges {
  display: flex;
  gap: 0.5rem;
  flex-wrap: wrap;
}

/* Table Cells */
.table-cell-content {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

/* Providers List */
.providers-list {
  display: flex;
  flex-wrap: wrap;
  gap: 0.75rem;
}

/* Setup Container */
.setup-container {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 60vh;
}

.setup-card {
  max-width: 500px;
  width: 100%;
  border-radius: 16px;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.1);
}

.setup-content {
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  padding: 2rem;
}

.setup-icon {
  margin-bottom: 1.5rem;
}

.setup-title {
  margin: 0 0 0.5rem 0;
}

.setup-description {
  margin: 0 0 2rem 0;
}

.setup-input {
  width: 100%;
  margin-bottom: 1.5rem;
}

/* Responsive */
@media (max-width: 768px) {
  .dashboard-container {
    padding: 1rem;
  }

  .dashboard-title {
  }

  .stats-grid {
    grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
    gap: 1rem;
  }

  .stats-content {
    flex-direction: column;
    text-align: center;
    gap: 1rem;
  }

  .stats-number {
  }

  .user-info {
    flex-direction: column;
    text-align: center;
  }

  .header-content {
    flex-direction: column;
    align-items: flex-start;
  }
}
</style>
