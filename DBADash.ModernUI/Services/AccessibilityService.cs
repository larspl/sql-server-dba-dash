using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;

namespace DBADash.ModernUI.Services;

public interface IAccessibilityService
{
    void ConfigureAccessibility(FrameworkElement element, string accessibleName, string accessibleDescription = "");
    void SetKeyboardNavigation(Panel container);
    void AnnounceToScreenReader(string message);
    bool IsHighContrastMode { get; }
    event EventHandler<bool> HighContrastModeChanged;
}

public class AccessibilityService : IAccessibilityService
{
    public bool IsHighContrastMode { get; private set; }
    public event EventHandler<bool>? HighContrastModeChanged;

    public AccessibilityService()
    {
        // Monitor high contrast mode changes
        Application.Current.RequestedThemeChanged += OnRequestedThemeChanged;
        UpdateHighContrastMode();
    }

    public void ConfigureAccessibility(FrameworkElement element, string accessibleName, string accessibleDescription = "")
    {
        // Set accessibility properties
        AutomationProperties.SetName(element, accessibleName);
        
        if (!string.IsNullOrEmpty(accessibleDescription))
        {
            AutomationProperties.SetHelpText(element, accessibleDescription);
        }

        // Ensure the element is focusable for keyboard navigation
        if (element is Control control)
        {
            control.IsTabStop = true;
            
            // Add keyboard handlers for common controls
            if (control is Button || control is AppBarButton)
            {
                control.KeyDown += OnControlKeyDown;
            }
        }
    }

    public void SetKeyboardNavigation(Panel container)
    {
        // Configure tab navigation
        KeyboardNavigationMode.SetTabNavigation(container, KeyboardNavigationMode.Cycle);
        KeyboardNavigationMode.SetDirectionalNavigation(container, KeyboardNavigationMode.Cycle);

        // Set up arrow key navigation for grid-like layouts
        if (container is Grid)
        {
            container.KeyDown += OnGridKeyDown;
        }
    }

    public void AnnounceToScreenReader(string message)
    {
        try
        {
            // Create a temporary TextBlock for screen reader announcements
            var announcement = new TextBlock
            {
                Text = message,
                Visibility = Visibility.Collapsed
            };

            // Add to visual tree temporarily
            if (App.MainWindow?.Content is Panel rootPanel)
            {
                rootPanel.Children.Add(announcement);
                
                // Set automation properties to trigger screen reader
                AutomationProperties.SetLiveSetting(announcement, AutomationLiveSetting.Assertive);
                AutomationProperties.SetName(announcement, message);
                
                // Remove after a short delay
                var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    rootPanel.Children.Remove(announcement);
                };
                timer.Start();
            }
        }
        catch
        {
            // Silently fail if screen reader announcement fails
        }
    }

    private void OnControlKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (sender is Control control)
        {
            switch (e.Key)
            {
                case VirtualKey.Enter:
                case VirtualKey.Space:
                    if (control is Button button)
                    {
                        // Simulate button click
                        button.Command?.Execute(button.CommandParameter);
                        e.Handled = true;
                    }
                    break;
            }
        }
    }

    private void OnGridKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (sender is Grid grid)
        {
            var focusedElement = FocusManager.GetFocusedElement(grid.XamlRoot);
            if (focusedElement is FrameworkElement element)
            {
                switch (e.Key)
                {
                    case VirtualKey.Up:
                        NavigateInGrid(grid, element, -1, 0);
                        e.Handled = true;
                        break;
                    case VirtualKey.Down:
                        NavigateInGrid(grid, element, 1, 0);
                        e.Handled = true;
                        break;
                    case VirtualKey.Left:
                        NavigateInGrid(grid, element, 0, -1);
                        e.Handled = true;
                        break;
                    case VirtualKey.Right:
                        NavigateInGrid(grid, element, 0, 1);
                        e.Handled = true;
                        break;
                }
            }
        }
    }

    private void NavigateInGrid(Grid grid, FrameworkElement currentElement, int rowDelta, int columnDelta)
    {
        var currentRow = Grid.GetRow(currentElement);
        var currentColumn = Grid.GetColumn(currentElement);
        
        var newRow = Math.Max(0, Math.Min(grid.RowDefinitions.Count - 1, currentRow + rowDelta));
        var newColumn = Math.Max(0, Math.Min(grid.ColumnDefinitions.Count - 1, currentColumn + columnDelta));

        // Find element at new position
        foreach (var child in grid.Children)
        {
            if (child is FrameworkElement childElement &&
                Grid.GetRow(childElement) == newRow &&
                Grid.GetColumn(childElement) == newColumn &&
                childElement is Control focusableControl &&
                focusableControl.IsTabStop)
            {
                focusableControl.Focus(FocusState.Keyboard);
                break;
            }
        }
    }

    private void OnRequestedThemeChanged(object sender, object e)
    {
        UpdateHighContrastMode();
    }

    private void UpdateHighContrastMode()
    {
        var wasHighContrast = IsHighContrastMode;
        
        // Check if high contrast mode is enabled
        try
        {
            var uiSettings = new Windows.UI.ViewManagement.UISettings();
            IsHighContrastMode = uiSettings.HighContrast;
        }
        catch
        {
            IsHighContrastMode = false;
        }

        if (wasHighContrast != IsHighContrastMode)
        {
            HighContrastModeChanged?.Invoke(this, IsHighContrastMode);
        }
    }
}

// Extension class for keyboard navigation modes
public static class KeyboardNavigationMode
{
    public static readonly DependencyProperty TabNavigationProperty =
        DependencyProperty.RegisterAttached(
            "TabNavigation",
            typeof(KeyboardNavigationMode),
            typeof(KeyboardNavigationMode),
            new PropertyMetadata(KeyboardNavigationMode.Local));

    public static readonly DependencyProperty DirectionalNavigationProperty =
        DependencyProperty.RegisterAttached(
            "DirectionalNavigation",
            typeof(KeyboardNavigationMode),
            typeof(KeyboardNavigationMode),
            new PropertyMetadata(KeyboardNavigationMode.None));

    public static void SetTabNavigation(UIElement element, KeyboardNavigationMode value)
    {
        element.SetValue(TabNavigationProperty, value);
    }

    public static KeyboardNavigationMode GetTabNavigation(UIElement element)
    {
        return (KeyboardNavigationMode)element.GetValue(TabNavigationProperty);
    }

    public static void SetDirectionalNavigation(UIElement element, KeyboardNavigationMode value)
    {
        element.SetValue(DirectionalNavigationProperty, value);
    }

    public static KeyboardNavigationMode GetDirectionalNavigation(UIElement element)
    {
        return (KeyboardNavigationMode)element.GetValue(DirectionalNavigationProperty);
    }
}

public enum KeyboardNavigationMode
{
    Continue,
    Local,
    Contained,
    Cycle,
    Once,
    None
}
