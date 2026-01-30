import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { fileURLToPath, URL } from 'node:url'
import viteCompression from 'vite-plugin-compression'

export default defineConfig({
  plugins: [
    vue(),
    // Gzip 压缩
    viteCompression({
      verbose: true,
      disable: false,
      threshold: 10240, // 10KB 以上的文件才压缩
      algorithm: 'gzip',
      ext: '.gz',
      deleteOriginFile: false
    }),
    // Brotli 压缩（更好的压缩率）
    viteCompression({
      verbose: true,
      disable: false,
      threshold: 10240,
      algorithm: 'brotliCompress',
      ext: '.br',
      deleteOriginFile: false
    })
  ],
  base: '/',
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url))
    }
  },
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:5082',
        changeOrigin: true
      }
    }
  },
  build: {
    outDir: 'dist',
    assetsDir: 'assets',
    emptyOutDir: true,
    // 优化构建
    minify: 'terser',
    terserOptions: {
      compress: {
        drop_console: true,
        drop_debugger: true,
        pure_funcs: ['console.log', 'console.info', 'console.debug']
      }
    },
    // 代码分割优化
    rollupOptions: {
      output: {
        manualChunks: {
          // 将大型依赖分离到单独的 chunk
          'vendor-vue': ['vue', 'vue-router', 'pinia'],
          'vendor-ui': ['vuestic-ui'],
          'vendor-markdown': ['easymde'],
          'vendor-utils': ['axios', 'vue-i18n']
        },
        // 优化 chunk 文件名
        chunkFileNames: 'assets/[name]-[hash].js',
        entryFileNames: 'assets/[name]-[hash].js',
        assetFileNames: 'assets/[name]-[hash].[ext]'
      }
    },
    // 启用 CSS 代码分割
    cssCodeSplit: true,
    // 设置 chunk 大小警告限制
    chunkSizeWarningLimit: 500,
    // 禁用 sourcemap（生产环境）
    sourcemap: false,
    // 优化资源内联
    assetsInlineLimit: 4096,
    // 启用 CSS 压缩
    cssMinify: true
  },
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: './src/test/setup.ts'
  }
})
