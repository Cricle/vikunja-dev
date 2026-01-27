<template>
  <va-app>
    <div class="app-layout">
      <!-- Fixed Navigation Bar -->
      <va-navbar color="backgroundElement" class="app-navbar">
        <template #left>
          <va-navbar-item>
            <div class="brand-content">
              <span class="brand-icon">🔔</span>
              <span class="brand-title">{{ t('app.title') }}</span>
            </div>
          </va-navbar-item>
        </template>

        <template #right>
          <!-- Desktop Navigation -->
          <va-navbar-item class="navbar-item-desktop">
            <va-button-group>
              <va-button
                :to="{ name: 'dashboard' }"
                icon="dashboard"
              >
                {{ t('nav.dashboard') }}
              </va-button>
              <va-button
                :to="{ name: 'providers' }"
                icon="notifications"
              >
                {{ t('nav.providers') }}
              </va-button>
              <va-button
                :to="{ name: 'projects' }"
                icon="folder"
              >
                {{ t('nav.projects') }}
              </va-button>
              <va-button
                :to="{ name: 'templates' }"
                icon="edit_note"
              >
                {{ t('nav.templates') }}
              </va-button>
            </va-button-group>
          </va-navbar-item>

          <!-- Mobile Navigation Dropdown -->
          <va-navbar-item class="navbar-item-mobile">
            <va-dropdown placement="bottom-end">
              <template #anchor>
                <va-button preset="plain" icon="menu" />
              </template>
              <va-dropdown-content>
                <va-list>
                  <va-list-item :to="{ name: 'dashboard' }" :class="{ 'active-menu-item': $route.name === 'dashboard' }" clickable>
                    <va-list-item-section icon>
                      <va-icon name="dashboard" />
                    </va-list-item-section>
                    <va-list-item-section>
                      <va-list-item-label>{{ t('nav.dashboard') }}</va-list-item-label>
                    </va-list-item-section>
                  </va-list-item>
                  <va-list-item :to="{ name: 'providers' }" :class="{ 'active-menu-item': $route.name === 'providers' }" clickable>
                    <va-list-item-section icon>
                      <va-icon name="notifications" />
                    </va-list-item-section>
                    <va-list-item-section>
                      <va-list-item-label>{{ t('nav.providers') }}</va-list-item-label>
                    </va-list-item-section>
                  </va-list-item>
                  <va-list-item :to="{ name: 'projects' }" :class="{ 'active-menu-item': $route.name === 'projects' }" clickable>
                    <va-list-item-section icon>
                      <va-icon name="folder" />
                    </va-list-item-section>
                    <va-list-item-section>
                      <va-list-item-label>{{ t('nav.projects') }}</va-list-item-label>
                    </va-list-item-section>
                  </va-list-item>
                  <va-list-item :to="{ name: 'templates' }" :class="{ 'active-menu-item': $route.name === 'templates' }" clickable>
                    <va-list-item-section icon>
                      <va-icon name="edit_note" />
                    </va-list-item-section>
                    <va-list-item-section>
                      <va-list-item-label>{{ t('nav.templates') }}</va-list-item-label>
                    </va-list-item-section>
                  </va-list-item>
                  <va-divider />
                  <va-list-item :to="{ name: 'settings' }" :class="{ 'active-menu-item': $route.name === 'settings' }" clickable>
                    <va-list-item-section icon>
                      <va-icon name="settings" />
                    </va-list-item-section>
                    <va-list-item-section>
                      <va-list-item-label>{{ t('nav.settings') }}</va-list-item-label>
                    </va-list-item-section>
                  </va-list-item>
                </va-list>
              </va-dropdown-content>
            </va-dropdown>
          </va-navbar-item>

          <!-- Language Selector -->
          <va-navbar-item>
            <va-dropdown placement="bottom-end">
              <template #anchor>
                <va-button preset="plain" icon="language">
                  {{ locale === 'zh' ? '中文' : 'EN' }}
                </va-button>
              </template>
              <va-dropdown-content>
                <va-list>
                  <va-list-item @click="switchLocale('zh')" clickable>
                    <va-list-item-section>
                      <va-list-item-label>{{ t('language.zh') }}</va-list-item-label>
                    </va-list-item-section>
                  </va-list-item>
                  <va-list-item @click="switchLocale('en')" clickable>
                    <va-list-item-section>
                      <va-list-item-label>{{ t('language.en') }}</va-list-item-label>
                    </va-list-item-section>
                  </va-list-item>
                </va-list>
              </va-dropdown-content>
            </va-dropdown>
          </va-navbar-item>

          <!-- Settings Menu -->
          <va-navbar-item>
            <va-dropdown placement="bottom-end">
              <template #anchor>
                <va-button preset="plain" icon="settings" />
              </template>
              <va-dropdown-content>
                <va-list>
                  <va-list-item :to="{ name: 'settings' }" clickable>
                    <va-list-item-section icon>
                      <va-icon name="tune" />
                    </va-list-item-section>
                    <va-list-item-section>
                      <va-list-item-label>{{ t('settings.preferences') }}</va-list-item-label>
                    </va-list-item-section>
                  </va-list-item>
                  <va-list-item @click="toggleTheme" clickable>
                    <va-list-item-section icon>
                      <va-icon :name="isDark ? 'light_mode' : 'dark_mode'" />
                    </va-list-item-section>
                    <va-list-item-section>
                      <va-list-item-label>{{ isDark ? t('theme.light') : t('theme.dark') }}</va-list-item-label>
                    </va-list-item-section>
                  </va-list-item>
                </va-list>
              </va-dropdown-content>
            </va-dropdown>
          </va-navbar-item>
        </template>
      </va-navbar>

      <!-- Scrollable Main Content -->
      <main class="app-main">
        <router-view />
      </main>
    </div>
  </va-app>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useColors } from 'vuestic-ui'
