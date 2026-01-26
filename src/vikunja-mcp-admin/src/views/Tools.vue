<template>
  <div class="tools-page">
    <!-- Tools List -->
    <VaCard class="mb-4">
      <VaCardTitle>
        <div class="d-flex justify-space-between align-center">
          <span>Registered Tools</span>
          <VaButton 
            size="small" 
            @click="refreshTools"
            :loading="serverStore.loading"
          >
            <VaIcon name="mdi-refresh" class="mr-1" />
            Refresh
          </VaButton>
        </div>
      </VaCardTitle>
      <VaCardContent>
        <VaInnerLoading :loading="serverStore.loading">
          <div v-if="serverStore.tools.length > 0" class="row">
            <div v-for="tool in serverStore.tools" :key="tool.name" class="flex xs12 md6 lg4">
              <VaCard class="tool-card" :class="{ 'selected': selectedTool?.name === tool.name }">
                <VaCardTitle>
                  <div class="d-flex justify-space-between align-center">
                    <span class="tool-name">{{ tool.name }}</span>
                    <VaBadge :text="`${tool.subcommands.length} subcommands`" color="info" />
                  </div>
                </VaCardTitle>
                <VaCardContent>
                  <p class="mb-4">{{ tool.description }}</p>
                  
                  <VaDivider class="my-3" />
                  
                  <div>
                    <div class="text-secondary mb-2">Subcommands:</div>
                    <div class="d-flex flex-wrap" style="gap: 0.5rem;">
                      <VaChip
                        v-for="subcommand in tool.subcommands"
                        :key="subcommand"
                        size="small"
                        color="primary"
                        outline
                        clickable
                        @click="selectToolForTest(tool, subcommand)"
                      >
                        {{ subcommand }}
                      </VaChip>
                    </div>
                  </div>

                  <VaDivider class="my-3" />

                  <VaButton 
                    size="small" 
                    block
                    @click="selectToolForTest(tool, tool.subcommands[0])"
                  >
                    <VaIcon name="mdi-play" class="mr-1" />
                    Test Tool
                  </VaButton>
                </VaCardContent>
              </VaCard>
            </div>
          </div>

          <div v-else class="text-center py-6">
            <VaIcon name="mdi-information" size="large" class="mb-3" color="secondary" />
            <h3 class="va-h3 mb-2">No Tools Registered</h3>
            <p class="text-secondary">No MCP tools are currently registered.</p>
          </div>
        </VaInnerLoading>
      </VaCardContent>
    </VaCard>

    <!-- Tool Testing Panel -->
    <VaCard v-if="selectedTool" class="test-panel">
      <VaCardTitle>
        <div class="d-flex justify-space-between align-center">
          <span>Test Tool: {{ selectedTool.name }}</span>
          <VaButton 
            size="small" 
            preset="plain"
            @click="clearSelection"
          >
            <VaIcon name="mdi-close" />
          </VaButton>
        </div>
      </VaCardTitle>
      <VaCardContent>
        <div class="row">
          <!-- Left Column: Input Form -->
          <div class="flex xs12 md6">
            <div class="form-section">
              <h3 class="va-h3 mb-3">Configuration</h3>

              <VaSelect
                v-model="selectedSubcommand"
                label="Subcommand"
                :options="selectedTool.subcommands"
                class="mb-4"
              />

              <VaSelect
                v-model="selectedSession"
                label="Session (Optional)"
                :options="sessionOptions"
                clearable
                class="mb-4"
              >
                <template #prepend>
                  <VaIcon name="mdi-key" />
                </template>
              </VaSelect>

              <h4 class="va-h4 mb-3">Parameters</h4>
              
              <VaTextarea
                v-model="parametersJson"
                label="Parameters (JSON)"
                placeholder='{"key": "value"}'
                :min-rows="8"
                :max-rows="15"
                class="mb-4"
                :error="!!jsonError"
                :error-messages="jsonError ? [jsonError] : []"
              />

              <div class="d-flex" style="gap: 0.5rem;">
                <VaButton 
                  @click="executeToolTest"
                  :loading="executing"
                  :disabled="!!jsonError"
                >
                  <VaIcon name="mdi-play" class="mr-2" />
                  Execute
                </VaButton>
                <VaButton 
                  preset="secondary"
                  @click="clearParameters"
                >
                  <VaIcon name="mdi-close" class="mr-2" />
                  Clear
                </VaButton>
                <VaButton 
                  preset="secondary"
                  @click="loadSampleParameters"
                >
                  <VaIcon name="mdi-code-tags" class="mr-2" />
                  Sample
                </VaButton>
              </div>
            </div>
          </div>

          <!-- Right Column: Results -->
          <div class="flex xs12 md6">
            <div class="results-section">
              <h3 class="va-h3 mb-3">Execution Result</h3>

              <div v-if="executionResult" class="result-container">
                <div class="result-header mb-3">
                  <VaBadge 
                    :text="executionResult.success ? 'Success' : 'Failed'" 
                    :color="executionResult.success ? 'success' : 'danger'"
                    size="large"
                  />
                  <span v-if="executionResult.executionTime" class="execution-time">
                    Execution time: {{ executionResult.executionTime }}ms
                  </span>
                </div>

                <VaDivider class="my-3" />

                <div v-if="executionResult.success && executionResult.result">
                  <h4 class="va-h4 mb-2">Result:</h4>
                  <pre class="result-content">{{ formatResult(executionResult.result) }}</pre>
                </div>

                <div v-if="!executionResult.success && executionResult.error">
                  <h4 class="va-h4 mb-2">Error:</h4>
                  <VaAlert color="danger" class="mb-0">
                    {{ executionResult.error }}
                  </VaAlert>
                </div>
              </div>

              <div v-else class="text-center py-6">
                <VaIcon name="mdi-flask" size="large" class="mb-3" color="secondary" />
                <p class="text-secondary">Execute the tool to see results here</p>
              </div>
            </div>
          </div>
        </div>
      </VaCardContent>
    </VaCard>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useServerStore } from '../stores/server'
