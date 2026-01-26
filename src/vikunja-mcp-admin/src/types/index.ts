export interface ServerHealth {
  status: string
  timestamp: string
  server: string
  version: string
}

export interface ServerInfo {
  name: string
  version: string
  capabilities: string[]
}

export interface Tool {
  name: string
  description: string
  subcommands: string[]
}

export interface Configuration {
  vikunja: VikunjaConfig
  mcp: McpConfig
  cors: CorsConfig
  rateLimit: RateLimitConfig
}

export interface VikunjaConfig {
  defaultTimeout: number
}

export interface McpConfig {
  serverName: string
  version: string
  maxConcurrentConnections: number
}

export interface CorsConfig {
  allowedOrigins: string[]
  allowedMethods: string[]
  allowedHeaders: string[]
}

export interface RateLimitConfig {
  enabled: boolean
  requestsPerMinute: number
  requestsPerHour: number
}

export interface AuthSession {
  sessionId: string
  apiUrl: string
  authType: 'ApiToken' | 'Jwt'
  createdAt: string
}

export interface Session {
  sessionId: string
  apiUrl: string
  authType: string
  created: string
  lastAccessed: string
  isExpired: boolean
}

export interface ServerStats {
  server: {
    name: string
    version: string
    uptime: string
  }
  sessions: {
    total: number
    active: number
  }
  tools: {
    total: number
    subcommands: number
  }
  memory: {
    workingSet: number
    privateMemory: number
  }
}

export interface LogEntry {
  timestamp: string
  level: string
  message: string
}

export interface ToolExecutionRequest {
  toolName: string
  subcommand: string
  parameters: Record<string, any>
  sessionId?: string
}

export interface ToolExecutionResult {
  success: boolean
  result?: any
  error?: string
  executionTime?: number
}
