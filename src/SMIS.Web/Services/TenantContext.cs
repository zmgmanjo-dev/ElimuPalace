namespace SMIS.Web.Services;

/// <summary>
/// Per-request holder for "which school's database is this request talking to".
/// Populated by TenantResolutionMiddleware before any ApplicationDbContext
/// is constructed for the request.
/// </summary>
public class TenantContext
{
    public string? DatabaseName { get; set; }
    public string? SchoolName { get; set; }
    public bool SlugNotFound { get; set; }
    public bool IsSuspended { get; set; }
}