import { adminApi } from '../services/api'
import type { Tool, ToolExecutionResult, Session } from '../types'
import { useToast } from 'vuestic-ui'

const serverStore = useServerStore()
const { init: notify } = useToast()

const selectedTool = ref<Tool | null>(null)
const selectedSubcommand = ref<string>('')
const selectedSession = ref<string>('')
const parametersJson = ref('{}')
const jsonError = ref<string>('')
const executing = ref(false)
const executionResult = ref<ToolExecutionResult | null>(null)
const sessions = ref<Session[]>([])

const sessionOptions = computed(() => {
  return sessions.value.map(s => ({
    text: `${s.sessionId.substring(0, 8)}... (${s.apiUrl})`,
    value: s.sessionId
  }))
})

function selectToolForTest(tool: Tool, subcommand: string) {
  selectedTool.value = tool
  selectedSubcommand.value = subcommand
  parametersJson.value = '{}'
  executionResult.value = null
  jsonError.value = ''
}

function clearSelection() {
  selectedTool.value = null
  selectedSubcommand.value = ''
  parametersJson.value = '{}'
  executionResult.value = null
  jsonError.value = ''
}

function clearParameters() {
  parametersJson.value = '{}'
  jsonError.value = ''
}

function loadSampleParameters() {
  const samples: Record<string, any> = {
    'vikunja_projects': {
      'list': {},
      'get': { 'projectId': 1 },
      'create': { 'title': 'New Project', 'description': 'Project description' }
    },
    'vikunja_tasks': {
      'list': { 'projectId': 1 },
      'get': { 'taskId': 1 },
      'create': { 'title': 'New Task', 'projectId': 1 }
    }
  }

  const toolName = selectedTool.value?.name
  const subcommand = selectedSubcommand.value

  if (toolName && subcommand && samples[toolName]?.[subcommand]) {
    parametersJson.value = JSON.stringify(samples[toolName][subcommand], null, 2)
  } else {
    parametersJson.value = JSON.stringify({ 'example': 'value' }, null, 2)
  }
  jsonError.value = ''
}

