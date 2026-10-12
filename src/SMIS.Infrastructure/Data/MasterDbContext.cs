using Microsoft.EntityFrameworkCore;
using SMIS.Domain.Entities.Platform;

namespace SMIS.Infrastructure.Data;

/// <summary>
/// The one platform-wide database: school registrations and super admins.
/// Never contains any school's operational data.
/// </summary>
public class MasterDbContext : DbContext
{
    public MasterDbContext(DbContextOptions<MasterDbContext> options) : base(options) { }

    public DbSet<SchoolRegistration> SchoolRegistrations => Set<SchoolRegistration>();
    public DbSet<SuperAdmin> SuperAdmins => Set<SuperAdmin>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<SchoolRegistration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SchoolName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.Property(e => e.DatabaseName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ContactEmail).IsRequired().HasMaxLength(200);
            entity.Property(e => e.AdminEmail).IsRequired().HasMaxLength(200);
        });

        builder.Entity<SuperAdmin>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Email).IsUnique();
        });
    }
}