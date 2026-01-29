<template>
  <div class="provider-config-container">
    <va-inner-loading :loading="loading">
      <!-- Header -->
      <div class="config-header">
        <div class="header-content">
          <div class="header-text">
            <h1 class="config-title">{{ t('providers.title') }}</h1>
            <p class="config-subtitle">{{ t('providers.description') }}</p>
          </div>
          <div class="header-actions">
            <va-button @click="showAddModal = true" icon="add" color="primary" size="large">
              {{ t('providers.add') }}
            </va-button>
          </div>
        </div>
      </div>

      <!-- Default Providers Selection -->
      <va-card v-if="providers.length > 0" class="default-providers-card">
        <va-card-title>
          <div class="card-title-content">
            <va-icon name="star" color="warning" />
            <span>{{ t('providers.defaultProviders') }}</span>
          </div>
        </va-card-title>
        <va-card-content>
          <p class="default-providers-description">{{ t('providers.defaultProvidersDescription') }}</p>
          <div class="default-providers-selection">
            <va-chip
              v-for="provider in providers"
              :key="provider.providerType"
              :color="isDefaultProvider(provider.providerType) ? 'primary' : 'secondary'"
              :outline="!isDefaultProvider(provider.providerType)"
              size="large"
              @click="toggleDefaultProvider(provider.providerType)"
              class="provider-chip"
            >
              <va-icon 
                :name="isDefaultProvider(provider.providerType) ? 'check_circle' : 'radio_button_unchecked'" 
                size="small"
              />
              {{ provider.settings.name || provider.providerType }}
            </va-chip>
          </div>
          <va-button
            v-if="hasDefaultProvidersChanged"
            @click="saveDefaultProviders"
            icon="save"
            color="primary"
            size="small"
            class="save-defaults-button"
          >
            {{ t('common.save') }}
          </va-button>
        </va-card-content>
      </va-card>

      <!-- Providers List -->
      <div v-if="providers.length > 0" class="providers-list">
        <va-card
          v-for="provider in providers"
          :key="provider.providerType"
          class="provider-card"
        >
          <va-card-content>
            <div class="provider-content">
              <div class="provider-main">
                <div class="provider-icon">
                  <va-avatar color="primary" size="large">
                    <va-icon name="notifications" size="large" />
                  </va-avatar>
                </div>
                <div class="provider-info">
                  <div class="provider-header-row">
                    <h3 class="provider-name">{{ provider.settings.name || provider.providerType }}</h3>
                    <va-chip 
                      :color="provider.settings.enabled !== 'false' ? 'success' : 'secondary'" 
                      size="small"
                    >
                      {{ provider.settings.enabled !== 'false' ? t('providers.enabled') : t('providers.disabled') }}
                    </va-chip>
                  </div>
                  <p class="provider-type">{{ provider.providerType }}</p>
                  <div class="provider-settings-inline">
                    <va-chip
                      v-for="(value, key) in provider.settings"
                      :key="key"
                      v-show="key !== 'name' && key !== 'enabled'"
                      size="small"
                      outline
                    >
                      {{ key }}: {{ maskSensitive(key, value) }}
                    </va-chip>
                  </div>
                </div>
              </div>
              <div class="provider-actions">
                <va-button
                  @click="testProvider(provider)"
                  icon="send"
                  preset="secondary"
                  size="small"
                >
                  {{ t('providers.test') }}
                </va-button>
                <va-button
                  @click="editProvider(provider)"
                  icon="edit"
                  preset="secondary"
                  size="small"
                >
                  {{ t('common.edit') }}
                </va-button>
                <va-button
                  @click="confirmDelete(provider)"
                  icon="delete"
                  color="danger"
                  preset="secondary"
                  size="small"
                >
                  {{ t('common.delete') }}
                </va-button>
              </div>
            </div>
          </va-card-content>
        </va-card>
      </div>

      <!-- Empty State -->
      <va-card v-else class="empty-state-card">
        <va-card-content>
          <div class="empty-state">
            <va-icon name="notifications_off" size="4rem" color="secondary" />
            <h3 class="empty-title">{{ t('providers.noProviders') }}</h3>
            <p class="empty-description">{{ t('providers.noProvidersDescription') }}</p>
            <va-button @click="showAddModal = true" icon="add" color="primary" size="large">
              {{ t('providers.add') }}
            </va-button>
          </div>
        </va-card-content>
      </va-card>
    </va-inner-loading>

    <!-- Add/Edit Modal -->
    <va-modal
      v-model="showAddModal"
      :title="editingProvider ? t('providers.edit') : t('providers.add')"
      size="medium"
      @ok="saveProvider"
      @cancel="cancelEdit"
    >
      <div class="modal-form">
        <va-input
          v-model="formData.settings.name"
          :label="t('providers.name')"
          class="form-field"
        >
          <template #prepend>
            <va-icon name="label" />
          </template>
        </va-input>
        
        <va-select
          v-model="formData.providerType"
          :label="t('providers.type')"
          :options="availableProviders"
          :no-options-text="t('common.noOptions')"
          :disabled="!!editingProvider"
          class="form-field"
        >
          <template #prepend>
            <va-icon name="category" />
          </template>
        </va-select>
        
        <va-switch
          v-model="formData.settings.enabled"
          :label="t('providers.enabled')"
          true-value="true"
          false-value="false"
          class="form-field"
        />
        
        <va-input
          v-if="formData.providerType === 'pushdeer'"
          v-model="formData.settings.pushkey"
          :label="t('providers.pushkey')"
          type="password"
          class="form-field"
        >
          <template #prepend>
            <va-icon name="key" />
          </template>
        </va-input>
      </div>
    </va-modal>

    <!-- Test Result Modal -->
    <va-modal
      v-model="showTestModal"
      :title="t('providers.test')"
      size="small"
      hide-default-actions
    >
      <va-alert
        v-if="testResult"
        :color="testResult.success ? 'success' : 'danger'"
        border="left"
        class="test-result-alert"
      >
        <div class="test-result-content">
          <va-icon 
            :name="testResult.success ? 'check_circle' : 'error'" 
            size="large"
          />
          <div>
            <strong>{{ testResult.success ? t('providers.testSuccess') : t('providers.testFailed') }}</strong>
            <div v-if="testResult.errorMessage" class="error-message">
              {{ testResult.errorMessage }}
            </div>
          </div>
        </div>
      </va-alert>
      <template #footer>
        <va-button @click="showTestModal = false" color="primary">
          {{ t('common.close') }}
        </va-button>
      </template>
    </va-modal>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useConfigStore } from '@/stores/configStore'