function validateJson() {
  try {
    JSON.parse(parametersJson.value)
    jsonError.value = ''
    return true
  } catch (error) {
    jsonError.value = 'Invalid JSON format'
    return false
  }
}

async function executeToolTest() {
  if (!selectedTool.value || !selectedSubcommand.value) {
    return
  }

  if (!validateJson()) {
    notify({
      message: 'Invalid JSON parameters',
      color: 'danger'
    })
    return
  }

  executing.value = true
  executionResult.value = null

  try {
    const parameters = JSON.parse(parametersJson.value)
    const result = await adminApi.executeTool({
      toolName: selectedTool.value.name,
      subcommand: selectedSubcommand.value,
      parameters,
      sessionId: selectedSession.value || undefined
    })

    executionResult.value = result
    
    if (result.success) {
      notify({
        message: 'Tool executed successfully',
        color: 'success'
      })
    } else {
      notify({
        message: 'Tool execution failed',
        color: 'danger'
      })
    }
  } catch (error) {
    executionResult.value = {
      success: false,
      error: error instanceof Error ? error.message : 'Unknown error occurred'
    }
    notify({
      message: 'Failed to execute tool',
      color: 'danger'
    })
  } finally {
    executing.value = false
  }
}

function formatResult(result: any): string {
  if (typeof result === 'string') {
    return result
  }
  return JSON.stringify(result, null, 2)
}

async function refreshTools() {
  await serverStore.fetchTools()
}

async function loadSessions() {
  try {
    sessions.value = await adminApi.getSessions()
  } catch (error) {
    console.error('Failed to load sessions:', error)
  }
}

watch(parametersJson, () => {
  validateJson()
})

onMounted(() => {
  serverStore.fetchTools()
  loadSessions()
})
</script>

<style scoped>
.tools-page {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.row {
  display: flex;
  flex-wrap: wrap;
  margin: -0.5rem;
}

.flex {
  padding: 0.5rem;
}

.xs12 { flex: 0 0 100%; max-width: 100%; }
.md6 { flex: 0 0 50%; max-width: 50%; }
.lg4 { flex: 0 0 33.333%; max-width: 33.333%; }

@media (max-width: 960px) {
  .lg4 {
    flex: 0 0 50%;
    max-width: 50%;
  }
  .md6 {
    flex: 0 0 100%;
    max-width: 100%;
  }
}

@media (max-width: 600px) {
  .md6, .lg4 {
    flex: 0 0 100%;
    max-width: 100%;
  }
}

.tool-card {
  height: 100%;
  transition: all 0.3s;
}

.tool-card.selected {
  border: 2px solid var(--va-primary);
  box-shadow: 0 4px 12px rgba(var(--va-primary-rgb), 0.3);
}

.tool-name {
  font-family: 'Courier New', monospace;
  font-weight: 600;
}

.d-flex {
  display: flex;
}

.flex-wrap {
  flex-wrap: wrap;
}

.justify-space-between {
  justify-content: space-between;
}

.align-center {
  align-items: center;
}

.text-center {
  text-align: center;
}

.py-6 {
  padding-top: 2rem;
  padding-bottom: 2rem;
}

.test-panel {
  background: linear-gradient(135deg, rgba(var(--va-primary-rgb), 0.05) 0%, rgba(var(--va-background-element-rgb), 1) 100%);
}

.form-section,
.results-section {
  height: 100%;
}

.result-container {
  background: #1e1e1e;
  color: #d4d4d4;
  padding: 1rem;
  border-radius: 4px;
}

.result-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.execution-time {
  font-size: 0.875rem;
  color: #858585;
}

.result-content {
  font-family: 'Courier New', monospace;
  font-size: 0.875rem;
  background: #2d2d2d;
  padding: 1rem;
  border-radius: 4px;
  overflow-x: auto;
  max-height: 400px;
  overflow-y: auto;
  margin: 0;
  white-space: pre-wrap;
  word-break: break-word;
}
</style>
