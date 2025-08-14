# DBADash Web - SQL Server Monitoring Dashboard

A modern web-based version of DBADash, built with **Blazor Server/WebAssembly hybrid**, **ASP.NET Core 8.0**, and **SignalR** for real-time SQL Server monitoring and management.

## 🚀 Features

- **Hybrid Blazor Architecture**: Supports both Blazor Server and WebAssembly modes
- **Real-time Monitoring**: Live performance metrics with SignalR
- **Cross-platform**: Runs on Windows, Linux, and Docker containers
- **Background Services**: Quartz.NET-based job scheduling for data collection
- **Responsive UI**: Modern, mobile-friendly dashboard
- **Centralized Database**: Single repository for all monitored instances
- **Alert Management**: Real-time alerting with acknowledgment workflow
- **Performance Monitoring**: CPU, memory, blocking, and query performance tracking
- **Backup Monitoring**: Comprehensive backup history and validation
- **Docker Support**: Full containerization with Docker Compose

## 🏗️ Architecture

### Solution Structure
```
DBADash.Web/
├── DBADash.Web.Server/     # Main web application (Blazor Server + API)
├── DBADash.Web.Client/     # Blazor WebAssembly client
├── DBADash.Web.Shared/     # Shared models and DTOs
├── DBADash.Web.API/        # API controllers for WebAssembly mode
├── DBADash.Web.Core/       # Business logic and data access
├── Dockerfile              # Docker container configuration
├── docker-compose.yml      # Multi-container deployment
└── nginx.conf             # Reverse proxy configuration
```

### Technology Stack
- **Frontend**: Blazor Server/WebAssembly, Bootstrap 5, SignalR
- **Backend**: ASP.NET Core 8.0, Entity Framework Core, Quartz.NET
- **Database**: SQL Server (existing DBADash schema compatible)
- **Deployment**: Docker, IIS, Linux/Nginx
- **Logging**: Serilog with structured logging

## 📋 Prerequisites

### For Docker Deployment
- Docker 20.10+ and Docker Compose
- 4GB+ RAM available for containers
- SQL Server instance (existing DBADash database)

### For Traditional Deployment
- .NET 8.0 Runtime
- SQL Server 2016+ (existing DBADash database)
- IIS 10+ (Windows) or Nginx (Linux)

## 🐳 Quick Start with Docker

1. **Clone and navigate to the web solution**:
   ```bash
   cd DBADash.Web
   ```

2. **Update connection string** in `docker-compose.yml`:
   ```yaml
   environment:
     - ConnectionStrings__DefaultConnection=Server=your-sql-server;Database=DBADash;...
   ```

3. **Start the application**:
   ```bash
   docker-compose up -d
   ```

4. **Access the application**:
   - HTTP: http://localhost:8080
   - HTTPS (with Nginx): https://localhost

## 🔧 Traditional Deployment

### Windows IIS Deployment

1. **Publish the application**:
   ```bash
   dotnet publish DBADash.Web.Server -c Release -o ./publish
   ```

2. **Configure IIS**:
   - Create new Application Pool (.NET Core, No Managed Code)
   - Create new Website pointing to publish folder
   - Install ASP.NET Core Hosting Bundle

3. **Update configuration** in `appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=your-server;Database=DBADash;..."
     }
   }
   ```

### Linux Deployment

1. **Install .NET 8.0 Runtime**:
   ```bash
   curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 8.0 --runtime aspnetcore
   ```

2. **Deploy application**:
   ```bash
   sudo mkdir -p /var/www/dbadash
   sudo cp -r ./publish/* /var/www/dbadash/
   sudo chown -R www-data:www-data /var/www/dbadash
   ```

3. **Configure systemd service** (`/etc/systemd/system/dbadash.service`):
   ```ini
   [Unit]
   Description=DBADash Web Application
   After=network.target

   [Service]
   Type=notify
   ExecStart=/usr/bin/dotnet /var/www/dbadash/DBADash.Web.Server.dll
   Restart=always
   User=www-data
   Environment=ASPNETCORE_ENVIRONMENT=Production
   Environment=ASPNETCORE_URLS=http://localhost:5000

   [Install]
   WantedBy=multi-user.target
   ```

4. **Configure Nginx** (use provided `nginx.conf` template)

## ⚙️ Configuration

### Application Settings

