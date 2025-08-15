using System.Threading.Tasks;
using DBADashGUI;
using DBADash;

namespace DBADash.ModernUI.Services;

/// <summary>
/// Data service interface for accessing DBA Dash data
/// </summary>
public interface IDataService
{
    Task<SummaryData?> GetSummaryDataAsync();
    Task<AlertCounts?> GetAlertCountsAsync();
    Task<IEnumerable<InstanceInfo>> GetInstancesAsync();
    Task<PerformanceData?> GetPerformanceDataAsync(int instanceId, DateTime? fromDate = null, DateTime? toDate = null);
}

/// <summary>
/// Data service implementation that wraps existing DBA Dash data access
/// </summary>
public class DataService : IDataService
{
    private readonly string _connectionString;

    public DataService()
    {
        // Get connection string from existing configuration
        _connectionString = Common.ConnectionString;
    }

    public async Task<SummaryData?> GetSummaryDataAsync()
    {
        try
        {
            // This would wrap existing CommonData methods
            await Task.Delay(100); // Simulate async operation
            
            return new SummaryData
            {
                ConnectionString = _connectionString,
                ServerName = ExtractServerName(_connectionString),
                DatabaseName = ExtractDatabaseName(_connectionString),
                TotalInstances = 0, // Get from existing data access
                ActiveAlerts = 0,   // Get from existing data access
                LastUpdated = DateTime.Now
            };
        }
        catch (Exception ex)
        {
            // Log error
            throw new DataAccessException("Failed to load summary data", ex);
        }
    }

    public async Task<AlertCounts?> GetAlertCountsAsync()
    {
        try
        {
            await Task.Delay(50); // Simulate async operation
            
            // This would use existing alert counting logic
            return new AlertCounts
            {
                ActiveAlerts = 0,    // Get from existing data access
                SummaryAlerts = 0,   // Get from existing data access
                CriticalAlerts = 0,  // Get from existing data access
                WarningAlerts = 0    // Get from existing data access
            };
        }
        catch (Exception ex)
        {
            throw new DataAccessException("Failed to load alert counts", ex);
        }
    }

    public async Task<IEnumerable<InstanceInfo>> GetInstancesAsync()
    {
        try
        {
            await Task.Delay(100); // Simulate async operation
            
            // This would wrap existing instance enumeration
            return new List<InstanceInfo>();
        }
        catch (Exception ex)
        {
            throw new DataAccessException("Failed to load instances", ex);
        }
    }

    public async Task<PerformanceData?> GetPerformanceDataAsync(int instanceId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        try
        {
            await Task.Delay(150); // Simulate async operation
            
            // This would wrap existing performance data access
            return new PerformanceData
            {
                InstanceId = instanceId,
                FromDate = fromDate ?? DateTime.Now.AddHours(-24),
                ToDate = toDate ?? DateTime.Now,
                Metrics = new List<PerformanceMetric>()
            };
        }
        catch (Exception ex)
        {
            throw new DataAccessException("Failed to load performance data", ex);
        }
    }

    private static string ExtractServerName(string connectionString)
    {
        try
        {
            var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
            return builder.DataSource ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    private static string ExtractDatabaseName(string connectionString)
    {
        try
        {
            var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
            return builder.InitialCatalog ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }
}

/// <summary>
/// Data transfer objects
/// </summary>
public class SummaryData
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ServerName { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public int TotalInstances { get; set; }
    public int ActiveAlerts { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class AlertCounts
{
    public int ActiveAlerts { get; set; }
    public int SummaryAlerts { get; set; }
    public int CriticalAlerts { get; set; }
    public int WarningAlerts { get; set; }
}

public class InstanceInfo
{
    public int InstanceId { get; set; }
    public string InstanceName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime LastSeen { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class PerformanceData
{
    public int InstanceId { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public List<PerformanceMetric> Metrics { get; set; } = new();
}

public class PerformanceMetric
{
    public DateTime Timestamp { get; set; }
    public string MetricName { get; set; } = string.Empty;
    public double Value { get; set; }
    public string Unit { get; set; } = string.Empty;
}

/// <summary>
/// Custom exception for data access errors
/// </summary>
public class DataAccessException : Exception
{
    public DataAccessException(string message) : base(message) { }
    public DataAccessException(string message, Exception innerException) : base(message, innerException) { }
}
