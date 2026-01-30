<template>
  <div class="scheduled-push-container">
    <div class="header">
      <h1>定时推送</h1>
      <p class="subtitle">每天定时推送未完成的任务</p>
    </div>
    
    <va-card>
      <va-card-content>
        <va-button color="primary" @click="showDialog = true">新增配置</va-button>
        
        <div v-if="configs.length === 0" class="empty">
          暂无配置
        </div>
        
        <div v-for="config in configs" :key="config.id" class="config-item">
          <div>{{ config.pushTime }} - {{ config.titleTemplate }}</div>
          <va-button size="small" @click="deleteConfig(config.id)">删除</va-button>
        </div>
      </va-card-content>
    </va-card>
    
    <va-modal v-model="showDialog" size="large">
      <template #header><h2>新增配置</h2></template>
      <va-input v-model="formData.pushTime" type="time" label="推送时间" />
      <va-input v-model="formData.titleTemplate" label="标题模板" />
      <va-textarea v-model="formData.bodyTemplate" label="正文模板" />
      <template #footer>
        <va-button @click="saveConfig">保存</va-button>
      </template>
    </va-modal>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'

const configs = ref<any[]>([])
const showDialog = ref(false)
const formData = ref({
  pushTime: '09:00',
  titleTemplate: '📋 今日待办任务',
  bodyTemplate: '## 未完成的任务\n\n{{tasks}}\n\n共 {{count}} 个任务待处理',
  minPriority: 0,
  labelIds: [] as number[],
  providers: ['PushDeer'],
  enabled: true
})

onMounted(async () => {
  await loadConfigs()
})

async function loadConfigs() {
  try {
    const response = await fetch('/api/scheduled-push/default')
    if (response.ok) {
      configs.value = await response.json()
    }
  } catch (error) {
    console.error('Failed to load configs:', error)
  }
}

async function saveConfig() {
  try {
    const config = {
      ...formData.value,
      id: Date.now().toString(),
      userId: 'default'
    }
    
    const response = await fetch('/api/scheduled-push/default', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(config)
    })
    
    if (response.ok) {
      await loadConfigs()
      showDialog.value = false
    }
  } catch (error) {
    console.error('Failed to save config:', error)
  }
}

async function deleteConfig(id: string) {
  try {
    const url = `/api/scheduled-push/default/${id}`
    const response = await fetch(url, {
      method: 'DELETE'
    })
    
    if (response.ok) {
      await loadConfigs()
    }
  } catch (error) {
    console.error('Failed to delete config:', error)
  }
}
</script>

<style scoped>
.scheduled-push-container {
  padding: 2rem;
}

.header {
  margin-bottom: 2rem;
}

.header h1 {
  margin: 0 0 0.5rem 0;
}

.subtitle {
  margin: 0;
  color: var(--va-text-secondary);
}

.empty {
  padding: 2rem;
  text-align: center;
  color: var(--va-text-secondary);
}

.config-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1rem;
  margin: 1rem 0;
  border: 1px solid var(--va-background-border);
  border-radius: 8px;
}
</style>