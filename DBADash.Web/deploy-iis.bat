@echo off
setlocal enabledelayedexpansion

echo ===========================================
echo DBADash Web Deployment Script for IIS
echo ===========================================
echo.

REM Check if running as administrator
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo ERROR: This script must be run as Administrator
    echo Please run Command Prompt as Administrator and try again.
    pause
    exit /b 1
)

REM Configuration variables
set "APP_NAME=DBADash"
set "SITE_NAME=Default Web Site"
set "APP_POOL_NAME=DBADash_AppPool"
set "DEPLOY_PATH=C:\inetpub\wwwroot\DBADash"
set "PROJECT_PATH=%~dp0DBADash.Web.Server"
set "BACKUP_PATH=C:\DBADash_Backup_%date:~-4,4%%date:~-10,2%%date:~-7,2%_%time:~0,2%%time:~3,2%%time:~6,2%"

echo Configuration:
echo - Application Name: %APP_NAME%
echo - Site Name: %SITE_NAME%
echo - Application Pool: %APP_POOL_NAME%
echo - Deploy Path: %DEPLOY_PATH%
echo - Project Path: %PROJECT_PATH%
echo - Backup Path: %BACKUP_PATH%
echo.

REM Check prerequisites
echo Checking prerequisites...

REM Check if .NET 8.0 Hosting Bundle is installed
reg query "HKEY_LOCAL_MACHINE\SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost" /v Version 2>nul | findstr "8." >nul
if %errorLevel% neq 0 (
    echo ERROR: .NET 8.0 Hosting Bundle is not installed
    echo Please download and install from: https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

REM Check if IIS is installed and running
sc query W3SVC | findstr "RUNNING" >nul
if %errorLevel% neq 0 (
    echo ERROR: IIS is not running or not installed
    echo Please install IIS with ASP.NET Core features
    pause
    exit /b 1
)

REM Check if project exists
if not exist "%PROJECT_PATH%" (
    echo ERROR: Project path not found: %PROJECT_PATH%
    pause
    exit /b 1
)

echo Prerequisites check completed successfully.
echo.

REM Backup existing deployment if it exists
if exist "%DEPLOY_PATH%" (
    echo Creating backup of existing deployment...
    xcopy "%DEPLOY_PATH%" "%BACKUP_PATH%" /E /I /Q
    if %errorLevel% neq 0 (
        echo WARNING: Backup failed, continuing anyway...
    ) else (
        echo Backup created at: %BACKUP_PATH%
    )
    echo.
)

REM Stop application pool if it exists
echo Stopping application pool if it exists...
%windir%\system32\inetsrv\appcmd list apppool "%APP_POOL_NAME%" >nul 2>&1
if %errorLevel% equ 0 (
    %windir%\system32\inetsrv\appcmd stop apppool "%APP_POOL_NAME%"
    timeout /t 5 /nobreak >nul
)

REM Create application pool
echo Creating/updating application pool...
%windir%\system32\inetsrv\appcmd list apppool "%APP_POOL_NAME%" >nul 2>&1
if %errorLevel% neq 0 (
    %windir%\system32\inetsrv\appcmd add apppool /name:"%APP_POOL_NAME%"
)

REM Configure application pool for .NET Core
%windir%\system32\inetsrv\appcmd set apppool "%APP_POOL_NAME%" /managedRuntimeVersion:""
%windir%\system32\inetsrv\appcmd set apppool "%APP_POOL_NAME%" /processModel.identityType:ApplicationPoolIdentity
%windir%\system32\inetsrv\appcmd set apppool "%APP_POOL_NAME%" /recycling.periodicRestart.time:00:00:00
%windir%\system32\inetsrv\appcmd set apppool "%APP_POOL_NAME%" /processModel.idleTimeout:00:00:00

echo Application pool configured successfully.
echo.

REM Build and publish the application
echo Building and publishing application...
cd /d "%PROJECT_PATH%"

REM Clean previous builds
dotnet clean -c Release
if %errorLevel% neq 0 (
    echo ERROR: Failed to clean project
    pause
    exit /b 1
)

REM Restore packages
dotnet restore
if %errorLevel% neq 0 (
    echo ERROR: Failed to restore packages
    pause
    exit /b 1
)

REM Build application
dotnet build -c Release --no-restore
if %errorLevel% neq 0 (
    echo ERROR: Failed to build application
    pause
    exit /b 1
)

REM Publish application
dotnet publish -c Release -o "%DEPLOY_PATH%" --no-build
if %errorLevel% neq 0 (
    echo ERROR: Failed to publish application
    pause
    exit /b 1
)

echo Application published successfully.
echo.

REM Create logs directory
if not exist "%DEPLOY_PATH%\logs" (
    mkdir "%DEPLOY_PATH%\logs"
)

REM Set permissions for application pool identity
echo Setting permissions...
icacls "%DEPLOY_PATH%" /grant "IIS AppPool\%APP_POOL_NAME%":(OI)(CI)F /T
icacls "%DEPLOY_PATH%\logs" /grant "IIS AppPool\%APP_POOL_NAME%":(OI)(CI)F /T

echo Permissions set successfully.
echo.

REM Create or update application in IIS
echo Configuring IIS application...
%windir%\system32\inetsrv\appcmd list app "%SITE_NAME%/%APP_NAME%" >nul 2>&1
if %errorLevel% neq 0 (
    %windir%\system32\inetsrv\appcmd add app /site.name:"%SITE_NAME%" /path:"/%APP_NAME%" /physicalPath:"%DEPLOY_PATH%"
) else (
    %windir%\system32\inetsrv\appcmd set app "%SITE_NAME%/%APP_NAME%" /applicationPool:"%APP_POOL_NAME%"
)

%windir%\system32\inetsrv\appcmd set app "%SITE_NAME%/%APP_NAME%" /applicationPool:"%APP_POOL_NAME%"

echo IIS application configured successfully.
echo.

REM Start application pool
echo Starting application pool...
%windir%\system32\inetsrv\appcmd start apppool "%APP_POOL_NAME%"

echo.
echo ===========================================
echo Deployment completed successfully!
echo ===========================================
echo.
echo Application URL: http://localhost/%APP_NAME%
echo Application Path: %DEPLOY_PATH%
echo Application Pool: %APP_POOL_NAME%
echo.
echo Next steps:
echo 1. Update connection strings in appsettings.json
echo 2. Configure SSL certificate if needed
echo 3. Test the application
echo.
echo Backup location (if created): %BACKUP_PATH%
echo.

pause
