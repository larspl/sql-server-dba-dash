using DBADash.Web.Shared.Models;

namespace DBADash.Web.Core.Services;

/// <summary>
/// Service interface for managing SQL Server instances
/// </summary>
public interface IInstanceService
{
    Task<IEnumerable<InstanceDto>> GetInstancesAsync();
    Task<InstanceDto?> GetInstanceAsync(int instanceId);
    Task<InstanceDto> CreateInstanceAsync(InstanceDto instance);
    Task<InstanceDto> UpdateInstanceAsync(InstanceDto instance);
    Task<bool> DeleteInstanceAsync(int instanceId);
    Task<bool> TestConnectionAsync(string connectionString);
    Task<IEnumerable<InstanceSummaryDto>> GetInstanceSummariesAsync();
}

/// <summary>
/// Service interface for performance monitoring
/// </summary>
public interface IPerformanceService
{
    Task<PerformanceSnapshotDto> GetCurrentPerformanceSnapshotAsync();
    Task<InstancePerformanceDto> GetInstancePerformanceAsync(int instanceId);
    Task<IEnumerable<PerformanceCounterDto>> GetPerformanceCountersAsync(int instanceId, DateTime from, DateTime to);
    Task<InstancePerformanceDto> CollectPerformanceDataAsync(int instanceId);
    Task<IEnumerable<DatabaseSpaceDto>> GetDatabaseSpaceUsageAsync(int instanceId);
}

/// <summary>
/// Service interface for backup monitoring
/// </summary>
public interface IBackupService
{
    Task<IEnumerable<BackupDto>> GetBackupsAsync(int instanceId, DateTime? from = null, DateTime? to = null);
    Task<BackupDto?> GetLastBackupAsync(int instanceId, string databaseName);
    Task<IEnumerable<BackupDto>> GetBackupSummaryAsync();
    Task<bool> ValidateBackupIntegrityAsync(int backupId);
}

/// <summary>
/// Service interface for configuration management
/// </summary>
public interface IConfigurationService
{
    Task<IEnumerable<ConfigurationDto>> GetConfigurationsAsync(int instanceId);
    Task<ConfigurationDto?> GetConfigurationAsync(int instanceId, string configurationName);
    Task<bool> UpdateConfigurationAsync(int instanceId, string configurationName, string value);
    Task<IEnumerable<ConfigurationDto>> GetConfigurationChangesAsync(int instanceId, DateTime from, DateTime to);
}

/// <summary>
/// Service interface for alert management
/// </summary>
public interface IAlertService
{
    Task<IEnumerable<AlertDto>> GetActiveAlertsAsync();
    Task<IEnumerable<AlertDto>> GetAlertsAsync(int? instanceId = null, DateTime? from = null, DateTime? to = null);
    Task<AlertDto> CreateAlertAsync(AlertDto alert);
    Task<bool> AcknowledgeAlertAsync(int alertId, string? acknowledgedBy = null, string? comment = null);
    Task<IEnumerable<AlertDto>> CheckAlertsAsync(int instanceId, InstancePerformanceDto performanceData);
    Task<int> GetActiveAlertCountAsync();
}

/// <summary>
/// Service interface for report generation
/// </summary>
public interface IReportService
{
    Task<DashboardSummaryDto> GetDashboardSummaryAsync();
    Task<byte[]> GeneratePerformanceReportAsync(int instanceId, DateTime from, DateTime to, string format = "pdf");
    Task<byte[]> GenerateBackupReportAsync(int instanceId, DateTime from, DateTime to, string format = "pdf");
    Task<IEnumerable<object>> GetCustomReportDataAsync(string reportName, Dictionary<string, object> parameters);
}
