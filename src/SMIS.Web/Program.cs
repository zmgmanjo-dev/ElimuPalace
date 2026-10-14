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
        options.UseSqlServer(builder.Configuration.GetConnectionString("MasterConnection")));
}

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options => { /* password rules, etc. */ })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    if (!isStandalone)
        options.Events.OnValidatePrincipal = _ => Task.CompletedTask;
});

if (!isStandalone)
{
    builder.Services.AddAuthentication().AddCookie("SuperAdminScheme", options => { options.LoginPath = "/SuperAdmin/Login"; });
}

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<SMIS.Web.Services.PdfService>();
builder.Services.AddScoped<SMIS.Web.Services.EmailService>();
if (!isStandalone)
{
    builder.Services.AddScoped<SMIS.Web.Services.TenantProvisioningService>();
    builder.Services.AddScoped<SMIS.Web.Services.GlobalUserDirectoryService>();
}

var app = builder.Build();
app.UseDeveloperExceptionPage();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
if (!isStandalone) app.UseMiddleware<SMIS.Web.Middleware.TenantResolutionMiddleware>();
app.UseAuthorization();

// ... your existing app.MapControllerRoute(...) calls for each controller stay exactly as they are

if (!isStandalone)
{
    app.MapControllerRoute(name: "registration", pattern: "Register/{action=Index}/{id?}", defaults: new { controller = "Registration" });
    app.MapControllerRoute(name: "superadmin", pattern: "SuperAdmin/{action=Login}/{id?}", defaults: new { controller = "SuperAdmin" });
    app.MapControllerRoute(name: "commonLogin", pattern: "Login", defaults: new { controller = "Account", action = "Login" });
    app.MapControllerRoute(name: "tenantLogin", pattern: "{slug}/Login", defaults: new { controller = "Account", action = "TenantLogin" });
}
app.MapControllerRoute(name: "default", pattern: "{controller=" + (isStandalone ? "Account" : "Registration") + "}/{action=" + (isStandalone ? "Login" : "Index") + "}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        if (isStandalone)
        {
            var db = services.GetRequiredService<ApplicationDbContext>();
            await db.Database.MigrateAsync();
        }
        await DbSeeder.SeedAsync(services);
    }
    catch (Exception ex) { Console.WriteLine($"Seeder error: {ex.Message}"); }

    if (!isStandalone)
    {
        try
        {
            var masterDb = services.GetRequiredService<SMIS.Infrastructure.Data.MasterDbContext>();
            await SMIS.Infrastructure.Data.MasterDbSeeder.SeedAsync(masterDb);

            var config = services.GetRequiredService<IConfiguration>();
            var template = config.GetConnectionString("TenantServerTemplate")!;
            var directoryService = services.GetRequiredService<SMIS.Web.Services.GlobalUserDirectoryService>();

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

                    // One-time backfill: register every existing user in this school's
                    // database into the master directory, so the common login page can find them.
                    var users = await tenantDb.Users.ToListAsync();
                    foreach (var user in users)
                    {
                        if (!string.IsNullOrEmpty(user.Email))
                            await directoryService.RegisterUserAsync(user.Email, school.DatabaseName, school.SchoolName, school.Id);
                    }
                }
                catch (Exception ex) { Console.WriteLine($"Migration/backfill failed for {school.SchoolName}: {ex.Message}"); }
            }
        }
        catch (Exception ex) { Console.WriteLine($"Master startup error: {ex.Message}"); }
    }
}
app.Run();