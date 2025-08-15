using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DBADash.ModernUI.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace DBADash.ModernUI.ViewModels;

/// <summary>
/// Main view model for the application
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly IThemeService _themeService;
    private readonly IDataService _dataService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private string currentPageTitle = "Summary";

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string loadingMessage = string.Empty;

    [ObservableProperty]
    private string statusMessage = "Ready";

    [ObservableProperty]
    private string connectionInfo = string.Empty;

    [ObservableProperty]
    private string lastUpdated = string.Empty;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private bool isDarkTheme;

    [ObservableProperty]
    private int summaryAlertCount;

    [ObservableProperty]
    private int activeAlertCount;

    [ObservableProperty]
    private bool hasActiveAlerts;

    [ObservableProperty]
    private bool hasSummaryAlerts;

    [ObservableProperty]
    private ConnectionStatusViewModel connectionStatus;

    public INavigationService NavigationService => _navigationService;

    public MainViewModel(
        INavigationService navigationService,
        IThemeService themeService,
        IDataService dataService,
        IDialogService dialogService)
    {
        _navigationService = navigationService;
        _themeService = themeService;
        _dataService = dataService;
        _dialogService = dialogService;

        ConnectionStatus = new ConnectionStatusViewModel();
        
        // Initialize commands
        RefreshCommand = new AsyncRelayCommand(RefreshDataAsync);
        OpenSettingsCommand = new AsyncRelayCommand(OpenSettingsAsync);
        
        // Subscribe to theme changes
        _themeService.ThemeChanged += OnThemeChanged;
        IsDarkTheme = _themeService.IsDarkTheme;
        
        // Initialize data
        _ = InitializeAsync();
    }

    public ICommand RefreshCommand { get; }
    public ICommand OpenSettingsCommand { get; }

    private async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            LoadingMessage = "Initializing application...";
            
            // Load initial data
            await LoadSummaryDataAsync();
            await LoadAlertCountsAsync();
            
            StatusMessage = "Application initialized successfully";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error initializing: {ex.Message}";
            await _dialogService.ShowErrorAsync("Initialization Error", ex.Message);
        }
        finally
        {
            IsLoading = false;
            LoadingMessage = string.Empty;
        }
    }

    private async Task RefreshDataAsync()
    {
        try
        {
            IsLoading = true;
            LoadingMessage = "Refreshing data...";
            
            await LoadSummaryDataAsync();
            await LoadAlertCountsAsync();
            
            LastUpdated = $"Last updated: {DateTime.Now:HH:mm:ss}";
            StatusMessage = "Data refreshed successfully";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error refreshing data: {ex.Message}";
            await _dialogService.ShowErrorAsync("Refresh Error", ex.Message);
        }
        finally
        {
            IsLoading = false;
            LoadingMessage = string.Empty;
        }
    }

    private async Task OpenSettingsAsync()
    {
        try
        {
            // Navigate to settings page or show settings dialog
            await _dialogService.ShowInfoAsync("Settings", "Settings functionality will be implemented here.");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Settings Error", ex.Message);
        }
    }

    public void NavigateToPage(string pageKey)
    {
        try
        {
            _navigationService.NavigateTo(pageKey);
            CurrentPageTitle = GetPageTitle(pageKey);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Navigation error: {ex.Message}";
        }
    }

    public void OnSearchSuggestionChosen(AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is string suggestion)
        {
            SearchText = suggestion;
        }
    }

    public async void OnSearchSubmitted(AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        try
        {
            var query = args.QueryText;
            if (!string.IsNullOrWhiteSpace(query))
            {
                // Implement search functionality
                StatusMessage = $"Searching for: {query}";
                await Task.Delay(1000); // Simulate search
                StatusMessage = "Search completed";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Search error: {ex.Message}";
        }
    }

    private async Task LoadSummaryDataAsync()
    {
        try
        {
            // Load summary data from data service
            var summaryData = await _dataService.GetSummaryDataAsync();
            
            // Update connection info
            ConnectionInfo = summaryData?.ConnectionString ?? "Not connected";
            
            // Update connection status
            ConnectionStatus.IsConnected = !string.IsNullOrEmpty(summaryData?.ConnectionString);
            ConnectionStatus.ServerName = summaryData?.ServerName ?? "Unknown";
            ConnectionStatus.DatabaseName = summaryData?.DatabaseName ?? "Unknown";
        }
        catch (Exception ex)
        {
            ConnectionStatus.IsConnected = false;
            ConnectionStatus.ErrorMessage = ex.Message;
        }
    }

    private async Task LoadAlertCountsAsync()
    {
        try
        {
            var alertCounts = await _dataService.GetAlertCountsAsync();
            
            ActiveAlertCount = alertCounts?.ActiveAlerts ?? 0;
            SummaryAlertCount = alertCounts?.SummaryAlerts ?? 0;
            
            HasActiveAlerts = ActiveAlertCount > 0;
            HasSummaryAlerts = SummaryAlertCount > 0;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading alert counts: {ex.Message}";
        }
    }

    private void OnThemeChanged(object? sender, ThemeChangedEventArgs e)
    {
        IsDarkTheme = e.IsDarkTheme;
    }

    private static string GetPageTitle(string pageKey) => pageKey switch
    {
        "Summary" => "Summary",
        "Instances" => "Instances",
        "Performance" => "Performance",
        "Alerts" => "Alerts",
        "Jobs" => "Jobs",
        "Backups" => "Backups",
        "Queries" => "Queries",
        "Reports" => "Reports",
        "Configuration" => "Configuration",
        "Help" => "Help",
        "About" => "About",
        _ => "DBA Dash"
    };
}

/// <summary>
/// View model for connection status indicator
/// </summary>
public partial class ConnectionStatusViewModel : ObservableObject
{
    [ObservableProperty]
    private bool isConnected;

    [ObservableProperty]
    private string serverName = string.Empty;

    [ObservableProperty]
    private string databaseName = string.Empty;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private DateTime lastConnectionCheck = DateTime.Now;

    public string StatusText => IsConnected 
        ? $"Connected to {ServerName}" 
        : $"Disconnected - {ErrorMessage}";

    public string ToolTipText => IsConnected
        ? $"Server: {ServerName}\nDatabase: {DatabaseName}\nLast Check: {LastConnectionCheck:HH:mm:ss}"
        : $"Connection Error: {ErrorMessage}\nLast Check: {LastConnectionCheck:HH:mm:ss}";
}
