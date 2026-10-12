using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SMIS.Infrastructure.Data;

namespace SMIS.Web.Controllers;

[Authorize(Roles = "Administrator")]
public class UserManagementController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _db;

    public UserManagementController(
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext db)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _db = db;
    }

    // GET: /UserManagement
    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users.ToListAsync();
        var userRoles = new Dictionary<string, IList<string>>();

        foreach (var user in users)
            userRoles[user.Id] = await _userManager.GetRolesAsync(user);

        ViewBag.UserRoles = userRoles;
        return View(users);
    }

    // GET: /UserManagement/CreateUser
    public async Task<IActionResult> CreateUser()
    {
        ViewBag.Roles = await _roleManager.Roles
            .OrderBy(r => r.Name).ToListAsync();
        ViewBag.Classes = await _db.ClassLevels
            .OrderBy(c => c.OrderIndex).ToListAsync();
        return View();
    }

    // POST: /UserManagement/CreateUser
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(string fullName,
        string email, string password, string role,
        string? phone, int? classLevelId)
    {
        // Check if email already exists
        var existing = await _userManager.FindByEmailAsync(email);
        if (existing != null)
        {
            TempData["Error"] = "A user with this email already exists!";
            return RedirectToAction(nameof(CreateUser));
        }

        var user = new IdentityUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            PhoneNumber = phone
        };

        var result = await _userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            TempData["Error"] = string.Join(", ",
                result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(CreateUser));
        }

        // Assign role
        if (!string.IsNullOrEmpty(role))
            await _userManager.AddToRoleAsync(user, role);

        // Store full name in claims
        await _userManager.AddClaimAsync(user,
            new System.Security.Claims.Claim("FullName", fullName));

        TempData["Success"] =
            $"User '{fullName}' created successfully with role '{role}'!";
        return RedirectToAction(nameof(Index));
    }

    // GET: /UserManagement/EditUser/id
    public async Task<IActionResult> EditUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var userRoles = await _userManager.GetRolesAsync(user);
        var allRoles = await _roleManager.Roles
            .OrderBy(r => r.Name).ToListAsync();
        var claims = await _userManager.GetClaimsAsync(user);
        var fullName = claims.FirstOrDefault(c => c.Type == "FullName")?.Value ?? "";

        ViewBag.UserRoles = userRoles;
        ViewBag.AllRoles = allRoles;
        ViewBag.FullName = fullName;
        ViewBag.Classes = await _db.ClassLevels
            .OrderBy(c => c.OrderIndex).ToListAsync();

        return View(user);
    }

    // POST: /UserManagement/UpdateUser
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateUser(string id,
        string fullName, string? phone, string role, bool isActive)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        // Update phone
        user.PhoneNumber = phone;
        user.LockoutEnabled = !isActive;
        if (!isActive)
            user.LockoutEnd = DateTimeOffset.MaxValue;
        else
            user.LockoutEnd = null;

        await _userManager.UpdateAsync(user);

        // Update full name claim
        var claims = await _userManager.GetClaimsAsync(user);
        var existingClaim = claims.FirstOrDefault(c => c.Type == "FullName");
        if (existingClaim != null)
            await _userManager.ReplaceClaimAsync(user, existingClaim,
                new System.Security.Claims.Claim("FullName", fullName));
        else
            await _userManager.AddClaimAsync(user,
                new System.Security.Claims.Claim("FullName", fullName));

        // Update role
        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        if (!string.IsNullOrEmpty(role))
            await _userManager.AddToRoleAsync(user, role);

        TempData["Success"] = $"User updated successfully!";
        return RedirectToAction(nameof(Index));
    }

    // POST: /UserManagement/ResetPassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(string id,
        string newPassword)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user,
            token, newPassword);

        if (result.Succeeded)
            TempData["Success"] = "Password reset successfully!";
        else
            TempData["Error"] = string.Join(", ",
                result.Errors.Select(e => e.Description));

        return RedirectToAction(nameof(Index));
    }

    // POST: /UserManagement/ToggleUser
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        // Cannot deactivate yourself
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser?.Id == id)
        {
            TempData["Error"] = "You cannot deactivate your own account!";
            return RedirectToAction(nameof(Index));
        }

        if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.Now)
        {
            // Activate
            user.LockoutEnd = null;
            user.LockoutEnabled = true;
            TempData["Success"] = "User activated successfully!";
        }
        else
        {
            // Deactivate
            user.LockoutEnd = DateTimeOffset.MaxValue;
            user.LockoutEnabled = true;
            TempData["Success"] = "User deactivated successfully!";
        }

        await _userManager.UpdateAsync(user);
        return RedirectToAction(nameof(Index));
    }

    // GET: /UserManagement/Roles
    public async Task<IActionResult> Roles()
    {
        var roles = await _roleManager.Roles
            .OrderBy(r => r.Name).ToListAsync();

        var roleUserCounts = new Dictionary<string, int>();
        foreach (var role in roles)
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
            roleUserCounts[role.Name!] = usersInRole.Count;
        }

        ViewBag.RoleUserCounts = roleUserCounts;
        ViewBag.RolePermissions = RolePermissions.GetAllPermissions();
        return View(roles);
    }
    // ── PARENT ACCOUNTS ──────────────────────────────────────────

