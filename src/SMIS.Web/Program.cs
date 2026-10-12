using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SMIS.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

var deploymentMode = builder.Configuration["DeploymentMode"] ?? "Cloud";
var isStandalone = deploymentMode.Equals("Standalone", StringComparison.OrdinalIgnoreCase);

builder.Services.AddScoped<SMIS.Web.Services.TenantContext>();

builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
{
    if (isStandalone)
    {
        // Standalone: always the one local database. No tenant switching.
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
        return;
    }

    var tenantContext = sp.GetRequiredService<SMIS.Web.Services.TenantContext>();
    var config = sp.GetRequiredService<IConfiguration>();

    if (!string.IsNullOrEmpty(tenantContext.DatabaseName))
    {
        var template = config.GetConnectionString("TenantServerTemplate")!;
        options.UseSqlServer(string.Format(template, tenantContext.DatabaseName));
    }
    else
    {
        options.UseSqlServer(config.GetConnectionString("DefaultConnection"));
    }
});

if (!isStandalone)
{
    builder.Services.AddDbContext<SMIS.Infrastructure.Data.MasterDbContext>(options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("MasterConnection")));
}

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.None;
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;

    if (!isStandalone)
    {
        // See earlier note: disabled to avoid Identity's background
        // security-stamp check racing tenant resolution in Cloud mode.
        // Not needed in Standalone (single fixed database), so left at
        // its normal default there.
        options.Events.OnValidatePrincipal = _ => Task.CompletedTask;
    }
});

if (!isStandalone)
{
    builder.Services.AddAuthentication()
        .AddCookie("SuperAdminScheme", options =>
        {
            options.LoginPath = "/SuperAdmin/Login";
            options.AccessDeniedPath = "/SuperAdmin/Login";
            options.Cookie.Name = "SuperAdminAuth";
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
        });
}

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<SMIS.Web.Services.PdfService>();
builder.Services.AddScoped<SMIS.Web.Services.EmailService>();

if (!isStandalone)
{
    builder.Services.AddScoped<SMIS.Web.Services.TenantProvisioningService>();
}

var app = builder.Build();

app.UseDeveloperExceptionPage();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();

if (!isStandalone)
{
    app.UseMiddleware<SMIS.Web.Middleware.TenantResolutionMiddleware>();
}

app.UseAuthorization();

app.MapControllerRoute(
    name: "account",
    pattern: "Account/{action=Login}/{id?}",
    defaults: new { controller = "Account" });

app.MapControllerRoute(
    name: "accessdenied",
    pattern: "Account/AccessDenied",
    defaults: new { controller = "Account", action = "AccessDenied" });

app.MapControllerRoute(
    name: "dashboard",
    pattern: "Dashboard/{action=Index}/{id?}",
    defaults: new { controller = "Dashboard" });

app.MapControllerRoute(
    name: "students",
    pattern: "Students/{action=Index}/{id?}",
    defaults: new { controller = "Students" });

app.MapControllerRoute(
    name: "setup",
    pattern: "Setup/{action=SchoolInfo}/{id?}",
    defaults: new { controller = "Setup" });

app.MapControllerRoute(
    name: "attendance",
    pattern: "Attendance/{action=Index}/{id?}",
    defaults: new { controller = "Attendance" });

app.MapControllerRoute(
    name: "finance",
    pattern: "Finance/{action=FeeCategories}/{id?}",
    defaults: new { controller = "Finance" });

app.MapControllerRoute(
    name: "cbc",
    pattern: "CBC/{action=Index}/{id?}",
    defaults: new { controller = "CBC" });

app.MapControllerRoute(
    name: "library",
    pattern: "Library/{action=Index}/{id?}",
    defaults: new { controller = "Library" });

app.MapControllerRoute(
    name: "usermanagement",
    pattern: "UserManagement/{action=Index}/{id?}",
    defaults: new { controller = "UserManagement" });

app.MapControllerRoute(
    name: "permissions",
    pattern: "Permissions/{action=Index}/{id?}",
    defaults: new { controller = "Permissions" });

app.MapControllerRoute(
    name: "reports",
    pattern: "Reports/{action=Index}/{id?}",
    defaults: new { controller = "Reports" });

app.MapControllerRoute(
    name: "newsletter",
    pattern: "Newsletter/{action=Index}/{id?}",
    defaults: new { controller = "Newsletter" });

app.MapControllerRoute(
    name: "accounting",
    pattern: "Accounting/{action=Index}/{id?}",
    defaults: new { controller = "Accounting" });

app.MapControllerRoute(
    name: "accountspayable",
    pattern: "AccountsPayable/{action=Index}/{id?}",
    defaults: new { controller = "AccountsPayable" });

app.MapControllerRoute(
    name: "payroll",
    pattern: "Payroll/{action=Staff}/{id?}",
    defaults: new { controller = "Payroll" });

app.MapControllerRoute(
    name: "promotion",
    pattern: "Promotion/{action=Index}/{id?}",
    defaults: new { controller = "Promotion" });

if (!isStandalone)
{
    app.MapControllerRoute(
        name: "registration",
        pattern: "Register/{action=Index}/{id?}",
        defaults: new { controller = "Registration" });

    app.MapControllerRoute(
        name: "superadmin",
        pattern: "SuperAdmin/{action=Login}/{id?}",
        defaults: new { controller = "SuperAdmin" });

    app.MapControllerRoute(
        name: "tenantLogin",
        pattern: "{slug}/Login",
        defaults: new { controller = "Account", action = "TenantLogin" });
}

// Standalone lands straight on the login page, matching how Elimu Palace
// behaved before Cloud mode existed. Cloud still lands on registration.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=" + (isStandalone ? "Account" : "Registration") + "}/{action=" + (isStandalone ? "Login" : "Index") + "}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    // 1. Migrate the main database (ELIMUPALACEDB in Cloud mode, the local
    //    database in Standalone mode) and seed roles/admin.
    try
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
        await DbSeeder.SeedAsync(services);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Seeder error: {ex.Message}");
    }

    // 2. Cloud only: seed the Super Admin, then migrate every school's database.
    if (!isStandalone)
    {
        try
        {
            var masterDb = services.GetRequiredService<SMIS.Infrastructure.Data.MasterDbContext>();
            await SMIS.Infrastructure.Data.MasterDbSeeder.SeedAsync(masterDb);

            var config = services.GetRequiredService<IConfiguration>();
            var template = config.GetConnectionString("TenantServerTemplate")!;

            var schools = await masterDb.SchoolRegistrations
                .Where(r => r.Status == SMIS.Domain.Entities.Platform.RegistrationStatus.Approved
                         || r.Status == SMIS.Domain.Entities.Platform.RegistrationStatus.Suspended)
                .ToListAsync();

            foreach (var school in schools)
            {
                try
                {
                    var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                    optionsBuilder.UseSqlServer(string.Format(template, school.DatabaseName));
                    using var tenantDb = new ApplicationDbContext(optionsBuilder.Options);
                    await tenantDb.Database.MigrateAsync();
                    Console.WriteLine($"Database up to date: {school.SchoolName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Migration failed for {school.SchoolName}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Master startup error: {ex.Message}");
        }
    }
}

app.Run();