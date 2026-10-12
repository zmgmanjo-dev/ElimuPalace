using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using SMIS.Domain.Entities.Security;
using SMIS.Infrastructure.Data;

namespace SMIS.Web.Controllers;

public class BaseController : Controller
{
    protected readonly ApplicationDbContext _db;

    public BaseController(ApplicationDbContext db)
    {
        _db = db;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        try
        {
            var school = _db.SchoolProfiles.FirstOrDefault();
            ViewBag.School = school;
        }
        catch { }
        base.OnActionExecuting(context);
    }

    protected async Task LogAudit(string module, string action,
        string? description = null, bool isSuccess = true)
    {
        try
        {
            _db.AuditLogs.Add(new AuditLog
            {
                UserEmail = User.Identity?.Name ?? "system",
                Action = action,
                Module = module,
                Description = description,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                Timestamp = DateTime.UtcNow,
                IsSuccess = isSuccess
            });
            await _db.SaveChangesAsync();
        }
        catch { /* never let audit logging break the main action */ }
    }
}