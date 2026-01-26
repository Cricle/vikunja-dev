import { defineStore } from 'pinia'
import { ref } from 'vue'
import { serverApi } from '../services/api'
import type { ServerHealth, ServerInfo, Tool } from '../types'

export const useServerStore = defineStore('server', () => {
  const health = ref<ServerHealth | null>(null)
  const info = ref<ServerInfo | null>(null)
  const tools = ref<Tool[]>([])
  const loading = ref(false)
  const error = ref<string | null>(null)

  async function checkHealth() {
    loading.value = true
    error.value = null
    try {
      health.value = await serverApi.getHealth()
    } catch (e: any) {
      error.value = e.message
      throw e
    } finally {
      loading.value = false
    }
  }

  async function fetchInfo() {
    loading.value = true
    error.value = null
    try {
      info.value = await serverApi.getInfo()
    } catch (e: any) {
      error.value = e.message
    } finally {
      loading.value = false
    }
  }

  async function fetchTools() {
    loading.value = true
    error.value = null
    try {
      const response = await serverApi.getTools()
      tools.value = response.tools
    } catch (e: any) {
      error.value = e.message
    } finally {
      loading.value = false
    }
  }

  return {
    health,
    info,
    tools,
    loading,
    error,
    checkHealth,
    fetchInfo,
    fetchTools
  }
})
