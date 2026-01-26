# Vikunja MCP Admin

A Vue 3 + TypeScript + Vuestic UI admin interface for managing the Vikunja MCP C# Server.

## Features

- 📊 **Dashboard** - Server status overview and quick stats
- ⚙️ **Configuration** - Manage server settings (Vikunja, MCP, CORS, Rate Limiting)
- 🔧 **Tools** - View all registered MCP tools and their subcommands
- 👥 **Sessions** - Monitor active authentication sessions
- 📝 **Logs** - View and filter server logs in real-time

## Tech Stack

- **Vue 3** - Progressive JavaScript framework
- **TypeScript** - Type-safe development
- **Vuestic UI** - Vue 3 UI framework with beautiful components
- **Pinia** - State management
- **Vue Router** - Client-side routing
- **Axios** - HTTP client
- **Vite** - Fast build tool

## Getting Started

### Prerequisites

- Node.js 18+ and npm
- Vikunja MCP C# Server running on `http://localhost:5082`

### Installation

```bash
cd src/vikunja-mcp-admin
npm install
```

### Development

```bash
npm run dev
```

The admin interface will be available at `http://localhost:3000`

### Build for Production

```bash
npm run build
```

The built files will be in the `dist` directory.

### Preview Production Build

```bash
npm run preview
```

## Configuration

The admin interface connects to the MCP server via a proxy configured in `vite.config.ts`:

```typescript
server: {
  port: 3000,
  proxy: {
    '/api': {
      target: 'http://localhost:5082',
      changeOrigin: true,
      rewrite: (path) => path.replace(/^\/api/, '')
    }
  }
}
```

## Project Structure

```
src/vikunja-mcp-admin/
├── src/
│   ├── views/           # Page components
│   │   ├── Dashboard.vue
│   │   ├── Configuration.vue
│   │   ├── Tools.vue
│   │   ├── Sessions.vue
│   │   └── Logs.vue
│   ├── stores/          # Pinia stores
│   │   ├── server.ts
│   │   └── config.ts
│   ├── services/        # API services
│   │   └── api.ts
│   ├── types/           # TypeScript types
│   │   └── index.ts
│   ├── router/          # Vue Router configuration
│   │   └── index.ts
│   ├── App.vue          # Root component
│   └── main.ts          # Application entry point
├── index.html
├── vite.config.ts
├── tsconfig.json
└── package.json
```

## Features in Detail

### Dashboard
- Real-time server health status
- Tool and subcommand statistics
- Quick action buttons
- Auto-refresh capability

### Configuration
- **Vikunja Settings**: Configure API timeout
- **MCP Settings**: Server name, version, max connections
- **CORS Settings**: Manage allowed origins, methods, and headers
- **Rate Limiting**: Enable/disable and configure request limits
- Save and reset functionality

### Tools
- List all registered MCP tools
- View tool descriptions
- Display all subcommands for each tool
- Visual badges for subcommand counts

### Sessions
- View all active authentication sessions
- Session details (ID, API URL, auth type, creation time)
- Disconnect sessions manually
- Support for both API Token and JWT authentication

### Logs
- Real-time log viewing
- Filter by log level (Debug, Info, Warning, Error)
- Auto-refresh option
- Clear logs functionality
- Syntax-highlighted log display

## API Integration

The admin interface communicates with the MCP server through these endpoints:

- `GET /mcp/health` - Server health check
- `GET /mcp/info` - Server information
- `GET /mcp/tools` - List all tools

Note: Configuration management and session management endpoints need to be implemented on the server side.

## Future Enhancements

- [ ] Real-time log streaming via WebSocket
- [ ] Configuration file upload/download
- [ ] Session management API integration
- [ ] Performance metrics and charts
- [ ] Dark/light theme toggle
- [ ] Export logs to file
- [ ] Tool testing interface
- [ ] Authentication for admin interface

## License

Same as the parent project.