Key configuration options in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=server;Database=DBADash;..."
  },
  "MonitoringSettings": {
    "DataCollectionIntervalMinutes": 1,
    "PerformanceMonitoringIntervalSeconds": 30,
    "AlertCheckIntervalMinutes": 5,
    "MaxRetryAttempts": 3
  },
  "Quartz": {
    "quartz.scheduler.instanceName": "DBADashScheduler",
    "quartz.threadPool.threadCount": 5
  },
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      { "Name": "Console" },
      { 
        "Name": "File", 
        "Args": { 
          "path": "logs/dbadash-.log",
          "rollingInterval": "Day" 
        }
      }
    ]
  }
}
```

### Environment Variables

For Docker deployment:

```env
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=Server=...
MonitoringSettings__DataCollectionIntervalMinutes=1
Serilog__MinimumLevel=Information
```

## 🔍 Monitoring Features

### Real-time Dashboard
- **Instance Overview**: Status, version, uptime
- **Performance Metrics**: CPU, memory, connections, blocking
- **Alert Summary**: Active alerts with severity indicators
- **Recent Activity**: Latest backups, configuration changes

### Performance Monitoring
- **Live Metrics**: Real-time CPU, memory, and I/O statistics
- **Historical Trends**: Performance graphs with customizable time ranges
- **Blocked Processes**: Current blocking chains with wait information
- **Expensive Queries**: Top resource-consuming queries with execution plans

### Backup Monitoring
- **Backup History**: Complete backup timeline with size and duration
- **Missing Backups**: Databases without recent backups
- **Backup Validation**: Integrity check status and results
- **Restore Testing**: Automated restore verification

### Alert Management
- **Real-time Alerts**: Instant notifications via SignalR
- **Alert Rules**: Configurable thresholds and conditions
- **Acknowledgment**: Alert workflow with comments and tracking
- **Escalation**: Multi-level alerting with severity-based routing

## 🔧 Development

### Prerequisites
- .NET 8.0 SDK
- Visual Studio 2022 or VS Code
- SQL Server (local or remote)

### Running Locally

1. **Clone the repository**
2. **Update connection string** in `appsettings.Development.json`
3. **Start the application**:
   ```bash
   dotnet run --project DBADash.Web.Server
   ```
4. **Access**: https://localhost:7001

### Building for Production

```bash
# Restore packages
dotnet restore

# Build solution
dotnet build -c Release

# Run tests
dotnet test

# Publish
dotnet publish DBADash.Web.Server -c Release -o ./publish
```

## 📊 Performance Considerations

### Scaling
- **Horizontal Scaling**: Multiple server instances with load balancer
- **SignalR Scale-out**: Redis backplane for multi-server deployments
- **Database Optimization**: Proper indexing and query optimization
- **Caching**: In-memory caching for frequently accessed data

### Resource Requirements
- **Memory**: 512MB minimum, 2GB recommended
- **CPU**: 2+ cores recommended for production
- **Disk**: 10GB+ for logs and temporary files
- **Network**: Low latency connection to monitored SQL Servers

## 🔒 Security

### Authentication & Authorization
- Integrated Windows Authentication support
- JWT token-based authentication for API
- Role-based access control (RBAC)
- SQL Server authentication for database connections

### Security Headers
- HTTPS enforcement
- Content Security Policy (CSP)
- X-Frame-Options protection
- XSS protection headers

### Network Security
- TLS 1.2+ encryption
- Rate limiting on API endpoints
- Firewall rules for SQL Server access
- VPN/private network recommendations

## 🐛 Troubleshooting

### Common Issues

1. **Connection Issues**:
   ```bash
   # Test SQL Server connectivity
   docker exec -it dbadash-web dotnet DBADash.Web.Server.dll --test-connection
   ```

2. **Performance Issues**:
   ```bash
   # Check container resources
   docker stats dbadash-web
   
   # Review logs
   docker logs dbadash-web
   ```

3. **SignalR Issues**:
   - Verify WebSocket support in proxy configuration
   - Check firewall rules for WebSocket connections
   - Review browser developer tools for connection errors

### Logging

Application logs are available in:
- **Docker**: `docker logs dbadash-web`
- **File System**: `logs/dbadash-{date}.log`
- **Console**: Real-time output during development

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## 📝 License

This project maintains the same license as the original DBADash project.

## 🔗 Related Links

- [Original DBADash Project](https://github.com/trimble-oss/dba-dash)
- [Blazor Documentation](https://docs.microsoft.com/aspnet/core/blazor)
- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core)
- [Docker Documentation](https://docs.docker.com/)

---

**Note**: This web version maintains compatibility with existing DBADash databases and can run alongside the original Windows application during migration.
