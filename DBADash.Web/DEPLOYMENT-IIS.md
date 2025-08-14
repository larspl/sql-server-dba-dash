# DBADash Web Deployment Guide for IIS

This guide provides comprehensive instructions for deploying the DBADash Web application to Internet Information Services (IIS) on Windows Server.

## 📋 Prerequisites

### System Requirements
- **Windows Server 2016** or later (Windows 10/11 for development)
- **IIS 10.0** or later with ASP.NET Core Hosting Bundle
- **SQL Server 2016** or later (for DBADash repository database)
- **PowerShell 5.1** or later (for automated deployment)

### Required Components
1. **.NET 8.0 Hosting Bundle**
   - Download from: https://dotnet.microsoft.com/download/dotnet/8.0
   - Install the "Hosting Bundle" (not just the runtime)

2. **IIS Features**
   ```powershell
   # Enable required IIS features
   Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole,IIS-WebServer,IIS-CommonHttpFeatures,IIS-HttpErrors,IIS-HttpLogging,IIS-RequestFiltering,IIS-StaticContent,IIS-DefaultDocument,IIS-DirectoryBrowsing,IIS-NetFxExtensibility45,IIS-ISAPIExtensions,IIS-ISAPIFilter,IIS-NetFxExtensibility,IIS-ASPNET,IIS-ASPNET45,IIS-WebSockets
   ```

3. **IIS Management Tools**
   ```powershell
   Enable-WindowsOptionalFeature -Online -FeatureName IIS-ManagementConsole,IIS-IIS6ManagementCompatibility,IIS-Metabase
   ```

## 🚀 Automated Deployment

### Option 1: PowerShell Script (Recommended)

```powershell
# Run as Administrator
.\deploy-iis.ps1 -SiteName "Default Web Site" -AppName "DBADash" -DeployPath "C:\inetpub\wwwroot\DBADash"
```

**Parameters:**
- `-SiteName`: IIS site name (default: "Default Web Site")
- `-AppName`: Application name (default: "DBADash")
- `-AppPoolName`: Application pool name (default: "DBADash_AppPool")
- `-DeployPath`: Deployment directory (default: "C:\inetpub\wwwroot\DBADash")
- `-CreateBackup`: Create backup of existing deployment (default: true)
- `-SkipBuild`: Skip build and use existing published files
- `-Force`: Overwrite existing configuration

### Option 2: Batch Script

```cmd
REM Run as Administrator
deploy-iis.bat
```

## 🔧 Manual Deployment

### Step 1: Build and Publish

```powershell
# Navigate to the web project
cd DBADash.Web\DBADash.Web.Server

# Clean and restore
dotnet clean -c Release
dotnet restore

# Build
dotnet build -c Release --no-restore

# Publish to deployment directory
dotnet publish -c Release -o "C:\inetpub\wwwroot\DBADash" --no-build
```

### Step 2: Create Application Pool

```powershell
# Import WebAdministration module
Import-Module WebAdministration

# Create application pool
New-WebAppPool -Name "DBADash_AppPool"

# Configure for .NET Core
Set-ItemProperty -Path "IIS:\AppPools\DBADash_AppPool" -Name managedRuntimeVersion -Value ""
Set-ItemProperty -Path "IIS:\AppPools\DBADash_AppPool" -Name processModel.identityType -Value ApplicationPoolIdentity
```

### Step 3: Create Web Application

```powershell
# Create application
New-WebApplication -Site "Default Web Site" -Name "DBADash" -PhysicalPath "C:\inetpub\wwwroot\DBADash" -ApplicationPool "DBADash_AppPool"
```

### Step 4: Set Permissions

```powershell
# Grant permissions to application pool identity
icacls "C:\inetpub\wwwroot\DBADash" /grant "IIS AppPool\DBADash_AppPool:(OI)(CI)F" /T
icacls "C:\inetpub\wwwroot\DBADash\logs" /grant "IIS AppPool\DBADash_AppPool:(OI)(CI)F" /T
```

## ⚙️ Configuration

### Connection String Configuration

Edit `appsettings.Production.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-sql-server;Database=DBADash;Integrated Security=true;TrustServerCertificate=true;"
  }
}
```

**Authentication Options:**

1. **Windows Authentication (Recommended)**
   ```json
   "DefaultConnection": "Server=server;Database=DBADash;Integrated Security=true;TrustServerCertificate=true;"
   ```

