using Microsoft.AspNetCore.SignalR;
using DBADash.Web.Core.Services;

namespace DBADash.Web.Server.Hubs;

/// <summary>
/// SignalR Hub for real-time monitoring updates
/// Provides live data updates to connected web clients
/// </summary>
public class MonitoringHub : Hub
{
    private readonly ILogger<MonitoringHub> _logger;
    private readonly IPerformanceService _performanceService;

    public MonitoringHub(ILogger<MonitoringHub> logger, IPerformanceService performanceService)
    {
        _logger = logger;
        _performanceService = performanceService;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        
        // Send initial data to newly connected client
        try
        {
            var initialData = await _performanceService.GetCurrentPerformanceSnapshotAsync();
            await Clients.Caller.SendAsync("InitialData", initialData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending initial data to client {ConnectionId}", Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        
        if (exception != null)
        {
            _logger.LogError(exception, "Client {ConnectionId} disconnected with error", Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Join a specific instance monitoring group
    /// </summary>
    public async Task JoinInstanceGroup(int instanceId)
    {
        var groupName = $"Instance_{instanceId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogDebug("Client {ConnectionId} joined group {GroupName}", Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Leave a specific instance monitoring group
    /// </summary>
    public async Task LeaveInstanceGroup(int instanceId)
    {
        var groupName = $"Instance_{instanceId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogDebug("Client {ConnectionId} left group {GroupName}", Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Request specific performance data for an instance
    /// </summary>
    public async Task RequestInstanceData(int instanceId)
    {
        try
        {
            var data = await _performanceService.GetInstancePerformanceAsync(instanceId);
            await Clients.Caller.SendAsync("InstanceData", new { InstanceId = instanceId, Data = data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting instance data for {InstanceId}", instanceId);
            await Clients.Caller.SendAsync("Error", $"Failed to get data for instance {instanceId}");
        }
    }
}

/// <summary>
/// SignalR Hub for alert notifications
/// </summary>
public class AlertHub : Hub
{
    private readonly ILogger<AlertHub> _logger;
    private readonly IAlertService _alertService;

    public AlertHub(ILogger<AlertHub> logger, IAlertService alertService)
    {
        _logger = logger;
        _alertService = alertService;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Alert client connected: {ConnectionId}", Context.ConnectionId);
        
        // Send current active alerts to newly connected client
        try
        {
            var activeAlerts = await _alertService.GetActiveAlertsAsync();
            await Clients.Caller.SendAsync("ActiveAlerts", activeAlerts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending active alerts to client {ConnectionId}", Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Alert client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Acknowledge an alert
    /// </summary>
    public async Task AcknowledgeAlert(int alertId, string? comment = null)
    {
        try
        {
            await _alertService.AcknowledgeAlertAsync(alertId, Context.User?.Identity?.Name, comment);
            
            // Notify all clients about the acknowledgment
            await Clients.All.SendAsync("AlertAcknowledged", new { AlertId = alertId, Comment = comment });
            
            _logger.LogInformation("Alert {AlertId} acknowledged by {User}", alertId, Context.User?.Identity?.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acknowledging alert {AlertId}", alertId);
            await Clients.Caller.SendAsync("Error", $"Failed to acknowledge alert {alertId}");
        }
    }

    /// <summary>
    /// Subscribe to alerts for specific instances
    /// </summary>
    public async Task SubscribeToInstanceAlerts(int instanceId)
    {
        var groupName = $"AlertsInstance_{instanceId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogDebug("Client {ConnectionId} subscribed to alerts for instance {InstanceId}", Context.ConnectionId, instanceId);
    }

    /// <summary>
    /// Unsubscribe from alerts for specific instances
    /// </summary>
    public async Task UnsubscribeFromInstanceAlerts(int instanceId)
    {
        var groupName = $"AlertsInstance_{instanceId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogDebug("Client {ConnectionId} unsubscribed from alerts for instance {InstanceId}", Context.ConnectionId, instanceId);
    }
}
