using DBADash.Web.Core.Services;
using DBADash.Web.Server.Hubs;
using Microsoft.AspNetCore.SignalR;
using Quartz;

namespace DBADash.Web.Server.Services;

/// <summary>
/// Background service for continuous monitoring and data collection
/// Replaces the Windows Service functionality from original DBADash
/// </summary>
public class MonitoringBackgroundService : BackgroundService
{
    private readonly ILogger<MonitoringBackgroundService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<MonitoringHub> _hubContext;
    private readonly IConfiguration _configuration;

    public MonitoringBackgroundService(
        ILogger<MonitoringBackgroundService> logger,
        IServiceScopeFactory scopeFactory,
        IHubContext<MonitoringHub> hubContext,
        IConfiguration configuration)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Monitoring Background Service starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformMonitoringCycle(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during monitoring cycle");
            }

            // Wait for next cycle (configurable interval)
            var intervalMinutes = _configuration.GetValue<int>("Monitoring:IntervalMinutes", 5);
            await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
        }

        _logger.LogInformation("Monitoring Background Service stopping");
    }

    private async Task PerformMonitoringCycle(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        
        var instanceService = scope.ServiceProvider.GetRequiredService<IInstanceService>();
        var performanceService = scope.ServiceProvider.GetRequiredService<IPerformanceService>();
        var alertService = scope.ServiceProvider.GetRequiredService<IAlertService>();

        try
        {
            // Check instance health
            var instances = await instanceService.GetInstancesAsync();
            foreach (var instance in instances)
            {
                if (cancellationToken.IsCancellationRequested) break;

                await CheckInstanceHealth(instance, performanceService, alertService);
            }

            // Notify connected clients of updates
            await _hubContext.Clients.All.SendAsync("MonitoringUpdate", 
                new { Timestamp = DateTime.UtcNow, Status = "Completed" }, 
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in monitoring cycle");
            
            await _hubContext.Clients.All.SendAsync("MonitoringError", 
                new { Error = ex.Message, Timestamp = DateTime.UtcNow }, 
                cancellationToken);
        }
    }

    private async Task CheckInstanceHealth(
        dynamic instance, 
        IPerformanceService performanceService, 
        IAlertService alertService)
    {
        try
        {
            // Collect performance data
            var performanceData = await performanceService.CollectPerformanceDataAsync(instance.InstanceId);
            
            // Check for alerts
            var alerts = await alertService.CheckAlertsAsync(instance.InstanceId, performanceData);
            
            if (alerts.Any())
            {
                await _hubContext.Clients.All.SendAsync("NewAlerts", new { 
                    InstanceId = instance.InstanceId, 
                    Alerts = alerts 
                });
            }

            _logger.LogDebug("Health check completed for instance {InstanceId}", instance.InstanceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking health for instance {InstanceId}", instance.InstanceId);
        }
    }
}

/// <summary>
/// Quartz job for data collection - can be scheduled independently
/// </summary>
[DisallowConcurrentExecution]
public class DataCollectionJob : IJob
{
    private readonly ILogger<DataCollectionJob> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public DataCollectionJob(ILogger<DataCollectionJob> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Data collection job starting");

        using var scope = _scopeFactory.CreateScope();
        var instanceService = scope.ServiceProvider.GetRequiredService<IInstanceService>();

        try
        {
            var instances = await instanceService.GetInstancesAsync();
            
            foreach (var instance in instances)
            {
                await CollectInstanceData(instance);
            }

            _logger.LogInformation("Data collection job completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in data collection job");
            throw;
        }
    }

    private async Task CollectInstanceData(dynamic instance)
    {
        // Implement data collection logic here
        // This replaces the collection logic from the original Windows Service
        _logger.LogDebug("Collecting data for instance {InstanceId}", instance.InstanceId);
        
        // TODO: Implement actual data collection
        await Task.Delay(100); // Placeholder
    }
}

/// <summary>
/// Quartz job for performance monitoring
/// </summary>
[DisallowConcurrentExecution]
public class PerformanceMonitoringJob : IJob
{
    private readonly ILogger<PerformanceMonitoringJob> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<MonitoringHub> _hubContext;

    public PerformanceMonitoringJob(
        ILogger<PerformanceMonitoringJob> logger, 
        IServiceScopeFactory scopeFactory,
        IHubContext<MonitoringHub> hubContext)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogDebug("Performance monitoring job starting");

        using var scope = _scopeFactory.CreateScope();
        var performanceService = scope.ServiceProvider.GetRequiredService<IPerformanceService>();

        try
        {
            var performanceSnapshot = await performanceService.GetCurrentPerformanceSnapshotAsync();
            
            // Send real-time updates to connected clients
            await _hubContext.Clients.All.SendAsync("PerformanceUpdate", performanceSnapshot);

            _logger.LogDebug("Performance monitoring job completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in performance monitoring job");
        }
    }
}
