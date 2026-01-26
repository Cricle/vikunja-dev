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
