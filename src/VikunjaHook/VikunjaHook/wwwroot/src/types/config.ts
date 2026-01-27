export interface UserConfig {
  userId: string
  providers: ProviderConfig[]
  projectRules: ProjectRule[]
  templates: Record<string, NotificationTemplate>
  lastModified?: string
}

export interface ProviderConfig {
  providerType: string
  settings: Record<string, string>
}

export interface ProjectRule {
  projectId: string
  enabledEvents: string[]
  providerType?: string
}

export interface NotificationTemplate {
  eventType: string
  title: string
  body: string
  format: NotificationFormat
}

export enum NotificationFormat {
  Text = 'Text',
  Markdown = 'Markdown',
  Html = 'Html'
}

export interface NotificationResult {
  success: boolean
  errorMessage?: string
  timestamp: string
}

export interface TestNotificationRequest {
  providerType: string
  title?: string
  body?: string
}
