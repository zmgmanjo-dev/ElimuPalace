using SMIS.Domain.Common;

namespace SMIS.Domain.Entities.Platform;

public enum RegistrationStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Suspended = 4
}

/// <summary>
/// One row per school that has registered on the platform, living in the
/// Master database (never in any school's own tenant database).
/// </summary>
public class SchoolRegistration : AuditableEntity
{
    public string SchoolName { get; set; } = null!;
    public string Slug { get; set; } = null!;         // e.g. "tumaini" → /tumaini/login
    public string DatabaseName { get; set; } = null!;  // e.g. "SMIS_tumaini"

    public string ContactEmail { get; set; } = null!;
    public string? ContactPhone { get; set; }
    public string PhysicalAddress { get; set; } = "";

    // The admin login this school will use once approved
    public string AdminFullName { get; set; } = null!;
    public string AdminEmail { get; set; } = null!;
    public string AdminPasswordHash { get; set; } = null!; // hashed at registration time

    public RegistrationStatus Status { get; set; } = RegistrationStatus.Pending;
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedBy { get; set; }
    public string? RejectionReason { get; set; }
}