import axios from 'axios'
import type { ServerHealth, ServerInfo, Tool, Configuration } from '../types'

const api = axios.create({
  baseURL: '/api',
  timeout: 10000
})

export const serverApi = {
  async getHealth(): Promise<ServerHealth> {
    const response = await api.get<ServerHealth>('/mcp/health')
    return response.data
  },

  async getInfo(): Promise<ServerInfo> {
    const response = await api.get<ServerInfo>('/mcp/info')
    return response.data
  },

  async getTools(): Promise<{ tools: Tool[]; count: number }> {
    const response = await api.get<{ tools: Tool[]; count: number }>('/mcp/tools')
    return response.data
  }
}

export const configApi = {
  async getConfiguration(): Promise<Configuration> {
    const response = await api.get<Configuration>('/configuration')
    return response.data
  },

  async updateConfiguration(config: Configuration): Promise<void> {
    await api.put('/configuration', config)
  }
}

export default api
