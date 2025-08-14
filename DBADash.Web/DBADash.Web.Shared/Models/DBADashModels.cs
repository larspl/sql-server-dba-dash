using System.ComponentModel.DataAnnotations;

namespace DBADash.Web.Shared.Models;

/// <summary>
/// SQL Server Instance information
/// </summary>
public class InstanceDto
{
    public int InstanceId { get; set; }
    
    [Required]
    public string InstanceName { get; set; } = string.Empty;
    
    public string? ConnectionString { get; set; }
    
    public string? ServerName { get; set; }
    
    public string? DatabaseEngine { get; set; }
    
    public string? Version { get; set; }
    
    public string? Edition { get; set; }
    
    public bool IsActive { get; set; }
    
    public bool IsOnline { get; set; }
    
    public DateTime? LastSeen { get; set; }
    
    public DateTime CreatedDate { get; set; }
    
    public string? Tags { get; set; }
    
    public int? Port { get; set; }
    
    public bool IsAzure { get; set; }
}

/// <summary>
/// Performance counter data
/// </summary>
public class PerformanceCounterDto
{
    public int InstanceId { get; set; }
    
    public string CounterName { get; set; } = string.Empty;
    
    public string? ObjectName { get; set; }
    
    public string? InstanceName { get; set; }
    
    public decimal Value { get; set; }
    
    public DateTime SampleTime { get; set; }
    
    public string? Unit { get; set; }
}

/// <summary>
/// Database backup information
/// </summary>
public class BackupDto
{
    public int BackupId { get; set; }
    
    public int InstanceId { get; set; }
    
    public string DatabaseName { get; set; } = string.Empty;
    
    public string BackupType { get; set; } = string.Empty;
    
    public DateTime BackupStartDate { get; set; }
    
    public DateTime BackupFinishDate { get; set; }
    
    public decimal BackupSizeMB { get; set; }
    
    public decimal CompressedBackupSizeMB { get; set; }
    
    public string? BackupPath { get; set; }
    
    public bool IsEncrypted { get; set; }
    
    public string? ChecksumAlgorithm { get; set; }
    
    public TimeSpan Duration => BackupFinishDate - BackupStartDate;
}

/// <summary>
/// System alert information
/// </summary>
public class AlertDto
{
    public int AlertId { get; set; }
    
    public int InstanceId { get; set; }
    
    public string AlertType { get; set; } = string.Empty;
    
    public string Title { get; set; } = string.Empty;
    
    public string Message { get; set; } = string.Empty;
    
    public string Severity { get; set; } = string.Empty;
    
    public DateTime CreatedDate { get; set; }
    
    public DateTime? AcknowledgedDate { get; set; }
    
    public string? AcknowledgedBy { get; set; }
    
    public string? AcknowledgeComment { get; set; }
    
    public bool IsActive { get; set; }
    
    public string? Source { get; set; }
    
    public string? DatabaseName { get; set; }
}

/// <summary>
/// Real-time performance snapshot
/// </summary>
public class PerformanceSnapshotDto
{
    public DateTime Timestamp { get; set; }
    
    public List<InstancePerformanceDto> Instances { get; set; } = new();
}

/// <summary>
/// Instance-specific performance data
/// </summary>
public class InstancePerformanceDto
{
    public int InstanceId { get; set; }
    
    public string InstanceName { get; set; } = string.Empty;
    
    public bool IsOnline { get; set; }
    
    public decimal CpuUsagePercent { get; set; }
    
    public decimal MemoryUsagePercent { get; set; }
    
    public int ActiveConnections { get; set; }
    
    public decimal DiskIOPS { get; set; }
    
    public decimal NetworkIOKBps { get; set; }
    
    public int BlockedProcesses { get; set; }
    
    public List<PerformanceCounterDto> Counters { get; set; } = new();
}

/// <summary>
/// Database space usage information
/// </summary>
public class DatabaseSpaceDto
{
    public int InstanceId { get; set; }
    
    public string DatabaseName { get; set; } = string.Empty;
    
    public decimal TotalSizeMB { get; set; }
    
    public decimal UsedSizeMB { get; set; }
    
    public decimal FreeSizeMB { get; set; }
    
    public decimal PercentUsed => TotalSizeMB > 0 ? (UsedSizeMB / TotalSizeMB) * 100 : 0;
    
    public DateTime LastUpdated { get; set; }
    
    public string? FileGroup { get; set; }
    
    public string? LogicalName { get; set; }
    
    public string? PhysicalName { get; set; }
}

/// <summary>
/// Configuration setting information
/// </summary>
public class ConfigurationDto
{
    public int InstanceId { get; set; }
    
    public string ConfigurationName { get; set; } = string.Empty;
    
    public string? Value { get; set; }
    
    public string? DefaultValue { get; set; }
    
    public bool IsDefault => Value == DefaultValue;
    
    public string? Description { get; set; }
    
    public DateTime LastUpdated { get; set; }
    
    public string? Category { get; set; }
}

/// <summary>
/// Dashboard summary information
/// </summary>
public class DashboardSummaryDto
{
    public int TotalInstances { get; set; }
    
    public int OnlineInstances { get; set; }
    
    public int OfflineInstances { get; set; }
    
    public int ActiveAlerts { get; set; }
    
    public int CriticalAlerts { get; set; }
    
    public DateTime LastRefresh { get; set; }
    
    public List<InstanceSummaryDto> RecentInstances { get; set; } = new();
    
    public List<AlertDto> RecentAlerts { get; set; } = new();
}

/// <summary>
/// Instance summary for dashboard
/// </summary>
public class InstanceSummaryDto
{
    public int InstanceId { get; set; }
    
    public string InstanceName { get; set; } = string.Empty;
    
    public bool IsOnline { get; set; }
    
    public string Status { get; set; } = string.Empty;
    
    public DateTime? LastSeen { get; set; }
    
    public int AlertCount { get; set; }
    
    public string? Version { get; set; }
}
