using CommunityToolkit.Mvvm.ComponentModel;

namespace DBADash.ModernUI.ViewModels;

public partial class PerformanceViewModel : ObservableObject
{
    [ObservableProperty]
    private string title = "Performance Monitoring";
    
    public void OnNavigatedTo(object? parameter) { }
    public void OnNavigatedFrom() { }
}

public partial class InstancesViewModel : ObservableObject
{
    [ObservableProperty]
    private string title = "SQL Server Instances";
    
    public void OnNavigatedTo(object? parameter) { }
    public void OnNavigatedFrom() { }
}

public partial class AlertsViewModel : ObservableObject
{
    [ObservableProperty]
    private string title = "Active Alerts";
    
    public void OnNavigatedTo(object? parameter) { }
    public void OnNavigatedFrom() { }
}

public partial class JobsViewModel : ObservableObject
{
    [ObservableProperty]
    private string title = "SQL Agent Jobs";
    
    public void OnNavigatedTo(object? parameter) { }
    public void OnNavigatedFrom() { }
}

public partial class BackupsViewModel : ObservableObject
{
    [ObservableProperty]
    private string title = "Database Backups";
    
    public void OnNavigatedTo(object? parameter) { }
    public void OnNavigatedFrom() { }
}

public partial class QueriesViewModel : ObservableObject
{
    [ObservableProperty]
    private string title = "Query Performance";
    
    public void OnNavigatedTo(object? parameter) { }
    public void OnNavigatedFrom() { }
}
