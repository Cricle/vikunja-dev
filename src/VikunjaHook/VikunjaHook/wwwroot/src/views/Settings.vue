<template>
  <div class="settings-container">
    <!-- Header -->
    <div class="settings-header">
      <div class="header-content">
        <div class="header-text">
          <h1 class="settings-title">{{ t('settings.title') }}</h1>
          <p class="settings-subtitle">{{ t('settings.description') }}</p>
        </div>
      </div>
    </div>

    <div class="settings-content">
      <!-- Theme Settings Card -->
      <va-card class="settings-card">
        <va-card-title>
          <div class="card-title-content">
            <va-icon name="palette" />
            <span>{{ t('settings.theme') }}</span>
          </div>
        </va-card-title>
        <va-card-content>
          <div class="setting-section">
            <va-button-toggle
              v-model="themeMode"
              :options="themeOptions"
              @update:model-value="updateTheme"
              class="theme-toggle"
            />
            <p class="setting-description">
              {{ t('theme.current') }}: <strong>{{ getCurrentThemeLabel() }}</strong>
            </p>
          </div>
        </va-card-content>
      </va-card>

      <!-- Language Settings Card -->
      <va-card class="settings-card">
        <va-card-title>
          <div class="card-title-content">
            <va-icon name="language" />
            <span>{{ t('settings.language') }}</span>
          </div>
        </va-card-title>
        <va-card-content>
          <div class="setting-section">
            <va-button-toggle
              v-model="locale"
              :options="languageOptions"
              class="language-toggle"
            />
          </div>
        </va-card-content>
      </va-card>

      <!-- User ID Card -->
      <va-card class="settings-card">
        <va-card-title>
          <div class="card-title-content">
            <va-icon name="person" />
            <span>{{ t('settings.userId') }}</span>
          </div>
        </va-card-title>
        <va-card-content>
          <div class="setting-section">
            <p class="setting-description">{{ t('settings.userIdDesc') }}</p>
            <va-input
              v-model="userId"
              readonly
              class="user-id-input"
            >
              <template #prepend>
                <va-icon name="badge" />
              </template>
            </va-input>
          </div>
        </va-card-content>
      </va-card>

      <!-- Backup & Restore Card -->
      <va-card class="settings-card backup-card">
        <va-card-title>
          <div class="card-title-content">
            <va-icon name="backup" />
            <span>{{ t('settings.backup') }}</span>
          </div>
        </va-card-title>
        <va-card-content>
          <div class="backup-grid">
            <div class="backup-section">
              <div class="backup-header">
                <va-icon name="download" size="large" color="primary" />
                <div class="backup-info">
                  <h3 class="backup-title">{{ t('settings.backupTitle') }}</h3>
                  <p class="backup-description">{{ t('settings.backupDesc') }}</p>
                </div>
              </div>
              <va-button
                @click="exportConfig"
                icon="download"
                :disabled="!config"
                color="primary"
                class="backup-button"
              >
                {{ t('settings.backupButton') }}
              </va-button>
            </div>
            
            <va-divider orientation="vertical" class="backup-divider" />
            
            <div class="backup-section">
              <div class="backup-header">
                <va-icon name="upload" size="large" color="success" />
                <div class="backup-info">
                  <h3 class="backup-title">{{ t('settings.restoreTitle') }}</h3>
                  <p class="backup-description">{{ t('settings.restoreDesc') }}</p>
                </div>
              </div>
              <va-file-upload
                v-model="uploadedFiles"
                type="single"
                file-types=".json"
                @file-added="importConfig"
                class="restore-upload"
              >
                <va-button icon="upload" color="success" class="backup-button">
                  {{ t('settings.restoreButton') }}
                </va-button>
              </va-file-upload>
            </div>
          </div>
        </va-card-content>
      </va-card>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useColors } from 'vuestic-ui'
import { useConfigStore } from '@/stores/configStore'

const { t, locale } = useI18n()
const { applyPreset } = useColors()
const configStore = useConfigStore()

const themeMode = ref<'auto' | 'light' | 'dark'>('auto')
const uploadedFiles = ref<File[]>([])
const userId = ref('default')

const config = computed(() => configStore.config)

const themeOptions = computed(() => [
  { label: t('theme.auto'), value: 'auto' },
  { label: t('theme.light'), value: 'light' },
  { label: t('theme.dark'), value: 'dark' }
])

const languageOptions = [
  { label: '中文', value: 'zh' },
  { label: 'English', value: 'en' }
]

