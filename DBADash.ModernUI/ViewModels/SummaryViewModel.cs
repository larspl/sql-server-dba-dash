using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DBADash.ModernUI.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace DBADash.ModernUI.ViewModels;

/// <summary>
/// View model for the Summary page
/// </summary>
public partial class SummaryViewModel : ObservableObject
{
    private readonly IDataService _dataService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private int totalInstances;

    [ObservableProperty]
    private int activeInstances;

    [ObservableProperty]
    private int activeAlerts;

    [ObservableProperty]
    private int criticalAlerts;

    [ObservableProperty]
    private double performanceScore = 85.5;

    [ObservableProperty]
    private string performanceTrend = "+2.3% vs last week";

    [ObservableProperty]
    private string performanceTrendIcon = "&#xE74E;"; // Up arrow

    [ObservableProperty]
    private string lastUpdatedTime = DateTime.Now.ToString("HH:mm:ss");

    [ObservableProperty]
    private string lastUpdatedDate = DateTime.Now.ToString("MMM dd, yyyy");

    [ObservableProperty]
    private ObservableCollection<RecentAlert> recentAlerts = new();

    [ObservableProperty]
    private ISeries[] performanceSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ISeries[] statusSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] xAxes = Array.Empty<Axis>();

    [ObservableProperty]
    private Axis[] yAxes = Array.Empty<Axis>();

    public Microsoft.UI.Xaml.Media.Brush PerformanceTrendColor =>
        new Microsoft.UI.Xaml.Media.SolidColorBrush(
            Microsoft.UI.Colors.Green); // Would be dynamic based on trend

    public SummaryViewModel(
        IDataService dataService,
        INavigationService navigationService,
        IDialogService dialogService)
    {
        _dataService = dataService;
        _navigationService = navigationService;
        _dialogService = dialogService;

        // Initialize commands
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        ExportCommand = new AsyncRelayCommand(ExportAsync);
        ViewAllAlertsCommand = new RelayCommand(ViewAllAlerts);
        AddInstanceCommand = new AsyncRelayCommand(AddInstanceAsync);
        RunHealthCheckCommand = new AsyncRelayCommand(RunHealthCheckAsync);
        ViewReportsCommand = new RelayCommand(ViewReports);

        // Initialize charts
        InitializeCharts();
        
        // Load initial data
        _ = LoadDataAsync();
    }

    public ICommand RefreshCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand ViewAllAlertsCommand { get; }
    public ICommand AddInstanceCommand { get; }
    public ICommand RunHealthCheckCommand { get; }
    public ICommand ViewReportsCommand { get; }

    public void OnNavigatedTo(object? parameter)
    {
        // Refresh data when navigating to this page
        _ = LoadDataAsync();
    }

    public void OnNavigatedFrom()
    {
        // Cleanup if needed
    }

    private async Task LoadDataAsync()
    {
        try
        {
            // Load summary data
            var summaryData = await _dataService.GetSummaryDataAsync();
            if (summaryData != null)
            {
                TotalInstances = summaryData.TotalInstances;
                ActiveInstances = summaryData.TotalInstances; // For demo
                LastUpdatedTime = summaryData.LastUpdated.ToString("HH:mm:ss");
                LastUpdatedDate = summaryData.LastUpdated.ToString("MMM dd, yyyy");
            }

            // Load alert counts
            var alertCounts = await _dataService.GetAlertCountsAsync();
            if (alertCounts != null)
            {
                ActiveAlerts = alertCounts.ActiveAlerts;
                CriticalAlerts = alertCounts.CriticalAlerts;
            }

            // Load recent alerts
            LoadRecentAlerts();
            
            // Update charts
            UpdatePerformanceChart();
            UpdateStatusChart();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Data Loading Error", ex.Message);
        }
    }

    private async Task RefreshAsync()
    {
        await LoadDataAsync();
    }

    private async Task ExportAsync()
    {
        await _dialogService.ShowInfoAsync("Export", "Export functionality will be implemented here.");
    }

    private void ViewAllAlerts()
    {
        _navigationService.NavigateTo("Alerts");
    }

    private async Task AddInstanceAsync()
    {
        await _dialogService.ShowInfoAsync("Add Instance", "Add instance functionality will be implemented here.");
    }

    private async Task RunHealthCheckAsync()
    {
        await _dialogService.ShowInfoAsync("Health Check", "Health check functionality will be implemented here.");
    }

    private void ViewReports()
    {
        _navigationService.NavigateTo("Reports");
    }

    private void InitializeCharts()
    {
        // Initialize performance chart axes
        XAxes = new Axis[]
        {
            new Axis
            {
                Name = "Time",
                NameTextSize = 14,
                TextSize = 12,
                SeparatorsPaint = new SolidColorPaint(SKColors.LightGray),
                LabelsPaint = new SolidColorPaint(SKColors.Gray)
            }
        };

        YAxes = new Axis[]
        {
            new Axis
            {
                Name = "Performance (%)",
                NameTextSize = 14,
                TextSize = 12,
                SeparatorsPaint = new SolidColorPaint(SKColors.LightGray),
                LabelsPaint = new SolidColorPaint(SKColors.Gray)
            }
        };
    }

    private void UpdatePerformanceChart()
    {
        // Generate sample performance data
        var values = new List<double>();
        var now = DateTime.Now;
        
        for (int i = 23; i >= 0; i--)
        {
            var baseValue = 80 + Random.Shared.NextDouble() * 20;
            values.Add(baseValue);
        }

        PerformanceSeries = new ISeries[]
        {
            new LineSeries<double>
            {
                Values = values,
                Name = "CPU Usage",
                Stroke = new SolidColorPaint(SKColors.Blue) { StrokeThickness = 2 },
                Fill = new SolidColorPaint(SKColors.Blue.WithAlpha(50)),
                GeometrySize = 4
            }
        };
    }

    private void UpdateStatusChart()
    {
        StatusSeries = new ISeries[]
        {
            new PieSeries<double>
            {
                Values = new[] { 15.0 },
                Name = "Online",
                Fill = new SolidColorPaint(SKColors.Green)
            },
            new PieSeries<double>
            {
                Values = new[] { 2.0 },
                Name = "Warning",
                Fill = new SolidColorPaint(SKColors.Orange)
            },
            new PieSeries<double>
            {
                Values = new[] { 1.0 },
                Name = "Critical",
                Fill = new SolidColorPaint(SKColors.Red)
            },
            new PieSeries<double>
            {
                Values = new[] { 1.0 },
                Name = "Offline",
                Fill = new SolidColorPaint(SKColors.Gray)
            }
        };
    }

    private void LoadRecentAlerts()
    {
        // Sample data - replace with real data
        RecentAlerts.Clear();
        RecentAlerts.Add(new RecentAlert
        {
            Message = "High CPU usage detected on SQL-PROD-01",
            Timestamp = "2 minutes ago",
            SeverityColor = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red)
        });
        RecentAlerts.Add(new RecentAlert
        {
            Message = "Backup job failed on SQL-DEV-02",
            Timestamp = "15 minutes ago",
            SeverityColor = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Orange)
        });
        RecentAlerts.Add(new RecentAlert
        {
            Message = "Disk space low on SQL-TEST-03",
            Timestamp = "1 hour ago",
            SeverityColor = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Orange)
        });
    }
}

/// <summary>
/// Model for recent alerts display
/// </summary>
public class RecentAlert
{
    public string Message { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
    public Microsoft.UI.Xaml.Media.Brush SeverityColor { get; set; } = 
        new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray);
}
