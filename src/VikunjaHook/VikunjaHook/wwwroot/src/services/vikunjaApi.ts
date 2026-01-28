import axios from 'axios'

// MCP endpoint for Vikunja operations
const MCP_API_URL = '/mcp'

const mcpClient = axios.create({
  baseURL: MCP_API_URL,
  headers: {
    'Content-Type': 'application/json'
  }
})

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
    
    if (response.data.error) {
      throw new Error(response.data.error.message)
    }
    
    return response.data.result.content[0].text ? JSON.parse(response.data.result.content[0].text) : response.data.result
  } catch (error) {
    console.error(`MCP tool call failed: ${toolName}`, error)
    throw error
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
      return await callMcpTool<VikunjaProject[]>('ListProjects', {})
    } catch (error) {
      console.error('Failed to fetch projects:', error)
      return []
    }
  },

  async getProject(id: number): Promise<VikunjaProject | null> {
    try {
      return await callMcpTool<VikunjaProject>('GetProject', { projectId: id })
    } catch (error) {
      console.error(`Failed to fetch project ${id}:`, error)
      return null
    }
  },

  // Tasks
  async getTasks(projectId?: number): Promise<VikunjaTask[]> {
    try {
      return await callMcpTool<VikunjaTask[]>('ListTasks', projectId ? { projectId } : {})
    } catch (error) {
      console.error('Failed to fetch tasks:', error)
      return []
    }
  },

  async getTask(id: number): Promise<VikunjaTask | null> {
    try {
      return await callMcpTool<VikunjaTask>('GetTask', { taskId: id })
    } catch (error) {
      console.error(`Failed to fetch task ${id}:`, error)
      return null
    }
  },

  // Users
  async getCurrentUser(): Promise<VikunjaUser | null> {
    try {
      return await callMcpTool<VikunjaUser>('GetCurrentUser', {})
    } catch (error) {
      console.error('Failed to fetch current user:', error)
      return null
    }
  },

  async getUsers(): Promise<VikunjaUser[]> {
    try {
      // SearchUsers requires a search parameter, use empty string to get all
      return await callMcpTool<VikunjaUser[]>('SearchUsers', { search: '' })
    } catch (error) {
      console.error('Failed to fetch users:', error)
      return []
    }
  },

  // Labels
  async getLabels(): Promise<VikunjaLabel[]> {
    try {
      return await callMcpTool<VikunjaLabel[]>('ListLabels', {})
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

