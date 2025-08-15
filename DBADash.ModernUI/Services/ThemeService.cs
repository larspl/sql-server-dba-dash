using Microsoft.UI.Xaml;
using System;

namespace DBADash.ModernUI.Services;

/// <summary>
/// Theme service interface
/// </summary>
public interface IThemeService
{
    event EventHandler<ThemeChangedEventArgs> ThemeChanged;
    
    bool IsDarkTheme { get; }
    void SetTheme(ElementTheme theme);
    void ToggleTheme();
}

/// <summary>
/// Theme service implementation
/// </summary>
public class ThemeService : IThemeService
{
    private ElementTheme _currentTheme = ElementTheme.Default;

    public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    public bool IsDarkTheme => _currentTheme == ElementTheme.Dark;

    public void SetTheme(ElementTheme theme)
    {
        if (_currentTheme != theme)
        {
            _currentTheme = theme;
            
            // Apply theme to current window
            if (Application.Current.MainWindow?.Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = theme;
            }
            
            ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(theme));
        }
    }

    public void ToggleTheme()
    {
        var newTheme = _currentTheme switch
        {
            ElementTheme.Light => ElementTheme.Dark,
            ElementTheme.Dark => ElementTheme.Light,
            ElementTheme.Default => ElementTheme.Dark,
            _ => ElementTheme.Light
        };
        
        SetTheme(newTheme);
    }
}

/// <summary>
/// Theme changed event arguments
/// </summary>
public class ThemeChangedEventArgs : EventArgs
{
    public ElementTheme Theme { get; }
    public bool IsDarkTheme { get; }

    public ThemeChangedEventArgs(ElementTheme theme)
    {
        Theme = theme;
        IsDarkTheme = theme == ElementTheme.Dark;
    }
}
