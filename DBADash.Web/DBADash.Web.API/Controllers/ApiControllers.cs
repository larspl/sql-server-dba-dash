using Microsoft.AspNetCore.Mvc;
using DBADash.Web.Core.Services;
using DBADash.Web.Shared.Models;

namespace DBADash.Web.API.Controllers;

/// <summary>
/// API controller for instance management
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class InstancesController : ControllerBase
{
    private readonly IInstanceService _instanceService;
    private readonly ILogger<InstancesController> _logger;

    public InstancesController(IInstanceService instanceService, ILogger<InstancesController> logger)
    {
        _instanceService = instanceService;
        _logger = logger;
    }

    /// <summary>
    /// Get all SQL Server instances
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<InstanceDto>>> GetInstances()
    {
        try
        {
            var instances = await _instanceService.GetInstancesAsync();
            return Ok(instances);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting instances");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get instance summaries for dashboard
    /// </summary>
    [HttpGet("summaries")]
    public async Task<ActionResult<IEnumerable<InstanceSummaryDto>>> GetInstanceSummaries()
    {
        try
        {
            var summaries = await _instanceService.GetInstanceSummariesAsync();
            return Ok(summaries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting instance summaries");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get specific instance by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<InstanceDto>> GetInstance(int id)
    {
        try
        {
            var instance = await _instanceService.GetInstanceAsync(id);
            if (instance == null)
            {
                return NotFound();
            }
            return Ok(instance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting instance {InstanceId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Test connection to an instance
    /// </summary>
    [HttpPost("{id}/test-connection")]
    public async Task<ActionResult<bool>> TestConnection(int id)
    {
        try
        {
            var result = await _instanceService.TestConnectionAsync(id);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing connection for instance {InstanceId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Add new instance
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<InstanceDto>> CreateInstance([FromBody] InstanceDto instance)
    {
        try
        {
            var createdInstance = await _instanceService.AddInstanceAsync(instance);
            return CreatedAtAction(nameof(GetInstance), new { id = createdInstance.InstanceId }, createdInstance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating instance");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update existing instance
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<InstanceDto>> UpdateInstance(int id, [FromBody] InstanceDto instance)
    {
        try
        {
            if (id != instance.InstanceId)
            {
                return BadRequest("Instance ID mismatch");
            }

            var updatedInstance = await _instanceService.UpdateInstanceAsync(instance);
            if (updatedInstance == null)
            {
                return NotFound();
            }

            return Ok(updatedInstance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating instance {InstanceId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Delete instance
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteInstance(int id)
    {
        try
        {
            var result = await _instanceService.DeleteInstanceAsync(id);
            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting instance {InstanceId}", id);
            return StatusCode(500, "Internal server error");
        }
    }
}

/// <summary>
/// API controller for performance monitoring
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PerformanceController : ControllerBase
{
    private readonly IPerformanceService _performanceService;
    private readonly ILogger<PerformanceController> _logger;

    public PerformanceController(IPerformanceService performanceService, ILogger<PerformanceController> logger)
    {
        _performanceService = performanceService;
        _logger = logger;
    }

    /// <summary>
    /// Get current performance snapshot for an instance
    /// </summary>
    [HttpGet("current/{instanceId}")]
    public async Task<ActionResult<InstancePerformanceDto>> GetCurrentPerformance(int instanceId)
    {
        try
        {
            var performance = await _performanceService.GetCurrentPerformanceSnapshotAsync(instanceId);
            if (performance == null)
            {
                return NotFound();
            }
            return Ok(performance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current performance for instance {InstanceId}", instanceId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get performance history for an instance
    /// </summary>
    [HttpGet("history/{instanceId}")]
    public async Task<ActionResult<IEnumerable<PerformanceSnapshotDto>>> GetPerformanceHistory(
        int instanceId, 
        [FromQuery] DateTime? from = null, 
        [FromQuery] DateTime? to = null)
    {
        try
        {
            var history = await _performanceService.GetPerformanceHistoryAsync(instanceId, from, to);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance history for instance {InstanceId}", instanceId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get blocked processes for an instance
    /// </summary>
    [HttpGet("blocked/{instanceId}")]
    public async Task<ActionResult<IEnumerable<BlockedProcessDto>>> GetBlockedProcesses(int instanceId)
    {
        try
        {
            var blockedProcesses = await _performanceService.GetBlockedProcessesAsync(instanceId);
            return Ok(blockedProcesses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting blocked processes for instance {InstanceId}", instanceId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get expensive queries for an instance
    /// </summary>
    [HttpGet("expensive-queries/{instanceId}")]
    public async Task<ActionResult<IEnumerable<ExpensiveQueryDto>>> GetExpensiveQueries(int instanceId)
    {
        try
        {
            var queries = await _performanceService.GetExpensiveQueriesAsync(instanceId);
            return Ok(queries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expensive queries for instance {InstanceId}", instanceId);
            return StatusCode(500, "Internal server error");
        }
    }
}

/// <summary>
/// API controller for backup monitoring
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BackupsController : ControllerBase
{
    private readonly IBackupService _backupService;
    private readonly ILogger<BackupsController> _logger;

    public BackupsController(IBackupService backupService, ILogger<BackupsController> logger)
    {
        _backupService = backupService;
        _logger = logger;
    }

    /// <summary>
    /// Get backups for an instance
    /// </summary>
    [HttpGet("{instanceId}")]
    public async Task<ActionResult<IEnumerable<BackupDto>>> GetBackups(
        int instanceId, 
        [FromQuery] DateTime? from = null, 
        [FromQuery] DateTime? to = null)
    {
        try
        {
            var backups = await _backupService.GetBackupsAsync(instanceId, from, to);
            return Ok(backups);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting backups for instance {InstanceId}", instanceId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get last backup for a specific database
    /// </summary>
    [HttpGet("{instanceId}/database/{databaseName}/last")]
    public async Task<ActionResult<BackupDto>> GetLastBackup(int instanceId, string databaseName)
    {
        try
        {
            var backup = await _backupService.GetLastBackupAsync(instanceId, databaseName);
            if (backup == null)
            {
                return NotFound();
            }
            return Ok(backup);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting last backup for database {DatabaseName} on instance {InstanceId}", 
                databaseName, instanceId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Validate backup integrity
    /// </summary>
    [HttpPost("{backupId}/validate")]
    public async Task<ActionResult<bool>> ValidateBackup(int backupId)
    {
        try
        {
            var result = await _backupService.ValidateBackupIntegrityAsync(backupId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating backup {BackupId}", backupId);
            return StatusCode(500, "Internal server error");
        }
    }
}

/// <summary>
/// API controller for alerts
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AlertsController : ControllerBase
{
    private readonly IAlertService _alertService;
    private readonly ILogger<AlertsController> _logger;

    public AlertsController(IAlertService alertService, ILogger<AlertsController> logger)
    {
        _alertService = alertService;
        _logger = logger;
    }

    /// <summary>
    /// Get active alerts
    /// </summary>
    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<AlertDto>>> GetActiveAlerts()
    {
        try
        {
            var alerts = await _alertService.GetActiveAlertsAsync();
            return Ok(alerts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active alerts");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get alerts with optional filtering
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AlertDto>>> GetAlerts(
        [FromQuery] int? instanceId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        try
        {
            var alerts = await _alertService.GetAlertsAsync(instanceId, from, to);
            return Ok(alerts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting alerts");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Acknowledge an alert
    /// </summary>
    [HttpPost("{alertId}/acknowledge")]
    public async Task<ActionResult<bool>> AcknowledgeAlert(
        int alertId, 
        [FromBody] AcknowledgeAlertRequest request)
    {
        try
        {
            var result = await _alertService.AcknowledgeAlertAsync(alertId, request.AcknowledgedBy, request.Comment);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acknowledging alert {AlertId}", alertId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get active alert count
    /// </summary>
    [HttpGet("count")]
    public async Task<ActionResult<int>> GetActiveAlertCount()
    {
        try
        {
            var count = await _alertService.GetActiveAlertCountAsync();
            return Ok(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active alert count");
            return StatusCode(500, "Internal server error");
        }
    }
}

/// <summary>
/// API controller for reports and dashboard
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IReportService reportService, ILogger<ReportsController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    /// <summary>
    /// Get dashboard summary
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardSummaryDto>> GetDashboard()
    {
        try
        {
            var dashboard = await _reportService.GetDashboardSummaryAsync();
            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard summary");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Generate performance report
    /// </summary>
    [HttpPost("performance/{instanceId}")]
    public async Task<ActionResult> GeneratePerformanceReport(
        int instanceId,
        [FromBody] ReportRequest request)
    {
        try
        {
            var report = await _reportService.GeneratePerformanceReportAsync(
                instanceId, request.From, request.To, request.Format);

            var contentType = request.Format.ToLower() == "pdf" ? "application/pdf" : "application/vnd.ms-excel";
            var fileName = $"PerformanceReport_{instanceId}_{DateTime.Now:yyyyMMdd}.{request.Format}";

            return File(report, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating performance report for instance {InstanceId}", instanceId);
            return StatusCode(500, "Internal server error");
        }
    }
}

/// <summary>
/// Request DTOs for API endpoints
/// </summary>
public class AcknowledgeAlertRequest
{
    public string? AcknowledgedBy { get; set; }
    public string? Comment { get; set; }
}

public class ReportRequest
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public string Format { get; set; } = "pdf";
}
