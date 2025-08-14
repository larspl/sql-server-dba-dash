# DBADash Copilot Instructions

This repository contains **DBADash**, a SQL Server monitoring tool with both a traditional Windows application and a modern **web-based architecture**.

## 🏗️ Architecture Overview

### Dual Architecture Pattern
- **Legacy**: Windows Forms GUI (`DBADashGUI/`) + Windows Service (`DBADashService/`) + Shared Library (`DBADash/`)
- **Modern Web**: Hybrid Blazor (`DBADash.Web/`) with Server/WebAssembly modes + Background Services + SignalR real-time updates

### Key Data Flow
1. **Windows Service/Background Service** → Collects SQL Server metrics via SMO
2. **Repository Database** → Central SQL Server database (`DBADashDB/`) stores all monitoring data
3. **GUI/Web UI** → Displays dashboards, alerts, and historical data via direct DB queries

## 🎯 Web Migration Patterns

### Service Architecture (DBADash.Web/)
```
DBADash.Web.Server/     # Main ASP.NET Core app (hybrid Blazor + API)
├── Program.cs          # DI configuration, Quartz jobs, SignalR hubs
├── Services/           # MonitoringBackgroundService replaces Windows Service
└── Hubs/              # Real-time updates (MonitoringHub, AlertHub)

DBADash.Web.Core/      # Business logic & data access
├── Services/          # Service interfaces & implementations
├── Data/             # Entity Framework DbContext
└── IServices.cs      # Service contracts

DBADash.Web.Shared/   # DTOs shared between Server/Client
└── Models/           # Data transfer objects (InstanceDto, AlertDto, etc.)
```

### Critical Patterns
- **Hybrid Blazor**: Both Server (`MapBlazorHub()`) and WebAssembly (`UseBlazorFrameworkFiles()`) support in same app
- **Background Processing**: `MonitoringBackgroundService` + Quartz jobs replace Windows Service functionality
- **Real-time Updates**: SignalR hubs push performance data and alerts to connected clients
- **Service Layer**: Repository pattern with scoped services injected via DI

## 🔧 Development Workflows

### Running Web Application
```bash
cd DBADash.Web
dotnet run --project DBADash.Web.Server   # Development server
```

### Docker Development
```bash
cd DBADash.Web
docker-compose up -d   # Full stack with SQL Server + Redis + Nginx
```

### Database Schema
- **Existing DBADash DB**: Entity Framework maps to existing schema via `DBADashContext`
- **Entity Naming**: `InstanceEntity` → `Instances` table, `AlertEntity` → `Alerts` table
- **Performance Indexes**: Critical for `PerformanceSnapshots`, `Backups`, `Alerts` queries

## 🚨 Integration Points

### Windows Service Data Collection (`DBADash/`)
- **SMO Usage**: `SchemaSnapshotDB.cs`, `PerformanceCounters.cs` use SQL Server Management Objects
- **Data Import**: `DBImporter.cs` handles bulk inserts to repository database
- **Configuration**: `CollectionConfig.cs` manages source connections and schedules

### SignalR Real-time Communication
```csharp
// Background service pushes to all clients
await _hubContext.Clients.All.SendAsync("PerformanceUpdate", data);

// Clients can join instance-specific groups
await Groups.AddToGroupAsync(Context.ConnectionId, $"Instance_{instanceId}");
```

### Cross-Component Data Models
- **Original**: DataTables + raw SQL queries (`CommonData.cs` patterns)
- **Web**: DTOs + Entity Framework (`InstanceDto`, `PerformanceSnapshotDto`)
- **Shared Interfaces**: `IInstanceService`, `IPerformanceService` abstract data access

## 🔍 Code Conventions

### Dependency Injection Pattern
```csharp
// Service registration in Program.cs
builder.Services.AddScoped<IInstanceService, InstanceService>();

// Service usage with scoped lifetime
using var scope = _scopeFactory.CreateScope();
var service = scope.ServiceProvider.GetRequiredService<IInstanceService>();
```

### Background Service Pattern
- **IHostedService**: For long-running background processes
- **Quartz.NET**: For scheduled jobs with cron expressions
- **Service Scope**: Always create scopes for scoped services in background threads

### Entity Framework Conventions
- **Entities**: End with `Entity` suffix, map to existing DBA Dash schema
- **DbSets**: Plural names matching table names
- **Relationships**: FK constraints with cascade delete for instance-related data

## ⚡ Performance Considerations

### Data Collection Efficiency
- **Bulk Operations**: Use `DataTable` patterns from original codebase for large datasets
- **Connection Pooling**: Reuse SQL connections in background services
- **Memory Management**: Dispose of SMO objects properly in collection loops

### Real-time Updates Optimization
- **SignalR Groups**: Target specific instance monitoring groups, not broadcast to all
- **Data Throttling**: Limit update frequency for high-frequency metrics
- **Background Processing**: Use Quartz for heavy data collection, SignalR for lightweight updates

## 🔧 Debugging & Troubleshooting

### Common Issues
- **Service Scope**: Background services need `IServiceScopeFactory` for scoped dependencies
- **Entity Tracking**: Disable change tracking for read-only Entity Framework queries
- **Connection Strings**: Web app uses different connection format than Windows service config

### Logging Patterns
```csharp
// Structured logging with Serilog
_logger.LogInformation("Monitoring cycle completed for {InstanceCount} instances", instances.Count());
_logger.LogError(ex, "Failed to collect data for instance {InstanceId}", instanceId);
```

---

When working on this codebase, prioritize understanding the **data flow from SQL Server → Repository DB → Web UI** and the **service layer abstractions** that enable both Windows and Web architectures to coexist.
