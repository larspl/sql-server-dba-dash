using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DBADash.ModernUI.Services;
using DBADash.ModernUI.Models;

namespace DBADash.ModernUI.ViewModels;

public partial class InstancesViewModel : ObservableObject
{
    private readonly IDataService _dataService;
    private readonly IDialogService _dialogService;
    
    [ObservableProperty]
    private string title = "SQL Server Instances";
    
    [ObservableProperty]
    private string searchText = string.Empty;
    
    [ObservableProperty]
    private int statusFilterIndex = 0;
    
    [ObservableProperty]
    private int versionFilterIndex = 0;
    
    [ObservableProperty]
    private SqlInstanceModel? selectedInstance;

    public ObservableCollection<SqlInstanceModel> Instances { get; }
    public ObservableCollection<SqlInstanceModel> FilteredInstances { get; }

    public InstancesViewModel(IDataService dataService, IDialogService dialogService)
    {
        _dataService = dataService;
        _dialogService = dialogService;
        
        Instances = new ObservableCollection<SqlInstanceModel>();
        FilteredInstances = new ObservableCollection<SqlInstanceModel>();
        
        _ = LoadInstancesAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilters();
    }

    partial void OnStatusFilterIndexChanged(int value)
    {
        ApplyFilters();
    }

    partial void OnVersionFilterIndexChanged(int value)
    {
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        FilteredInstances.Clear();
        
        var filteredItems = Instances.AsEnumerable();
        
        // Apply search filter
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filteredItems = filteredItems.Where(i => 
                i.InstanceName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                i.ServerName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }
        
        // Apply status filter
        if (StatusFilterIndex > 0)
        {
            var statusFilter = StatusFilterIndex switch
            {
                1 => "Online",
                2 => "Offline", 
                3 => "Warning",
                4 => "Critical",
                _ => string.Empty
            };
            
            if (!string.IsNullOrEmpty(statusFilter))
            {
                filteredItems = filteredItems.Where(i => i.Status == statusFilter);
            }
        }
        
        // Apply version filter
        if (VersionFilterIndex > 0)
        {
            var versionFilter = VersionFilterIndex switch
            {
                1 => "2022",
                2 => "2019",
                3 => "2017", 
                4 => "2016",
                5 => "2014",
                _ => string.Empty
            };
            
            if (!string.IsNullOrEmpty(versionFilter))
            {
                filteredItems = filteredItems.Where(i => i.Version.Contains(versionFilter));
            }
        }
        
        foreach (var item in filteredItems)
        {
            FilteredInstances.Add(item);
        }
    }

