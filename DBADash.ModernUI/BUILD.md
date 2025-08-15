# DBA Dash Modern UI - Build Instructions

## Platform Requirements

**⚠️ Important**: The DBA Dash Modern UI is built with **WinUI 3**, which is a **Windows-only** framework. This project can only be built and run on Windows machines.

## Prerequisites

### For Windows Development
- **Windows 10** version 1809 (build 17763) or later
- **Windows 11** recommended for best experience
- **.NET 8.0 SDK** or later
- **Visual Studio 2022** version 17.3 or later with:
  - .NET Desktop Development workload
  - Windows App SDK components
  - Windows 10/11 SDK (version 10.0.19041.0 or later)

### For Cross-Platform Development (Design/Planning Only)
- Any platform for viewing source code and documentation
- Building requires Windows environment

## Building on Windows

### Command Line Build
```bash
# Clone the repository
git clone https://github.com/larspl/sql-server-dba-dash.git
cd sql-server-dba-dash

# Switch to the Modern UI branch
git checkout feature-newUI

# Navigate to the Modern UI project
cd DBADash.ModernUI

# Restore packages
dotnet restore

# Build the project
dotnet build

# Run the application
dotnet run
```

### Visual Studio Build
1. Open `DBADash.sln` in Visual Studio 2022
2. Set `DBADash.ModernUI` as the startup project
3. Select **Debug** or **Release** configuration
4. Press **F5** to build and run

## Building for Distribution

### MSIX Package (Recommended)
```bash
# Build for MSIX deployment
dotnet publish -c Release -r win-x64 --self-contained

# Package will be created in bin/Release/net8.0-windows10.0.19041.0/win-x64/publish/
```

### Self-Contained Executable
```bash
# Build self-contained executable
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## Development Workflow

### Cross-Platform Development
Since this is a Windows-only application, we recommend the following workflow for cross-platform teams:

1. **Design & Architecture**: Can be done on any platform
2. **Code Development**: 
   - View models and services can be developed on any platform
   - XAML and Windows-specific code requires Windows
3. **Testing & Building**: Must be done on Windows

### GitHub Actions/CI Build
Create a Windows-based build pipeline:

```yaml
name: Build Modern UI
on: [push, pull_request]
jobs:
  build:
    runs-on: windows-latest
    steps:
    - uses: actions/checkout@v3
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    - name: Restore dependencies
      run: dotnet restore DBADash.ModernUI/DBADash.ModernUI.csproj
    - name: Build
      run: dotnet build DBADash.ModernUI/DBADash.ModernUI.csproj --no-restore
    - name: Test
      run: dotnet test DBADash.ModernUI/DBADash.ModernUI.csproj --no-build --verbosity normal
```

## Troubleshooting

### Common Issues

**Error: NETSDK1100 - EnableWindowsTargeting**
- This error occurs when trying to build on non-Windows platforms
- **Solution**: Build on a Windows machine or use Windows Subsystem for Linux (WSL) with Windows .NET SDK

**Missing Windows App SDK**
- **Solution**: Install via Visual Studio Installer or download from Microsoft

**DevExpress License Issues**
- DevExpress Community edition is free but requires registration
- **Solution**: Register at DevExpress website and accept license terms

**WinUI 3 Runtime Issues**
- **Solution**: Install Windows App Runtime from Microsoft Store or as redistributable

### Development Tips

1. **Use Windows Virtual Machine**: For cross-platform teams, set up a Windows VM for WinUI development
2. **Remote Development**: Use VS Code Remote Development to connect to a Windows machine
3. **Windows Subsystem for Linux**: Some .NET development possible in WSL, but WinUI requires native Windows

## Alternative Approaches

If cross-platform development is required, consider:

1. **Uno Platform**: Cross-platform alternative to WinUI 3
2. **Avalonia UI**: Cross-platform XAML framework
3. **MAUI**: Microsoft's cross-platform framework (though less mature for desktop)

However, these would require significant architectural changes and may not provide the same Windows 11 native experience as WinUI 3.

## Current Status

✅ **Completed**: Project structure, MVVM architecture, basic UI framework
⚠️ **Windows Required**: Building and running the application
🔄 **In Progress**: Asset creation, advanced features, full data integration

---

For questions about Windows development environment setup, see the main project documentation or create an issue on GitHub.
