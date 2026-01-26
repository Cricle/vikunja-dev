import axios from 'axios'
import type { 
  ServerHealth, 
  ServerInfo, 
  Tool, 
  Configuration, 
  Session, 
  ServerStats, 
  LogEntry,
  ToolExecutionRequest,
  ToolExecutionResult
} from '../types'

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

export const adminApi = {
  async getSessions(): Promise<Session[]> {
    try {
      const response = await api.get<{ sessions: Session[]; count: number }>('/admin/sessions')
      return response.data.sessions
    } catch (error) {
      console.error('Failed to fetch sessions:', error)
      throw error
    }
  },

  async disconnectSession(sessionId: string): Promise<void> {
    try {
      await api.delete(`/admin/sessions/${sessionId}`)
    } catch (error) {
      console.error(`Failed to disconnect session ${sessionId}:`, error)
      throw error
    }
  },

  async disconnectAllSessions(): Promise<void> {
    try {
      await api.delete('/admin/sessions')
    } catch (error) {
      console.error('Failed to disconnect all sessions:', error)
      throw error
    }
  },

  async getStats(): Promise<ServerStats> {
    try {
      const response = await api.get<ServerStats>('/admin/stats')
      return response.data
    } catch (error) {
      console.error('Failed to fetch server stats:', error)
      throw error
    }
  },

  async executeTool(request: ToolExecutionRequest): Promise<ToolExecutionResult> {
    try {
      const { toolName, subcommand, parameters, sessionId } = request
      const headers: any = {}
      if (sessionId) {
        headers['X-Session-Id'] = sessionId
      }
      
      const response = await api.post<{ success: boolean; tool: string; subcommand: string; data: any }>(
        `/admin/tools/${toolName}/${subcommand}`,
        parameters,
        { headers }
      )
      
      return {
        success: response.data.success,
        result: response.data.data
      }
    } catch (error: any) {
      console.error('Failed to execute tool:', error)
      return {
        success: false,
        error: error.response?.data?.error || error.message || 'Unknown error'
      }
    }
  },

  async getLogs(count: number = 100, level?: string): Promise<LogEntry[]> {
    try {
      const params: any = { count }
      if (level && level !== 'All') {
        params.level = level
      }
      const response = await api.get<{ logs: LogEntry[]; count: number }>('/admin/logs', { params })
      return response.data.logs
    } catch (error) {
      console.error('Failed to fetch logs:', error)
      throw error
    }
  },

  async clearLogs(): Promise<void> {
    try {
      await api.delete('/admin/logs')
    } catch (error) {
      console.error('Failed to clear logs:', error)
      throw error
    }
  }
}

export default api
