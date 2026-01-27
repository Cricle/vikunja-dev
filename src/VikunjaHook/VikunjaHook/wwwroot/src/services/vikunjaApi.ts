import axios from 'axios'

// Get Vikunja API URL and token from environment or config
const VIKUNJA_API_URL = import.meta.env.VITE_VIKUNJA_API_URL || 'https://vtodo.site/api/v1'
const VIKUNJA_API_TOKEN = import.meta.env.VITE_VIKUNJA_API_TOKEN || 'tk_8de21f609961d6782e5c7334d2e3bbf1d576aad6'

const vikunjaApi = axios.create({
  baseURL: VIKUNJA_API_URL,
  headers: {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${VIKUNJA_API_TOKEN}`
  }
})

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
      const response = await vikunjaApi.get('/projects')
      return response.data
    } catch (error) {
      console.error('Failed to fetch projects:', error)
      return []
    }
  },

  async getProject(id: number): Promise<VikunjaProject | null> {
    try {
      const response = await vikunjaApi.get(`/projects/${id}`)
      return response.data
    } catch (error) {
      console.error(`Failed to fetch project ${id}:`, error)
      return null
    }
  },

  // Tasks
  async getTasks(projectId?: number): Promise<VikunjaTask[]> {
    try {
      const url = projectId ? `/projects/${projectId}/tasks` : '/tasks/all'
      const response = await vikunjaApi.get(url)
      return response.data
    } catch (error) {
      console.error('Failed to fetch tasks:', error)
      return []
    }
  },

  async getTask(id: number): Promise<VikunjaTask | null> {
    try {
      const response = await vikunjaApi.get(`/tasks/${id}`)
      return response.data
    } catch (error) {
      console.error(`Failed to fetch task ${id}:`, error)
      return null
    }
  },

  // Users
  async getCurrentUser(): Promise<VikunjaUser | null> {
    try {
      const response = await vikunjaApi.get('/user')
      return response.data
    } catch (error) {
      console.error('Failed to fetch current user:', error)
      return null
    }
  },

  async getUsers(): Promise<VikunjaUser[]> {
    try {
      const response = await vikunjaApi.get('/users')
      return response.data
    } catch (error) {
      console.error('Failed to fetch users:', error)
      return []
    }
  },

  // Labels
  async getLabels(): Promise<VikunjaLabel[]> {
    try {
      const response = await vikunjaApi.get('/labels')
      return response.data
    } catch (error) {
      console.error('Failed to fetch labels:', error)
      return []
    }
  },

  // Statistics
  async getStatistics(): Promise<any> {
    try {
      const response = await vikunjaApi.get('/user/settings/general')
      return response.data
    } catch (error) {
      console.error('Failed to fetch statistics:', error)
      return null
    }
  }
}

