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

public partial class AlertsViewModel : ObservableObject
{
    private readonly IDataService _dataService;
    private readonly IDialogService _dialogService;
    private readonly IRealtimeService _realtimeService;
    
    [ObservableProperty]
    private string title = "Alert Management";
    
    [ObservableProperty]
    private string searchText = string.Empty;
    
    [ObservableProperty]
    private int severityFilterIndex = 0;
    
    [ObservableProperty]
    private int statusFilterIndex = 0;
    
    [ObservableProperty]
    private int instanceFilterIndex = 0;
    
    [ObservableProperty]
    private DateTimeOffset fromDate = DateTimeOffset.Now.AddDays(-7);
    
    [ObservableProperty]
    private string criticalAlerts = "5";
    
    [ObservableProperty]
    private string warningAlerts = "12";
    
    [ObservableProperty]
    private string infoAlerts = "23";
    
    [ObservableProperty]
    private string acknowledgedAlerts = "8";

    public ObservableCollection<AlertModel> Alerts { get; }
    public ObservableCollection<AlertModel> FilteredAlerts { get; }
    public ObservableCollection<AlertModel> SelectedAlerts { get; }
    public ObservableCollection<string> InstanceNames { get; }

    public AlertsViewModel(IDataService dataService, IDialogService dialogService, IRealtimeService realtimeService)
    {
        _dataService = dataService;
        _dialogService = dialogService;
        _realtimeService = realtimeService;
        
        Alerts = new ObservableCollection<AlertModel>();
        FilteredAlerts = new ObservableCollection<AlertModel>();
        SelectedAlerts = new ObservableCollection<AlertModel>();
        InstanceNames = new ObservableCollection<string> { "All Instances" };
        
        // Subscribe to real-time alert updates
        _realtimeService.AlertReceived += OnAlertReceived;
        
        _ = LoadAlertsAsync();
        _ = LoadInstanceNamesAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilters();
    }

    partial void OnSeverityFilterIndexChanged(int value)
    {
        ApplyFilters();
    }

    partial void OnStatusFilterIndexChanged(int value)
    {
        ApplyFilters();
    }

    partial void OnInstanceFilterIndexChanged(int value)
    {
        ApplyFilters();
    }

    partial void OnFromDateChanged(DateTimeOffset value)
    {
        ApplyFilters();
    }

    private void OnAlertReceived(object? sender, AlertEventArgs e)
    {
        // Add new alert to collection
        App.MainWindow.DispatcherQueue.TryEnqueue(() =>
        {
            Alerts.Insert(0, e.Alert); // Add to beginning for newest first
            ApplyFilters();
            UpdateAlertCounts();
        });
    }

    private void ApplyFilters()
    {
        FilteredAlerts.Clear();
        
        var filteredItems = Alerts.AsEnumerable();
        
        // Apply search filter
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filteredItems = filteredItems.Where(a => 
                a.Message.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                a.InstanceName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                a.AlertType.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }
        
        // Apply severity filter
        if (SeverityFilterIndex > 0)
        {
            var severityFilter = SeverityFilterIndex switch
            {
                1 => "Critical",
                2 => "Warning",
                3 => "Info",
                _ => string.Empty
            };
            
            if (!string.IsNullOrEmpty(severityFilter))
            {
                filteredItems = filteredItems.Where(a => a.Severity == severityFilter);
            }
        }
        
        // Apply status filter
        if (StatusFilterIndex > 0)
        {
            var statusFilter = StatusFilterIndex switch
            {
                1 => "Active",
                2 => "Acknowledged",
                3 => "Resolved",
                _ => string.Empty
            };
            
            if (!string.IsNullOrEmpty(statusFilter))
            {
                filteredItems = filteredItems.Where(a => a.Status == statusFilter);
            }
        }
        
        // Apply instance filter
        if (InstanceFilterIndex > 0 && InstanceFilterIndex < InstanceNames.Count)
        {
            var instanceFilter = InstanceNames[InstanceFilterIndex];
            filteredItems = filteredItems.Where(a => a.InstanceName == instanceFilter);
        }
        
        // Apply date filter
        filteredItems = filteredItems.Where(a => a.Timestamp >= FromDate.DateTime);
        
        // Order by timestamp (newest first)
        filteredItems = filteredItems.OrderByDescending(a => a.Timestamp);
        
        foreach (var item in filteredItems)
        {
            FilteredAlerts.Add(item);
        }
    }

