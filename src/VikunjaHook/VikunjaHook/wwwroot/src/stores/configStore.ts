import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { UserConfig, ProviderConfig, NotificationTemplate } from '@/types/config'
import { apiService } from '@/services/api'

export const useConfigStore = defineStore('config', () => {
  const config = ref<UserConfig | null>(null)
  const loading = ref(false)
  const error = ref<string | null>(null)

  const userId = computed(() => config.value?.userId || '')
  const providers = computed(() => config.value?.providers || [])
  const defaultProviders = computed(() => config.value?.defaultProviders || [])
  const templates = computed(() => config.value?.templates || {})

  async function loadConfig(userId: string) {
    loading.value = true
    error.value = null
    try {
      const loadedConfig = await apiService.getUserConfig(userId)
      
      // If config doesn't exist, create a default one
      if (!loadedConfig) {
        config.value = {
          userId: userId,
          providers: [],
          defaultProviders: [],
          templates: {},
          lastModified: new Date().toISOString()
        }
      } else {
        config.value = loadedConfig
      }
    } catch (err: unknown) {
      // If 404, create default config
      const isAxiosError = err && typeof err === 'object' && 'response' in err
      if (isAxiosError && (err as { response?: { status?: number } }).response?.status === 404) {
        config.value = {
          userId: userId,
          providers: [],
          defaultProviders: [],
          templates: {},
          lastModified: new Date().toISOString()
        }
      } else {
        error.value = err instanceof Error ? err.message : 'Failed to load configuration'
        throw err
      }
    } finally {
      loading.value = false
    }
  }

  async function saveConfig() {
    if (!config.value) {
      throw new Error('No configuration to save')
    }

    loading.value = true
    error.value = null
    try {
      // Update lastModified before saving
      config.value.lastModified = new Date().toISOString()
      config.value = await apiService.updateUserConfig(config.value.userId, config.value)
    } catch (err: unknown) {
      error.value = err instanceof Error ? err.message : 'Failed to save configuration'
      throw err
    } finally {
      loading.value = false
    }
  }

  function addProvider(provider: ProviderConfig) {
    if (!config.value) return
    config.value.providers.push(provider)
  }

  function removeProvider(providerType: string) {
    if (!config.value) return
    config.value.providers = config.value.providers.filter(p => p.providerType !== providerType)
    // Also remove from defaultProviders if present
    config.value.defaultProviders = config.value.defaultProviders.filter(p => p !== providerType)
  }

  function updateProvider(providerType: string, updates: Partial<ProviderConfig>) {
    if (!config.value) return
    const provider = config.value.providers.find(p => p.providerType === providerType)
    if (provider) {
      Object.assign(provider, updates)
    }
  }

  function setDefaultProviders(providerTypes: string[]) {
    if (!config.value) return
    config.value.defaultProviders = providerTypes
  }

  function setTemplate(eventType: string, template: NotificationTemplate) {
    if (!config.value) return
    config.value.templates[eventType] = template
  }

  function removeTemplate(eventType: string) {
    if (!config.value) return
    delete config.value.templates[eventType]
  }

  return {
    config,
    loading,
    error,
    userId,
    providers,
    defaultProviders,
    templates,
    loadConfig,
    saveConfig,
    addProvider,
    removeProvider,
    updateProvider,
    setDefaultProviders,
    setTemplate,
    removeTemplate
  }
})
