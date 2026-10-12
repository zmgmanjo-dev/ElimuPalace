using SMIS.Domain.Common;

namespace SMIS.Domain.Entities.Platform;

public class SuperAdmin : AuditableEntity
{
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public bool IsActive { get; set; } = true;
}