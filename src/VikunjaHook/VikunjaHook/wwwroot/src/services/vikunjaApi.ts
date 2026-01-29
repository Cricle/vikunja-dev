import axios from 'axios'

// MCP endpoint for Vikunja operations
const MCP_API_URL = '/mcp'

const mcpClient = axios.create({
  baseURL: MCP_API_URL,
  headers: {
    'Content-Type': 'application/json',
    'Accept': 'application/json'
  },
  // Don't transform response, we'll handle it manually
  transformResponse: [(data) => data]
})

// Helper function to parse SSE response from MCP
function parseSseResponse(sseText: string): unknown {
  // SSE format: "event: message\ndata: {...}"
  const lines = sseText.split('\n')
  for (const line of lines) {
    if (line.startsWith('data: ')) {
      return JSON.parse(line.substring(6))
    }
  }
  throw new Error('Invalid SSE response format')
}

// Helper function to call MCP tools
async function callMcpTool<T>(toolName: string, args: Record<string, unknown> = {}): Promise<T> {
  try {
    const response = await mcpClient.post('', {
      jsonrpc: '2.0',
      id: Date.now(),
      method: 'tools/call',
      params: {
        name: toolName,
        arguments: args
      }
    })
    
    // Check if response is valid
    if (!response.data) {
      throw new Error('Empty response from MCP server')
    }
    
    // Parse response data
    let data = response.data
    
    // If data is a string, try to parse it
    if (typeof data === 'string') {
      // Check if it's an HTML error page
      if (data.trim().startsWith('<') || data.startsWith('<!DOCTYPE')) {
        throw new Error('MCP server returned HTML instead of JSON. Check server configuration.')
      }
      
      // Check if it's a plain error message
      if (data.startsWith('An error') || data.startsWith('Error')) {
        throw new Error(data)
      }
      
      // Try to parse as JSON
      try {
        data = JSON.parse(data)
      } catch (e) {
        // If not JSON, try SSE format
        data = parseSseResponse(data)
      }
    }
    
    // Check for JSON-RPC error
    if (data.error) {
      throw new Error(data.error.message || JSON.stringify(data.error))
    }
    
    // Extract result
    if (!data.result) {
      throw new Error('No result in MCP response')
    }
    
    // Handle different result formats
    if (data.result.content && Array.isArray(data.result.content) && data.result.content.length > 0) {
      const content = data.result.content[0]
      if (content.text) {
        try {
          return JSON.parse(content.text)
        } catch (e) {
          // If parsing fails, return the text as-is
          return content.text as T
        }
      }
    }
    
    return data.result as T
  } catch (error) {
    const errorMessage = error instanceof Error ? error.message : String(error)
    console.error(`MCP tool call failed: ${toolName}`, errorMessage)
    throw new Error(`MCP ${toolName} failed: ${errorMessage}`)
  }
}

export interface VikunjaProject {
  id: number
  title: string
  description: string
  created: string
  updated: string
  owner?: {
    id: number
    username: string
    name: string
  }
}

export interface VikunjaTask {
  id: number
  title: string
  description: string
  done: boolean
  dueDate: string | null
  priority: number
  projectId: number
  created: string
  updated: string
  createdBy?: {
    id: number
    username: string
    name: string
  }
}

export interface VikunjaUser {
  id: number
  username: string
  name: string
  email: string
  created: string
  updated: string
}

export interface VikunjaLabel {
  id: number
  title: string
  description: string
  hexColor: string
  created: string
  updated: string
}

export const vikunjaService = {
  // Projects
  async getProjects(): Promise<VikunjaProject[]> {
    try {
      return await callMcpTool<VikunjaProject[]>('list_projects', {})
    } catch (error) {
      console.error('Failed to fetch projects:', error)
      return []
    }
  },

  async getProject(id: number): Promise<VikunjaProject | null> {
    try {
      return await callMcpTool<VikunjaProject>('get_project', { projectId: id })
    } catch (error) {
      console.error(`Failed to fetch project ${id}:`, error)
      return null
    }
  },

  // Tasks
  async getTasks(projectId?: number): Promise<VikunjaTask[]> {
    try {
      return await callMcpTool<VikunjaTask[]>('list_tasks', projectId ? { projectId } : {})
    } catch (error) {
      console.error('Failed to fetch tasks:', error)
      return []
    }
  },

  async getTask(id: number): Promise<VikunjaTask | null> {
    try {
      return await callMcpTool<VikunjaTask>('get_task', { taskId: id })
    } catch (error) {
      console.error(`Failed to fetch task ${id}:`, error)
      return null
    }
  },

  // Users
  async getCurrentUser(): Promise<VikunjaUser | null> {
    try {
      return await callMcpTool<VikunjaUser>('get_current_user', {})
    } catch (error) {
      console.error('Failed to fetch current user:', error)
      return null
    }
  },

  async getUsers(): Promise<VikunjaUser[]> {
    try {
      // search_users requires a search parameter, use empty string to get all
      return await callMcpTool<VikunjaUser[]>('search_users', { search: '' })
    } catch (error) {
      console.error('Failed to fetch users:', error)
      return []
    }
  },

  // Labels
  async getLabels(): Promise<VikunjaLabel[]> {
    try {
      return await callMcpTool<VikunjaLabel[]>('list_labels', {})
    } catch (error) {
      console.error('Failed to fetch labels:', error)
      return []
    }
  },

  // Statistics
  async getStatistics(): Promise<VikunjaUser | null> {
    try {
      // Use current user as a proxy for statistics
      return await this.getCurrentUser()
    } catch (error) {
      console.error('Failed to fetch statistics:', error)
      return null
    }
  }
}

// Push History API
export interface ProviderPushResult {
  providerType: string
  success: boolean
  message?: string
  timestamp: string
  notificationContent?: {
    title: string
    body: string
    format: string
  }
}

export interface PushEventRecord {
  id: string
  eventName: string
  timestamp: string
  eventData: {
    title: string
    body: string
    format: string
  }
  providers: ProviderPushResult[]
}

export interface PushHistoryResponse {
  records: PushEventRecord[]
  totalCount: number
}

const apiClient = axios.create({
  baseURL: '/api',
  headers: {
    'Content-Type': 'application/json'
  }
})

export const pushHistoryService = {
  async getHistory(count?: number): Promise<PushHistoryResponse> {
    const response = await apiClient.get<PushHistoryResponse>('/push-history', {
      params: { count }
    })
    return response.data
  },

  async clearHistory(): Promise<void> {
    await apiClient.delete('/push-history')
  }
}