function getCurrentThemeLabel() {
  if (themeMode.value === 'auto') {
    const isDark = window.matchMedia('(prefers-color-scheme: dark)').matches
    return isDark ? t('theme.dark') : t('theme.light')
  }
  return themeMode.value === 'dark' ? t('theme.dark') : t('theme.light')
}

function updateTheme(mode: 'auto' | 'light' | 'dark') {
  localStorage.setItem('themeMode', mode)
  
  if (mode === 'auto') {
    const isDark = window.matchMedia('(prefers-color-scheme: dark)').matches
    applyPreset(isDark ? 'dark' : 'light')
  } else {
    applyPreset(mode)
  }
}

function exportConfig() {
  if (!config.value) return
  
  const dataStr = JSON.stringify(config.value, null, 2)
  const dataBlob = new Blob([dataStr], { type: 'application/json' })
  const url = URL.createObjectURL(dataBlob)
  const link = document.createElement('a')
  link.href = url
  link.download = `webhook-config-${userId.value}-${Date.now()}.json`
  link.click()
  URL.revokeObjectURL(url)
}

async function importConfig(file: File) {
  try {
    const text = await file.text()
    const importedConfig = JSON.parse(text)
    
    if (!importedConfig.userId || !importedConfig.providers || !importedConfig.projectRules) {
      throw new Error('Invalid configuration file')
    }
    
    await configStore.saveConfig(importedConfig)
    await configStore.loadConfig(userId.value)
  } catch (error) {
    console.error('Failed to import config:', error)
  } finally {
    uploadedFiles.value = []
  }
}

onMounted(() => {
  const savedThemeMode = localStorage.getItem('themeMode') as 'auto' | 'light' | 'dark' | null
  if (savedThemeMode) {
    themeMode.value = savedThemeMode
  } else {
    themeMode.value = 'auto'
  }
  updateTheme(themeMode.value)
  
  const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)')
  mediaQuery.addEventListener('change', (e) => {
    if (themeMode.value === 'auto') {
      applyPreset(e.matches ? 'dark' : 'light')
    }
  })
  
  configStore.loadConfig(userId.value)
})
</script>

<style scoped>
.settings-container {
  padding: 1.5rem;
  width: 100%;
}

.settings-header {
  margin-bottom: 2rem;
}

.header-content {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 1rem;
}

.header-text {
  flex: 1;
}

.settings-title {
  font-size: 2rem;
  font-weight: 700;
  margin: 0 0 0.5rem 0;
  color: var(--va-primary);
}

.settings-subtitle {
  font-size: 1rem;
  margin: 0;
  opacity: 0.7;
}

/* Settings Content */
.settings-content {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.settings-card {
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
}

.card-title-content {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  font-size: 1.25rem;
  font-weight: 600;
}

.setting-section {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.setting-description {
  margin: 0;
  font-size: 0.875rem;
  opacity: 0.7;
}

/* Theme Toggle */
.theme-toggle {
  align-self: flex-start;
}

/* Language Toggle */
.language-toggle {
  align-self: flex-start;
}

/* User ID Input */
.user-id-input {
  max-width: 400px;
}

/* Backup Card */
.backup-card {
  background: linear-gradient(135deg, var(--va-background-element) 0%, var(--va-background-secondary) 100%);
}

.backup-grid {
  display: grid;
  grid-template-columns: 1fr auto 1fr;
  gap: 2rem;
  align-items: center;
}

.backup-section {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.backup-header {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.backup-info {
  flex: 1;
}

.backup-title {
  font-size: 1.125rem;
  font-weight: 600;
  margin: 0 0 0.5rem 0;
}

.backup-description {
  margin: 0;
  font-size: 0.875rem;
  opacity: 0.7;
}

.backup-button {
  align-self: flex-start;
  min-width: 140px;
}

.backup-divider {
  height: 100px;
}

.restore-upload {
  align-self: flex-start;
}

/* Responsive */
@media (max-width: 768px) {
  .settings-container {
    padding: 1rem;
  }

  .settings-title {
    font-size: 1.5rem;
  }

  .backup-grid {
    grid-template-columns: 1fr;
    gap: 1.5rem;
  }

  .backup-divider {
    display: none;
  }

  .backup-section {
    text-align: center;
  }

  .backup-header {
    flex-direction: column;
    text-align: center;
  }

  .backup-button {
    width: 100%;
  }

  .user-id-input {
    max-width: 100%;
  }

  .theme-toggle,
  .language-toggle {
    align-self: stretch;
  }
}

@media (max-width: 480px) {
  .backup-header {
    gap: 0.5rem;
  }

  .backup-title {
    font-size: 1rem;
  }
}
</style>
