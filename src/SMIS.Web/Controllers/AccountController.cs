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
    private readonly IConfiguration _config;

    public AccountController(
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        SMIS.Web.Services.TenantContext tenantContext,
        IConfiguration config)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _tenantContext = tenantContext;
        _config = config;
    }

    private bool IsStandalone =>
        (_config["DeploymentMode"] ?? "Cloud").Equals("Standalone", StringComparison.OrdinalIgnoreCase);

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
    // In Standalone mode this is the only login path, and behaves exactly as
    // before. In Cloud mode this is now the common login for every school:
    // it looks the email up in the platform-wide directory to find which
    // school's database to check the password against, with no /{slug}/
    // needed. The old /{slug}/Login further down still works unchanged, as
    // a fallback.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
            return View(model);

        if (IsStandalone)
        {
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
                ViewBag.Error = "Account locked. Try again in 15 minutes.";
                ModelState.AddModelError("", "Account locked. Try again in 15 minutes.");
                return View(model);
            }

            ViewBag.Error = "Invalid email or password.";
            ModelState.AddModelError("", "Invalid email or password.");
            return View(model);
        }

        // ── Cloud mode: common login, resolved via the platform directory ──
        var directory = HttpContext.RequestServices
            .GetRequiredService<SMIS.Web.Services.GlobalUserDirectoryService>();
        var masterDb = HttpContext.RequestServices
            .GetRequiredService<SMIS.Infrastructure.Data.MasterDbContext>();

        var matches = await directory.FindByEmailAsync(model.Email);

        if (matches.Count == 0)
        {
            ViewBag.Error = "Invalid email or password.";
            ModelState.AddModelError("", "Invalid email or password.");
            return View(model);
        }

        if (matches.Count > 1)
        {
            ViewBag.Error = "This email is linked to more than one school account. Please use your school's direct login link, or contact support.";
            ModelState.AddModelError("", ViewBag.Error);
            return View(model);
        }

        var entry = matches[0];
        var registration = await masterDb.SchoolRegistrations
            .FirstOrDefaultAsync(r => r.DatabaseName == entry.DatabaseName);

        if (registration == null)
        {
            ViewBag.Error = "Invalid email or password.";
            ModelState.AddModelError("", "Invalid email or password.");
            return View(model);
        }

        if (registration.Status == SMIS.Domain.Entities.Platform.RegistrationStatus.Suspended)
        {
            ViewBag.SchoolName = registration.SchoolName;
            return View("AccountSuspended");
        }

        if (registration.Status != SMIS.Domain.Entities.Platform.RegistrationStatus.Approved)
        {
            ViewBag.Error = "Invalid email or password.";
            ModelState.AddModelError("", "Invalid email or password.");
            return View(model);
        }

        var template = _config.GetConnectionString("TenantServerTemplate")!;
        var optionsBuilder = new DbContextOptionsBuilder<SMIS.Infrastructure.Data.ApplicationDbContext>();
        optionsBuilder.UseSqlServer(string.Format(template, entry.DatabaseName));
        await using var tenantDb = new SMIS.Infrastructure.Data.ApplicationDbContext(optionsBuilder.Options);

        var normalizedEmail = model.Email.Trim().ToUpperInvariant();
        var tenantUser = await tenantDb.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);

        if (tenantUser == null || string.IsNullOrEmpty(tenantUser.PasswordHash))
        {
            ViewBag.Error = "Invalid email or password.";
            ModelState.AddModelError("", "Invalid email or password.");
            return View(model);
        }

        var hasher = new PasswordHasher<IdentityUser>();
        var verify = hasher.VerifyHashedPassword(tenantUser, tenantUser.PasswordHash, model.Password);
        if (verify == PasswordVerificationResult.Failed)
        {
            ViewBag.Error = "Invalid email or password.";
            ModelState.AddModelError("", "Invalid email or password.");
            return View(model);
        }

        // Build the sign-in principal from the tenant database directly —
        // we can't use the injected _signInManager/_userManager here, because
        // (unlike /{slug}/Login) there was no slug to tell the middleware
        // which database to point them at before this request started.
        var roleIds = await tenantDb.UserRoles.Where(ur => ur.UserId == tenantUser.Id).Select(ur => ur.RoleId).ToListAsync();
        var roleNames = await tenantDb.Roles.Where(r => roleIds.Contains(r.Id)).Select(r => r.Name!).ToListAsync();
        var fullNameClaim = (await tenantDb.UserClaims
            .FirstOrDefaultAsync(c => c.UserId == tenantUser.Id && c.ClaimType == "FullName"))?.ClaimValue;

        var claims = new List<System.Security.Claims.Claim>
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, tenantUser.Id),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, tenantUser.UserName ?? tenantUser.Email ?? model.Email),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, tenantUser.Email ?? model.Email),
            new System.Security.Claims.Claim("TenantDb", entry.DatabaseName),
            new System.Security.Claims.Claim("TenantSchoolName", registration.SchoolName)
        };
        foreach (var roleName in roleNames)
            claims.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, roleName));
        if (!string.IsNullOrEmpty(fullNameClaim))
            claims.Add(new System.Security.Claims.Claim("FullName", fullNameClaim));

        var identity = new System.Security.Claims.ClaimsIdentity(claims,
            Microsoft.AspNetCore.Identity.IdentityConstants.ApplicationScheme);
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            Microsoft.AspNetCore.Identity.IdentityConstants.ApplicationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = model.RememberMe });

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        if (roleNames.Contains("Parent") || roleNames.Contains("Student"))
            return RedirectToAction("Dashboard", "Portal");

        return RedirectToAction("Index", "Dashboard");
    }

    // POST: /Account/Logout
    // Simplified: login is common now, so logout just goes back to the
    // common Login page — no need to look up the school's slug anymore.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    // GET: /{slug}/Login — kept as a fallback (direct school link, and the
    // escape hatch if an email is ever linked to more than one school).
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