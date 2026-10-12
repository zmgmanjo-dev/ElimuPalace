using SMIS.Domain.Common;

namespace SMIS.Domain.Entities.Security;

/// <summary>
/// Tracks all important actions in the system for audit purposes
/// </summary>
public class AuditLog : BaseEntity
{
    public string UserEmail { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsSuccess { get; set; } = true;
}