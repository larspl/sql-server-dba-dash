# Quick Deployment Commands for DBADash Web on IIS

## Prerequisites Check
```powershell
# Check .NET 8.0 Hosting Bundle
Get-ChildItem "HKLM:\SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost" | Get-ItemProperty | Where-Object {$_.Version -like "8.*"}

# Check IIS Service
Get-Service W3SVC

# Enable IIS features (run as Administrator)
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole,IIS-WebServer,IIS-CommonHttpFeatures,IIS-HttpErrors,IIS-HttpLogging,IIS-RequestFiltering,IIS-StaticContent,IIS-DefaultDocument,IIS-ASPNET45,IIS-NetFxExtensibility45,IIS-ISAPIExtensions,IIS-ISAPIFilter,IIS-WebSockets,IIS-ManagementConsole
```

## Automated Deployment
```powershell
# Standard deployment
.\deploy-iis.ps1

# Custom deployment
.\deploy-iis.ps1 -SiteName "Default Web Site" -AppName "DBADash" -DeployPath "C:\inetpub\wwwroot\DBADash" -AppPoolName "DBADash_AppPool"

# Skip build (use existing published files)
.\deploy-iis.ps1 -SkipBuild

# Force overwrite existing configuration
.\deploy-iis.ps1 -Force
```

## Manual Commands
```powershell
# Build and publish
cd DBADash.Web\DBADash.Web.Server
dotnet publish -c Release -o "C:\inetpub\wwwroot\DBADash"

# Create Application Pool
Import-Module WebAdministration
New-WebAppPool -Name "DBADash_AppPool"
Set-ItemProperty -Path "IIS:\AppPools\DBADash_AppPool" -Name managedRuntimeVersion -Value ""

# Create Web Application  
New-WebApplication -Site "Default Web Site" -Name "DBADash" -PhysicalPath "C:\inetpub\wwwroot\DBADash" -ApplicationPool "DBADash_AppPool"

# Set Permissions
icacls "C:\inetpub\wwwroot\DBADash" /grant "IIS AppPool\DBADash_AppPool:(OI)(CI)F" /T
```

## Database Setup
```sql
-- Grant database access to application pool identity
CREATE LOGIN [IIS AppPool\DBADash_AppPool] FROM WINDOWS;
USE DBADash;
CREATE USER [IIS AppPool\DBADash_AppPool] FOR LOGIN [IIS AppPool\DBADash_AppPool];
ALTER ROLE db_datareader ADD MEMBER [IIS AppPool\DBADash_AppPool];
ALTER ROLE db_datawriter ADD MEMBER [IIS AppPool\DBADash_AppPool];
ALTER ROLE db_executor ADD MEMBER [IIS AppPool\DBADash_AppPool];
```

## Configuration
Update `appsettings.Production.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-server;Database=DBADash;Integrated Security=true;TrustServerCertificate=true;"
  }
}
```

## Troubleshooting
```powershell
# Check application pool status
Get-WebAppPoolState -Name "DBADash_AppPool"

# View recent logs
Get-Content "C:\inetpub\wwwroot\DBADash\logs\dbadash-*.log" -Tail 50

# Check IIS logs
Get-Content "C:\inetpub\logs\LogFiles\W3SVC1\*.log" -Tail 20

# Test application
Invoke-WebRequest -Uri "http://localhost/DBADash" -UseBasicParsing
```

## Access URLs
- **Application**: http://localhost/DBADash
- **Health Check**: http://localhost/DBADash/health
- **API**: http://localhost/DBADash/api/

---
**Note**: Run PowerShell as Administrator for deployment commands.
