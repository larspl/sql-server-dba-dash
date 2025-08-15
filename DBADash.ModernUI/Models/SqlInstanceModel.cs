using System;

namespace DBADash.ModernUI.Models;

public class SqlInstanceModel
{
    public int InstanceId { get; set; }
    public string InstanceName { get; set; } = string.Empty;
    public string ServerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Edition { get; set; } = string.Empty;
    public double CpuUsage { get; set; }
    public double MemoryUsage { get; set; }
    public int ActiveConnections { get; set; }
    public DateTime LastBackup { get; set; }
    public string LastBackupAge { get; set; } = string.Empty;
    public string LastBackupStatus { get; set; } = string.Empty;
    public DateTime LastSeen { get; set; }
    
    // Additional properties for detailed monitoring
    public string ConnectionString { get; set; } = string.Empty;
    public bool IsMonitored { get; set; } = true;
    public TimeSpan CollectionInterval { get; set; } = TimeSpan.FromMinutes(1);
    public string Environment { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
