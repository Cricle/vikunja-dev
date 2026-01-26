<template>
  <div class="configuration">
    <h1 class="va-h3">Configuration</h1>

    <VaInnerLoading :loading="configStore.loading">
      <div v-if="config" class="config-sections">
        <VaCard>
          <VaCardTitle>Vikunja Settings</VaCardTitle>
          <VaCardContent>
            <VaInput
              v-model.number="config.vikunja.defaultTimeout"
              type="number"
              label="Default Timeout (ms)"
              :min="1000"
              :max="120000"
              class="mb-4"
            >
              <template #appendInner>
                <VaIcon name="schedule" size="small" />
              </template>
            </VaInput>
            <div class="va-text-secondary" style="font-size: 0.875rem; margin-top: -0.75rem;">
              Timeout for HTTP requests to Vikunja API
            </div>
          </VaCardContent>
        </VaCard>

        <VaCard>
          <VaCardTitle>MCP Server Settings</VaCardTitle>
          <VaCardContent>
            <VaInput
              v-model="config.mcp.serverName"
              label="Server Name"
              class="mb-4"
            >
              <template #appendInner>
                <VaIcon name="dns" size="small" />
              </template>
            </VaInput>
            <VaInput
              v-model="config.mcp.version"
              label="Version"
              class="mb-4"
            >
              <template #appendInner>
                <VaIcon name="info" size="small" />
              </template>
            </VaInput>
            <VaInput
              v-model.number="config.mcp.maxConcurrentConnections"
              type="number"
              label="Max Concurrent Connections"
              :min="1"
              :max="1000"
            >
              <template #appendInner>
                <VaIcon name="people" size="small" />
              </template>
            </VaInput>
          </VaCardContent>
        </VaCard>

        <VaCard>
          <VaCardTitle>CORS Settings</VaCardTitle>
          <VaCardContent>
            <div class="mb-4">
              <label class="va-input-label">Allowed Origins</label>
              <div class="chips-container">
                <VaChip
                  v-for="(origin, index) in config.cors.allowedOrigins"
                  :key="index"
                  closeable
                  @update:modelValue="removeOrigin(index)"
                  color="primary"
                >
                  {{ origin }}
                </VaChip>
                <VaButton
                  size="small"
                  @click="showAddOriginModal = true"
                  icon="add"
                >
                  Add Origin
                </VaButton>
              </div>
            </div>

            <VaDivider />

            <div class="mb-4 mt-4">
              <label class="va-input-label">Allowed Methods</label>
              <div class="chips-container">
                <VaChip
                  v-for="(method, index) in config.cors.allowedMethods"
                  :key="index"
                  color="success"
                  outline
                >
                  {{ method }}
                </VaChip>
              </div>
            </div>

            <VaDivider />

            <div class="mt-4">
              <label class="va-input-label">Allowed Headers</label>
              <div class="chips-container">
                <VaChip
                  v-for="(header, index) in config.cors.allowedHeaders"
                  :key="index"
                  color="info"
                  outline
                >
                  {{ header }}
                </VaChip>
              </div>
            </div>
          </VaCardContent>
        </VaCard>

        <VaCard>
          <VaCardTitle>Rate Limiting</VaCardTitle>
          <VaCardContent>
            <VaSwitch
              v-model="config.rateLimit.enabled"
              label="Enable Rate Limiting"
              class="mb-4"
            />
            <VaInput
              v-model.number="config.rateLimit.requestsPerMinute"
              type="number"
              label="Requests Per Minute"
              :min="1"
              :max="1000"
              :disabled="!config.rateLimit.enabled"
              class="mb-4"
            />
            <VaInput
              v-model.number="config.rateLimit.requestsPerHour"
              type="number"
              label="Requests Per Hour"
              :min="1"
              :max="10000"
              :disabled="!config.rateLimit.enabled"
              class="mb-4"
            />
            <div class="va-text-secondary" style="font-size: 0.875rem;">
              Rate limits are applied per authentication token
            </div>
          </VaCardContent>
        </VaCard>

        <div class="actions-bar">
          <VaButton
            @click="saveConfiguration"
            :loading="saving"
            icon="save"
          >
            Save Configuration
          </VaButton>
          <VaButton
            @click="resetConfiguration"
            preset="secondary"
            icon="refresh"
          >
            Reset
          </VaButton>
        </div>
      </div>
    </VaInnerLoading>

    <VaModal
      v-model="showAddOriginModal"
      title="Add Allowed Origin"
      ok-text="Add"
      @ok="addOrigin"
    >
      <VaInput
        v-model="newOrigin"
        label="Origin URL"
        placeholder="https://example.com"
        @keyup.enter="addOrigin"
      />
    </VaModal>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from "vue"
import { useToast } from "vuestic-ui"
import { useConfigStore } from "../stores/config"
import type { Configuration } from "../types"

const { init: notify } = useToast()
const configStore = useConfigStore()
const config = ref<Configuration | null>(null)
const saving = ref(false)
const showAddOriginModal = ref(false)
const newOrigin = ref("")

function removeOrigin(index: number) {
  if (config.value) {
    config.value.cors.allowedOrigins.splice(index, 1)
  }
}

function addOrigin() {
  if (config.value && newOrigin.value) {
    if (!newOrigin.value.startsWith('http://') && !newOrigin.value.startsWith('https://')) {
      notify({
        message: 'Origin must start with http:// or https://',
        color: 'warning'
      })
      return
    }
    config.value.cors.allowedOrigins.push(newOrigin.value)
    newOrigin.value = ""
    showAddOriginModal.value = false
    notify({
      message: 'Origin added successfully',
      color: 'success'
    })
  }
}

async function saveConfiguration() {
  if (!config.value) return
  saving.value = true
  try {
    await configStore.updateConfiguration(config.value)
    notify({
      message: 'Configuration saved successfully! Please restart the server for changes to take effect.',
      color: 'success',
      duration: 5000
    })
  } catch (error) {
    notify({
      message: 'Failed to save configuration',
      color: 'danger'
    })
  } finally {
    saving.value = false
  }
}

async function resetConfiguration() {
  await configStore.fetchConfiguration()
  config.value = configStore.configuration ? { ...configStore.configuration } : null
  notify({
    message: 'Configuration reset to current values',
    color: 'info'
  })
}

onMounted(async () => {
  await configStore.fetchConfiguration()
  config.value = configStore.configuration ? { ...configStore.configuration } : null
})
</script>

<style scoped>
.configuration {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.config-sections {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.chips-container {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  align-items: center;
  margin-top: 0.5rem;
}

.actions-bar {
  display: flex;
  gap: 0.75rem;
  padding-top: 0.5rem;
}

@media (max-width: 768px) {
  .actions-bar {
    flex-direction: column;
  }
}
</style>
