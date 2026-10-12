using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SMIS.Domain.Entities.Security;
using SMIS.Infrastructure.Data;

namespace SMIS.Web.Controllers;

[Authorize(Roles = "Administrator")]
public class PermissionsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly RoleManager<IdentityRole> _roleManager;

    public PermissionsController(ApplicationDbContext db,
        RoleManager<IdentityRole> roleManager)
    {
        _db = db;
        _roleManager = roleManager;
    }

    // System modules
    public static List<string> SystemModules => new()
    {
        "Dashboard",
        "Students",
        "Attendance",
        "CBC Academic",
        "Examinations",
        "Finance",
        "Fee Categories",
        "Fee Structures",
        "Invoices",
        "Library",
        "School Setup",
        "Academic Years",
        "Classes",
        "Subjects",
        "User Management",
        "Permissions",
        "Reports"
    };

    // GET: /Permissions
    public async Task<IActionResult> Index()
    {
        var roles = await _roleManager.Roles
            .OrderBy(r => r.Name).ToListAsync();
        var permissions = await _db.RolePermissions.ToListAsync();

        ViewBag.Roles = roles;
        ViewBag.Modules = SystemModules;
        ViewBag.Permissions = permissions;

        return View();
    }

    // POST: /Permissions/SavePermissions
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePermissions(string roleName,
        List<string> modules, List<bool> canView, List<bool> canCreate,
        List<bool> canEdit, List<bool> canDelete, List<bool> canPrint,
        List<bool> canExport)
    {
        // Remove existing permissions for this role
        var existing = await _db.RolePermissions
            .Where(p => p.RoleName == roleName)
            .ToListAsync();
        _db.RolePermissions.RemoveRange(existing);

        // Add new permissions
        for (int i = 0; i < modules.Count; i++)
        {
            var permission = new RolePermission
            {
                RoleName = roleName,
                Module = modules[i],
                CanView = canView.Count > i && canView[i],
                CanCreate = canCreate.Count > i && canCreate[i],
                CanEdit = canEdit.Count > i && canEdit[i],
                CanDelete = canDelete.Count > i && canDelete[i],
                CanPrint = canPrint.Count > i && canPrint[i],
                CanExport = canExport.Count > i && canExport[i],
            };
            _db.RolePermissions.Add(permission);
        }

        await _db.SaveChangesAsync();

        // Log audit
        await LogAudit("Update Permissions",
            "Permissions",
            $"Updated permissions for role: {roleName}");

        TempData["Success"] =
            $"Permissions for '{roleName}' saved successfully!";
        return RedirectToAction(nameof(Index),
            new { selectedRole = roleName });
    }

    // POST: /Permissions/SeedDefaultPermissions
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SeedDefaultPermissions()
    {
        if (await _db.RolePermissions.AnyAsync())
        {
            TempData["Error"] = "Permissions already exist!";
            return RedirectToAction(nameof(Index));
        }

        var defaultPermissions = new List<RolePermission>();

        // Administrator — full access
        foreach (var module in SystemModules)
        {
            defaultPermissions.Add(new RolePermission
            {
                RoleName = "Administrator",
                Module = module,
                CanView = true, CanCreate = true,
                CanEdit = true, CanDelete = true,
                CanPrint = true, CanExport = true
            });
        }

        // Principal — view all, limited edit
        var principalModules = new[] { "Dashboard", "Students",
            "Attendance", "CBC Academic", "Finance", "Invoices",
            "Library", "Reports" };
        foreach (var module in principalModules)
        {
            defaultPermissions.Add(new RolePermission
            {
                RoleName = "Principal",
                Module = module,
                CanView = true, CanCreate = false,
                CanEdit = false, CanDelete = false,
                CanPrint = true, CanExport = true
            });
        }

        // Teacher — limited access
        var teacherModules = new[] { "Dashboard", "Students",
            "Attendance", "CBC Academic" };
        foreach (var module in teacherModules)
        {
            defaultPermissions.Add(new RolePermission
            {
                RoleName = "Teacher",
                Module = module,
                CanView = true,
                CanCreate = module == "Attendance" || module == "CBC Academic",
                CanEdit = module == "Attendance" || module == "CBC Academic",
                CanDelete = false,
                CanPrint = true,
                CanExport = false
            });
        }

        // Bursar — finance access
        var bursarModules = new[] { "Dashboard", "Students",
            "Finance", "Fee Categories", "Fee Structures",
            "Invoices", "Reports" };
        foreach (var module in bursarModules)
        {
            defaultPermissions.Add(new RolePermission
            {
                RoleName = "Bursar",
                Module = module,
                CanView = true,
                CanCreate = true,
                CanEdit = true,
                CanDelete = false,
                CanPrint = true,
                CanExport = true
            });
        }

        // Librarian — library access
        var librarianModules = new[] { "Dashboard", "Library" };
        foreach (var module in librarianModules)
        {
            defaultPermissions.Add(new RolePermission
            {
                RoleName = "Librarian",
                Module = module,
                CanView = true,
                CanCreate = true,
                CanEdit = true,
                CanDelete = false,
                CanPrint = true,
                CanExport = false
            });
        }

        _db.RolePermissions.AddRange(defaultPermissions);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Default permissions seeded successfully!";
        return RedirectToAction(nameof(Index));
    }

    // Helper: Log audit trail
    private async Task LogAudit(string action, string module,
        string description)
    {
        var log = new AuditLog
        {
            UserEmail = User.Identity?.Name ?? "system",
            Action = action,
            Module = module,
            Description = description,
            Timestamp = DateTime.UtcNow,
            IsSuccess = true
        };
        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync();
    }
}