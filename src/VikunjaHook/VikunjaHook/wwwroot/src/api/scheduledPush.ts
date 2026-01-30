import type { ScheduledPushConfig, ScheduledPushRecord } from '@/types/scheduledPush'

const API_BASE = '/api'

export const scheduledPushApi = {
  // 获取用户的定时推送配置列表
  async getConfigs(userId: string): Promise<ScheduledPushConfig[]> {
    const response = await fetch(`${API_BASE}/scheduled-push/${userId}`)
    if (!response.ok) {
      throw new Error('Failed to fetch scheduled push configs')
    }
    return response.json()
  },

  // 保存定时推送配置
  async saveConfig(userId: string, config: ScheduledPushConfig): Promise<ScheduledPushConfig> {
    const response = await fetch(`${API_BASE}/scheduled-push/${userId}`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(config),
    })
    if (!response.ok) {
      throw new Error('Failed to save scheduled push config')
    }
    return response.json()
  },

  // 删除定时推送配置
  async deleteConfig(userId: string, configId: string): Promise<void> {
    const response = await fetch(`${API_BASE}/scheduled-push/${userId}/${configId}`, {
      method: 'DELETE',
    })
    if (!response.ok) {
      throw new Error('Failed to delete scheduled push config')
    }
  },

  // 获取推送历史记录
  async getHistory(count: number = 50): Promise<{ records: ScheduledPushRecord[]; totalCount: number }> {
    const response = await fetch(`${API_BASE}/scheduled-push-history?count=${count}`)
    if (!response.ok) {
      throw new Error('Failed to fetch scheduled push history')
    }
    return response.json()
  },

  // 清空推送历史记录
  async clearHistory(): Promise<void> {
    const response = await fetch(`${API_BASE}/scheduled-push-history`, {
      method: 'DELETE',
    })
    if (!response.ok) {
      throw new Error('Failed to clear scheduled push history')
    }
  },
}
