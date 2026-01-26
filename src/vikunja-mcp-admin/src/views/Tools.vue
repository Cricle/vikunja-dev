<template>
  <div>
    <VaInnerLoading :loading="serverStore.loading">
      <div v-if="serverStore.tools.length > 0" class="row">
        <div v-for="tool in serverStore.tools" :key="tool.name" class="flex xs12 md6 lg4">
          <VaCard class="tool-card">
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
                  >
                    {{ subcommand }}
                  </VaChip>
                </div>
              </div>
            </VaCardContent>
          </VaCard>
        </div>
      </div>

      <VaCard v-else class="text-center">
        <VaCardContent>
          <VaIcon name="info" size="large" class="mb-2" />
          <p>No tools registered</p>
        </VaCardContent>
      </VaCard>
    </VaInnerLoading>
  </div>
</template>

<script setup lang="ts">
import { onMounted } from 'vue'
import { useServerStore } from '../stores/server'

const serverStore = useServerStore()

onMounted(() => {
  serverStore.fetchTools()
})
</script>

<style scoped>
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
}

@media (max-width: 600px) {
  .md6, .lg4 {
    flex: 0 0 100%;
    max-width: 100%;
  }
}

.tool-card {
  height: 100%;
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
</style>