    [RelayCommand]
    private async Task AcknowledgeSelected()
    {
        if (!SelectedAlerts.Any())
        {
            await _dialogService.ShowMessageDialogAsync(
                "No Selection",
                "Please select one or more alerts to acknowledge.",
                "OK");
            return;
        }

        try
        {
            var alertsToAcknowledge = SelectedAlerts.Where(a => a.CanAcknowledge).ToList();
            
            if (!alertsToAcknowledge.Any())
            {
                await _dialogService.ShowMessageDialogAsync(
                    "No Valid Selection",
                    "The selected alerts cannot be acknowledged (they may already be acknowledged or resolved).",
                    "OK");
                return;
            }

            foreach (var alert in alertsToAcknowledge)
            {
                alert.Status = "Acknowledged";
                alert.AcknowledgedBy = "Current User"; // In real app, get from auth service
                alert.AcknowledgedAt = DateTime.Now;
            }

            SelectedAlerts.Clear();
            ApplyFilters();
            UpdateAlertCounts();

            await _dialogService.ShowMessageDialogAsync(
                "Alerts Acknowledged",
                $"Successfully acknowledged {alertsToAcknowledge.Count} alert(s).",
                "OK");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorDialogAsync("Error", $"Failed to acknowledge alerts: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ResolveSelected()
    {
        if (!SelectedAlerts.Any())
        {
            await _dialogService.ShowMessageDialogAsync(
                "No Selection",
                "Please select one or more alerts to resolve.",
                "OK");
            return;
        }

        try
        {
            var alertsToResolve = SelectedAlerts.Where(a => a.CanResolve).ToList();
            
            if (!alertsToResolve.Any())
            {
                await _dialogService.ShowMessageDialogAsync(
                    "No Valid Selection",
                    "The selected alerts cannot be resolved (they may already be resolved).",
                    "OK");
                return;
            }

            foreach (var alert in alertsToResolve)
            {
                alert.Status = "Resolved";
                alert.ResolvedBy = "Current User"; // In real app, get from auth service
                alert.ResolvedAt = DateTime.Now;
            }

            SelectedAlerts.Clear();
            ApplyFilters();
            UpdateAlertCounts();

            await _dialogService.ShowMessageDialogAsync(
                "Alerts Resolved",
                $"Successfully resolved {alertsToResolve.Count} alert(s).",
                "OK");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorDialogAsync("Error", $"Failed to resolve alerts: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task Export()
    {
        try
        {
            await _dialogService.ShowMessageDialogAsync(
                "Export Alerts",
                "This feature will export the filtered alert data to your chosen format (CSV, Excel, or PDF).",
                "OK");
                
            // In real implementation, use export service
            // var exportService = App.GetService<IExportService>();
            // var filePath = await exportService.ShowSaveFileDialogAsync("alerts_export", ExportFormat.Csv);
            // if (!string.IsNullOrEmpty(filePath))
            // {
            //     var progress = new Progress<ExportProgress>();
            //     await exportService.ExportToCsvAsync(FilteredAlerts, filePath, progress);
            // }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorDialogAsync("Error", $"Failed to export alerts: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task Refresh()
    {
        try
        {
            await LoadAlertsAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorDialogAsync("Error", $"Failed to refresh alerts: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ViewAlert(AlertModel alert)
    {
        if (alert == null) return;
        
        try
        {
            await _dialogService.ShowMessageDialogAsync(
                "Alert Details",
                $"Alert: {alert.Message}\n\nDetails: {alert.Details}\n\nInstance: {alert.InstanceName}\nSeverity: {alert.Severity}\nTimestamp: {alert.Timestamp:yyyy-MM-dd HH:mm:ss}\nStatus: {alert.Status}",
                "OK");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorDialogAsync("Error", $"Failed to view alert details: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task Acknowledge(AlertModel alert)
    {
        if (alert == null || !alert.CanAcknowledge) return;

        try
        {
            alert.Status = "Acknowledged";
            alert.AcknowledgedBy = "Current User";
            alert.AcknowledgedAt = DateTime.Now;
            
            ApplyFilters();
            UpdateAlertCounts();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorDialogAsync("Error", $"Failed to acknowledge alert: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task Resolve(AlertModel alert)
    {
        if (alert == null || !alert.CanResolve) return;

        try
        {
            alert.Status = "Resolved";
            alert.ResolvedBy = "Current User";
            alert.ResolvedAt = DateTime.Now;
            
            ApplyFilters();
            UpdateAlertCounts();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorDialogAsync("Error", $"Failed to resolve alert: {ex.Message}");
        }
    }

    private async Task LoadAlertsAsync()
    {
        try
        {
            // Simulate loading alerts - in real implementation, call data service
            await Task.Delay(300);
            
            var sampleAlerts = GenerateSampleAlerts();
            
            Alerts.Clear();
            foreach (var alert in sampleAlerts)
            {
                Alerts.Add(alert);
            }
            
            ApplyFilters();
            UpdateAlertCounts();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorDialogAsync("Error", $"Failed to load alerts: {ex.Message}");
        }
    }

    private async Task LoadInstanceNamesAsync()
    {
        try
        {
            // Simulate loading instance names
            await Task.Delay(100);
            
            var instanceNames = new[] { "PROD-SQL-01", "PROD-SQL-02", "DEV-SQL-01", "TEST-SQL-01", "STAGING-SQL-01" };
            
            foreach (var name in instanceNames)
            {
                InstanceNames.Add(name);
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorDialogAsync("Error", $"Failed to load instance names: {ex.Message}");
        }
    }

    private void UpdateAlertCounts()
    {
        CriticalAlerts = Alerts.Count(a => a.Severity == "Critical" && a.Status == "Active").ToString();
        WarningAlerts = Alerts.Count(a => a.Severity == "Warning" && a.Status == "Active").ToString();
        InfoAlerts = Alerts.Count(a => a.Severity == "Info" && a.Status == "Active").ToString();
        AcknowledgedAlerts = Alerts.Count(a => a.Status == "Acknowledged").ToString();
    }

    private List<AlertModel> GenerateSampleAlerts()
    {
        var random = new Random();
        var severities = new[] { "Critical", "Warning", "Info" };
        var statuses = new[] { "Active", "Acknowledged", "Resolved" };
        var instances = new[] { "PROD-SQL-01", "PROD-SQL-02", "DEV-SQL-01", "TEST-SQL-01", "STAGING-SQL-01" };
        var alertTypes = new[] { "Performance", "Backup", "Connection", "Storage", "Memory", "CPU", "Security" };
        
        var alerts = new List<AlertModel>();
        
        for (int i = 1; i <= 50; i++)
        {
            var severity = severities[random.Next(severities.Length)];
            var status = statuses[random.Next(statuses.Length)];
            var instance = instances[random.Next(instances.Length)];
            var alertType = alertTypes[random.Next(alertTypes.Length)];
            
            var alert = new AlertModel
            {
                AlertId = i,
                InstanceName = instance,
                AlertType = alertType,
                Severity = severity,
                Status = status,
                Timestamp = DateTime.Now.AddMinutes(-random.Next(1, 10080)), // Random time in last week
                Message = GenerateAlertMessage(alertType, severity),
                Details = GenerateAlertDetails(alertType, severity)
            };
            
            if (status != "Active")
            {
                alert.AcknowledgedBy = "Admin User";
                alert.AcknowledgedAt = alert.Timestamp.AddMinutes(random.Next(5, 60));
                
                if (status == "Resolved")
                {
                    alert.ResolvedBy = "Admin User";
                    alert.ResolvedAt = alert.AcknowledgedAt?.AddMinutes(random.Next(10, 120));
                }
            }
            
            alerts.Add(alert);
        }
        
        return alerts.OrderByDescending(a => a.Timestamp).ToList();
    }

    private string GenerateAlertMessage(string alertType, string severity)
    {
        return alertType switch
        {
            "Performance" => severity == "Critical" ? "Critical performance degradation detected" : "Performance threshold exceeded",
            "Backup" => severity == "Critical" ? "Backup failure detected" : "Backup completion delayed",
            "Connection" => severity == "Critical" ? "Database connection failure" : "High connection count detected",
            "Storage" => severity == "Critical" ? "Disk space critically low" : "Storage threshold exceeded",
            "Memory" => severity == "Critical" ? "Memory usage critical" : "High memory consumption detected",
            "CPU" => severity == "Critical" ? "CPU usage at critical level" : "High CPU utilization detected",
            "Security" => severity == "Critical" ? "Security breach detected" : "Suspicious activity detected",
            _ => "System alert generated"
        };
    }

    private string GenerateAlertDetails(string alertType, string severity)
    {
        var random = new Random();
        
        return alertType switch
        {
            "Performance" => $"Query response time: {random.Next(5000, 15000)}ms, Blocking sessions: {random.Next(10, 50)}",
            "Backup" => $"Backup duration: {random.Next(120, 600)} minutes, Size: {random.Next(50, 500)}GB",
            "Connection" => $"Active connections: {random.Next(500, 1000)}, Max configured: 1000",
            "Storage" => $"Free space: {random.Next(1, 15)}GB, Total capacity: {random.Next(500, 2000)}GB",
            "Memory" => $"Memory usage: {random.Next(85, 98)}%, Available: {random.Next(1, 8)}GB",
            "CPU" => $"CPU utilization: {random.Next(85, 100)}%, Wait time: {random.Next(20, 80)}%",
            "Security" => $"Failed login attempts: {random.Next(10, 100)}, Source IP: 192.168.{random.Next(1, 255)}.{random.Next(1, 255)}",
            _ => "Alert details not available"
        };
    }
}