2. **SQL Server Authentication**
   ```json
   "DefaultConnection": "Server=server;Database=DBADash;User Id=dbadash_user;Password=your_password;TrustServerCertificate=true;"
   ```

### Application Pool Identity Database Access

Grant database permissions to the application pool identity:

```sql
-- Create login for application pool identity
CREATE LOGIN [IIS AppPool\DBADash_AppPool] FROM WINDOWS;

-- Grant database access
USE DBADash;
CREATE USER [IIS AppPool\DBADash_AppPool] FOR LOGIN [IIS AppPool\DBADash_AppPool];

-- Grant required permissions
ALTER ROLE db_datareader ADD MEMBER [IIS AppPool\DBADash_AppPool];
ALTER ROLE db_datawriter ADD MEMBER [IIS AppPool\DBADash_AppPool];
ALTER ROLE db_executor ADD MEMBER [IIS AppPool\DBADash_AppPool];
```

## 🔒 Security Configuration

### SSL/TLS Setup

1. **Obtain SSL Certificate**
   - Use Let's Encrypt, commercial CA, or self-signed for testing

2. **Bind Certificate to Site**
   ```powershell
   # List available certificates
   Get-ChildItem Cert:\LocalMachine\My

   # Bind certificate to site
   New-WebBinding -Name "Default Web Site" -Protocol https -Port 443
   ```

3. **Force HTTPS** (already configured in web.config)

### Firewall Configuration

```powershell
# Allow HTTP/HTTPS traffic
New-NetFirewallRule -DisplayName "DBADash HTTP" -Direction Inbound -Protocol TCP -LocalPort 80 -Action Allow
New-NetFirewallRule -DisplayName "DBADash HTTPS" -Direction Inbound -Protocol TCP -LocalPort 443 -Action Allow
```

## 📊 Monitoring and Troubleshooting

### Application Logs

Logs are written to: `C:\inetpub\wwwroot\DBADash\logs\`

- `dbadash-{date}.log` - General application logs
- `dbadash-errors-{date}.log` - Error logs only

### IIS Logs

Located in: `C:\inetpub\logs\LogFiles\W3SVC1\`

### Event Viewer

Check Windows Event Viewer:
- Application and Services Logs → Microsoft → Windows → IIS-W3SVC-WP

### Common Issues

1. **500.30 Error - ASP.NET Core app failed to start**
   - Check .NET 8.0 Hosting Bundle is installed
   - Verify application pool identity has permissions
   - Check application logs for detailed error

2. **502.5 Error - Process failure**
   - Verify web.config points to correct DLL
   - Check application pool configuration
   - Ensure .NET 8.0 runtime is available

3. **Database Connection Issues**
   - Verify connection string format
   - Check SQL Server allows connections
   - Validate application pool identity has database access

4. **SignalR Connection Issues**
   - Ensure WebSockets are enabled in IIS
   - Check firewall allows WebSocket traffic
   - Verify ARR (Application Request Routing) settings if using load balancer

### Performance Monitoring

```powershell
# Monitor application pool
Get-Counter "\Process(w3wp*)\% Processor Time"
Get-Counter "\Process(w3wp*)\Working Set"

# Monitor IIS
Get-Counter "\Web Service(_Total)\Current Connections"
Get-Counter "\Web Service(_Total)\Bytes Received/sec"
```

## 🔄 Updates and Maintenance

### Application Updates

1. **Stop Application Pool**
   ```powershell
   Stop-WebAppPool -Name "DBADash_AppPool"
   ```

2. **Deploy New Version**
   ```powershell
   dotnet publish -c Release -o "C:\inetpub\wwwroot\DBADash" --no-build
   ```

3. **Start Application Pool**
   ```powershell
   Start-WebAppPool -Name "DBADash_AppPool"
   ```

### Database Schema Updates

The application automatically handles database schema updates via Entity Framework migrations on startup.

## 📞 Support

For deployment issues:

1. Check application logs first
2. Verify all prerequisites are installed
3. Test with a simple ASP.NET Core app to isolate IIS issues
4. Review IIS and Windows Event Logs

---

**Note**: This deployment creates the web interface for DBADash. The data collection service (DBADashService) should be configured separately to populate the monitoring data.
