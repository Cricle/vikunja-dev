export interface ScheduledPushConfig {
  id: string
  userId: string
  enabled: boolean
  pushTime: string // 格式: "HH:mm"
  minPriority: number // 0-5
  labelIds: number[]
  titleTemplate: string
  bodyTemplate: string
  providers: string[]
  lastPushTime?: string
  created: string
  updated: string
}

export interface ScheduledPushRecord {
  id: string
  configId: string
  userId: string
  timestamp: string
  taskCount: number
  title: string
  body: string
  providers: string[]
  success: boolean
  errorMessage?: string
}

export interface ScheduledPushFormData {
  enabled: boolean
  pushTime: string
  minPriority: number
  labelIds: number[]
  titleTemplate: string
  bodyTemplate: string
  providers: string[]
}