// GET: /UserManagement/ParentAccounts
public async Task<IActionResult> ParentAccounts(string? search)
{
    var parentUsers = (await _userManager.GetUsersInRoleAsync("Parent")).ToList();

    if (!string.IsNullOrEmpty(search))
        parentUsers = parentUsers
            .Where(u => (u.Email ?? "").Contains(search, StringComparison.OrdinalIgnoreCase))
            .ToList();

    var parentIds = parentUsers.Select(u => u.Id).ToList();

    var links = await _db.ParentStudentLinks
        .Include(l => l.Student).ThenInclude(s => s.ClassLevel)
        .Where(l => parentIds.Contains(l.ParentUserId))
        .ToListAsync();

    var linksByParent = links
        .GroupBy(l => l.ParentUserId)
        .ToDictionary(g => g.Key, g => g.ToList());

    var fullNames = new Dictionary<string, string>();
    foreach (var u in parentUsers)
    {
        var claims = await _userManager.GetClaimsAsync(u);
        fullNames[u.Id] = claims.FirstOrDefault(c => c.Type == "FullName")?.Value
            ?? u.Email ?? "-";
    }

    ViewBag.LinksByParent = linksByParent;
    ViewBag.FullNames = fullNames;
    ViewBag.Search = search;
    ViewBag.AllStudents = await _db.Students
        .Include(s => s.ClassLevel)
        .Where(s => s.Status == SMIS.Domain.Enums.StudentStatus.Active)
        .OrderBy(s => s.LastName).ToListAsync();

    return View(parentUsers.OrderBy(u => fullNames[u.Id]).ToList());
}

// GET: /UserManagement/CreateParentAccount
public async Task<IActionResult> CreateParentAccount()
{
    ViewBag.Students = await _db.Students
        .Include(s => s.ClassLevel)
        .Where(s => s.Status == SMIS.Domain.Enums.StudentStatus.Active)
        .OrderBy(s => s.LastName).ToListAsync();
    return View();
}

// POST: /UserManagement/CreateParentAccount
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> CreateParentAccount(string fullName,
    string email, string password, string? phone,
    string relationship, List<int> studentIds)
{
    var existing = await _userManager.FindByEmailAsync(email);
    if (existing != null)
    {
        TempData["Error"] = "A user with this email already exists!";
        return RedirectToAction(nameof(CreateParentAccount));
    }

    if (studentIds == null || !studentIds.Any())
    {
        TempData["Error"] = "Please select at least one student to link.";
        return RedirectToAction(nameof(CreateParentAccount));
    }

    var user = new IdentityUser
    {
        UserName = email,
        Email = email,
        EmailConfirmed = true,
        PhoneNumber = phone
    };

    var result = await _userManager.CreateAsync(user, password);
    if (!result.Succeeded)
    {
        TempData["Error"] = string.Join(", ", result.Errors.Select(e => e.Description));
        return RedirectToAction(nameof(CreateParentAccount));
    }

    await _userManager.AddToRoleAsync(user, "Parent");
    await _userManager.AddClaimAsync(user,
        new System.Security.Claims.Claim("FullName", fullName));

    foreach (var studentId in studentIds.Distinct())
    {
        _db.ParentStudentLinks.Add(new SMIS.Domain.Entities.Students.ParentStudentLink
        {
            ParentUserId = user.Id,
            StudentId = studentId,
            Relationship = string.IsNullOrWhiteSpace(relationship) ? "Parent" : relationship,
            CreatedBy = User.Identity?.Name ?? "system",
            CreatedAt = DateTime.UtcNow
        });
    }
    await _db.SaveChangesAsync();

    TempData["Success"] =
        $"Parent account '{fullName}' created and linked to {studentIds.Count} student(s)!";
    return RedirectToAction(nameof(ParentAccounts));
}

