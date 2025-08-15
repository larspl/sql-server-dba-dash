using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DBADash.ModernUI.ViewModels;
using DBADash.ModernUI.Services;
using DBADash.ModernUI.Views;
using DBADashGUI;
using System;

namespace DBADash.ModernUI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private Window _window;
    private IHost _host;

    /// <summary>
    /// Initializes the singleton application object. This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();
        ConfigureServices();
    }

    /// <summary>
    /// Configure dependency injection services
    /// </summary>
    private void ConfigureServices()
    {
        var builder = Host.CreateDefaultBuilder();
        
        builder.ConfigureServices((context, services) =>
        {
                    // Services
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddTransient<IDataService, DataService>();
        services.AddTransient<IDialogService, DialogService>();
        services.AddSingleton<IRealtimeService, RealtimeService>();
        services.AddTransient<IExportService, ExportService>();
        services.AddSingleton<IAccessibilityService, AccessibilityService>();
        services.AddTransient<IPerformanceOptimizationService, PerformanceOptimizationService>();
            
        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<SummaryViewModel>();
        services.AddTransient<PerformanceViewModel>();
        services.AddTransient<InstancesViewModel>();
        services.AddTransient<AlertsViewModel>();
        services.AddTransient<PageViewModel>();            // Views
            services.AddTransient<MainWindow>();
            services.AddTransient<SummaryPage>();
            services.AddTransient<PerformancePage>();
            services.AddTransient<InstancesPage>();
            services.AddTransient<AlertsPage>();
            services.AddTransient<JobsPage>();
            services.AddTransient<BackupsPage>();
            services.AddTransient<QueriesPage>();
        });
        
        _host = builder.Build();
    }

    public static T GetService<T>() where T : class
    {
        if ((Application.Current as App)?._host?.Services == null)
            throw new InvalidOperationException("Services not configured");
            
        return ((App)Application.Current)._host.Services.GetRequiredService<T>();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        _window = GetService<MainWindow>();
        _window.Activate();
    }
}