import { useI18n } from 'vue-i18n'

const { applyPreset } = useColors()
const { t, locale } = useI18n()

const isDark = ref(true)

function toggleTheme() {
  isDark.value = !isDark.value
  applyPreset(isDark.value ? 'dark' : 'light')
  localStorage.setItem('themeMode', isDark.value ? 'dark' : 'light')
}

function switchLocale(newLocale: string) {
  locale.value = newLocale
  localStorage.setItem('locale', newLocale)
}

function applyThemeFromSettings() {
  const savedThemeMode = localStorage.getItem('themeMode') as 'auto' | 'light' | 'dark' | null
  
  if (savedThemeMode === 'auto' || !savedThemeMode) {
    const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches
    isDark.value = prefersDark
    applyPreset(prefersDark ? 'dark' : 'light')
  } else {
    isDark.value = savedThemeMode === 'dark'
    applyPreset(savedThemeMode)
  }
}

onMounted(() => {
  const savedLocale = localStorage.getItem('locale')
  if (savedLocale) {
    locale.value = savedLocale
  }
  
  applyThemeFromSettings()
  
  // Listen to system theme changes
  const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)')
  mediaQuery.addEventListener('change', (e) => {
    const savedThemeMode = localStorage.getItem('themeMode')
    if (savedThemeMode === 'auto' || !savedThemeMode) {
      isDark.value = e.matches
      applyPreset(e.matches ? 'dark' : 'light')
    }
  })
})
</script>

<style scoped>
.app-layout {
  display: flex;
  flex-direction: column;
  height: 100vh;
  overflow: hidden;
}

.app-navbar {
  flex-shrink: 0;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
  backdrop-filter: blur(10px);
}

.app-main {
  flex: 1;
  overflow-y: auto;
  overflow-x: hidden;
  -webkit-overflow-scrolling: touch;
}

.brand-content {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.brand-icon {
}

.brand-title {
}

.navbar-item-mobile {
  display: none;
}

:deep(.active-menu-item) {
}

:deep(.va-list-item) {
  color: var(--va-text-primary);
}

:deep(.va-list-item .va-icon) {
  color: var(--va-text-primary);
}

:deep(.va-list-item .va-list-item-label) {
  color: var(--va-text-primary);
}

@media (max-width: 768px) {
  .navbar-item-mobile {
    display: flex;
  }

  .navbar-item-desktop {
    display: none;
  }

  .brand-title {
  }
}

@media (max-width: 480px) {
  .brand-title {
    display: none;
  }

  .brand-icon {
  }
}
</style>