    [RelayCommand]
    private async Task AddInstance()
    {
        try
        {
            await _dialogService.ShowMessageDialogAsync(
                "Add New Instance",
                "This feature will open the Add Instance dialog to configure a new SQL Server connection.",
                "OK");
                
            // In real implementation, open add instance dialog
            // var result = await _dialogService.ShowAddInstanceDialogAsync();
            // if (result != null)
            // {
            //     await _dataService.AddInstanceAsync(result);
            //     await LoadInstancesAsync();
            // }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorDialogAsync("Error", $"Failed to add instance: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task RefreshAll()
    {
        try
        {
            await LoadInstancesAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorDialogAsync("Error", $"Failed to refresh instances: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task Export()
    {
        try
        {
            await _dialogService.ShowMessageDialogAsync(
                "Export Data",
                "This feature will export the instance data to CSV, Excel, or PDF format.",
                "OK");
                
            // In real implementation, show export options dialog
            // var exportOptions = await _dialogService.ShowExportDialogAsync();
            // if (exportOptions != null)
            // {
            //     await _dataService.ExportInstancesAsync(FilteredInstances, exportOptions);
            // }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorDialogAsync("Error", $"Failed to export data: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ViewDetails(SqlInstanceModel instance)
    {
        if (instance == null) return;
        
        try
        {
            await _dialogService.ShowMessageDialogAsync(
                "Instance Details",
                $"This will open detailed monitoring for {instance.InstanceName} including performance metrics, configuration, and health status.",
                "OK");
                
            // In real implementation, navigate to instance details page
            // _navigationService.NavigateTo(typeof(InstanceDetailsPage), instance.InstanceId);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorDialogAsync("Error", $"Failed to load instance details: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task Configure(SqlInstanceModel instance)
    {
        if (instance == null) return;
        
        try
        {
            await _dialogService.ShowMessageDialogAsync(
                "Configure Instance",
                $"This will open configuration settings for {instance.InstanceName} including connection settings, monitoring thresholds, and collection options.",
                "OK");
                
            // In real implementation, open configuration dialog
            // var config = await _dataService.GetInstanceConfigAsync(instance.InstanceId);
            // var result = await _dialogService.ShowInstanceConfigDialogAsync(config);
            // if (result != null)
            // {
            //     await _dataService.UpdateInstanceConfigAsync(result);
            // }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorDialogAsync("Error", $"Failed to configure instance: {ex.Message}");
        }
    }

    private async Task LoadInstancesAsync()
    {
        try
        {
            // Simulate loading instances - in real implementation, this would call the data service
            await Task.Delay(500);
            
            var sampleInstances = GenerateSampleInstances();
            
            Instances.Clear();
            foreach (var instance in sampleInstances)
            {
                Instances.Add(instance);
            }
            
            ApplyFilters();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorDialogAsync("Error", $"Failed to load instances: {ex.Message}");
        }
    }

    private List<SqlInstanceModel> GenerateSampleInstances()
    {
        var random = new Random();
        var statuses = new[] { "Online", "Offline", "Warning", "Critical" };
        var versions = new[] { "SQL Server 2022", "SQL Server 2019", "SQL Server 2017", "SQL Server 2016" };
        var editions = new[] { "Enterprise", "Standard", "Developer", "Express" };
        
        return new List<SqlInstanceModel>
        {
            new SqlInstanceModel
            {
                InstanceId = 1,
                InstanceName = "PROD-SQL-01",
                ServerName = "prod-sql-01.company.com",
                Status = "Online",
                Version = "SQL Server 2022 (RTM) - 16.0.1000.6",
                Edition = "Enterprise",
                CpuUsage = 75.3,
                MemoryUsage = 89.7,
                ActiveConnections = 234,
                LastBackup = DateTime.Now.AddHours(-2),
                LastBackupAge = "2 hours ago",
                LastBackupStatus = "OK",
                LastSeen = DateTime.Now.AddMinutes(-5)
            },
            new SqlInstanceModel
            {
                InstanceId = 2,
                InstanceName = "PROD-SQL-02",
                ServerName = "prod-sql-02.company.com",
                Status = "Warning",
                Version = "SQL Server 2019 (RTM-CU18) - 15.0.4261.1",
                Edition = "Enterprise",
                CpuUsage = 92.1,
                MemoryUsage = 76.4,
                ActiveConnections = 156,
                LastBackup = DateTime.Now.AddDays(-1),
                LastBackupAge = "1 day ago",
                LastBackupStatus = "Warning",
                LastSeen = DateTime.Now.AddMinutes(-2)
            },
            new SqlInstanceModel
            {
                InstanceId = 3,
                InstanceName = "DEV-SQL-01",
                ServerName = "dev-sql-01.company.com",
                Status = "Online",
                Version = "SQL Server 2019 (RTM-CU15) - 15.0.4198.2",
                Edition = "Developer",
                CpuUsage = 45.2,
                MemoryUsage = 62.8,
                ActiveConnections = 23,
                LastBackup = DateTime.Now.AddHours(-6),
                LastBackupAge = "6 hours ago",
                LastBackupStatus = "OK",
                LastSeen = DateTime.Now.AddMinutes(-1)
            },
            new SqlInstanceModel
            {
                InstanceId = 4,
                InstanceName = "TEST-SQL-01",
                ServerName = "test-sql-01.company.com",
                Status = "Offline",
                Version = "SQL Server 2017 (RTM-CU31) - 14.0.3456.2",
                Edition = "Standard",
                CpuUsage = 0,
                MemoryUsage = 0,
                ActiveConnections = 0,
                LastBackup = DateTime.Now.AddDays(-7),
                LastBackupAge = "7 days ago",
                LastBackupStatus = "Critical",
                LastSeen = DateTime.Now.AddHours(-2)
            },
            new SqlInstanceModel
            {
                InstanceId = 5,
                InstanceName = "STAGING-SQL-01",
                ServerName = "staging-sql-01.company.com",
                Status = "Critical",
                Version = "SQL Server 2016 (SP3) - 13.0.6300.2",
                Edition = "Standard",
                CpuUsage = 98.7,
                MemoryUsage = 95.2,
                ActiveConnections = 412,
                LastBackup = DateTime.Now.AddDays(-3),
                LastBackupAge = "3 days ago",
                LastBackupStatus = "Critical",
                LastSeen = DateTime.Now.AddMinutes(-10)
            }
        };
    }
}
