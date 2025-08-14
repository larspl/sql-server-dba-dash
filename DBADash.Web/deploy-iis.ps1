# DBADash Web IIS Deployment Script
# PowerShell version for more advanced deployment scenarios

param(
    [string]$SiteName = "Default Web Site",
    [string]$AppName = "DBADash",
    [string]$AppPoolName = "DBADash_AppPool",
    [string]$DeployPath = "C:\inetpub\wwwroot\DBADash",
    [string]$ProjectPath = ".\DBADash.Web.Server",
    [switch]$CreateBackup = $true,
    [switch]$SkipBuild = $false,
    [switch]$Force = $false
)

# Requires PowerShell to be run as Administrator
#Requires -RunAsAdministrator

Write-Host "=========================================" -ForegroundColor Green
Write-Host "DBADash Web IIS Deployment Script" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Green
Write-Host ""

# Import required modules
Import-Module WebAdministration -ErrorAction SilentlyContinue
if (-not (Get-Module WebAdministration)) {
    Write-Error "WebAdministration module not available. Please install IIS Management Tools."
    exit 1
}

# Configuration
$config = @{
    SiteName = $SiteName
    AppName = $AppName
    AppPoolName = $AppPoolName
    DeployPath = $DeployPath
    ProjectPath = (Resolve-Path $ProjectPath).Path
    BackupPath = "C:\DBADash_Backup_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
}

Write-Host "Configuration:" -ForegroundColor Yellow
$config.GetEnumerator() | ForEach-Object { Write-Host "  $($_.Key): $($_.Value)" }
Write-Host ""

# Functions
function Test-Prerequisites {
    Write-Host "Checking prerequisites..." -ForegroundColor Yellow
    
    # Check .NET 8.0 Hosting Bundle
    $hostingBundle = Get-ChildItem "HKLM:\SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost" -ErrorAction SilentlyContinue
    if (-not $hostingBundle -or -not ($hostingBundle.GetValue("Version") -match "^8\.")) {
        throw ".NET 8.0 Hosting Bundle is not installed. Download from: https://dotnet.microsoft.com/download/dotnet/8.0"
    }
    
    # Check IIS
    $iisService = Get-Service W3SVC -ErrorAction SilentlyContinue
    if (-not $iisService -or $iisService.Status -ne "Running") {
        throw "IIS is not running or not installed"
    }
    
    # Check project path
    if (-not (Test-Path $config.ProjectPath)) {
        throw "Project path not found: $($config.ProjectPath)"
    }
    
    # Check dotnet CLI
    try {
        $dotnetVersion = dotnet --version
        Write-Host "  ✓ .NET SDK version: $dotnetVersion" -ForegroundColor Green
    }
    catch {
        throw ".NET SDK not found in PATH"
    }
    
    Write-Host "  ✓ Prerequisites check completed" -ForegroundColor Green
    Write-Host ""
}

function Backup-ExistingDeployment {
    if ($CreateBackup -and (Test-Path $config.DeployPath)) {
        Write-Host "Creating backup..." -ForegroundColor Yellow
        try {
            Copy-Item $config.DeployPath $config.BackupPath -Recurse -Force
            Write-Host "  ✓ Backup created: $($config.BackupPath)" -ForegroundColor Green
        }
        catch {
            Write-Warning "Backup failed: $($_.Exception.Message)"
        }
        Write-Host ""
    }
}

function Stop-ApplicationPool {
    Write-Host "Managing application pool..." -ForegroundColor Yellow
    
    if (Get-IISAppPool -Name $config.AppPoolName -ErrorAction SilentlyContinue) {
        Write-Host "  Stopping existing application pool..." -ForegroundColor Gray
        Stop-WebAppPool -Name $config.AppPoolName -ErrorAction SilentlyContinue
        
        # Wait for pool to stop
        $timeout = 30
        $elapsed = 0
        while ((Get-WebAppPoolState -Name $config.AppPoolName).Value -ne "Stopped" -and $elapsed -lt $timeout) {
            Start-Sleep -Seconds 1
            $elapsed++
        }
        
        if ($elapsed -ge $timeout) {
            Write-Warning "Application pool did not stop within timeout period"
        }
    }
}

function New-ApplicationPool {
    Write-Host "Configuring application pool..." -ForegroundColor Yellow
    
    # Remove existing pool if Force is specified
    if ($Force -and (Get-IISAppPool -Name $config.AppPoolName -ErrorAction SilentlyContinue)) {
        Remove-WebAppPool -Name $config.AppPoolName -Confirm:$false
    }
    
    # Create or update application pool
    if (-not (Get-IISAppPool -Name $config.AppPoolName -ErrorAction SilentlyContinue)) {
        New-WebAppPool -Name $config.AppPoolName
        Write-Host "  ✓ Application pool created" -ForegroundColor Green
    }
    
    # Configure for .NET Core
    Set-ItemProperty -Path "IIS:\AppPools\$($config.AppPoolName)" -Name managedRuntimeVersion -Value ""
    Set-ItemProperty -Path "IIS:\AppPools\$($config.AppPoolName)" -Name processModel.identityType -Value ApplicationPoolIdentity
    Set-ItemProperty -Path "IIS:\AppPools\$($config.AppPoolName)" -Name recycling.periodicRestart.time -Value "00:00:00"
    Set-ItemProperty -Path "IIS:\AppPools\$($config.AppPoolName)" -Name processModel.idleTimeout -Value "00:00:00"
    Set-ItemProperty -Path "IIS:\AppPools\$($config.AppPoolName)" -Name processModel.maxProcesses -Value 1
    
    Write-Host "  ✓ Application pool configured for .NET Core" -ForegroundColor Green
    Write-Host ""
}

