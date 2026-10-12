using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SMIS.Domain.Entities.Platform;
using SMIS.Infrastructure.Data;
using System.Security.Claims;

namespace SMIS.Web.Controllers;

public class SuperAdminController : Controller
{
    private readonly MasterDbContext _masterDb;
private readonly PasswordHasher<SuperAdmin> _hasher = new();
private readonly SMIS.Web.Services.TenantProvisioningService _provisioningService;

public SuperAdminController(MasterDbContext masterDb, SMIS.Web.Services.TenantProvisioningService provisioningService)
{
    _masterDb = masterDb;
    _provisioningService = provisioningService;
}

    // GET: /SuperAdmin/Login
    [HttpGet]
    public IActionResult Login() => View();

    // POST: /SuperAdmin/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password)
    {
        var admin = await _masterDb.SuperAdmins
            .FirstOrDefaultAsync(a => a.Email == email && a.IsActive);

        if (admin == null)
        {
            ViewBag.Error = "Invalid email or password.";
            return View();
        }

        var result = _hasher.VerifyHashedPassword(admin, admin.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed)
        {
            ViewBag.Error = "Invalid email or password.";
            return View();
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, admin.Id.ToString()),
            new Claim(ClaimTypes.Email, admin.Email),
            new Claim(ClaimTypes.Name, admin.FullName)
        };
        var identity = new ClaimsIdentity(claims, "SuperAdminScheme");
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync("SuperAdminScheme", principal,
            new AuthenticationProperties { IsPersistent = true });

        return RedirectToAction(nameof(Dashboard));
    }

    // POST: /SuperAdmin/Logout
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(AuthenticationSchemes = "SuperAdminScheme")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("SuperAdminScheme");
        return RedirectToAction(nameof(Login));
    }

    // GET: /SuperAdmin/Dashboard
    [Authorize(AuthenticationSchemes = "SuperAdminScheme")]
    public async Task<IActionResult> Dashboard(string? status)
    {
        var query = _masterDb.SchoolRegistrations.AsQueryable();

        if (!string.IsNullOrEmpty(status) &&
            Enum.TryParse<RegistrationStatus>(status, out var parsedStatus))
            query = query.Where(r => r.Status == parsedStatus);

        var registrations = await query
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        ViewBag.StatusFilter = status;
        ViewBag.PendingCount = await _masterDb.SchoolRegistrations
            .CountAsync(r => r.Status == RegistrationStatus.Pending);
        ViewBag.ApprovedCount = await _masterDb.SchoolRegistrations
            .CountAsync(r => r.Status == RegistrationStatus.Approved);
        ViewBag.SuspendedCount = await _masterDb.SchoolRegistrations
            .CountAsync(r => r.Status == RegistrationStatus.Suspended);

        return View(registrations);
    }

    // POST: /SuperAdmin/Approve/5
[HttpPost]
[ValidateAntiForgeryToken]
[Authorize(AuthenticationSchemes = "SuperAdminScheme")]
public async Task<IActionResult> Approve(int id)
{
    var reg = await _masterDb.SchoolRegistrations.FindAsync(id);
    if (reg == null) return NotFound();

    try
    {
        await _provisioningService.ProvisionSchoolAsync(reg);

        reg.Status = RegistrationStatus.Approved;
        reg.ApprovedAt = DateTime.UtcNow;
        reg.ApprovedBy = User.Identity?.Name ?? "SuperAdmin";
        await _masterDb.SaveChangesAsync();

        TempData["Success"] = $"'{reg.SchoolName}' approved and provisioned! " +
            $"They can now log in with {reg.AdminEmail}.";
    }
    catch (Exception ex)
    {
        TempData["Error"] = $"Provisioning failed for '{reg.SchoolName}': {ex.Message}. " +
            "The application remains Pending — you can try approving again.";
    }

    return RedirectToAction(nameof(Dashboard));
}

    // POST: /SuperAdmin/Reject/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(AuthenticationSchemes = "SuperAdminScheme")]
    public async Task<IActionResult> Reject(int id, string reason)
    {
        var reg = await _masterDb.SchoolRegistrations.FindAsync(id);
        if (reg == null) return NotFound();

        reg.Status = RegistrationStatus.Rejected;
        reg.RejectionReason = reason;
        await _masterDb.SaveChangesAsync();

        TempData["Success"] = $"'{reg.SchoolName}' rejected.";
        return RedirectToAction(nameof(Dashboard));
    }

    // POST: /SuperAdmin/Suspend/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(AuthenticationSchemes = "SuperAdminScheme")]
    public async Task<IActionResult> Suspend(int id)
    {
        var reg = await _masterDb.SchoolRegistrations.FindAsync(id);
        if (reg == null) return NotFound();

        reg.Status = RegistrationStatus.Suspended;
        await _masterDb.SaveChangesAsync();

        TempData["Success"] = $"'{reg.SchoolName}' suspended — their login is now blocked.";
        return RedirectToAction(nameof(Dashboard));
    }

    // POST: /SuperAdmin/Reactivate/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(AuthenticationSchemes = "SuperAdminScheme")]
    public async Task<IActionResult> Reactivate(int id)
    {
        var reg = await _masterDb.SchoolRegistrations.FindAsync(id);
        if (reg == null) return NotFound();

        reg.Status = RegistrationStatus.Approved;
        await _masterDb.SaveChangesAsync();

        TempData["Success"] = $"'{reg.SchoolName}' reactivated.";
        return RedirectToAction(nameof(Dashboard));
    }
}