// POST: /UserManagement/LinkStudentToParent
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> LinkStudentToParent(
    string parentUserId, int studentId, string relationship)
{
    var user = await _userManager.FindByIdAsync(parentUserId);
    if (user == null) return NotFound();

    var already = await _db.ParentStudentLinks
        .AnyAsync(l => l.ParentUserId == parentUserId && l.StudentId == studentId);
    if (already)
    {
        TempData["Error"] = "This student is already linked to this parent.";
        return RedirectToAction(nameof(ParentAccounts));
    }

    _db.ParentStudentLinks.Add(new SMIS.Domain.Entities.Students.ParentStudentLink
    {
        ParentUserId = parentUserId,
        StudentId = studentId,
        Relationship = string.IsNullOrWhiteSpace(relationship) ? "Parent" : relationship,
        CreatedBy = User.Identity?.Name ?? "system",
        CreatedAt = DateTime.UtcNow
    });
    await _db.SaveChangesAsync();

    TempData["Success"] = "Student linked successfully!";
    return RedirectToAction(nameof(ParentAccounts));
}

// POST: /UserManagement/UnlinkStudent
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> UnlinkStudent(int linkId)
{
    var link = await _db.ParentStudentLinks.FindAsync(linkId);
    if (link == null) return NotFound();

    _db.ParentStudentLinks.Remove(link);
    await _db.SaveChangesAsync();

    TempData["Success"] = "Student unlinked successfully!";
    return RedirectToAction(nameof(ParentAccounts));
}
}

/// <summary>
/// Defines what each role can access in the system
/// </summary>
public static class RolePermissions
{
    public static Dictionary<string, List<string>> GetAllPermissions()
    {
        return new Dictionary<string, List<string>>
        {
            ["Administrator"] = new List<string>
            {
                "Full system access",
                "User management",
                "All modules",
                "System settings",
                "Reports"
            },
            ["Principal"] = new List<string>
            {
                "View all modules",
                "CBC reports",
                "Student management",
                "Staff management",
                "Fee reports",
                "Attendance reports"
            },
            ["DeputyPrincipal"] = new List<string>
            {
                "Student management",
                "Attendance management",
                "CBC assessments",
                "Class reports",
                "Timetable"
            },
            ["AcademicMaster"] = new List<string>
            {
                "CBC assessments",
                "Examination management",
                "Report cards",
                "Subject management",
                "Class reports"
            },
            ["Teacher"] = new List<string>
            {
                "View own class students",
                "Enter attendance",
                "Enter CBC assessments",
                "View class report"
            },
            ["ClassTeacher"] = new List<string>
            {
                "View own class students",
                "Enter attendance",
                "Enter CBC assessments",
                "Write report card remarks",
                "View class report"
            },
            ["Bursar"] = new List<string>
            {
                "Fee management",
                "Generate invoices",
                "Record payments",
                "Fee reports",
                "View students"
            },
            ["AccountsClerk"] = new List<string>
            {
                "Record payments",
                "View invoices",
                "Print receipts"
            },
            ["Secretary"] = new List<string>
            {
                "Student registration",
                "View attendance",
                "Library management",
                "Communication"
            },
            ["Librarian"] = new List<string>
            {
                "Full library access",
                "Issue/return books",
                "Manage book catalog"
            },
            ["Parent"] = new List<string>
            {
                "View own child's profile",
                "View fee statement",
                "View report card",
                "View attendance"
            },
            ["Student"] = new List<string>
            {
                "View own profile",
                "View own report card",
                "View library books"
            }
        };
    }
}