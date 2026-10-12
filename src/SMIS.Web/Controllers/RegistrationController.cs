using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SMIS.Domain.Entities.Platform;
using SMIS.Infrastructure.Data;

namespace SMIS.Web.Controllers;

public class RegistrationController : Controller
{
    private readonly MasterDbContext _masterDb;
    private readonly PasswordHasher<SchoolRegistration> _hasher = new();

    public RegistrationController(MasterDbContext masterDb)
    {
        _masterDb = masterDb;
    }

    // GET: /Register
    [HttpGet]
    public IActionResult Index() => View();

    // POST: /Register
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(
        string schoolName, string slug, string contactEmail, string? contactPhone,
        string physicalAddress, string adminFullName, string adminEmail, string adminPassword)
    {
        slug = slug.Trim().ToLowerInvariant().Replace(" ", "-");

        var reservedSlugs = new[] { "account", "superadmin", "register", "dashboard", "portal", "newsletter", "css", "js", "lib", "uploads", "favicon.ico" };
if (reservedSlugs.Contains(slug))
{
    ViewBag.Error = "That school URL is reserved — please choose another.";
    return View();
}

        if (await _masterDb.SchoolRegistrations.AnyAsync(r => r.Slug == slug))
        {
            ViewBag.Error = "That school URL is already taken — please choose another.";
            return View();
        }
        if (await _masterDb.SchoolRegistrations.AnyAsync(r => r.AdminEmail == adminEmail))
        {
            ViewBag.Error = "An account with this admin email is already registered.";
            return View();
        }

        var registration = new SchoolRegistration
        {
            SchoolName = schoolName,
            Slug = slug,
            DatabaseName = $"SMIS_{slug}",
            ContactEmail = contactEmail,
            ContactPhone = contactPhone,
            PhysicalAddress = physicalAddress,
            AdminFullName = adminFullName,
            AdminEmail = adminEmail,
            Status = RegistrationStatus.Pending,
            CreatedBy = "self-registration",
            CreatedAt = DateTime.UtcNow
        };
        registration.AdminPasswordHash = _hasher.HashPassword(registration, adminPassword);

        _masterDb.SchoolRegistrations.Add(registration);
        await _masterDb.SaveChangesAsync();

        return RedirectToAction(nameof(Submitted));
    }

    // GET: /Register/Submitted
    [HttpGet]
    public IActionResult Submitted() => View();
}