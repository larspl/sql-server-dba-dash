using System;

namespace DBADash.ModernUI.Models;

public class AlertModel
{
    public int AlertId { get; set; }
    public string InstanceName { get; set; } = string.Empty;
    public string AlertType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Status { get; set; } = string.Empty;
    public string AcknowledgedBy { get; set; } = string.Empty;
    public DateTime? AcknowledgedAt { get; set; }
    public string ResolvedBy { get; set; } = string.Empty;
    public DateTime? ResolvedAt { get; set; }
    
    // UI Helper Properties
    public bool CanAcknowledge => Status == "Active";
    public bool CanResolve => Status == "Active" || Status == "Acknowledged";
    public bool IsActive => Status == "Active";
    public bool IsAcknowledged => Status == "Acknowledged";
    public bool IsResolved => Status == "Resolved";
}