import { apiService } from '@/services/api'
import type { NotificationResult } from '@/types/config'
import { useToast } from 'vuestic-ui'

const { t } = useI18n()
const { init: notify } = useToast()
const configStore = useConfigStore()

const providers = computed(() => configStore.providers)
const loading = computed(() => configStore.loading)
const defaultProviders = computed(() => configStore.defaultProviders)

const localDefaultProviders = ref<string[]>([])
const hasDefaultProvidersChanged = computed(() => {
  return JSON.stringify(localDefaultProviders.value.sort()) !== JSON.stringify(defaultProviders.value.sort())
})

const columns = computed(() => [
  { key: 'name', label: t('providers.name'), sortable: true },
  { key: 'providerType', label: t('providers.type'), sortable: true },
  { key: 'enabled', label: t('providers.enabled'), sortable: true },
  { key: 'settings', label: t('providers.settings'), sortable: false },
  { key: 'actions', label: t('common.actions'), sortable: false }
])

const showAddModal = ref(false)
const showTestModal = ref(false)
const editingProvider = ref<typeof formData.value | null>(null)
const testResult = ref<NotificationResult | null>(null)

const formData = ref({
  providerType: 'pushdeer',
  settings: {
    name: '',
    enabled: 'true',
    pushkey: ''
  }
})

const availableProviders = ['pushdeer']

function maskSensitive(key: string, value: string): string {
  // Skip internal fields
  if (key === 'name' || key === 'enabled') {
    return value
  }
  if (key.includes('key') || key.includes('token') || key.includes('password')) {
    return '••••••••'
  }
  return value
}

function isDefaultProvider(providerType: string): boolean {
  return localDefaultProviders.value.includes(providerType)
}

function toggleDefaultProvider(providerType: string) {
  const index = localDefaultProviders.value.indexOf(providerType)
  if (index > -1) {
    localDefaultProviders.value.splice(index, 1)
  } else {
    localDefaultProviders.value.push(providerType)
  }
}

async function saveDefaultProviders() {
  try {
    configStore.setDefaultProviders(localDefaultProviders.value)
    await configStore.saveConfig()
    
    notify({
      message: t('providers.defaultProvidersSaved'),
      color: 'success'
    })
  } catch (error: unknown) {
    console.error('Failed to save default providers:', error)
    notify({
      message: t('common.error') + ': ' + (error instanceof Error ? error.message : 'Unknown error'),
      color: 'danger'
    })
  }
}

function editProvider(provider: typeof formData.value) {
  editingProvider.value = provider
  formData.value = {
    providerType: provider.providerType,
    settings: { ...provider.settings }
  }
  showAddModal.value = true
}

function cancelEdit() {
  editingProvider.value = null
  formData.value = {
    providerType: 'pushdeer',
    settings: {
      name: '',
      enabled: 'true',
      pushkey: ''
    }
  }
}

