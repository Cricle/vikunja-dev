import { defineStore } from 'pinia'
import { ref } from 'vue'
import { configApi } from '../services/api'
import type { Configuration } from '../types'

export const useConfigStore = defineStore('config', () => {
  const configuration = ref<Configuration | null>(null)
  const loading = ref(false)
  const error = ref<string | null>(null)

  async function fetchConfiguration() {
    loading.value = true
    error.value = null
    try {
      configuration.value = await configApi.getConfiguration()
    } catch (e: any) {
      error.value = e.message
    } finally {
      loading.value = false
    }
  }

  async function updateConfiguration(config: Configuration) {
    loading.value = true
    error.value = null
    try {
      await configApi.updateConfiguration(config)
      configuration.value = config
    } catch (e: any) {
      error.value = e.message
      throw e
    } finally {
      loading.value = false
    }
  }

  return {
    configuration,
    loading,
    error,
    fetchConfiguration,
    updateConfiguration
  }
})