function Build-Application {
    if ($SkipBuild) {
        Write-Host "Skipping build (SkipBuild specified)" -ForegroundColor Yellow
        return
    }
    
    Write-Host "Building and publishing application..." -ForegroundColor Yellow
    
    Push-Location $config.ProjectPath
    try {
        # Clean
        Write-Host "  Cleaning..." -ForegroundColor Gray
        dotnet clean -c Release | Out-Host
        if ($LASTEXITCODE -ne 0) { throw "Clean failed" }
        
        # Restore
        Write-Host "  Restoring packages..." -ForegroundColor Gray
        dotnet restore | Out-Host
        if ($LASTEXITCODE -ne 0) { throw "Restore failed" }
        
        # Build
        Write-Host "  Building..." -ForegroundColor Gray
        dotnet build -c Release --no-restore | Out-Host
        if ($LASTEXITCODE -ne 0) { throw "Build failed" }
        
        # Publish
        Write-Host "  Publishing..." -ForegroundColor Gray
        dotnet publish -c Release -o $config.DeployPath --no-build | Out-Host
        if ($LASTEXITCODE -ne 0) { throw "Publish failed" }
        
        Write-Host "  ✓ Application published successfully" -ForegroundColor Green
    }
    finally {
        Pop-Location
    }
    Write-Host ""
}

function Set-Permissions {
    Write-Host "Setting permissions..." -ForegroundColor Yellow
    
    # Create logs directory
    $logsPath = Join-Path $config.DeployPath "logs"
    if (-not (Test-Path $logsPath)) {
        New-Item -Path $logsPath -ItemType Directory -Force | Out-Null
    }
    
    # Set permissions for application pool identity
    $appPoolIdentity = "IIS AppPool\$($config.AppPoolName)"
    
    # Grant full control to deployment directory
    icacls $config.DeployPath /grant "$($appPoolIdentity):(OI)(CI)F" /T | Out-Null
    icacls $logsPath /grant "$($appPoolIdentity):(OI)(CI)F" /T | Out-Null
    
    Write-Host "  ✓ Permissions set for application pool identity" -ForegroundColor Green
    Write-Host ""
}

function New-WebApplication {
    Write-Host "Configuring IIS application..." -ForegroundColor Yellow
    
    $appPath = "/$($config.AppName)"
    $fullAppName = "$($config.SiteName)$appPath"
    
    # Remove existing application if Force is specified
    if ($Force -and (Get-WebApplication -Site $config.SiteName -Name $config.AppName -ErrorAction SilentlyContinue)) {
        Remove-WebApplication -Site $config.SiteName -Name $config.AppName -Confirm:$false
    }
    
    # Create or update application
    if (-not (Get-WebApplication -Site $config.SiteName -Name $config.AppName -ErrorAction SilentlyContinue)) {
        New-WebApplication -Site $config.SiteName -Name $config.AppName -PhysicalPath $config.DeployPath -ApplicationPool $config.AppPoolName
        Write-Host "  ✓ Web application created" -ForegroundColor Green
    } else {
        Set-ItemProperty -Path "IIS:\Sites\$($config.SiteName)\$($config.AppName)" -Name applicationPool -Value $config.AppPoolName
        Write-Host "  ✓ Web application updated" -ForegroundColor Green
    }
    
    Write-Host ""
}

function Start-ApplicationPool {
    Write-Host "Starting application pool..." -ForegroundColor Yellow
    
    Start-WebAppPool -Name $config.AppPoolName
    
    # Wait for pool to start
    $timeout = 30
    $elapsed = 0
    while ((Get-WebAppPoolState -Name $config.AppPoolName).Value -ne "Started" -and $elapsed -lt $timeout) {
        Start-Sleep -Seconds 1
        $elapsed++
    }
    
    if ((Get-WebAppPoolState -Name $config.AppPoolName).Value -eq "Started") {
        Write-Host "  ✓ Application pool started successfully" -ForegroundColor Green
    } else {
        Write-Warning "Application pool may not have started properly"
    }
    
    Write-Host ""
}

function Show-Summary {
    Write-Host "=========================================" -ForegroundColor Green
    Write-Host "Deployment completed successfully!" -ForegroundColor Green
    Write-Host "=========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Application Details:" -ForegroundColor Yellow
    Write-Host "  URL: http://localhost/$($config.AppName)" -ForegroundColor Cyan
    Write-Host "  Physical Path: $($config.DeployPath)" -ForegroundColor Gray
    Write-Host "  Application Pool: $($config.AppPoolName)" -ForegroundColor Gray
    Write-Host "  Site: $($config.SiteName)" -ForegroundColor Gray
    
    if ($CreateBackup -and (Test-Path $config.BackupPath)) {
        Write-Host "  Backup: $($config.BackupPath)" -ForegroundColor Gray
    }
    
    Write-Host ""
    Write-Host "Next Steps:" -ForegroundColor Yellow
    Write-Host "  1. Update connection strings in appsettings.json" -ForegroundColor Gray
    Write-Host "  2. Configure SSL certificate if needed" -ForegroundColor Gray
    Write-Host "  3. Test the application" -ForegroundColor Gray
    Write-Host "  4. Monitor application logs in: $($config.DeployPath)\logs" -ForegroundColor Gray
    Write-Host ""
}

# Main execution
try {
    Test-Prerequisites
    Backup-ExistingDeployment
    Stop-ApplicationPool
    New-ApplicationPool
    Build-Application
    Set-Permissions
    New-WebApplication
    Start-ApplicationPool
    Show-Summary
}
catch {
    Write-Error "Deployment failed: $($_.Exception.Message)"
    exit 1
}
