using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace SMIS.Infrastructure.Data;

/// <summary>
/// Creates the first admin user and roles when the app starts for the first time
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Create roles
        string[] roles = {
            "Administrator", "Principal", "DeputyPrincipal",
            "AcademicMaster", "Teacher", "ClassTeacher",
            "Bursar", "AccountsClerk", "Secretary",
            "Receptionist", "Librarian", "StoreKeeper",
            "Parent", "Student", "BoardMember"
        };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // ── One-time rename: old @smis.ac.ke admin account → new domain ──
        // Safe to leave permanently: once the old account is renamed, this
        // block finds nothing on future startups and does nothing.
        const string legacyAdminEmail = "admin@smis.ac.ke";
        var legacyAdmin = await userManager.FindByEmailAsync(legacyAdminEmail);
        if (legacyAdmin != null)
        {
            const string newAdminEmail = "admin@elimupalace.co.ke";

            var setEmailResult = await userManager.SetEmailAsync(legacyAdmin, newAdminEmail);
            var setUserNameResult = await userManager.SetUserNameAsync(legacyAdmin, newAdminEmail);

            if (setEmailResult.Succeeded && setUserNameResult.Succeeded)
            {
                // SetEmailAsync resets EmailConfirmed to false — restore it
                legacyAdmin.EmailConfirmed = true;
                await userManager.UpdateAsync(legacyAdmin);
                Console.WriteLine($"Renamed admin account: {legacyAdminEmail} → {newAdminEmail}");
            }
            else
            {
                var errors = string.Join("; ",
                    setEmailResult.Errors.Concat(setUserNameResult.Errors).Select(e => e.Description));
                Console.WriteLine($"Failed to rename admin account: {errors}");
            }
        }

        // Create default admin user (for brand-new installs going forward)
        const string adminEmail = "admin@elimupalace.co.ke";
        const string adminPassword = "Admin@1234";

        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var admin = new IdentityUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, "Administrator");
        }
    }
}