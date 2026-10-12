using SMIS.Domain.Common;

namespace SMIS.Domain.Entities.Security;

/// <summary>
/// Defines what each role can do in each module
/// </summary>
public class RolePermission : BaseEntity
{
    public string RoleName { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public bool CanView { get; set; } = false;
    public bool CanCreate { get; set; } = false;
    public bool CanEdit { get; set; } = false;
    public bool CanDelete { get; set; } = false;
    public bool CanPrint { get; set; } = false;
    public bool CanExport { get; set; } = false;
}