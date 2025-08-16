# DBA Dash Modern UI Build Script
# This script should be run on Windows with Visual Studio 2022 and .NET 8.0 SDK installed

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    
    [Parameter(Mandatory=$false)]
    [switch]$Clean,
    
    [Parameter(Mandatory=$false)]
    [switch]$Restore,
    
    [Parameter(Mandatory=$false)]
    [switch]$Build,
    
    [Parameter(Mandatory=$false)]
    [switch]$Run,
    
    [Parameter(Mandatory=$false)]
    [switch]$Package,
    
    [Parameter(Mandatory=$false)]
    [switch]$All
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Project paths
$ProjectPath = "DBADash.ModernUI.csproj"
$SolutionPath = "..\DBADash.sln"

Write-Host "🚀 DBA Dash Modern UI Build Script" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow

# Check if we're on Windows
if ($PSVersionTable.Platform -and $PSVersionTable.Platform -ne "Win32NT") {
    Write-Error "❌ This script must be run on Windows. WinUI 3 is Windows-only."
    exit 1
}

# Check for .NET SDK
try {
    $dotnetVersion = dotnet --version
    Write-Host "✅ .NET SDK Version: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Error "❌ .NET SDK not found. Please install .NET 8.0 SDK or later."
    exit 1
}

# Set default actions if none specified
if (-not ($Clean -or $Restore -or $Build -or $Run -or $Package)) {
    $All = $true
}

if ($All) {
    $Clean = $true
    $Restore = $true
    $Build = $true
}

try {
    # Clean
    if ($Clean) {
        Write-Host "🧹 Cleaning project..." -ForegroundColor Yellow
        dotnet clean $ProjectPath -c $Configuration
        if ($LASTEXITCODE -ne 0) { throw "Clean failed" }
        Write-Host "✅ Clean completed" -ForegroundColor Green
    }
    

    # Restore packages
    if ($Restore) {
        Write-Host "📦 Restoring packages..." -ForegroundColor Yellow
        dotnet restore $ProjectPath
        if ($LASTEXITCODE -ne 0) { throw "Restore failed" }
        Write-Host "✅ Packages restored" -ForegroundColor Green
    }

    # Build
    if ($Build) {
        Write-Host "🔨 Building project..." -ForegroundColor Yellow
        dotnet build $ProjectPath -c $Configuration --no-restore
        if ($LASTEXITCODE -ne 0) { throw "Build failed" }
        Write-Host "✅ Build completed" -ForegroundColor Green
    }

    # Run
    if ($Run) {
        Write-Host "▶️ Starting application..." -ForegroundColor Yellow
        Start-Process -FilePath "dotnet" -ArgumentList "run", "--project", $ProjectPath, "-c", $Configuration
        Write-Host "✅ Application started" -ForegroundColor Green
    }

    # Package
    if ($Package) {
        Write-Host "📦 Creating deployment package..." -ForegroundColor Yellow
        
        $PublishPath = "bin\$Configuration\net8.0-windows10.0.19041.0\win-x64\publish"
        
        # Create MSIX package
        dotnet publish $ProjectPath -c $Configuration -r win-x64 --self-contained
        if ($LASTEXITCODE -ne 0) { throw "Publish failed" }
        
        Write-Host "✅ Package created at: $PublishPath" -ForegroundColor Green
        
        # Open publish folder
        if (Test-Path $PublishPath) {
            Start-Process explorer.exe -ArgumentList $PublishPath
        }
    }

    Write-Host "🎉 All operations completed successfully!" -ForegroundColor Green

} catch {
    Write-Error "❌ Build failed: $_"
    exit 1
}


# Display usage if no parameters were provided
if ($args.Count -eq 0 -and -not $All) {
    Write-Host ""
    Write-Host "Usage Examples:" -ForegroundColor Cyan
    Write-Host "  .\build.ps1 -All                    # Clean, restore, and build"
    Write-Host "  .\build.ps1 -Build -Run             # Build and run"
    Write-Host "  .\build.ps1 -Configuration Release  # Build in Release mode"
    Write-Host "  .\build.ps1 -Package                # Create deployment package"
    Write-Host "  .\build.ps1 -Clean -Restore         # Clean and restore only"
}
