import axios, { AxiosInstance } from 'axios'
import type { UserConfig, NotificationResult, TestNotificationRequest } from '@/types/config'

class ApiService {
  private client: AxiosInstance

  constructor() {
    this.client = axios.create({
      baseURL: '/api',
      headers: {
        'Content-Type': 'application/json'
      }
    })
  }

  // Configuration endpoints
  async getUserConfig(userId: string): Promise<UserConfig> {
    const response = await this.client.get<UserConfig>(`/webhook-config/${userId}`)
    return response.data
  }

  async updateUserConfig(userId: string, config: UserConfig): Promise<UserConfig> {
    const response = await this.client.put<UserConfig>(`/webhook-config/${userId}`, config)
    return response.data
  }

  async testNotification(
    userId: string,
    request: TestNotificationRequest
  ): Promise<NotificationResult> {
    const response = await this.client.post<NotificationResult>(
      `/webhook-config/${userId}/test`,
      request
    )
    return response.data
  }
}

export const apiService = new ApiService()
