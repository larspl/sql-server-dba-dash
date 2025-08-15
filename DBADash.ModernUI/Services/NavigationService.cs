using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;

namespace DBADash.ModernUI.Services;

/// <summary>
/// Navigation service interface
/// </summary>
public interface INavigationService
{
    event EventHandler<NavigationEventArgs> Navigated;
    
    bool CanGoBack { get; }
    bool CanGoForward { get; }
    
    void Initialize(Frame frame);
    void NavigateTo(string pageKey, object? parameter = null);
    void GoBack();
    void GoForward();
    void ClearHistory();
}

/// <summary>
/// Navigation service implementation for WinUI 3
/// </summary>
public class NavigationService : INavigationService
{
    private Frame? _frame;
    private readonly Dictionary<string, Type> _pageTypes;

    public event EventHandler<NavigationEventArgs>? Navigated;

    public bool CanGoBack => _frame?.CanGoBack ?? false;
    public bool CanGoForward => _frame?.CanGoForward ?? false;

    public NavigationService()
    {
        _pageTypes = new Dictionary<string, Type>
        {
            { "Summary", typeof(Views.SummaryPage) },
            { "Instances", typeof(Views.InstancesPage) },
            { "Performance", typeof(Views.PerformancePage) },
            { "Alerts", typeof(Views.AlertsPage) },
            { "Jobs", typeof(Views.JobsPage) },
            { "Backups", typeof(Views.BackupsPage) },
            { "Queries", typeof(Views.QueriesPage) },
            { "Reports", typeof(Views.ReportsPage) },
            { "Configuration", typeof(Views.ConfigurationPage) },
            { "Help", typeof(Views.HelpPage) },
            { "About", typeof(Views.AboutPage) }
        };
    }

    public void Initialize(Frame frame)
    {
        _frame = frame;
        _frame.Navigated += OnFrameNavigated;
        
        // Navigate to initial page
        NavigateTo("Summary");
    }

    public void NavigateTo(string pageKey, object? parameter = null)
    {
        if (_frame == null)
            throw new InvalidOperationException("Navigation service not initialized");

        if (!_pageTypes.TryGetValue(pageKey, out var pageType))
            throw new ArgumentException($"Page key '{pageKey}' not found", nameof(pageKey));

        var navigationTransition = new EntranceNavigationTransitionInfo();
        _frame.Navigate(pageType, parameter, navigationTransition);
    }

    public void GoBack()
    {
        if (_frame?.CanGoBack == true)
        {
            _frame.GoBack();
        }
    }

    public void GoForward()
    {
        if (_frame?.CanGoForward == true)
        {
            _frame.GoForward();
        }
    }

    public void ClearHistory()
    {
        _frame?.BackStack.Clear();
        _frame?.ForwardStack.Clear();
    }

    private void OnFrameNavigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        var pageKey = GetPageKey(e.SourcePageType);
        Navigated?.Invoke(this, new NavigationEventArgs(pageKey, e.Parameter));
    }

    private string GetPageKey(Type pageType)
    {
        foreach (var kvp in _pageTypes)
        {
            if (kvp.Value == pageType)
                return kvp.Key;
        }
        return string.Empty;
    }
}

/// <summary>
/// Navigation event arguments
/// </summary>
public class NavigationEventArgs : EventArgs
{
    public string PageKey { get; }
    public object? Parameter { get; }

    public NavigationEventArgs(string pageKey, object? parameter)
    {
        PageKey = pageKey;
        Parameter = parameter;
    }
}
