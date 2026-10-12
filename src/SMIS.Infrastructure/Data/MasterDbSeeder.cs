using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SMIS.Domain.Entities.Platform;

namespace SMIS.Infrastructure.Data;

public static class MasterDbSeeder
{
    public static async Task SeedAsync(MasterDbContext masterDb)
    {
        if (await masterDb.SuperAdmins.AnyAsync())
            return; // already have at least one — nothing to do

        var superAdmin = new SuperAdmin
        {
            FullName = "Platform Owner",
            Email = "owner@elimupalace.co.ke",
            IsActive = true,
            CreatedBy = "system",
            CreatedAt = DateTime.UtcNow
        };

        var hasher = new PasswordHasher<SuperAdmin>();
        superAdmin.PasswordHash = hasher.HashPassword(superAdmin, "SuperAdmin@1234");

        masterDb.SuperAdmins.Add(superAdmin);
        await masterDb.SaveChangesAsync();

        Console.WriteLine($"Seeded default Super Admin: {superAdmin.Email} / SuperAdmin@1234 — change this password after first login.");
    }
}