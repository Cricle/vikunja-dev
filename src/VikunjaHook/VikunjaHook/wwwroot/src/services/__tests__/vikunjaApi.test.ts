import { describe, it, expect, vi, beforeEach } from 'vitest'
import axios from 'axios'

const mockPost = vi.fn()

vi.mock('axios', () => ({
  default: {
    create: vi.fn(() => ({
      post: mockPost
    }))
  }
}))

describe('vikunjaService', () => {
  beforeEach(async () => {
    vi.clearAllMocks()
    mockPost.mockClear()
    // Clear module cache to get fresh instance
    vi.resetModules()
  })

  describe('getProjects', () => {
    it('should fetch projects successfully', async () => {
      const mockProjects = [
        { id: 1, title: 'Project 1', description: 'Test project' }
      ]
      
      const mockResponse = {
        data: {
          result: {
            content: [
              { type: 'text', text: JSON.stringify(mockProjects) }
            ]
          }
        }
      }

      mockPost.mockResolvedValue(mockResponse)

      const { vikunjaService } = await import('../vikunjaApi')
      const result = await vikunjaService.getProjects()
      
      expect(result).toEqual(mockProjects)
      expect(mockPost).toHaveBeenCalledWith('', expect.objectContaining({
        method: 'tools/call',
        params: expect.objectContaining({
          name: 'list_projects'
        })
      }))
    })

    it('should return empty array on error', async () => {
      mockPost.mockRejectedValue(new Error('Network error'))

      const { vikunjaService } = await import('../vikunjaApi')
      const result = await vikunjaService.getProjects()
      
      expect(result).toEqual([])
    })
  })

  describe('getTasks', () => {
    it('should fetch tasks for a project', async () => {
      const mockTasks = [
        { id: 1, title: 'Task 1', done: false, projectId: 1 }
      ]
      
      const mockResponse = {
        data: {
          result: {
            content: [
              { type: 'text', text: JSON.stringify(mockTasks) }
            ]
          }
        }
      }

      mockPost.mockResolvedValue(mockResponse)

      const { vikunjaService } = await import('../vikunjaApi')
      const result = await vikunjaService.getTasks(1)
      
      expect(result).toEqual(mockTasks)
      expect(mockPost).toHaveBeenCalledWith('', expect.objectContaining({
        method: 'tools/call',
        params: expect.objectContaining({
          name: 'list_tasks',
          arguments: { projectId: 1 }
        })
      }))
    })

    it('should return empty array on error', async () => {
      mockPost.mockRejectedValue(new Error('Network error'))

      const { vikunjaService } = await import('../vikunjaApi')
      const result = await vikunjaService.getTasks(1)
      
      expect(result).toEqual([])
    })
  })

  describe('getCurrentUser', () => {
    it('should fetch current user', async () => {
      const mockUser = {
        id: 1,
        username: 'testuser',
        name: 'Test User',
        email: 'test@example.com'
      }
      
      const mockResponse = {
        data: {
          result: {
            content: [
              { type: 'text', text: JSON.stringify(mockUser) }
            ]
          }
        }
      }

      mockPost.mockResolvedValue(mockResponse)

      const { vikunjaService } = await import('../vikunjaApi')
      const result = await vikunjaService.getCurrentUser()
      
      expect(result).toEqual(mockUser)
      expect(mockPost).toHaveBeenCalledWith('', expect.objectContaining({
        method: 'tools/call',
        params: expect.objectContaining({
          name: 'get_current_user'
        })
      }))
    })

    it('should return null on error', async () => {
      mockPost.mockRejectedValue(new Error('Network error'))

      const { vikunjaService } = await import('../vikunjaApi')
      const result = await vikunjaService.getCurrentUser()
      
      expect(result).toBeNull()
    })
  })

  describe('getLabels', () => {
    it('should fetch labels', async () => {
      const mockLabels = [
        { id: 1, title: 'Label 1', hexColor: '#FF0000' }
      ]
      
      const mockResponse = {
        data: {
          result: {
            content: [
              { type: 'text', text: JSON.stringify(mockLabels) }
            ]
          }
        }
      }

      mockPost.mockResolvedValue(mockResponse)

      const { vikunjaService } = await import('../vikunjaApi')
      const result = await vikunjaService.getLabels()
      
      expect(result).toEqual(mockLabels)
      expect(mockPost).toHaveBeenCalledWith('', expect.objectContaining({
        method: 'tools/call',
        params: expect.objectContaining({
          name: 'list_labels'
        })
      }))
    })
  })
})
