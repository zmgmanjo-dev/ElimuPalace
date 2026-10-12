using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SMIS.Domain.Entities.Platform;
using SMIS.Domain.Entities.School;
using SMIS.Infrastructure.Data;

namespace SMIS.Web.Services;

public class TenantProvisioningService
{
    private readonly IConfiguration _config;

    private static readonly string[] Roles = {
        "Administrator", "Principal", "DeputyPrincipal",
        "AcademicMaster", "Teacher", "ClassTeacher",
        "Bursar", "AccountsClerk", "Secretary",
        "Receptionist", "Librarian", "StoreKeeper",
        "Parent", "Student", "BoardMember"
    };

    public TenantProvisioningService(IConfiguration config)
    {
        _config = config;
    }

    /// <summary>
    /// Creates a fresh, fully-migrated database for a newly approved school,
    /// seeds standard roles, and creates their admin login using the
    /// password hash captured at registration time.
    /// </summary>
    public async Task ProvisionSchoolAsync(SchoolRegistration reg)
    {
        var template = _config.GetConnectionString("TenantServerTemplate")
            ?? throw new InvalidOperationException("TenantServerTemplate connection string is not configured.");
        var tenantConnectionString = string.Format(template, reg.DatabaseName);

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer(tenantConnectionString);

        using var tenantDb = new ApplicationDbContext(optionsBuilder.Options);

        // Creates the database (if missing) and applies every existing migration
        await tenantDb.Database.MigrateAsync();

        // ── Seed roles ──────────────────────────────────────────
        foreach (var roleName in Roles)
        {
            if (!await tenantDb.Roles.AnyAsync(r => r.Name == roleName))
            {
                tenantDb.Roles.Add(new IdentityRole
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = roleName,
                    NormalizedName = roleName.ToUpperInvariant(),
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                });
            }
        }
        await tenantDb.SaveChangesAsync();

        // ── Seed the school's real admin account ───────────────
        // Reuses the password hash captured at registration — the algorithm
        // doesn't depend on the generic TUser parameter, so this hash is
        // already valid for IdentityUser. The plaintext password is never
        // stored or re-entered anywhere in this process.
        var adminUser = new IdentityUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = reg.AdminEmail,
            NormalizedUserName = reg.AdminEmail.ToUpperInvariant(),
            Email = reg.AdminEmail,
            NormalizedEmail = reg.AdminEmail.ToUpperInvariant(),
            EmailConfirmed = true,
            PasswordHash = reg.AdminPasswordHash,
            SecurityStamp = Guid.NewGuid().ToString("D"),
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };
        tenantDb.Users.Add(adminUser);
        await tenantDb.SaveChangesAsync();

        var adminRole = await tenantDb.Roles.FirstAsync(r => r.Name == "Administrator");
        tenantDb.UserRoles.Add(new IdentityUserRole<string>
        {
            UserId = adminUser.Id,
            RoleId = adminRole.Id
        });

        // ── Seed a starter SchoolProfile so branding works immediately ──
        tenantDb.SchoolProfiles.Add(new SchoolProfile
        {
            Name = reg.SchoolName,
            KemisCode = "PENDING", // required field — school can update this later in Setup
            PhysicalAddress = reg.PhysicalAddress,
            PhoneNumber = reg.ContactPhone ?? "",
            Email = reg.ContactEmail
        });

        await tenantDb.SaveChangesAsync();
    }
}