# Quick Start Guide

## 🚀 Get Started in 3 Steps

### Step 1: Install Dependencies

```bash
cd src/vikunja-mcp-admin
npm install
```

### Step 2: Start the MCP Server

In a separate terminal:

```bash
cd src/VikunjaHook/VikunjaHook
dotnet run
```

Wait for the server to start on `http://localhost:5082`

### Step 3: Start the Admin UI

```bash
npm run dev
```

Or use the PowerShell script:

```powershell
.\start.ps1
```

The admin interface will open at `http://localhost:3000`

## 📱 Using the Admin Interface

### Dashboard
- View server status and statistics
- Quick access to all features
- Real-time health monitoring

### Configuration
1. Navigate to **Configuration** in the sidebar
2. Modify settings:
   - Vikunja timeout
   - MCP server settings
   - CORS origins
   - Rate limiting
3. Click **Save Configuration**

### Tools
- View all 5 registered tools
- See 45+ subcommands
- Explore tool capabilities

### Sessions
- Monitor active authentication sessions
- View session details
- Disconnect sessions if needed

### Logs
- View server logs in real-time
- Filter by log level
- Enable auto-refresh for live updates

## 🔧 Configuration Tips

### Adding CORS Origins

1. Go to **Configuration** → **CORS Settings**
2. Click **Add Origin**
3. Enter the origin URL (e.g., `https://example.com`)
4. Click **Add**
5. Save configuration

### Adjusting Rate Limits

1. Go to **Configuration** → **Rate Limiting**
2. Toggle **Enable Rate Limiting**
3. Set **Requests Per Minute** (default: 60)
4. Set **Requests Per Hour** (default: 1000)
5. Save configuration

## 🐛 Troubleshooting

### Admin UI won't start

**Problem**: `npm run dev` fails

**Solution**:
```bash
# Clear node_modules and reinstall
rm -rf node_modules package-lock.json
npm install
```

### Can't connect to MCP server

**Problem**: "Failed to fetch" errors

**Solution**:
1. Ensure MCP server is running on port 5082
2. Check server health: `http://localhost:5082/mcp/health`
3. Verify CORS settings allow `http://localhost:3000`

### Configuration changes not saving

**Problem**: Save button doesn't work

**Solution**:
- Currently, configuration is stored in memory only
- To persist changes, you need to manually update `appsettings.json`
- Future versions will include a backend API for configuration management

## 📚 Next Steps

- Explore all 5 MCP tools and their subcommands
- Configure CORS for your production environment
- Adjust rate limits based on your needs
- Monitor active sessions
- Review server logs for debugging

## 🆘 Need Help?

- Check the main README.md for detailed documentation
- Review the MCP server documentation
- Check server logs for error messages

## 🎯 Production Deployment

### Build for Production

```bash
npm run build
```

### Serve Production Build

```bash
npm run preview
```

Or use a static file server:

```bash
npx serve dist
```

### Deploy to Web Server

1. Build the project: `npm run build`
2. Copy the `dist` folder to your web server
3. Configure your web server to:
   - Serve `index.html` for all routes (SPA mode)
   - Proxy `/api/*` requests to your MCP server
4. Update CORS settings on the MCP server to allow your domain

Example Nginx configuration:

```nginx
server {
    listen 80;
    server_name admin.example.com;
    root /var/www/vikunja-mcp-admin;
    index index.html;

    location / {
        try_files $uri $uri/ /index.html;
    }

    location /api/ {
        proxy_pass http://localhost:5082/;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
}
```

Enjoy managing your Vikunja MCP Server! 🎉