async function saveProvider() {
  try {
    const providerData = {
      providerType: formData.value.providerType,
      settings: { ...formData.value.settings }
    }
    
    if (editingProvider.value) {
      // Update existing provider
      configStore.updateProvider(editingProvider.value.providerType, providerData)
    } else {
      // Add new provider
      configStore.addProvider(providerData)
    }
    
    // Save to backend immediately
    await configStore.saveConfig()
    
    notify({
      message: t('providers.saveSuccess'),
      color: 'success'
    })
    
    cancelEdit()
    showAddModal.value = false
  } catch (error: unknown) {
    console.error('Failed to save provider:', error)
    notify({
      message: t('providers.saveFailed') + ': ' + (error instanceof Error ? error.message : 'Unknown error'),
      color: 'danger'
    })
  }
}

async function confirmDelete(provider: typeof formData.value) {
  if (confirm(t('providers.confirmDelete'))) {
    try {
      configStore.removeProvider(provider.providerType)
      await configStore.saveConfig()
      
      notify({
        message: t('common.success'),
        color: 'success'
      })
    } catch (error: unknown) {
      console.error('Failed to delete provider:', error)
      notify({
        message: t('common.error') + ': ' + (error instanceof Error ? error.message : 'Unknown error'),
        color: 'danger'
      })
    }
  }
}

async function testProvider(provider: typeof formData.value) {
  try {
    testResult.value = await apiService.testNotification(configStore.userId, {
      providerType: provider.providerType,
      title: 'Test Notification',
      body: 'This is a test notification from Vikunja Webhook System'
    })
    showTestModal.value = true
  } catch (err: unknown) {
    console.error('Test notification error:', err)
    testResult.value = {
      success: false,
      errorMessage: err instanceof Error ? err.message : 'Unknown error',
      timestamp: new Date().toISOString()
    }
    showTestModal.value = true
  }
}

onMounted(async () => {
  // Load config first
  try {
    await configStore.loadConfig('default')
    // Initialize local default providers
    localDefaultProviders.value = [...defaultProviders.value]
  } catch (error) {
    console.error('Failed to load config:', error)
    notify({
      message: t('common.error') + ': Failed to load configuration',
      color: 'danger'
    })
  }
})
</script>

<style scoped>
.provider-config-container {
  padding: 2rem;
  max-width: 1200px;
  margin: 0 auto;
}

.config-header {
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

.config-title {
  margin: 0 0 0.5rem 0;
}

.config-subtitle {
  margin: 0;
}

.default-providers-card {
  margin-bottom: 2rem;
  border-radius: 12px;
}

.card-title-content {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.default-providers-description {
  margin: 0 0 1rem 0;
  color: var(--va-text-secondary);
}

.default-providers-selection {
  display: flex;
  flex-wrap: wrap;
  gap: 1rem;
  margin-bottom: 1.5rem;
}

.provider-chip {
  cursor: pointer;
  transition: all 0.2s ease;
  padding: 0.5rem 1rem;
}

.provider-chip:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
}

.provider-chip .va-icon {
  margin-right: 0.5rem;
}

.save-defaults-button {
  margin-top: 0.5rem;
}

.providers-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.provider-card {
  border-radius: 12px;
}

.provider-content {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 2rem;
  flex-wrap: wrap;
}

.provider-main {
  display: flex;
  align-items: center;
  gap: 1.5rem;
  flex: 1;
  min-width: 300px;
}

.provider-icon {
  flex-shrink: 0;
}

.provider-info {
  flex: 1;
  min-width: 0;
}

.provider-header-row {
  display: flex;
  align-items: center;
  gap: 1rem;
  margin-bottom: 0.5rem;
  flex-wrap: wrap;
}

.provider-name {
  margin: 0;
  font-size: 1.125rem;
}

.provider-type {
  margin: 0 0 0.75rem 0;
  text-transform: uppercase;
  font-size: 0.8125rem;
  color: var(--va-text-secondary);
}

.provider-settings-inline {
  display: flex;
  flex-wrap: wrap;
  gap: 0.75rem;
}

.provider-actions {
  display: flex;
  gap: 0.75rem;
  flex-shrink: 0;
  flex-wrap: wrap;
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
  gap: 1.75rem;
}

.form-field {
  width: 100%;
}

.test-result-alert {
  margin-bottom: 1rem;
}

.test-result-content {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.error-message {
  margin-top: 0.5rem;
}

@media (max-width: 768px) {
  .provider-config-container {
    padding: 1rem;
  }

  .config-title {
  }

  .provider-content {
    flex-direction: column;
    align-items: stretch;
  }

  .provider-main {
    min-width: 100%;
  }

  .provider-actions {
    width: 100%;
    justify-content: stretch;
  }

  .provider-actions button {
    flex: 1;
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
}
</style>
