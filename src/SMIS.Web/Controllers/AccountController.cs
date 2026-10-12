using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SMIS.Web.ViewModels;

namespace SMIS.Web.Controllers;

/// <summary>
/// Handles login, logout and account actions
/// </summary>
public class AccountController : Controller
{
    private readonly SignInManager<IdentityUser> _signInManager;
private readonly UserManager<IdentityUser> _userManager;
private readonly SMIS.Web.Services.TenantContext _tenantContext;

public AccountController(
    SignInManager<IdentityUser> signInManager,
    UserManager<IdentityUser> userManager,
    SMIS.Web.Services.TenantContext tenantContext)
{
    _signInManager = signInManager;
    _userManager = userManager;
    _tenantContext = tenantContext;
}

    // GET: /Account/Login
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        // If already logged in, go to dashboard
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    // POST: /Account/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _signInManager.PasswordSignInAsync(
            model.Email,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
{
    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        return Redirect(returnUrl);

    var user = await _userManager.FindByEmailAsync(model.Email);
    var roles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();

    if (roles.Contains("Parent") || roles.Contains("Student"))
        return RedirectToAction("Dashboard", "Portal");

    return RedirectToAction("Index", "Dashboard");
}

        if (result.IsLockedOut)
        {
            ModelState.AddModelError("", "Account locked. Try again in 15 minutes.");
            return View(model);
        }

        ModelState.AddModelError("", "Invalid email or password.");
        return View(model);
    }

    // POST: /Account/Logout
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Logout()
{
    // Capture the tenant claim before signing out — once SignOutAsync
    // runs, the current User's claims are gone.
    var tenantDbClaim = User.FindFirst("TenantDb")?.Value;

    string? slug = null;
    if (!string.IsNullOrEmpty(tenantDbClaim))
    {
        // TenantDb is stored as the actual database name (e.g. "SMIS_demo"),
        // not the slug — look up the matching registration to get the slug back.
        var masterDb = HttpContext.RequestServices.GetRequiredService<SMIS.Infrastructure.Data.MasterDbContext>();
        var reg = await masterDb.SchoolRegistrations
            .FirstOrDefaultAsync(r => r.DatabaseName == tenantDbClaim);
        slug = reg?.Slug;
    }

    await _signInManager.SignOutAsync();

    if (!string.IsNullOrEmpty(slug))
    return RedirectToRoute("tenantLogin", new { slug });

    return RedirectToAction("Login");
}
    [HttpGet]
public IActionResult AccessDenied()
{
    return View();
}

// GET: /{slug}/Login
[HttpGet]
public IActionResult TenantLogin(string slug, bool suspended = false)
{
    if (suspended)
    {
        ViewBag.SchoolName = _tenantContext.SchoolName;
        return View("AccountSuspended");
    }

    if (User.Identity?.IsAuthenticated == true)
        return RedirectToAction("Index", "Dashboard");

    if (_tenantContext.IsSuspended)
    {
        ViewBag.SchoolName = _tenantContext.SchoolName;
        return View("AccountSuspended");
    }

    if (_tenantContext.SlugNotFound || string.IsNullOrEmpty(_tenantContext.DatabaseName))
        return View("TenantNotFound");

    ViewBag.SchoolName = _tenantContext.SchoolName;
    ViewBag.Slug = slug;
    return View();
}

// POST: /{slug}/Login
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> TenantLogin(string slug, LoginViewModel model)
{
    if (_tenantContext.IsSuspended)
    {
        ViewBag.SchoolName = _tenantContext.SchoolName;
        return View("AccountSuspended");
    }

    if (_tenantContext.SlugNotFound || string.IsNullOrEmpty(_tenantContext.DatabaseName))
        return View("TenantNotFound");

    ViewBag.SchoolName = _tenantContext.SchoolName;
    ViewBag.Slug = slug;

    if (!ModelState.IsValid)
        return View(model);

    var user = await _userManager.FindByEmailAsync(model.Email);
    if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
    {
        ModelState.AddModelError("", "Invalid email or password.");
        return View(model);
    }

    var principal = await _signInManager.CreateUserPrincipalAsync(user);
    var identity = (System.Security.Claims.ClaimsIdentity)principal.Identity!;
    identity.AddClaim(new System.Security.Claims.Claim("TenantDb", _tenantContext.DatabaseName!));
    identity.AddClaim(new System.Security.Claims.Claim("TenantSchoolName", _tenantContext.SchoolName ?? ""));

    await HttpContext.SignInAsync(
        Microsoft.AspNetCore.Identity.IdentityConstants.ApplicationScheme,
        principal,
        new AuthenticationProperties { IsPersistent = model.RememberMe });

    return RedirectToAction("Index", "Dashboard");
}
}