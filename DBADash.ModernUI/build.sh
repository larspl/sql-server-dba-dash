#!/bin/bash
# DBA Dash Modern UI Build Script for Unix-like systems
# Note: This script is for development/validation only
# The actual application can only be built and run on Windows

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Configuration
CONFIGURATION="${1:-Debug}"
PROJECT_PATH="DBADash.ModernUI.csproj"

echo -e "${CYAN}🚀 DBA Dash Modern UI Build Script${NC}"
echo -e "${YELLOW}Configuration: $CONFIGURATION${NC}"

# Check if we're on a non-Windows platform
if [[ "$OSTYPE" != "msys" && "$OSTYPE" != "win32" && "$OSTYPE" != "cygwin" ]]; then
    echo -e "${RED}❌ Warning: WinUI 3 applications can only be built and run on Windows.${NC}"
    echo -e "${YELLOW}This script will validate the project structure but cannot build the application.${NC}"
    VALIDATION_ONLY=true
fi

# Check for .NET SDK
if command -v dotnet &> /dev/null; then
    DOTNET_VERSION=$(dotnet --version)
    echo -e "${GREEN}✅ .NET SDK Version: $DOTNET_VERSION${NC}"
else
    echo -e "${RED}❌ .NET SDK not found. Please install .NET 8.0 SDK or later.${NC}"
    exit 1
fi

# Validate project structure
echo -e "${YELLOW}🔍 Validating project structure...${NC}"

if [[ ! -f "$PROJECT_PATH" ]]; then
    echo -e "${RED}❌ Project file not found: $PROJECT_PATH${NC}"
    exit 1
fi

# Check for required files
REQUIRED_FILES=(
    "App.xaml"
    "App.xaml.cs"
    "Views/MainWindow.xaml"
    "Views/MainWindow.xaml.cs"
    "ViewModels/MainViewModel.cs"
    "Services/NavigationService.cs"
    "Services/ThemeService.cs"
    "Services/DataService.cs"
    "Services/DialogService.cs"
    "Themes/ModernTheme.xaml"
)

for file in "${REQUIRED_FILES[@]}"; do
    if [[ -f "$file" ]]; then
        echo -e "${GREEN}✅ Found: $file${NC}"
    else
        echo -e "${RED}❌ Missing: $file${NC}"
    fi
done

# Check for asset files
echo -e "${YELLOW}📁 Checking asset files...${NC}"
ASSET_FILES=(
    "Assets/SplashScreen.scale-200.png"
    "Assets/Square150x150Logo.scale-200.png"
    "Assets/Square44x44Logo.scale-200.png"
    "Assets/StoreLogo.png"
)

for asset in "${ASSET_FILES[@]}"; do
    if [[ -f "$asset" ]]; then
        echo -e "${GREEN}✅ Found: $asset${NC}"
    else
        echo -e "${YELLOW}⚠️ Missing: $asset (placeholder will be used)${NC}"
    fi
done

if [[ "$VALIDATION_ONLY" == "true" ]]; then
    echo -e "${YELLOW}📋 Project structure validation completed.${NC}"
    echo -e "${CYAN}To build this project, run the following commands on a Windows machine:${NC}"
    echo -e "  ${GREEN}dotnet restore${NC}"
    echo -e "  ${GREEN}dotnet build -c $CONFIGURATION${NC}"
    echo -e "  ${GREEN}dotnet run -c $CONFIGURATION${NC}"
    echo ""
    echo -e "${CYAN}Or use the PowerShell build script:${NC}"
    echo -e "  ${GREEN}.\build.ps1 -All${NC}"
    exit 0
fi

# If we reach here, we're on Windows (in theory)
echo -e "${YELLOW}🔧 Attempting to restore packages...${NC}"
if dotnet restore "$PROJECT_PATH"; then
    echo -e "${GREEN}✅ Packages restored${NC}"
else
    echo -e "${RED}❌ Package restore failed${NC}"
    exit 1
fi

echo -e "${YELLOW}🔨 Attempting to build...${NC}"
if dotnet build "$PROJECT_PATH" -c "$CONFIGURATION" --no-restore; then
    echo -e "${GREEN}✅ Build completed successfully${NC}"
    echo -e "${CYAN}🎉 Modern UI project is ready!${NC}"
else
    echo -e "${RED}❌ Build failed${NC}"
    exit 1
fi

echo ""
echo -e "${CYAN}Usage:${NC}"
echo -e "  ${GREEN}./build.sh${NC}                    # Build in Debug mode"
echo -e "  ${GREEN}./build.sh Release${NC}            # Build in Release mode"
